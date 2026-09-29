using DemolitionBuster.Core.Timer;
using NUnit.Framework;

namespace DemolitionBuster.Tests
{
    public sealed class TimerModelTests
    {
        private static TimerSettings Settings() => new TimerSettings
        {
            maxTime = 5f,
            startTime = 2f,
            refillPerHit = 0.5f,
            baseDrainRate = 1f,
            drainRateIncreasePerHit = 0.1f,
            maxDrainRate = 1.5f,
        };

        [Test]
        public void Tick_BeforeStart_DoesNotDrain()
        {
            var timer = new TimerModel(Settings());

            timer.Tick(1f);

            Assert.That(timer.Remaining, Is.EqualTo(2f));
        }

        [Test]
        public void Tick_DrainsAndReportsExpirationOnce()
        {
            var timer = new TimerModel(Settings());
            timer.Start();

            Assert.That(timer.Tick(1f), Is.False);
            Assert.That(timer.Remaining, Is.EqualTo(1f).Within(1e-5));
            Assert.That(timer.Tick(5f), Is.True);
            Assert.That(timer.Remaining, Is.Zero);
            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.Tick(1f), Is.False, "An expired timer must not fire again.");
        }

        [Test]
        public void RegisterHit_RefillsUpToMaxTime()
        {
            var timer = new TimerModel(Settings());

            for (int i = 0; i < 20; i++) timer.RegisterHit();

            Assert.That(timer.Remaining, Is.EqualTo(5f));
            Assert.That(timer.Normalized, Is.EqualTo(1f));
        }

        [Test]
        public void DrainRate_IncreasesWithHitsAndIsCapped()
        {
            var timer = new TimerModel(Settings());
            Assert.That(timer.DrainRate, Is.EqualTo(1f));

            for (int i = 0; i < 3; i++) timer.RegisterHit();
            Assert.That(timer.DrainRate, Is.EqualTo(1.3f).Within(1e-5));

            for (int i = 0; i < 50; i++) timer.RegisterHit();
            Assert.That(timer.DrainRate, Is.EqualTo(1.5f));
        }

        [Test]
        public void Reset_RestoresStartState()
        {
            var timer = new TimerModel(Settings());
            timer.Start();
            timer.RegisterHit();
            timer.Tick(0.5f);

            timer.Reset();

            Assert.That(timer.Remaining, Is.EqualTo(2f));
            Assert.That(timer.Hits, Is.Zero);
            Assert.That(timer.IsRunning, Is.False);
        }
    }
}
