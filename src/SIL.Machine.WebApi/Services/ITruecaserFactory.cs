﻿namespace SIL.Machine.WebApi.Services;

public interface ITruecaserFactory
{
    Task<ITruecaser> CreateAsync(string engineId);
    ITrainer CreateTrainer(string engineId, ITextCorpus corpus);
    void Cleanup(string engineId);
}
