using System.Collections;
using DemolitionBuster.Config;
using DemolitionBuster.Core;
using DemolitionBuster.Core.Progression;
using DemolitionBuster.Core.Timer;
using DemolitionBuster.Core.Tower;
using DemolitionBuster.Core.Utils;
using DemolitionBuster.InputHandling;
using DemolitionBuster.Persistence;
using DemolitionBuster.UI;
using DemolitionBuster.Views;
using UnityEngine;

namespace DemolitionBuster.Gameplay
{
    /// <summary>
    /// Composition root and application flow (menu → run → game over, plus the shop).
    /// It wires engine-agnostic models (<see cref="GameSession"/>, <see cref="PlayerProgress"/>, <see cref="ShopService"/>)
    /// to the scene views. Dependencies are passed explicitly: no singletons, no FindObjectOfType.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        private enum AppState
        {
            Menu,
            Shop,
            Run,
            GameOver,
        }

        [Header("Data")]
        [SerializeField] private GameConfig _config;
        [SerializeField] private DestroyerCatalog _catalog;

        [Header("Scene")]
        [SerializeField] private TapInputReader _input;
        [SerializeField] private TowerView _towerView;
        [SerializeField] private PlayerView _playerView;
        [SerializeField] private HitEffects _hitEffects;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private GameUI _ui;

        [Header("Feel")]
        [SerializeField] private float _hitShake = 0.06f;
        [SerializeField] private float _deathShake = 0.35f;

        private GameSession _session;
        private PlayerProgress _progress;
        private ShopService _shop;
        private DestroyerDefinition _destroyer;
        private AppState _state;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            _progress = new PlayerProgress(new PlayerPrefsSaveStorage());
            _shop = new ShopService(_progress, _catalog.Destroyers);

            var generator = new RandomBlockGenerator(_config.Tower, new SystemRandomSource());
            var tower = new TowerModel(_config.Tower.visibleBlocks, generator);
            _session = new GameSession(tower, new TimerModel(_config.Timer));

            _towerView.Build(tower.Count);
        }

        private void Start()
        {
            _ui.Initialize();
            _ui.Menu.PlayClicked += StartRun;
            _ui.Menu.ShopClicked += OpenShop;
            _ui.GameOver.RetryClicked += StartRun;
            _ui.GameOver.MenuClicked += OpenMenu;
            _ui.Shop.CloseClicked += OpenMenu;
            _ui.Shop.Bind(_shop, _progress, _catalog.Destroyers);

            _shop.EquippedChanged += _ => ApplyEquippedDestroyer();
            ApplyEquippedDestroyer();

            OpenMenu();
        }

        private void OnEnable() => _input.SideTapped += HandleSideTapped;

        private void OnDisable() => _input.SideTapped -= HandleSideTapped;

        private void Update()
        {
            if (_state != AppState.Run) return;

            _session.Tick(Time.deltaTime);
            _ui.Hud.SetTime(_session.Timer.Normalized);

            if (_session.State == SessionState.Over) EndRun();
        }

        // ---- Flow -------------------------------------------------------------------------------

        private void OpenMenu()
        {
            ResetWorld();
            _state = AppState.Menu;
            _ui.Menu.SetStats(_progress.BestScore, _progress.Coins);
            _ui.ShowOnly(_ui.Menu);
        }

        private void OpenShop()
        {
            _state = AppState.Shop;
            _ui.Shop.Refresh();
            _ui.ShowOnly(_ui.Shop);
        }

        private void StartRun()
        {
            ResetWorld();
            _state = AppState.Run;
            _ui.Hud.SetScore(0, animate: false);
            _ui.Hud.SetCoins(0);
            _ui.Hud.SetTime(_session.Timer.Normalized);
            _ui.Hud.SetHintVisible(true);
            _ui.ShowOnly(_ui.Hud);
        }

        private void EndRun()
        {
            _state = AppState.GameOver;
            _playerView.PlayDeath();
            _cameraRig.Shake(_deathShake, 0.4f);

            bool isNewRecord = _progress.SubmitScore(_session.Score);
            _progress.AddCoins(_session.CoinsCollected);

            StartCoroutine(ShowGameOverAfterDelay(isNewRecord));
        }

        private IEnumerator ShowGameOverAfterDelay(bool isNewRecord)
        {
            yield return new WaitForSeconds(_config.GameOverDelay);
            _ui.GameOver.SetResult(_session.DeathCause, _session.Score, _progress.BestScore, isNewRecord, _session.CoinsCollected);
            _ui.ShowOnly(_ui.GameOver);
        }

        private void ResetWorld()
        {
            StopAllCoroutines();
            _session.Reset();
            _towerView.Render(_session.Tower);
            _playerView.ResetTo(Side.Left);
        }

        // ---- Gameplay ---------------------------------------------------------------------------

        private void HandleSideTapped(Side side)
        {
            if (_state != AppState.Run) return;
            if (!_session.TryHit(side, out HitResult result)) return;

            _ui.Hud.SetHintVisible(false);
            _playerView.MoveTo(side);

            if (result.DestroyedABlock)
            {
                _playerView.PlayStrike();
                PlayHitEffects(side);
                _towerView.PlayBlockDestroyed(_session.Tower, result.DestroyedBlock, side);
                _cameraRig.Shake(_hitShake, 0.08f);
                _ui.Hud.SetScore(_session.Score, animate: true);
                _ui.Hud.SetCoins(_session.CoinsCollected);
            }

            _ui.Hud.SetTime(_session.Timer.Normalized);

            if (_session.State == SessionState.Over) EndRun();
        }

        /// <summary>Must run before the tower descends: the impact point is on the block being destroyed.</summary>
        private void PlayHitEffects(Side side)
        {
            Vector3 impact = _towerView.ImpactPoint(side);
            if (_destroyer != null && _destroyer.StrikeEffect == StrikeEffect.EnergyBlast)
                _hitEffects.PlayEnergyBlast(_playerView.BlastOrigin(), impact, _destroyer.BlastColor);
            else
                _hitEffects.PlayImpact(impact, side);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ---- Debug (editor menu "Demolition Buster > Debug", stripped from release builds) ----------

        /// <summary>Unlocks every destroyer for free, to test them all without grinding coins.</summary>
        public void DebugUnlockAllDestroyers()
        {
            foreach (DestroyerDefinition destroyer in _catalog.Destroyers) _progress.TryBuy(destroyer.Id, 0);
            RefreshProgressViews();
        }

        public void DebugAddCoins(int amount)
        {
            _progress.AddCoins(amount);
            RefreshProgressViews();
        }

        private void RefreshProgressViews()
        {
            _ui.Shop.Refresh();
            _ui.Menu.SetStats(_progress.BestScore, _progress.Coins);
        }
#endif

        private void ApplyEquippedDestroyer()
        {
            DestroyerDefinition destroyer = _catalog.Find(_shop.EquippedItem.Id);
            if (destroyer == null) return;
            _destroyer = destroyer;
            _playerView.Apply(destroyer);
        }
    }
}
