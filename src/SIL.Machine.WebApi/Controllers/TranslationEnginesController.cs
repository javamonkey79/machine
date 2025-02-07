﻿namespace SIL.Machine.WebApi.Controllers;

[Route("translation-engines")]
[OpenApiTag("Translation Engines")]
public class TranslationEnginesController : ControllerBase
{
    private readonly ITranslationEngineService _translationEngineService;
    private readonly ICorpusService _corpusService;
    private readonly IBuildService _buildService;
    private readonly IPretranslationService _pretranslationService;
    private readonly IOptionsMonitor<ApiOptions> _apiOptions;
    private readonly IMapper _mapper;

    public TranslationEnginesController(
        IAuthorizationService authService,
        ITranslationEngineService translationEngineService,
        ICorpusService corpusService,
        IBuildService buildService,
        IPretranslationService pretranslationService,
        IOptionsMonitor<ApiOptions> apiOptions,
        IMapper mapper
    ) : base(authService)
    {
        _translationEngineService = translationEngineService;
        _corpusService = corpusService;
        _buildService = buildService;
        _pretranslationService = pretranslationService;
        _apiOptions = apiOptions;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets all translation engines.
    /// </summary>
    /// <response code="200">The engines.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpGet]
    public async Task<IEnumerable<TranslationEngineDto>> GetAllAsync()
    {
        return (await _translationEngineService.GetAllAsync(User.Identity!.Name!)).Select(
            _mapper.Map<TranslationEngineDto>
        );
    }

    /// <summary>
    /// Gets a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <response code="200">The translation engine.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TranslationEngineDto>> GetAsync([NotNull] string id)
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        return Ok(_mapper.Map<TranslationEngineDto>(engine));
    }

    /// <summary>
    /// Creates a new translation engine.
    /// </summary>
    /// <param name="engineConfig">The translation engine configuration.</param>
    /// <response code="201">The translation engine was created successfully.</response>
    [Authorize(Scopes.CreateTranslationEngines)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<TranslationEngineDto>> CreateAsync(
        [FromBody] TranslationEngineConfigDto engineConfig
    )
    {
        var newEngine = new TranslationEngine
        {
            Name = engineConfig.Name,
            SourceLanguageTag = engineConfig.SourceLanguageTag,
            TargetLanguageTag = engineConfig.TargetLanguageTag,
            Type = engineConfig.Type,
            Owner = User.Identity!.Name!
        };

        await _translationEngineService.CreateAsync(newEngine);
        TranslationEngineDto dto = _mapper.Map<TranslationEngineDto>(newEngine);
        return Created(dto.Href, dto);
    }

    /// <summary>
    /// Deletes a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <response code="200">The engine was successfully deleted.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.DeleteTranslationEngines)]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteAsync([NotNull] string id)
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        if (!await _translationEngineService.DeleteAsync(id))
            return NotFound();
        return Ok();
    }

    /// <summary>
    /// Translates a tokenized segment of text.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="segment">The tokenized source segment.</param>
    /// <response code="200">The translation result.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    /// <response code="405">The method is not supported.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpPost("{id}/translate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status405MethodNotAllowed)]
    public async Task<ActionResult<TranslationResultDto>> TranslateAsync(
        [NotNull] string id,
        [FromBody] string[] segment
    )
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        TranslationResult? result = await _translationEngineService.TranslateAsync(engine.Id, segment);
        if (result == null)
            return NotFound();
        return Ok(_mapper.Map<TranslationResultDto>(result));
    }

    /// <summary>
    /// Translates a tokenized segment of text into the top N results.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="n">The number of translations.</param>
    /// <param name="segment">The tokenized source segment.</param>
    /// <response code="200">The translation results.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    /// <response code="405">The method is not supported.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpPost("{id}/translate/{n}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status405MethodNotAllowed)]
    public async Task<ActionResult<IEnumerable<TranslationResultDto>>> TranslateAsync(
        [NotNull] string id,
        [NotNull] int n,
        [FromBody] string[] segment
    )
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        IEnumerable<TranslationResult>? results = await _translationEngineService.TranslateAsync(engine.Id, n, segment);
        if (results == null)
            return NotFound();
        return Ok(results.Select(_mapper.Map<TranslationResultDto>));
    }

    /// <summary>
    /// Gets the word graph that represents all possible translations of a tokenized segment of text.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="segment">The tokenized source segment.</param>
    /// <response code="200">The word graph.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    /// <response code="405">The method is not supported.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpPost("{id}/get-word-graph")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status405MethodNotAllowed)]
    public async Task<ActionResult<WordGraphDto>> InteractiveTranslateAsync(
        [NotNull] string id,
        [FromBody] string[] segment
    )
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        WordGraph? result = await _translationEngineService.GetWordGraphAsync(engine.Id, segment);
        if (result == null)
            return NotFound();
        return Ok(_mapper.Map<WordGraphDto>(result));
    }

    /// <summary>
    /// Incrementally trains a translation engine with a tokenized segment pair.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="segmentPair">The tokenized segment pair.</param>
    /// <response code="200">The engine was trained successfully.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    /// <response code="405">The method is not supported.</response>
    [Authorize(Scopes.UpdateTranslationEngines)]
    [HttpPost("{id}/train-segment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status405MethodNotAllowed)]
    public async Task<ActionResult> TrainSegmentAsync([NotNull] string id, [FromBody] SegmentPairDto segmentPair)
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        if (
            !await _translationEngineService.TrainSegmentAsync(
                engine.Id,
                segmentPair.SourceSegment,
                segmentPair.TargetSegment,
                segmentPair.SentenceStart
            )
        )
        {
            return NotFound();
        }
        return Ok();
    }

    /// <summary>
    /// Adds a corpus to a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="corpusInfo">The corpus configuration.</param>
    /// <response code="200">The corpus was added successfully.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.UpdateTranslationEngines)]
    [HttpPost("{id}/corpora")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TranslationEngineCorpusDto>> AddCorporaAsync(
        [NotNull] string id,
        [FromBody] TranslationEngineCorpusConfigDto corpusInfo
    )
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        Corpus? corpus = await _corpusService.GetAsync(corpusInfo.CorpusId);
        if (corpus == null)
            return UnprocessableEntity();
        if (!await AuthorizeIsOwnerAsync(corpus))
            return Forbid();

        var translationEngineCorpus = new TranslationEngineCorpus
        {
            CorpusRef = corpusInfo.CorpusId,
            Pretranslate = corpusInfo.Pretranslate ?? false
        };
        await _translationEngineService.AddCorpusAsync(id, translationEngineCorpus);
        return Ok(Map(id, translationEngineCorpus));
    }

    /// <summary>
    /// Gets all corpora for a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <response code="200">The files.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpGet("{id}/corpora")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TranslationEngineCorpusDto>>> GetAllCorporaAsync([NotNull] string id)
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        return Ok(engine.Corpora.Select(c => Map(id, c)));
    }

    /// <summary>
    /// Gets the configuration of a corpus for a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="corpusId">The corpus id.</param>
    /// <response code="200">The corpus configuration.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpGet("{id}/corpora/{corpusId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TranslationEngineCorpusDto>> GetCorpusAsync(
        [NotNull] string id,
        [NotNull] string corpusId
    )
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        TranslationEngineCorpus? corpus = engine.Corpora.FirstOrDefault(f => f.CorpusRef == corpusId);
        if (corpus == null)
            return NotFound();

        return Ok(Map(id, corpus));
    }

    /// <summary>
    /// Removes a corpus from a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="corpusId">The corpus id.</param>
    /// <response code="200">The data file was deleted successfully.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.UpdateTranslationEngines)]
    [HttpDelete("{id}/corpora/{corpusId}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteCorpusAsync([NotNull] string id, [NotNull] string corpusId)
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        if (!await _translationEngineService.DeleteCorpusAsync(id, corpusId))
            return NotFound();

        return Ok();
    }

    /// <summary>
    /// Gets all pretranslations in a corpus of a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="corpusId">The corpus id.</param>
    /// <response code="200">The pretranslations.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpGet("{id}/corpora/{corpusId}/pretranslations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PretranslationDto>>> GetAllPretranslationsAsync(
        [NotNull] string id,
        [NotNull] string corpusId
    )
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        return Ok((await _pretranslationService.GetAllAsync(id, corpusId)).Select(_mapper.Map<PretranslationDto>));
    }

    /// <summary>
    /// Gets all pretranslations in a corpus text of a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="corpusId">The corpus id.</param>
    /// <param name="textId">The text id.</param>
    /// <response code="200">The pretranslations.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpGet("{id}/corpora/{corpusId}/pretranslations/{textId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PretranslationDto>>> GetAllPretranslationsAsync(
        [NotNull] string id,
        [NotNull] string corpusId,
        [NotNull] string textId
    )
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        return Ok(
            (await _pretranslationService.GetAllAsync(id, corpusId, textId)).Select(_mapper.Map<PretranslationDto>)
        );
    }

    /// <summary>
    /// Gets all build jobs for a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <response code="200">The build jobs.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpGet("{id}/builds")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<BuildDto>>> GetAllBuildsAsync([NotNull] string id)
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        return Ok((await _buildService.GetAllAsync(id)).Select(_mapper.Map<BuildDto>));
    }

    /// <summary>
    /// Gets a build job.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="buildId">The build job id.</param>
    /// <param name="minRevision">The minimum revision.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">The build job.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    /// <response code="499">The long polling request timed out.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpGet("{id}/builds/{buildId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), 499)]
    public async Task<ActionResult<BuildDto>> GetBuildAsync(
        [NotNull] string id,
        [NotNull] string buildId,
        [FromQuery] long? minRevision,
        CancellationToken cancellationToken
    )
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id, cancellationToken);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        if (minRevision != null)
        {
            EntityChange<Build> change = await TaskEx.Timeout(
                ct => _buildService.GetNewerRevisionAsync(buildId, minRevision.Value, ct),
                _apiOptions.CurrentValue.LongPollTimeout,
                cancellationToken
            );
            return change.Type switch
            {
                EntityChangeType.None => StatusCode(StatusCodes.Status408RequestTimeout),
                EntityChangeType.Delete => NotFound(),
                _ => Ok(_mapper.Map<BuildDto>(change.Entity!)),
            };
        }
        else
        {
            Build? build = await _buildService.GetAsync(buildId, cancellationToken);
            if (build == null)
                return NotFound();

            return Ok(_mapper.Map<BuildDto>(build));
        }
    }

    /// <summary>
    /// Starts a build job for a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <response code="201">The build job was started successfully.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.UpdateTranslationEngines)]
    [HttpPost("{id}/builds")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BuildDto>> CreateBuildAsync([NotNull] string id)
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        Build? build = await _translationEngineService.StartBuildAsync(id);
        if (build == null)
            return NotFound();
        var dto = _mapper.Map<BuildDto>(build);
        return Created(dto.Href, dto);
    }

    /// <summary>
    /// Gets the currently running build job for a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <param name="minRevision">The minimum revision.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">The build job.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    /// <response code="499">The long polling request timed out.</response>
    [Authorize(Scopes.ReadTranslationEngines)]
    [HttpGet("{id}/current-build")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), 499)]
    public async Task<ActionResult<BuildDto>> GetCurrentBuildAsync(
        [NotNull] string id,
        [FromQuery] long? minRevision,
        CancellationToken cancellationToken
    )
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id, cancellationToken);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        if (minRevision != null)
        {
            EntityChange<Build> change = await TaskEx.Timeout(
                ct => _buildService.GetActiveNewerRevisionAsync(id, minRevision.Value, ct),
                _apiOptions.CurrentValue.LongPollTimeout,
                cancellationToken
            );
            return change.Type switch
            {
                EntityChangeType.None => StatusCode(StatusCodes.Status408RequestTimeout),
                EntityChangeType.Delete => NoContent(),
                _ => Ok(_mapper.Map<BuildDto>(change.Entity!)),
            };
        }
        else
        {
            Build? build = await _buildService.GetActiveAsync(id, cancellationToken);
            if (build == null)
                return NoContent();

            return Ok(_mapper.Map<BuildDto>(build));
        }
    }

    /// <summary>
    /// Cancels the current build job for a translation engine.
    /// </summary>
    /// <param name="id">The translation engine id.</param>
    /// <response code="200">The build job was cancelled successfully.</response>
    /// <response code="403">The authenticated client does not own the translation engine.</response>
    [Authorize(Scopes.UpdateTranslationEngines)]
    [HttpPost("{id}/current-build/cancel")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CancelBuildAsync([NotNull] string id)
    {
        TranslationEngine? engine = await _translationEngineService.GetAsync(id);
        if (engine == null)
            return NotFound();
        if (!await AuthorizeIsOwnerAsync(engine))
            return Forbid();

        await _translationEngineService.CancelBuildAsync(id);
        return Ok();
    }

    private TranslationEngineCorpusDto Map(string engineId, TranslationEngineCorpus corpus)
    {
        return _mapper.Map<TranslationEngineCorpusDto>(corpus, opts => opts.Items["EngineId"] = engineId);
    }
}
