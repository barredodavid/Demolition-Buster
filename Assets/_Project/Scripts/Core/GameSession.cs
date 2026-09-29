using System;
using DemolitionBuster.Core.Timer;
using DemolitionBuster.Core.Tower;

namespace DemolitionBuster.Core
{
    public enum SessionState
    {
        /// <summary>Tower is built, the timer waits for the first tap.</summary>
        Ready,
        Playing,
        Over,
    }

    public enum DeathCause
    {
        None,
        Crushed,
        Blocked,
        TimeOut,
    }

    /// <summary>
    /// The full rule set of one run: tower + timer + score. Engine-agnostic, so the whole
    /// game loop can be unit tested; the Unity side only feeds it input and time, then renders it.
    /// </summary>
    public sealed class GameSession
    {
        public TowerModel Tower { get; }
        public TimerModel Timer { get; }

        public SessionState State { get; private set; }
        public DeathCause DeathCause { get; private set; }
        public int Score { get; private set; }
        public int CoinsCollected { get; private set; }

        public GameSession(TowerModel tower, TimerModel timer)
        {
            Tower = tower ?? throw new ArgumentNullException(nameof(tower));
            Timer = timer ?? throw new ArgumentNullException(nameof(timer));
            Reset();
        }

        public void Reset()
        {
            Tower.Reset();
            Timer.Reset();
            Score = 0;
            CoinsCollected = 0;
            DeathCause = DeathCause.None;
            State = SessionState.Ready;
        }

        /// <summary>Processes a tap. Returns false (and ignores it) once the run is over.</summary>
        public bool TryHit(Side side, out HitResult result)
        {
            result = default;
            if (State == SessionState.Over) return false;

            if (State == SessionState.Ready)
            {
                State = SessionState.Playing;
                Timer.Start();
            }

            result = Tower.Hit(side);

            if (result.DestroyedABlock)
            {
                Score++;
                CoinsCollected += result.DestroyedBlock.CoinValue;
            }

            if (result.IsFatal)
            {
                End(result.Outcome == HitOutcome.Crushed ? DeathCause.Crushed : DeathCause.Blocked);
            }
            else
            {
                Timer.RegisterHit();
            }

            return true;
        }

        public void Tick(float deltaTime)
        {
            if (State != SessionState.Playing) return;
            if (Timer.Tick(deltaTime)) End(DeathCause.TimeOut);
        }

        private void End(DeathCause cause)
        {
            Timer.Stop();
            DeathCause = cause;
            State = SessionState.Over;
        }
    }
}
