using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.SequenceAlignment
{
    [TestFixture]
    public class PairwiseAlignmentAlgorithmTests : AlignmentAlgorithmTestsBase
    {
        [Test]
        public void GlobalAlign()
        {
            var scorer = new StringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "car", "bar", GetChars);
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r |", "| b a r |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.66).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "bar", GetChars);
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r t |", "| b a r - |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.25).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "art", GetChars);
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r t |", "| - a r t |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.50).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "start", "tan", GetChars);
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(2));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| s t a r t |", "| - t a - n |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.00).Within(0.01));
            AssertAlignmentsEqual(alignments[1], CreateAlignment("| s t a r t |", "| - t a n - |"));
            Assert.That(alignments[1].NormalizedScore, Is.EqualTo(0.00).Within(0.01));
        }

        [Test]
        public void LocalAlign()
        {
            var scorer = new StringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "car", "bar", GetChars)
            {
                Mode = AlignmentMode.Local
            };
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(2));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r |", "| b a r |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.66).Within(0.01));
            AssertAlignmentsEqual(alignments[1], CreateAlignment("c | a r |", "b | a r |"));
            Assert.That(alignments[1].NormalizedScore, Is.EqualTo(0.80).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "bar", GetChars)
            {
                Mode = AlignmentMode.Local
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(2));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r | t", "| b a r |  "));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.57).Within(0.01));
            AssertAlignmentsEqual(alignments[1], CreateAlignment("c | a r | t", "b | a r |  "));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.57).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "art", GetChars)
            {
                Mode = AlignmentMode.Local
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("c | a r t |", "  | a r t |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.86).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "start", "tan", GetChars)
            {
                Mode = AlignmentMode.Local
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(2));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("s | t a | rt", "  | t a | n"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.57).Within(0.01));
            AssertAlignmentsEqual(alignments[1], CreateAlignment("s | t a r | t", "  | t a n |  "));
            Assert.That(alignments[1].NormalizedScore, Is.EqualTo(0.50).Within(0.01));
        }

        [Test]
        public void HalfLocalAlign()
        {
            var scorer = new StringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "car", "bar", GetChars)
            {
                Mode = AlignmentMode.HalfLocal
            };
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r |", "| b a r |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.66).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "bar", GetChars)
            {
                Mode = AlignmentMode.HalfLocal
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r | t", "| b a r |  "));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.57).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "art", GetChars)
            {
                Mode = AlignmentMode.HalfLocal
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r t |", "| - a r t |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.50).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "start", "tan", GetChars)
            {
                Mode = AlignmentMode.HalfLocal
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(2));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| s t a | rt", "| - t a | n"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.25).Within(0.01));
            AssertAlignmentsEqual(alignments[1], CreateAlignment("| s t a r | t", "| - t a n |  "));
            Assert.That(alignments[1].NormalizedScore, Is.EqualTo(0.22).Within(0.01));
        }

        [Test]
        public void SemiGlobalAlign()
        {
            var scorer = new StringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "car", "bar", GetChars)
            {
                Mode = AlignmentMode.SemiGlobal
            };
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r |", "| b a r |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.66).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "bar", GetChars)
            {
                Mode = AlignmentMode.SemiGlobal
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r | t", "| b a r |  "));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.57).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "art", GetChars)
            {
                Mode = AlignmentMode.SemiGlobal
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("c | a r t |", "  | a r t |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.86).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "start", "tan", GetChars)
            {
                Mode = AlignmentMode.SemiGlobal
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("s | t a r | t", "  | t a n |  "));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.50).Within(0.01));
        }

        [Test]
        public void ExpansionCompressionAlign()
        {
            var scorer = new StringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "car", "bar", GetChars)
            {
                ExpansionCompressionEnabled = true
            };
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r |", "| b a r |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.66).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "bar", GetChars)
            {
                ExpansionCompressionEnabled = true
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a rt |", "| b a r  |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.50).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "cart", "art", GetChars)
            {
                ExpansionCompressionEnabled = true
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| ca r t |", "| a  r t |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.75).Within(0.01));

            msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "start", "tan", GetChars)
            {
                ExpansionCompressionEnabled = true
            };
            msa.Compute();
            alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(2));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| st ar t |", "| t  a  n |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0.40).Within(0.01));
            AssertAlignmentsEqual(alignments[1], CreateAlignment("| st a rt |", "| t  a n  |"));
            Assert.That(alignments[1].NormalizedScore, Is.EqualTo(0.40).Within(0.01));
        }

        [Test]
        public void ZeroMaxScore()
        {
            var scorer = new ZeroMaxScoreStringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "car", "bar", GetChars)
            {
                ExpansionCompressionEnabled = true
            };
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("| c a r |", "| b a r |"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0));
        }

        [Test]
        public void GlobalAlign_EmptySequence()
        {
            var scorer = new StringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "", "", GetChars);
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("||", "||"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0));
        }

        [Test]
        public void LocalAlign_EmptySequence()
        {
            var scorer = new StringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "", "", GetChars)
            {
                Mode = AlignmentMode.Local
            };
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("||", "||"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0));
        }

        [Test]
        public void HalfLocalAlign_EmptySequence()
        {
            var scorer = new StringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "", "", GetChars)
            {
                Mode = AlignmentMode.HalfLocal
            };
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("||", "||"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0));
        }

        [Test]
        public void SemiGlobalAlign_EmptySequence()
        {
            var scorer = new StringScorer();
            var msa = new PairwiseAlignmentAlgorithm<string, char>(scorer, "", "", GetChars)
            {
                Mode = AlignmentMode.SemiGlobal
            };
            msa.Compute();
            Alignment<string, char>[] alignments = msa.GetAlignments().ToArray();

            Assert.That(alignments.Length, Is.EqualTo(1));
            AssertAlignmentsEqual(alignments[0], CreateAlignment("||", "||"));
            Assert.That(alignments[0].NormalizedScore, Is.EqualTo(0));
        }
    }
}
