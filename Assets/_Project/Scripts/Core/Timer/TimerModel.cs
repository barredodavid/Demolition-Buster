using System;

namespace DemolitionBuster.Core.Timer
{
    /// <summary>
    /// The draining time bar. Each hit refills it a little but also makes it drain faster.
    /// Time is pushed in through <see cref="Tick"/>, so the model never reads a clock itself.
    /// </summary>
    public sealed class TimerModel
    {
        private readonly TimerSettings _settings;

        public float Remaining { get; private set; }
        public int Hits { get; private set; }
        public bool IsRunning { get; private set; }

        public float MaxTime => _settings.maxTime;
        public float Normalized => _settings.maxTime > 0f ? Remaining / _settings.maxTime : 0f;
        public bool IsExpired => Remaining <= 0f;

        public float DrainRate => Math.Min(_settings.maxDrainRate, _settings.baseDrainRate + Hits * _settings.drainRateIncreasePerHit);

        public TimerModel(TimerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (settings.maxTime <= 0f) throw new ArgumentOutOfRangeException(nameof(settings), "maxTime must be positive.");
            Reset();
        }

        public void Reset()
        {
            Remaining = Clamp(_settings.startTime);
            Hits = 0;
            IsRunning = false;
        }

        /// <summary>Refills the bar to its starting level but keeps the difficulty (hit count) reached so far.</summary>
        public void Revive()
        {
            Remaining = Clamp(_settings.startTime);
            IsRunning = false;
        }

        public void Start() => IsRunning = !IsExpired;

        public void Stop() => IsRunning = false;

        /// <summary>Advances the timer. Returns true on the frame the bar runs out.</summary>
        public bool Tick(float deltaTime)
        {
            if (!IsRunning || deltaTime <= 0f) return false;

            Remaining = Clamp(Remaining - DrainRate * deltaTime);
            if (!IsExpired) return false;

            IsRunning = false;
            return true;
        }

        public void RegisterHit()
        {
            if (IsExpired) return;
            Hits++;
            Remaining = Clamp(Remaining + _settings.refillPerHit);
        }

        private float Clamp(float value) => Math.Max(0f, Math.Min(_settings.maxTime, value));
    }
}
