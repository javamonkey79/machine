﻿namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class SmtTransferEngineRuntimeTests
{
    [Test]
    public async Task StartBuildAsync()
    {
        using var env = new TestEnvironment();
        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.ModelRevision, Is.EqualTo(0));
        await env.Runtime.InitNewAsync();
        // ensure that the SMT model was loaded before training
        await env.Runtime.TranslateAsync("esto es una prueba .".Split());
        Build build = await env.Runtime.StartBuildAsync();
        Assert.That(build, Is.Not.Null);
        await env.WaitForBuildToStartAsync(build.Id);
        build = env.Builds.Get(build.Id);
        Assert.That(build.State, Is.EqualTo(BuildState.Active));
        await env.WaitForBuildToFinishAsync(build.Id);
        env.SmtBatchTrainer.Received().Train(Arg.Any<IProgress<ProgressStatus>>(), Arg.Any<Action>());
        env.TruecaserTrainer.Received().Train(Arg.Any<IProgress<ProgressStatus>>(), Arg.Any<Action>());
        env.SmtBatchTrainer.Received().Save();
        await env.TruecaserTrainer.Received().SaveAsync();
        build = env.Builds.Get(build.Id);
        Assert.That(build.State, Is.EqualTo(BuildState.Completed));
        engine = env.Engines.Get("engine1");
        Assert.That(engine.ModelRevision, Is.EqualTo(1));
        // check if SMT model was reloaded upon first use after training
        env.SmtModel.ClearReceivedCalls();
        await env.Runtime.TranslateAsync("esto es una prueba .".Split());
        env.SmtModel.Received().Dispose();
        env.SmtModel.DidNotReceive().Save();
        await env.Truecaser.DidNotReceive().SaveAsync();
    }

    [Test]
    public async Task CancelBuildAsync()
    {
        using var env = new TestEnvironment();
        await env.Runtime.InitNewAsync();
        env.SmtBatchTrainer.Train(
            Arg.Any<IProgress<ProgressStatus>>(),
            Arg.Do<Action>(
                checkCanceled =>
                {
                    while (true)
                        checkCanceled();
                }
            )
        );
        Build build = await env.Runtime.StartBuildAsync();
        Assert.That(build, Is.Not.Null);
        await env.WaitForBuildToStartAsync(build.Id);
        build = env.Builds.Get(build.Id);
        Assert.That(build.State, Is.EqualTo(BuildState.Active));
        await env.Runtime.CancelBuildAsync();
        await env.WaitForBuildToFinishAsync(build.Id);
        env.SmtBatchTrainer.DidNotReceive().Save();
        await env.TruecaserTrainer.DidNotReceive().SaveAsync();
        build = env.Builds.Get(build.Id);
        Assert.That(build.State, Is.EqualTo(BuildState.Canceled));
    }

    [Test]
    public async Task StartBuildAsync_RestartUnfinishedBuild()
    {
        using var env = new TestEnvironment();
        await env.Runtime.InitNewAsync();
        env.SmtBatchTrainer.Train(
            Arg.Any<IProgress<ProgressStatus>>(),
            Arg.Do<Action>(
                checkCanceled =>
                {
                    while (true)
                        checkCanceled();
                }
            )
        );
        Build build = await env.Runtime.StartBuildAsync();
        Assert.That(build, Is.Not.Null);
        await env.WaitForBuildToStartAsync(build.Id);
        build = env.Builds.Get(build.Id);
        Assert.That(build.State, Is.EqualTo(BuildState.Active));
        env.StopServer();
        build = (await env.Builds.GetAsync(build.Id))!;
        Assert.That(build.State, Is.EqualTo(BuildState.Pending));
        env.SmtBatchTrainer.ClearSubstitute(ClearOptions.CallActions);
        env.StartServer();
        await env.WaitForBuildToFinishAsync(build.Id);
        build = env.Builds.Get(build.Id);
        Assert.That(build.State, Is.EqualTo(BuildState.Completed));
    }

    [Test]
    public async Task CommitAsync_LoadedInactive()
    {
        using var env = new TestEnvironment();
        env.EngineOptions.InactiveEngineTimeout = TimeSpan.Zero;
        await env.Runtime.InitNewAsync();
        await env.Runtime.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split(), true);
        await Task.Delay(10);
        await env.Runtime.CommitAsync();
        env.SmtModel.Received().Save();
        env.Truecaser
            .Received()
            .TrainSegment(Arg.Is<IReadOnlyList<string>>(x => x.SequenceEqual("this is a test .".Split())), true);
        Assert.That(env.Runtime.IsLoaded, Is.False);
    }

    [Test]
    public async Task CommitAsync_LoadedActive()
    {
        using var env = new TestEnvironment();
        env.EngineOptions.InactiveEngineTimeout = TimeSpan.FromHours(1);
        await env.Runtime.InitNewAsync();
        await env.Runtime.TrainSegmentPairAsync("esto es una prueba .".Split(), "this is a test .".Split(), true);
        await env.Runtime.CommitAsync();
        env.SmtModel.Received().Save();
        env.Truecaser
            .Received()
            .TrainSegment(Arg.Is<IReadOnlyList<string>>(x => x.SequenceEqual("this is a test .".Split())), true);
        Assert.That(env.Runtime.IsLoaded, Is.True);
    }

    [Test]
    public async Task TranslateAsync()
    {
        using var env = new TestEnvironment();
        env.EngineOptions.InactiveEngineTimeout = TimeSpan.FromHours(1);
        await env.Runtime.InitNewAsync();
        TranslationResult result = await env.Runtime.TranslateAsync("esto es una prueba .".Split());
        Assert.That(result.TargetSegment, Is.EqualTo("this is a TEST .".Split()));
    }

    private class TestEnvironment : DisposableBase
    {
        private readonly MemoryStorage _memoryStorage;
        private readonly BackgroundJobClient _jobClient;
        private BackgroundJobServer _jobServer;
        private readonly ISmtModelFactory _smtModelFactory;
        private readonly ITransferEngineFactory _transferEngineFactory;
        private readonly ITruecaserFactory _truecaserFactory;
        private readonly ICorpusService _dataFileService;
        private readonly IDistributedReaderWriterLockFactory _lockFactory;

        public TestEnvironment()
        {
            Engines = new MemoryRepository<TranslationEngine>();
            Engines.Add(
                new TranslationEngine
                {
                    Id = "engine1",
                    Owner = "client",
                    SourceLanguageTag = "es",
                    TargetLanguageTag = "en",
                    Type = TranslationEngineType.SmtTransfer
                }
            );
            Builds = new MemoryRepository<Build>();
            TrainSegmentPairs = new MemoryRepository<TrainSegmentPair>();
            EngineOptions = new TranslationEngineOptions();
            _memoryStorage = new MemoryStorage();
            _jobClient = new BackgroundJobClient(_memoryStorage);
            WebhookService = Substitute.For<IWebhookService>();
            SmtModel = Substitute.For<IInteractiveTranslationModel>();
            SmtBatchTrainer = Substitute.For<ITrainer>();
            SmtBatchTrainer.Stats.Returns(new TrainStats { Metrics = { { "bleu", 0.0 }, { "perplexity", 0.0 } } });
            Truecaser = Substitute.For<ITruecaser>();
            TruecaserTrainer = Substitute.For<ITrainer>();
            TruecaserTrainer.SaveAsync().Returns(Task.CompletedTask);
            _smtModelFactory = CreateSmtModelFactory();
            _transferEngineFactory = CreateTransferEngineFactory();
            _truecaserFactory = CreateTruecaserFactory();
            _dataFileService = CreateDataFileService();
            _lockFactory = new DistributedReaderWriterLockFactory(
                new OptionsWrapper<ServiceOptions>(new ServiceOptions { ServiceId = "host" }),
                new MemoryRepository<RWLock>()
            );
            _jobServer = CreateJobServer();
            Runtime = CreateRuntime();
        }

        public SmtTransferEngineRuntime Runtime { get; private set; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public MemoryRepository<Build> Builds { get; }
        public MemoryRepository<TrainSegmentPair> TrainSegmentPairs { get; }
        public ITrainer SmtBatchTrainer { get; }
        public IInteractiveTranslationModel SmtModel { get; }
        public TranslationEngineOptions EngineOptions { get; }
        public ITruecaser Truecaser { get; }
        public ITrainer TruecaserTrainer { get; }
        public IWebhookService WebhookService { get; }

        public void StopServer()
        {
            Runtime.Dispose();
            _jobServer.Dispose();
        }

        public void StartServer()
        {
            _jobServer = CreateJobServer();
            Runtime = CreateRuntime();
        }

        private BackgroundJobServer CreateJobServer()
        {
            var jobServerOptions = new BackgroundJobServerOptions
            {
                Activator = new EnvActivator(this),
                Queues = new[] { "smt_transfer" },
                CancellationCheckInterval = TimeSpan.FromMilliseconds(50),
            };
            return new BackgroundJobServer(jobServerOptions, _memoryStorage);
        }

        private SmtTransferEngineRuntime CreateRuntime()
        {
            var engineOptions = Substitute.For<IOptionsMonitor<TranslationEngineOptions>>();
            engineOptions.CurrentValue.Returns(EngineOptions);
            return new SmtTransferEngineRuntime(
                engineOptions,
                Engines,
                Builds,
                TrainSegmentPairs,
                _smtModelFactory,
                _transferEngineFactory,
                _truecaserFactory,
                _jobClient,
                _lockFactory,
                "engine1"
            );
        }

        private ISmtModelFactory CreateSmtModelFactory()
        {
            var factory = Substitute.For<ISmtModelFactory>();

            var engine = Substitute.For<IInteractiveTranslationEngine>();
            var translationResult = new TranslationResult(
                5,
                "this is a test .".Split(),
                new[] { 1.0, 1.0, 1.0, 1.0, 1.0 },
                new[]
                {
                    TranslationSources.Smt,
                    TranslationSources.Smt,
                    TranslationSources.Smt,
                    TranslationSources.Smt,
                    TranslationSources.Smt
                },
                new WordAlignmentMatrix(5, 5)
                {
                    [0, 0] = true,
                    [1, 1] = true,
                    [2, 2] = true,
                    [3, 3] = true,
                    [4, 4] = true
                },
                new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) }
            );
            engine.Translate(Arg.Any<IReadOnlyList<string>>()).Returns(translationResult);
            engine
                .GetWordGraph(Arg.Any<IReadOnlyList<string>>())
                .Returns(
                    new WordGraph(
                        new[]
                        {
                            new WordGraphArc(
                                0,
                                1,
                                1.0,
                                "this is".Split(),
                                new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                                Range<int>.Create(0, 2),
                                GetSources(2, false),
                                new[] { 1.0, 1.0 }
                            ),
                            new WordGraphArc(
                                1,
                                2,
                                1.0,
                                "a test".Split(),
                                new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                                Range<int>.Create(2, 4),
                                GetSources(2, false),
                                new[] { 1.0, 1.0 }
                            ),
                            new WordGraphArc(
                                2,
                                3,
                                1.0,
                                new[] { "." },
                                new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                                Range<int>.Create(4, 5),
                                GetSources(1, false),
                                new[] { 1.0 }
                            )
                        },
                        new[] { 3 }
                    )
                );
            SmtModel.CreateInteractiveEngine().Returns(engine);

            factory.Create(Arg.Any<string>()).Returns(SmtModel);
            factory.CreateTrainer(Arg.Any<string>(), Arg.Any<IParallelTextCorpus>()).Returns(SmtBatchTrainer);
            return factory;
        }

        private static ITransferEngineFactory CreateTransferEngineFactory()
        {
            var factory = Substitute.For<ITransferEngineFactory>();
            var engine = Substitute.For<ITranslationEngine>();
            engine
                .Translate(Arg.Any<IReadOnlyList<string>>())
                .Returns(
                    new TranslationResult(
                        5,
                        "this is a test .".Split(),
                        new[] { 1.0, 1.0, 1.0, 1.0, 1.0 },
                        new[]
                        {
                            TranslationSources.Transfer,
                            TranslationSources.Transfer,
                            TranslationSources.Transfer,
                            TranslationSources.Transfer,
                            TranslationSources.Transfer
                        },
                        new WordAlignmentMatrix(5, 5)
                        {
                            [0, 0] = true,
                            [1, 1] = true,
                            [2, 2] = true,
                            [3, 3] = true,
                            [4, 4] = true
                        },
                        new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) }
                    )
                );
            factory.Create(Arg.Any<string>()).Returns(engine);
            return factory;
        }

        private ITruecaserFactory CreateTruecaserFactory()
        {
            var factory = Substitute.For<ITruecaserFactory>();
            Truecaser
                .Truecase(Arg.Any<IReadOnlyList<string>>())
                .Returns(
                    x =>
                    {
                        var segment = x.Arg<IReadOnlyList<string>>();
                        return segment.Select(t => t == "test" ? "TEST" : t).ToArray();
                    }
                );
            factory.CreateAsync(Arg.Any<string>()).Returns(Task.FromResult(Truecaser));
            factory.CreateTrainer(Arg.Any<string>(), Arg.Any<ITextCorpus>()).Returns(TruecaserTrainer);
            return factory;
        }

        private static ICorpusService CreateDataFileService()
        {
            var dataFileService = Substitute.For<ICorpusService>();
            dataFileService
                .CreateTextCorpusAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult<ITextCorpus?>(null));
            return dataFileService;
        }

        private static IEnumerable<TranslationSources> GetSources(int count, bool isUnknown)
        {
            var sources = new TranslationSources[count];
            for (int i = 0; i < count; i++)
                sources[i] = isUnknown ? TranslationSources.None : TranslationSources.Smt;
            return sources;
        }

        public Task WaitForBuildToFinishAsync(string buildId)
        {
            return WaitForBuildState(buildId, b => b.DateFinished is not null);
        }

        public Task WaitForBuildToStartAsync(string buildId)
        {
            return WaitForBuildState(buildId, b => b.State != BuildState.Pending);
        }

        private async Task WaitForBuildState(string buildId, Func<Build, bool> predicate)
        {
            using ISubscription<Build> subscription = await Builds.SubscribeAsync(b => b.Id == buildId);
            while (true)
            {
                Build? build = subscription.Change.Entity;
                if (build != null && predicate(build))
                    break;
                await subscription.WaitForChangeAsync();
            }
        }

        protected override void DisposeManagedResources()
        {
            Runtime.Dispose();
            _jobServer.Dispose();
        }

        private class EnvActivator : JobActivator
        {
            private readonly TestEnvironment _env;

            public EnvActivator(TestEnvironment env)
            {
                _env = env;
            }

            public override object ActivateJob(Type jobType)
            {
                if (jobType == typeof(SmtTransferEngineBuildJob))
                {
                    return new SmtTransferEngineBuildJob(
                        _env.Engines,
                        _env.Builds,
                        _env.TrainSegmentPairs,
                        _env._lockFactory,
                        _env._dataFileService,
                        _env._truecaserFactory,
                        _env._smtModelFactory,
                        Substitute.For<ILogger<SmtTransferEngineBuildJob>>(),
                        _env.WebhookService
                    );
                }
                return base.ActivateJob(jobType);
            }
        }
    }
}
