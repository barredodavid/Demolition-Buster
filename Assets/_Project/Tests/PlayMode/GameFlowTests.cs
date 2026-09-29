using System.Collections;
using System.IO;
using System.Reflection;
using DemolitionBuster.Core;
using DemolitionBuster.Gameplay;
using DemolitionBuster.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DemolitionBuster.Tests
{
    /// <summary>
    /// End-to-end tests on the real scene: UI clicks and simulated keyboard input drive a full run.
    /// The player's real save is backed up and restored around each test.
    /// </summary>
    public sealed class GameFlowTests : InputTestFixture
    {
        private const string SaveKey = "DemolitionBuster.Save";

        private Keyboard _keyboard;
        private string _savedProgress;

        public override void Setup()
        {
            base.Setup();
            _savedProgress = PlayerPrefs.HasKey(SaveKey) ? PlayerPrefs.GetString(SaveKey) : null;
            PlayerPrefs.DeleteKey(SaveKey);
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (_savedProgress != null) PlayerPrefs.SetString(SaveKey, _savedProgress);
            else PlayerPrefs.DeleteKey(SaveKey);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Run_FromMenuToGameOver_SavesTheBestScore()
        {
            yield return LoadGame();
            VisualElement ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Assert.That(IsVisible(ui, "menu"), Is.True, "The game boots on the menu.");

            Click(ui.Q<Button>("menu-play"));
            yield return null;
            Assert.That(IsVisible(ui, "hud"), Is.True);

            // The first floors are always hazard-free, so three hits are safe on any side.
            for (int i = 0; i < 3; i++) yield return Tap(Side.Left);
            Assert.That(ui.Q<Label>("hud-score").text, Is.EqualTo("3"));

            // Let the time bar run out quickly.
            Time.timeScale = 20f;
            float timeout = Time.realtimeSinceStartup + 5f;
            while (!IsVisible(ui, "gameover") && Time.realtimeSinceStartup < timeout) yield return null;
            Time.timeScale = 1f;

            Assert.That(IsVisible(ui, "gameover"), Is.True);
            Assert.That(ui.Q<Label>("gameover-cause").text, Is.EqualTo("Trop lent, chef !"));
            Assert.That(new PlayerPrefsSaveStorage(SaveKey).Load().bestScore, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Shop_OpensFromMenuAndListsEveryDestroyer()
        {
            yield return LoadGame();
            VisualElement ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

            Click(ui.Q<Button>("menu-shop"));
            yield return null;

            Assert.That(IsVisible(ui, "shop"), Is.True);
            Assert.That(ui.Q<ScrollView>("shop-list").contentContainer.childCount, Is.EqualTo(4));

            Click(ui.Q<Button>("shop-close"));
            yield return null;
            Assert.That(IsVisible(ui, "menu"), Is.True);
        }

        /// <summary>
        /// Not part of the regular suite: renders the world and the UI into textures and saves PNGs
        /// to Logs/Screenshots. Run it with -testFilter to refresh portfolio screenshots.
        /// </summary>
        [UnityTest, Explicit("Writes screenshots to disk.")]
        public IEnumerator CaptureScreenshots()
        {
            yield return LoadGame();
            var document = Object.FindFirstObjectByType<UIDocument>();
            VisualElement ui = document.rootVisualElement;
            Camera camera = Camera.main;
            GameSession session = ReadSession();

            var worldTexture = new RenderTexture(1080, 1920, 24);
            var uiTexture = new RenderTexture(1080, 1920, 0);
            camera.targetTexture = worldTexture;
            document.panelSettings.targetTexture = uiTexture;
            document.panelSettings.clearColor = true;
            document.panelSettings.colorClearValue = Color.clear;
            string folder = Path.Combine(Application.dataPath, "..", "Logs", "Screenshots");
            Directory.CreateDirectory(folder);

            try
            {
                yield return WaitFrames(10);
                Save(folder, "01_menu", worldTexture, uiTexture);

                Click(ui.Q<Button>("menu-play"));
                yield return WaitFrames(3);
                Save(folder, "02_ready", worldTexture, uiTexture);

                Side preferred = Side.Left;
                for (int i = 0; i < 25; i++)
                {
                    Side danger = session.Tower[0].HasHazard ? session.Tower[0].Hazard : session.Tower[1].Hazard;
                    Side side = danger == Side.None ? preferred : danger.Opposite();
                    if (i % 3 == 0) preferred = preferred.Opposite();
                    yield return Tap(side);
                    yield return WaitFrames(4);
                }
                yield return Tap(session.Tower[1].Hazard == Side.None ? Side.Left : session.Tower[1].Hazard.Opposite());
                yield return WaitFrames(1);
                Save(folder, "03_gameplay", worldTexture, uiTexture);

                // Die on purpose: step into the next hazard that reaches the ground.
                while (session.State != SessionState.Over)
                {
                    Side next = session.Tower[1].Hazard;
                    yield return Tap(next == Side.None ? (session.Tower[0].Hazard == Side.Left ? Side.Right : Side.Left) : next);
                }
                yield return WaitFrames(8);
                Save(folder, "04_death", worldTexture, uiTexture);
                yield return new WaitForSeconds(1.2f);
                Save(folder, "05_gameover", worldTexture, uiTexture);

                Click(ui.Q<Button>("gameover-menu"));
                yield return null;
                Click(ui.Q<Button>("menu-shop"));
                yield return WaitFrames(5);
                Save(folder, "06_shop", worldTexture, uiTexture);
            }
            finally
            {
                camera.targetTexture = null;
                document.panelSettings.targetTexture = null;
                document.panelSettings.clearColor = false;
                Object.Destroy(worldTexture);
                Object.Destroy(uiTexture);
            }
        }

        // ---- Helpers ----------------------------------------------------------------------------

        private static IEnumerator LoadGame()
        {
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;
        }

        private IEnumerator Tap(Side side)
        {
            var key = side == Side.Left ? _keyboard.leftArrowKey : _keyboard.rightArrowKey;
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private static IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        private static bool IsVisible(VisualElement ui, string screen) => !ui.Q<VisualElement>(screen).ClassListContains("hidden");

        /// <summary>Buttons react to a navigation submit like to a click.</summary>
        private static void Click(Button button)
        {
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        /// <summary>Test-only peek at the model, to play smart moves for the screenshots.</summary>
        private static GameSession ReadSession()
        {
            var controller = Object.FindFirstObjectByType<GameController>();
            FieldInfo field = typeof(GameController).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance);
            return (GameSession)field.GetValue(controller);
        }

        private static void Save(string folder, string name, RenderTexture world, RenderTexture ui)
        {
            Color[] background = Read(world);
            Color[] overlay = Read(ui);
            for (int i = 0; i < background.Length; i++)
            {
                Color top = overlay[i];
                Color composed = Color.Lerp(background[i], top, top.a);
                composed.a = 1f;
                background[i] = composed;
            }

            var result = new Texture2D(world.width, world.height, TextureFormat.RGBA32, false);
            result.SetPixels(background);
            result.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), result.EncodeToPNG());
            Object.Destroy(result);
        }

        private static Color[] Read(RenderTexture source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            Color[] pixels = texture.GetPixels();
            Object.Destroy(texture);
            return pixels;
        }
    }
}
