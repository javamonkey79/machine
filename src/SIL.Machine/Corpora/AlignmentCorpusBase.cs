﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.Machine.Corpora
{
    public abstract class AlignmentCorpusBase : CorpusBase<AlignmentRow>, IAlignmentCorpus
    {
        public abstract IEnumerable<IAlignmentCollection> AlignmentCollections { get; }

        public override IEnumerable<AlignmentRow> GetRows()
        {
            return GetRows(null);
        }

        public abstract IEnumerable<AlignmentRow> GetRows(IEnumerable<string> alignmentCollectionIds);
    }
}
