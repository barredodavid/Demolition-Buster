using UnityEngine;
using UnityEngine.UIElements;

namespace DemolitionBuster.UI
{
    /// <summary>Owns the UIDocument: creates the screen presenters and keeps the layout inside the device safe area.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameUI : MonoBehaviour
    {
        public MenuScreen Menu { get; private set; }
        public HudScreen Hud { get; private set; }
        public GameOverScreen GameOver { get; private set; }
        public ShopScreen Shop { get; private set; }

        private VisualElement _safeArea;
        private Rect _appliedSafeArea;

        /// <summary>Called by the GameController; explicit so initialization order never depends on Awake/OnEnable ordering.</summary>
        public void Initialize()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _safeArea = root.Q<VisualElement>("safe-area");

            Menu = new MenuScreen(root.Q<VisualElement>("menu"));
            Hud = new HudScreen(root.Q<VisualElement>("hud"));
            GameOver = new GameOverScreen(root.Q<VisualElement>("gameover"));
            Shop = new ShopScreen(root.Q<VisualElement>("shop"));

            _safeArea.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea(force: true));
        }

        public void ShowOnly(UIScreen screen)
        {
            Menu.SetVisible(screen == Menu);
            Hud.SetVisible(screen == Hud);
            GameOver.SetVisible(screen == GameOver);
            Shop.SetVisible(screen == Shop);
        }

        private void Update() => ApplySafeArea(force: false);

        private void ApplySafeArea(bool force)
        {
            if (_safeArea?.panel == null) return;

            Rect safe = Screen.safeArea;
            if (!force && safe == _appliedSafeArea) return;

            float panelWidth = _safeArea.panel.visualTree.layout.width;
            if (float.IsNaN(panelWidth) || panelWidth <= 0f || Screen.width <= 0) return;

            // Screen pixels -> panel units (the panel is scaled with the screen size).
            float scale = panelWidth / Screen.width;
            _safeArea.style.paddingLeft = safe.xMin * scale;
            _safeArea.style.paddingRight = (Screen.width - safe.xMax) * scale;
            _safeArea.style.paddingTop = (Screen.height - safe.yMax) * scale;
            _safeArea.style.paddingBottom = safe.yMin * scale;
            _appliedSafeArea = safe;
        }
    }
}
