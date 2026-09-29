using DemolitionBuster.Core.Timer;
using DemolitionBuster.Core.Tower;
using UnityEngine;

namespace DemolitionBuster.Config
{
    /// <summary>Single entry point for game-design tuning, editable in the Inspector without touching code.</summary>
    [CreateAssetMenu(menuName = "Demolition Buster/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] private TowerSettings _tower = new TowerSettings();
        [SerializeField] private TimerSettings _timer = new TimerSettings();

        [Tooltip("Pause between the fatal hit and the game over screen, so the death animation reads.")]
        [SerializeField, Min(0f)] private float _gameOverDelay = 0.9f;

        public TowerSettings Tower => _tower;
        public TimerSettings Timer => _timer;
        public float GameOverDelay => _gameOverDelay;
    }
}
