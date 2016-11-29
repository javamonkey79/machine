﻿using NUnit.Framework;

namespace SIL.Machine.Translation.Thot.Tests
{
	[TestFixture]
	public class ThotSmtSessionTests
	{
		[Test]
		public void TranslateInteractively_TranslationCorrect()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveTranslationSession session = engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("me marcho hoy por la tarde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void AddToPrefix_TranslationCorrect()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveTranslationSession session = engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("me marcho hoy por la tarde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = session.AddToPrefix("i am".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				result = session.AddToPrefix("leaving".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		[Test]
		public void AddToPrefix_MissingWord_TranslationCorrect()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveTranslationSession session = engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("caminé a mi habitación .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("caminé to my room .".Split()));
				result = session.AddToPrefix("i walked".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i walked to my room .".Split()));
			}
		}

		[Test]
		public void SetPrefix_TranslationCorrect()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveTranslationSession session = engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("me marcho hoy por la tarde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
				result = session.AddToPrefix("i am".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i am leave today in the afternoon .".Split()));
				result = session.SetPrefix("i".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i leave today in the afternoon .".Split()));
			}
		}

		[Test]
		public void Approve_TwoSegmentsUnknownWord_LearnsUnknownWord()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			using (IInteractiveTranslationSession session = engine.StartSession())
			{
				TranslationResult result = session.TranslateInteractively("hablé con recepción .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("hablé with reception .".Split()));
				result = session.AddToPrefix("i talked".Split(), true);
				Assert.That(result.TargetSegment, Is.EqualTo("i talked with reception .".Split()));
				session.AddToPrefix("with reception .".Split(), true);
				session.Approve();

				result = session.TranslateInteractively("hablé hasta cinco en punto .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("talked until five o ' clock .".Split()));
			}
		}
	}
}
