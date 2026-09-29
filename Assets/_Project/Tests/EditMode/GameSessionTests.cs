using DemolitionBuster.Core;
using DemolitionBuster.Core.Timer;
using DemolitionBuster.Core.Tower;
using NUnit.Framework;

namespace DemolitionBuster.Tests
{
    public sealed class GameSessionTests
    {
        private static GameSession CreateSession(params Block[] blocks)
        {
            var tower = new TowerModel(4, new ScriptedBlockGenerator(blocks));
            var timer = new TimerModel(new TimerSettings { maxTime = 4f, startTime = 2f, refillPerHit = 0.25f, baseDrainRate = 1f, maxDrainRate = 1f });
            return new GameSession(tower, timer);
        }

        [Test]
        public void NewSession_IsReadyAndTimerIsIdle()
        {
            GameSession session = CreateSession();

            session.Tick(10f);

            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
            Assert.That(session.Timer.Remaining, Is.EqualTo(2f));
        }

        [Test]
        public void FirstHit_StartsTheRun()
        {
            GameSession session = CreateSession();

            Assert.That(session.TryHit(Side.Left, out _), Is.True);

            Assert.That(session.State, Is.EqualTo(SessionState.Playing));
            Assert.That(session.Timer.IsRunning, Is.True);
        }

        [Test]
        public void SuccessfulHits_ScoreCollectCoinsAndRefillTime()
        {
            GameSession session = CreateSession(new Block(Side.None, 5), Block.Empty, new Block(Side.None, 3));

            session.TryHit(Side.Left, out _);
            session.TryHit(Side.Right, out _);
            session.TryHit(Side.Left, out _);

            Assert.That(session.Score, Is.EqualTo(3));
            Assert.That(session.CoinsCollected, Is.EqualTo(8));
            Assert.That(session.Timer.Remaining, Is.EqualTo(2.75f).Within(1e-5));
        }

        [Test]
        public void FatalHit_EndsTheRunAndIgnoresFurtherTaps()
        {
            GameSession session = CreateSession(Block.Empty, new Block(Side.Right));

            session.TryHit(Side.Right, out HitResult result);

            Assert.That(result.Outcome, Is.EqualTo(HitOutcome.Crushed));
            Assert.That(session.State, Is.EqualTo(SessionState.Over));
            Assert.That(session.DeathCause, Is.EqualTo(DeathCause.Crushed));
            Assert.That(session.Timer.IsRunning, Is.False);
            Assert.That(session.TryHit(Side.Left, out _), Is.False);
            Assert.That(session.Score, Is.EqualTo(1), "The destroyed block still counts.");
        }

        [Test]
        public void WalkingIntoHazard_EndsWithBlockedCause()
        {
            GameSession session = CreateSession(new Block(Side.Left));

            session.TryHit(Side.Left, out _);

            Assert.That(session.DeathCause, Is.EqualTo(DeathCause.Blocked));
            Assert.That(session.Score, Is.Zero);
        }

        [Test]
        public void RunningOutOfTime_EndsWithTimeOut()
        {
            GameSession session = CreateSession();
            session.TryHit(Side.Left, out _);

            session.Tick(10f);

            Assert.That(session.State, Is.EqualTo(SessionState.Over));
            Assert.That(session.DeathCause, Is.EqualTo(DeathCause.TimeOut));
        }

        [Test]
        public void Reset_StartsAFreshRun()
        {
            GameSession session = CreateSession(new Block(Side.Left));
            session.TryHit(Side.Left, out _);

            session.Reset();

            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
            Assert.That(session.DeathCause, Is.EqualTo(DeathCause.None));
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.CoinsCollected, Is.Zero);
        }
    }
}
