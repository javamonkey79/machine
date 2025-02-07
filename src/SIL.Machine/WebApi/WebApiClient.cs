﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SIL.Machine.Annotations;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace SIL.Machine.WebApi.Client
{
    public class WebApiClient
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public WebApiClient(string baseUrl, IHttpClient httpClient)
        {
            HttpClient = httpClient;
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";
            HttpClient.BaseUrl = baseUrl;
        }

        public string BaseUrl => HttpClient.BaseUrl;
        public IHttpClient HttpClient { get; }

        public async Task<TranslationEngineDto> GetEngineAsync(string engineId)
        {
            string url = $"translation/engines/{engineId}";
            HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Get, url, null, null);
            if (!response.IsSuccess)
                throw new HttpException("Error getting project.") { StatusCode = response.StatusCode };
            return JsonConvert.DeserializeObject<TranslationEngineDto>(response.Content, SerializerSettings);
        }

        public async Task<WordGraph> GetWordGraph(string engineId, IReadOnlyList<string> sourceSegment)
        {
            string url = $"translation/engines/{engineId}/get-word-graph";
            string body = JsonConvert.SerializeObject(sourceSegment, SerializerSettings);
            HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, url, body, "application/json");
            if (!response.IsSuccess)
            {
                throw new HttpException("Error calling get-word-graph action.") { StatusCode = response.StatusCode };
            }
            var resultDto = JsonConvert.DeserializeObject<WordGraphDto>(response.Content, SerializerSettings);
            return CreateModel(resultDto);
        }

        public async Task TrainSegmentPairAsync(
            string engineId,
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        )
        {
            string url = $"translation/engines/{engineId}/train-segment";
            var pairDto = new SegmentPairDto
            {
                SourceSegment = sourceSegment.ToArray(),
                TargetSegment = targetSegment.ToArray()
            };
            string body = JsonConvert.SerializeObject(pairDto, SerializerSettings);
            HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Post, url, body, "application/json");
            if (!response.IsSuccess)
                throw new HttpException("Error calling train-segment action.") { StatusCode = response.StatusCode };
        }

        public async Task StartTrainingAsync(string engineId)
        {
            await CreateBuildAsync(engineId);
        }

        public async Task TrainAsync(string engineId, Action<ProgressStatus> progress, CancellationToken ct = default)
        {
            BuildDto buildDto = await CreateBuildAsync(engineId);
            progress(CreateProgressStatus(buildDto));
            await PollBuildProgressAsync(engineId, $"builds/{buildDto.Id}", buildDto.Revision + 1, progress, ct);
        }

        public async Task ListenForTrainingStatusAsync(
            string engineId,
            Action<ProgressStatus> progress,
            CancellationToken ct = default
        )
        {
            await PollBuildProgressAsync(engineId, "current-build", 0, progress, ct);
        }

        private async Task<BuildDto> CreateBuildAsync(string engineId)
        {
            HttpResponse response = await HttpClient.SendAsync(
                HttpRequestMethod.Post,
                $"translation/engines/{engineId}/builds",
                null,
                null,
                CancellationToken.None
            );
            if (!response.IsSuccess)
                throw new HttpException("Error starting build.") { StatusCode = response.StatusCode };
            return JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
        }

        private async Task PollBuildProgressAsync(
            string engineId,
            string buildRelativeUrl,
            int minRevision,
            Action<ProgressStatus> progress,
            CancellationToken ct
        )
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                string url = $"translation/engines/{engineId}/{buildRelativeUrl}?minRevision={minRevision}";
                HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Get, url, null, null, ct);
                if (response.StatusCode == 200)
                {
                    BuildDto buildDto = JsonConvert.DeserializeObject<BuildDto>(response.Content, SerializerSettings);
                    progress(CreateProgressStatus(buildDto));
                    buildRelativeUrl = $"builds/{buildDto.Id}";
                    if (buildDto.State == BuildState.Completed || buildDto.State == BuildState.Canceled)
                        break;
                    else if (buildDto.State == BuildState.Faulted)
                        throw new InvalidOperationException("Error occurred during build: " + buildDto.Message);
                    minRevision = buildDto.Revision + 1;
                }
                else if (response.StatusCode == 408)
                {
                    continue;
                }
                else if (response.StatusCode == 404 || response.StatusCode == 204)
                {
                    break;
                }
                else
                {
                    throw new HttpException("Error getting build status.") { StatusCode = response.StatusCode };
                }
            }
        }

        private static ProgressStatus CreateProgressStatus(BuildDto buildDto)
        {
            return new ProgressStatus(buildDto.Step, buildDto.PercentCompleted, buildDto.Message);
        }

        private static WordGraph CreateModel(WordGraphDto dto)
        {
            var arcs = new List<WordGraphArc>();
            foreach (WordGraphArcDto arcDto in dto.Arcs)
            {
                WordAlignmentMatrix alignment = CreateModel(
                    arcDto.Alignment,
                    arcDto.SourceSegmentRange.End - arcDto.SourceSegmentRange.Start,
                    arcDto.Words.Length
                );
                arcs.Add(
                    new WordGraphArc(
                        arcDto.PrevState,
                        arcDto.NextState,
                        arcDto.Score,
                        arcDto.Words,
                        alignment,
                        CreateModel(arcDto.SourceSegmentRange),
                        arcDto.Sources,
                        arcDto.Confidences.Cast<double>()
                    )
                );
            }

            return new WordGraph(arcs, dto.FinalStates, dto.InitialStateScore);
        }

        private static TranslationResult CreateModel(TranslationResultDto dto, int sourceSegmentLength)
        {
            if (dto == null)
                return null;

            return new TranslationResult(
                sourceSegmentLength,
                dto.Target,
                dto.Confidences.Cast<double>(),
                dto.Sources,
                CreateModel(dto.Alignment, sourceSegmentLength, dto.Target.Length),
                dto.Phrases.Select(CreateModel)
            );
        }

        private static WordAlignmentMatrix CreateModel(AlignedWordPairDto[] dto, int i, int j)
        {
            var alignment = new WordAlignmentMatrix(i, j);
            foreach (AlignedWordPairDto wordPairDto in dto)
                alignment[wordPairDto.SourceIndex, wordPairDto.TargetIndex] = true;
            return alignment;
        }

        private static Phrase CreateModel(PhraseDto dto)
        {
            return new Phrase(CreateModel(dto.SourceSegmentRange), dto.TargetSegmentCut, dto.Confidence);
        }

        private static Range<int> CreateModel(RangeDto dto)
        {
            return Range<int>.Create(dto.Start, dto.End);
        }
    }
}
