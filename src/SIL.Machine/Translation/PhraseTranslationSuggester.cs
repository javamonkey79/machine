﻿using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public class PhraseTranslationSuggester : ITranslationSuggester
    {
        public double ConfidenceThreshold { get; set; }

        public TranslationSuggestion GetSuggestion(int prefixCount, bool isLastWordComplete, TranslationResult result)
        {
            int startingJ = prefixCount;
            if (!isLastWordComplete)
            {
                // if the prefix ends with a partial word and it has been completed,
                // then make sure it is included as a suggestion,
                // otherwise, don't return any suggestions
                if ((result.WordSources[startingJ - 1] & TranslationSources.Smt) != 0)
                    startingJ--;
                else
                    return new TranslationSuggestion(result);
            }

            int k = 0;
            while (k < result.Phrases.Count && result.Phrases[k].TargetSegmentCut <= startingJ)
                k++;

            double minConfidence = -1;
            var indices = new List<int>();
            for (; k < result.Phrases.Count; k++)
            {
                Phrase phrase = result.Phrases[k];
                if (phrase.Confidence >= ConfidenceThreshold)
                {
                    bool hitBreakingWord = false;
                    for (int j = startingJ; j < phrase.TargetSegmentCut; j++)
                    {
                        string word = result.TargetSegment[j];
                        TranslationSources sources = result.WordSources[j];
                        if (sources == TranslationSources.None || word.All(char.IsPunctuation))
                        {
                            hitBreakingWord = true;
                            break;
                        }
                        indices.Add(j);
                    }
                    if (minConfidence < 0 || phrase.Confidence < minConfidence)
                        minConfidence = phrase.Confidence;
                    startingJ = phrase.TargetSegmentCut;
                    if (hitBreakingWord)
                        break;
                }
                else
                {
                    break;
                }
            }

            return new TranslationSuggestion(result, indices, minConfidence < 0 ? 0 : minConfidence);
        }
    }
}
