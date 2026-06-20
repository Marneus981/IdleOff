using System.IO;
using IdleOff.Actions;
using IdleOff.Combat;
using IdleOff.Maps;
using IdleOff.Player;
using IdleOff.Profiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleOff.Editor
{
    public static class IdleOffTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Test.unity";
        private const string ProfilePath = "Assets/Profiles/TestCharacterProfile.asset";
        private const string PlaceholderFolder = "Assets/Art/Placeholders";
        private const string PlatformsSortingLayer = "Platforms";
        private const string LaddersSortingLayer = "Ladders";
        private const string CharactersSortingLayer = "Characters";
        private const string PlatformObjectLayer = "Platform";
        private const string LadderObjectLayer = "Ladder";
        private const string PlayerObjectLayer = "Player";
        private const string MobObjectLayer = "Mob";

        [MenuItem("IdleOff/Create Test Scene")]
        public static void CreateTestScene()
        {
            EnsureObjectLayer(PlatformObjectLayer);
            EnsureObjectLayer(LadderObjectLayer);
            EnsureObjectLayer(PlayerObjectLayer);
            EnsureObjectLayer(MobObjectLayer);

            EnsureFolder("Assets/Art");
            EnsureFolder(PlaceholderFolder);
            EnsureFolder("Assets/Profiles");
            EnsureFolder("Assets/Scenes");

            Sprite playerSprite = GetOrCreatePlaceholderSprite("Player_Box", new Color32(79, 159, 255, 255));
            Sprite mobSprite = GetOrCreatePlaceholderSprite("Mob_Box", new Color32(232, 90, 86, 255));
            Sprite platformSprite = GetOrCreatePlaceholderSprite("Platform_Box", new Color32(96, 91, 83, 255));
            Sprite ladderSprite = GetOrCreatePlaceholderSprite("Ladder_Box", new Color32(201, 142, 69, 255));
            Sprite portalClosedSprite = GetOrCreatePlaceholderSprite("Portal_Closed", new Color32(130, 130, 130, 255));
            Sprite portalOpenSprite = GetOrCreatePlaceholderSprite("Portal_Open", new Color32(73, 199, 111, 255));

            CharacterProfile profile = GetOrCreateProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Test";

            CreateCamera();
            CreatePlayer(profile, playerSprite);
            CreateWorldDropSpawner();
            CreateMapManager(profile, platformSprite, ladderSprite, mobSprite, portalClosedSprite, portalOpenSprite);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            UnityEngine.Debug.Log("Created IdleOff test scene at " + ScenePath);
        }

        private static CharacterProfile GetOrCreateProfile()
        {
            CharacterProfile profile = AssetDatabase.LoadAssetAtPath<CharacterProfile>(ProfilePath);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<CharacterProfile>();
            profile.TryAddCharacter(new CharacterData("Apprentice", CharacterGender.Unspecified, 1));
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.backgroundColor = new Color32(35, 41, 49, 255);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(1f, -0.5f, -10f);
        }

        private static void CreatePlayer(CharacterProfile profile, Sprite sprite)
        {
            GameObject player = CreateBox("Player", new Vector2(-3f, -1.65f), new Vector2(0.55f, 1.1f), sprite, new Color32(79, 159, 255, 255));
            SetSorting(player, CharactersSortingLayer, 20);
            SetLayer(player, PlayerObjectLayer);
            player.AddComponent<BoxCollider2D>();

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;

            PlayerMovement2D movement = player.AddComponent<PlayerMovement2D>();
            movement.SetProfile(profile);

            PlayerCombatant combatant = player.AddComponent<PlayerCombatant>();
            combatant.SetProfile(profile);

            player.AddComponent<ActionController>();

            PlayerActionDriver actionDriver = player.AddComponent<PlayerActionDriver>();
            actionDriver.SetProfile(profile);

            player.AddComponent<PlayerInteractDriver>();
        }

        private static void CreateWorldDropSpawner()
        {
            GameObject spawner = new GameObject("World Drop Spawner");
            spawner.AddComponent<IdleOff.Drops.WorldDropSpawner>();
        }

        private static void CreateMapManager(
            CharacterProfile profile,
            Sprite platformSprite,
            Sprite ladderSprite,
            Sprite mobSprite,
            Sprite portalClosedSprite,
            Sprite portalOpenSprite)
        {
            GameObject managerObject = new GameObject("Map Manager");
            MapManager mapManager = managerObject.AddComponent<MapManager>();
            mapManager.Configure(profile, 1001, platformSprite, ladderSprite, mobSprite, portalClosedSprite, portalOpenSprite);
        }

        private static GameObject CreateBox(string name, Vector2 position, Vector2 size, Sprite sprite, Color color)
        {
            GameObject box = new GameObject(name);
            box.transform.position = position;
            box.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = box.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return box;
        }

        private static void SetSorting(GameObject target, string sortingLayerName, int orderInLayer)
        {
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            if (SortingLayerExists(sortingLayerName))
            {
                renderer.sortingLayerName = sortingLayerName;
            }

            renderer.sortingOrder = orderInLayer;
        }

        private static void SetLayer(GameObject target, string layerName)
        {
            int layer = GetObjectLayerIndex(layerName);
            if (layer >= 0)
            {
                target.layer = layer;
            }
        }

        private static Sprite GetOrCreatePlaceholderSprite(string name, Color color)
        {
            string path = PlaceholderFolder + "/" + name + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.name = name + "_Texture";

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 1f;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void EnsureObjectLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                return;
            }

            SerializedObject tagManager = GetTagManager();
            if (tagManager == null)
            {
                UnityEngine.Debug.LogWarning("Could not load TagManager.asset to create object layer " + layerName + ".");
                return;
            }

            SerializedProperty userLayers = GetUserLayers(tagManager);
            if (userLayers == null)
            {
                UnityEngine.Debug.LogWarning("Could not find user layers in TagManager.asset.");
                return;
            }

            for (int i = 8; i < userLayers.arraySize; i++)
            {
                SerializedProperty layer = userLayers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }

            UnityEngine.Debug.LogWarning("No empty Unity object layer slot is available for " + layerName + ".");
        }

        private static int GetObjectLayerIndex(string layerName)
        {
            int layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex >= 0)
            {
                return layerIndex;
            }

            SerializedObject tagManager = GetTagManager();
            SerializedProperty userLayers = tagManager == null ? null : GetUserLayers(tagManager);
            if (userLayers == null)
            {
                return -1;
            }

            for (int i = 8; i < userLayers.arraySize; i++)
            {
                if (userLayers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool SortingLayerExists(string sortingLayerName)
        {
            SortingLayer[] sortingLayers = SortingLayer.layers;
            for (int i = 0; i < sortingLayers.Length; i++)
            {
                if (sortingLayers[i].name == sortingLayerName)
                {
                    return true;
                }
            }

            return false;
        }

        private static SerializedObject GetTagManager()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            return assets == null || assets.Length == 0 ? null : new SerializedObject(assets[0]);
        }

        private static SerializedProperty GetUserLayers(SerializedObject tagManager)
        {
            return tagManager.FindProperty("layers") ?? tagManager.FindProperty("m_UserLayers");
        }
    }
}
