using System;
using System.IO;
using DemolitionBuster.Config;
using DemolitionBuster.Editor.Art;
using DemolitionBuster.Gameplay;
using DemolitionBuster.InputHandling;
using DemolitionBuster.UI;
using DemolitionBuster.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DemolitionBuster.Editor
{
    /// <summary>
    /// Builds every piece of content from code: sprites, data assets, prefabs, UI settings, the scene and
    /// the player settings. Safe to run repeatedly; tuned values in existing GameConfig assets are preserved.
    /// Menu: Demolition Buster > Build Project Content. Command line: -executeMethod DemolitionBuster.Editor.ProjectSetup.BuildAll
    /// </summary>
    public static class ProjectSetup
    {
        private const string Root = "Assets/_Project";
        private const string SpritesFolder = Root + "/Art/Sprites";
        private const string DataFolder = Root + "/Data";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string UIFolder = Root + "/UI";
        private const string ScenePath = Root + "/Scenes/Game.unity";

        // World layout, in units. The block sprite is 2 x 1 units.
        private const float BaseBlockY = -3f;
        private const float GroundY = BaseBlockY - 0.5f;
        private const float TowerHalfWidth = 1f;
        private const float PlayerSideOffset = 2f;
        private static readonly Color SkyColor = new Color(88 / 255f, 160 / 255f, 226 / 255f); // top of the sky gradient

        [MenuItem("Demolition Buster/Build Project Content", priority = 0)]
        public static void BuildAll()
        {
            // The scene is rebuilt from scratch: give the user a chance to save pending edits first.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (string folder in new[] { SpritesFolder, DataFolder + "/Destroyers", PrefabsFolder, Path.GetDirectoryName(ScenePath) })
            {
                Directory.CreateDirectory(folder);
            }
            AssetDatabase.Refresh();

            // 1. Generate and import assets. Imports can invalidate previously loaded objects, so each phase
            //    reloads what it needs by path instead of passing references along.
            ArtGenerator.Generate(SpritesFolder);
            // Third-party art: reimport so ArtImportRules applies (it may have been imported before the rules existed).
            foreach (string folder in new[] { ArtImportRules.CharactersFolder, ArtImportRules.EffectsFolder, ArtImportRules.UIFolder })
            {
                AssetDatabase.ImportAsset(folder.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            }
            // The stylesheet references generated and third-party images: reimport it now that they exist.
            AssetDatabase.ImportAsset(UIFolder + "/Game.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(UIFolder + "/Game.uxml", ImportAssetOptions.ForceUpdate);

            BuildDataAssets();
            BuildPrefabs(ArtGenerator.Load(SpritesFolder));
            AssetDatabase.SaveAssets();

            // 2. Wire the scene from freshly loaded assets.
            BuildScene();
            ConfigurePlayerSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("[Demolition Buster] Project content built. Open " + ScenePath + " and press Play.");
        }

        // ---- Data -------------------------------------------------------------------------------

        private static void BuildDataAssets()
        {
            // Created once, then left alone so designers keep their tuning.
            EditorUtility.SetDirty(LoadOrCreate<GameConfig>(DataFolder + "/GameConfig.asset"));
            BuildCatalog();
            BuildPanelSettings();
        }

        private static void BuildCatalog()
        {
            // Strike lunges bring each strike to the facade: the tower edge is 1 unit away from where the player stands,
            // the hammer reaches ~1.1 units, bare hands ~0.5 unit (measured on the baked frames).
            DestroyerDefinition worker = BuildDestroyer("worker", "Ouvrier de chantier",
                "Casque vissé, masse en main. Trente ans de démolition au compteur.", 0, "Worker", strikeLunge: 0.05f);

            DestroyerDefinition master = BuildDestroyer("martial_master", "Maître des arts martiaux",
                "Pas besoin d'outil : ses coups de pied sautés ont déjà rasé trois gratte-ciel.", 250, "Master", strikeLunge: 0.5f);

            DestroyerDefinition robot = BuildDestroyer("demolition_bot", "Robot démolisseur",
                "Prototype DB-9000 : pinces hydrauliques, zéro pause café.", 600, "Robot", strikeLunge: 0.5f);

            DestroyerDefinition kaze = BuildDestroyer("kaze", "Kaze",
                "Concentre son énergie dans ses paumes et pulvérise le béton à distance.", 1000, "Kaze", strikeLunge: 0.08f);
            // Launch point = her extended hand on the strike frame, relative to the feet.
            kaze.EditorConfigureEnergyBlast(new Vector2(0.44f, 0.54f), new Color(0.3f, 0.75f, 1f));

            DestroyerCatalog catalog = LoadOrCreate<DestroyerCatalog>(DataFolder + "/DestroyerCatalog.asset");
            catalog.EditorSetDestroyers(new[] { worker, master, robot, kaze });
            EditorUtility.SetDirty(catalog);
        }

        private static DestroyerDefinition BuildDestroyer(string id, string displayName, string description, int price,
            string frames, float strikeLunge)
        {
            var destroyer = LoadOrCreate<DestroyerDefinition>($"{DataFolder}/Destroyers/{id}.asset");
            destroyer.EditorConfigure(id, displayName, description, price,
                Load<Sprite>($"{ArtImportRules.CharactersFolder}{frames}_Idle.png"),
                Load<Sprite>($"{ArtImportRules.CharactersFolder}{frames}_Strike.png"),
                strikeLunge);
            EditorUtility.SetDirty(destroyer);
            return destroyer;
        }

        private static void BuildPanelSettings()
        {
            var panelSettings = LoadOrCreate<PanelSettings>(UIFolder + "/GamePanelSettings.asset");
            panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UIFolder + "/GameTheme.tss");
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1080, 1920);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0f; // Portrait game: width drives the scale so layouts never overflow horizontally.
            EditorUtility.SetDirty(panelSettings);
        }

        // ---- Prefabs ----------------------------------------------------------------------------

        private static void BuildPrefabs(ArtSet art)
        {
            BuildBlockPrefab(art, "Block", sortingOrder: 0, isDebris: false);
            BuildBlockPrefab(art, "Debris", sortingOrder: 20, isDebris: true);
            BuildCoinPopPrefab(art);
            BuildBurstPrefab();
            BuildEnergyBlastPrefab();
        }

        private static void BuildBlockPrefab(ArtSet art, string name, int sortingOrder, bool isDebris)
        {
            var root = new GameObject(name);
            root.AddComponent<SortingGroup>().sortingOrder = sortingOrder;

            // TowerView swaps the facade variant per floor; beams start slightly inside the wall to look embedded.
            SpriteRenderer floor = CreateSprite("Floor", root.transform, art.Floors[0], Vector3.zero, 0);
            SpriteRenderer left = CreateSprite("HazardLeft", root.transform, art.Beam, new Vector3(-TowerHalfWidth + 0.06f, 0.1f), 1);
            left.transform.localScale = new Vector3(-1f, 1f, 1f);
            SpriteRenderer right = CreateSprite("HazardRight", root.transform, art.Beam, new Vector3(TowerHalfWidth - 0.06f, 0.1f), 1);
            SpriteRenderer coin = CreateSprite("Coin", root.transform, art.Coin, new Vector3(0f, -0.05f), 2);

            var view = root.AddComponent<BlockView>();
            SetFields(view, ("_floor", floor), ("_hazardLeft", left), ("_hazardRight", right), ("_coin", coin));
            if (isDebris) root.AddComponent<DebrisView>();

            SavePrefab(root, name);
        }

        private static void BuildCoinPopPrefab(ArtSet art)
        {
            var root = new GameObject("CoinPop");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = art.Coin;
            renderer.sortingOrder = 30;
            root.AddComponent<CoinPopView>();
            SavePrefab(root, "CoinPop");
        }

        /// <summary>Generic effect sprite: HitEffects assigns its sprite, color and motion at runtime.</summary>
        private static void BuildBurstPrefab()
        {
            var root = new GameObject("SpriteBurst");
            root.AddComponent<SpriteRenderer>();
            root.AddComponent<SpriteBurstView>();
            SavePrefab(root, "SpriteBurst");
        }

        private static void BuildEnergyBlastPrefab()
        {
            var root = new GameObject("EnergyBlast");
            SpriteRenderer glow = CreateSprite("Glow", root.transform, Effect("light_03"), Vector3.zero, 26, scale: Vector3.one * 0.9f);
            SpriteRenderer swirl = CreateSprite("Swirl", root.transform, Effect("twirl_01"), Vector3.zero, 27, scale: Vector3.one * 0.75f);
            SpriteRenderer core = CreateSprite("Core", root.transform, Effect("circle_05"), Vector3.zero, 28, scale: Vector3.one * 0.45f);
            SpriteRenderer sparks = CreateSprite("Sparks", root.transform, Effect("spark_01"), Vector3.zero, 29, scale: Vector3.one * 0.7f);
            var view = root.AddComponent<EnergyBlastView>();
            SetFields(view, ("_glow", glow), ("_swirl", swirl), ("_core", core), ("_sparks", sparks));
            SavePrefab(root, "EnergyBlast");
        }

        private static void SavePrefab(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        // ---- Scene ------------------------------------------------------------------------------

        private static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ArtSet art = ArtGenerator.Load(SpritesFolder);
            var config = Load<GameConfig>(DataFolder + "/GameConfig.asset");
            var catalog = Load<DestroyerCatalog>(DataFolder + "/DestroyerCatalog.asset");
            var panelSettings = Load<PanelSettings>(UIFolder + "/GamePanelSettings.asset");
            var blockPrefab = Load<GameObject>(PrefabsFolder + "/Block.prefab").GetComponent<BlockView>();
            var debrisPrefab = Load<GameObject>(PrefabsFolder + "/Debris.prefab").GetComponent<DebrisView>();
            var coinPopPrefab = Load<GameObject>(PrefabsFolder + "/CoinPop.prefab").GetComponent<CoinPopView>();

            CameraRig cameraRig = BuildCamera();
            BuildEnvironment(art);

            var tower = new GameObject("Tower").AddComponent<TowerView>();
            tower.transform.position = new Vector3(0f, BaseBlockY, 0f);
            SetFields(tower, ("_blockPrefab", blockPrefab), ("_debrisPrefab", debrisPrefab), ("_coinPopPrefab", coinPopPrefab));
            // The slab bands already separate floors: a very light alternating tint is enough to read the descent.
            SetColors(tower, "_floorTints", Color.white, new Color(0.94f, 0.94f, 0.97f));
            SetObjects(tower, "_floorSprites", art.Floors);

            PlayerView player = BuildPlayer(catalog.Destroyers[0], art);
            HitEffects hitEffects = BuildHitEffects();

            var uiObject = new GameObject("UI");
            var document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = Load<VisualTreeAsset>(UIFolder + "/Game.uxml");
            var gameUI = uiObject.AddComponent<GameUI>();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var game = new GameObject("Game");
            var input = game.AddComponent<TapInputReader>();
            var controller = game.AddComponent<GameController>();
            SetFields(controller,
                ("_config", config), ("_catalog", catalog), ("_input", input), ("_towerView", tower),
                ("_playerView", player), ("_hitEffects", hitEffects), ("_cameraRig", cameraRig), ("_ui", gameUI));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static CameraRig BuildCamera()
        {
            var rig = new GameObject("CameraRig");
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;
            cameraObject.AddComponent<AudioListener>();

            var cameraRig = rig.AddComponent<CameraRig>();
            SetFields(cameraRig, ("_camera", camera));
            return cameraRig;
        }

        /// <summary>
        /// Construction-site backdrop, back to front: sky, sun and clouds, far skyline, a hazy crane, near skyline,
        /// site hoarding, street. Backdrop layers are 12 units wide: wider than the widest supported view (~7.5 units).
        /// </summary>
        private static void BuildEnvironment(ArtSet art)
        {
            var environment = new GameObject("Environment").transform;
            var sky = new GameObject("Sky").transform;
            sky.SetParent(environment, false);

            // The sky sprite is 1 unit tall: stretch it from the ground line up past the top of the tallest phones.
            float skyWidthScale = 12f * art.Sky.pixelsPerUnit / art.Sky.rect.width;
            CreateSprite("Gradient", sky, art.Sky, new Vector3(0f, GroundY + 7f), -200, scale: new Vector3(skyWidthScale, 14f, 1f));
            CreateSprite("Sun", sky, art.Sun, new Vector3(-1.75f, 3.1f), -190);
            CreateCloud(sky, art.Clouds[0], new Vector3(1.7f, 3.7f), 0.8f, speed: 0.12f, sortingOrder: -185);
            CreateCloud(sky, art.Clouds[1], new Vector3(-2.3f, 1.6f), 0.7f, speed: 0.08f, sortingOrder: -186);
            CreateCloud(sky, art.Clouds[0], new Vector3(2.6f, 0.2f), -0.55f, speed: 0.05f, sortingOrder: -187);

            var city = new GameObject("City").transform;
            city.SetParent(environment, false);
            CreateSprite("SkylineFar", city, art.SkylineFar, new Vector3(0f, GroundY), -170);
            CreateSprite("Crane", city, art.Crane, new Vector3(2.35f, GroundY), -160, scale: new Vector3(-0.75f, 0.75f, 1f));
            CreateSprite("SkylineNear", city, art.SkylineNear, new Vector3(0f, GroundY), -150);

            var street = new GameObject("Street").transform;
            street.SetParent(environment, false);
            CreateSprite("Hoarding", street, art.Hoarding, new Vector3(0f, GroundY), -60);
            CreateSprite("Ground", street, art.Ground, new Vector3(0f, GroundY), -20);
            CreateSprite("RoadFill", street, art.Square, new Vector3(0f, GroundY - 5f), -21, new Color(72 / 255f, 76 / 255f, 88 / 255f), new Vector3(24f, 6f, 1f));
            CreateSprite("TowerShadow", street, art.ContactShadow, new Vector3(0.1f, GroundY), -5, scale: new Vector3(2.8f, 1.2f, 1f));

            // In front of the tower's base, behind the player (order 10).
            CreateSprite("RubbleLeft", street, art.Rubble, new Vector3(-1.25f, GroundY), 5, scale: new Vector3(-1f, 1f, 1f));
            CreateSprite("RubbleRight", street, art.Rubble, new Vector3(1.25f, GroundY), 5);
            CreateSprite("ConeLeft", street, art.Cone, new Vector3(-2.72f, GroundY), 6);
            CreateSprite("ConeRight", street, art.Cone, new Vector3(2.72f, GroundY), 6);
        }

        private static void CreateCloud(Transform parent, Sprite sprite, Vector3 position, float scale, float speed, int sortingOrder)
        {
            // A negative scale mirrors the sprite, so one drawing gives two different-looking clouds.
            SpriteRenderer cloud = CreateSprite("Cloud", parent, sprite, position, sortingOrder, scale: new Vector3(scale, Mathf.Abs(scale), 1f));
            SetFloats(cloud.gameObject.AddComponent<DriftingSprite>(), ("_speed", speed));
        }

        private static PlayerView BuildPlayer(DestroyerDefinition defaultDestroyer, ArtSet art)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(-PlayerSideOffset, GroundY, 0f);
            CreateSprite("Shadow", player.transform, art.ContactShadow, Vector3.zero, 9, scale: new Vector3(1f, 0.8f, 1f));

            var visual = new GameObject("Visual").transform;
            visual.SetParent(player.transform, false);
            SpriteRenderer frame = CreateSprite("Frame", visual, defaultDestroyer.IdleFrame, Vector3.zero, 10);

            var view = player.AddComponent<PlayerView>();
            SetFields(view, ("_visualRoot", visual), ("_frame", frame));
            return view;
        }

        /// <summary>Hit feedback, tuned here once and editable afterwards on the HitEffects component.</summary>
        private static HitEffects BuildHitEffects()
        {
            var effects = new GameObject("HitEffects").AddComponent<HitEffects>();
            SetFields(effects,
                ("_burstPrefab", Load<GameObject>(PrefabsFolder + "/SpriteBurst.prefab").GetComponent<SpriteBurstView>()),
                ("_blastPrefab", Load<GameObject>(PrefabsFolder + "/EnergyBlast.prefab").GetComponent<EnergyBlastView>()));

            var serialized = new SerializedObject(effects);
            SetBurst(serialized, "_impactFlash", Effect("star_07"), new Color(1f, 0.97f, 0.85f), count: 1,
                startScale: 0.25f, endScale: 0.9f, duration: 0.14f, spin: 30f, speed: 0f, spread: 0f, gravityScale: 0f);
            SetBurst(serialized, "_impactDust", Effect("smoke_01"), new Color(0.86f, 0.82f, 0.75f, 0.85f), count: 3,
                startScale: 0.25f, endScale: 0.7f, duration: 0.45f, spin: 60f, speed: 2.2f, spread: 90f, gravityScale: -0.3f);
            SetBurst(serialized, "_impactChips", Effect("dirt_01"), new Color(0.7f, 0.66f, 0.6f), count: 2,
                startScale: 0.18f, endScale: 0.28f, duration: 0.4f, spin: 200f, speed: 4f, spread: 70f, gravityScale: 2f);
            SetBurst(serialized, "_blastFlash", Effect("star_07"), Color.white, count: 1,
                startScale: 0.4f, endScale: 1.4f, duration: 0.2f, spin: 45f, speed: 0f, spread: 0f, gravityScale: 0f);
            SetBurst(serialized, "_blastGlow", Effect("light_03"), new Color(1f, 1f, 1f, 0.9f), count: 1,
                startScale: 0.3f, endScale: 1.6f, duration: 0.3f, spin: 0f, speed: 0f, spread: 0f, gravityScale: 0f);
            SetBurst(serialized, "_blastSparks", Effect("spark_01"), Color.white, count: 3,
                startScale: 0.3f, endScale: 0.6f, duration: 0.22f, spin: 90f, speed: 3f, spread: 140f, gravityScale: 0f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return effects;
        }

        private static void SetBurst(SerializedObject serialized, string field, Sprite sprite, Color color, int count,
            float startScale, float endScale, float duration, float spin, float speed, float spread, float gravityScale)
        {
            SerializedProperty burst = serialized.FindProperty(field)
                ?? throw new InvalidOperationException($"HitEffects has no serialized field '{field}'.");
            burst.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            burst.FindPropertyRelative("color").colorValue = color;
            burst.FindPropertyRelative("count").intValue = count;
            burst.FindPropertyRelative("startScale").floatValue = startScale;
            burst.FindPropertyRelative("endScale").floatValue = endScale;
            burst.FindPropertyRelative("duration").floatValue = duration;
            burst.FindPropertyRelative("spin").floatValue = spin;
            burst.FindPropertyRelative("speed").floatValue = speed;
            burst.FindPropertyRelative("spread").floatValue = spread;
            burst.FindPropertyRelative("gravityScale").floatValue = gravityScale;
        }

        // ---- Settings ---------------------------------------------------------------------------

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "Demolition Buster";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        }

        // ---- Helpers ----------------------------------------------------------------------------

        private static Sprite Effect(string name) => Load<Sprite>($"{ArtImportRules.EffectsFolder}{name}.png");

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new FileNotFoundException($"Expected asset missing: {path}");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Vector3 localPosition, int sortingOrder,
            Color? color = null, Vector3? scale = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale ?? Vector3.one;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color ?? Color.white;
            return renderer;
        }

        /// <summary>Assigns private [SerializeField] references, failing loudly on a renamed field.</summary>
        private static void SetFields(Object target, params (string field, Object value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, Object value) in fields)
            {
                if (value == null) throw new InvalidOperationException($"Null value assigned to {target.GetType().Name}.{field}.");
                SerializedProperty property = serialized.FindProperty(field)
                    ?? throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");
                property.objectReferenceValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjects(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]
                    ?? throw new InvalidOperationException($"Null element {i} assigned to {target.GetType().Name}.{field}.");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloats(Object target, params (string field, float value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, float value) in fields)
            {
                SerializedProperty property = serialized.FindProperty(field)
                    ?? throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");
                property.floatValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColors(Object target, string field, params Color[] colors)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");
            property.arraySize = colors.Length;
            for (int i = 0; i < colors.Length; i++) property.GetArrayElementAtIndex(i).colorValue = colors[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
