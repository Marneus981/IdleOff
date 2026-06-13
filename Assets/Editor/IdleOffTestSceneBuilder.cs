using System.IO;
using IdleOff.Player;
using IdleOff.Profiles;
using IdleOff.World;
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

        [MenuItem("IdleOff/Create Test Scene")]
        public static void CreateTestScene()
        {
            EnsureObjectLayer(PlatformObjectLayer);
            EnsureObjectLayer(LadderObjectLayer);
            EnsureObjectLayer(PlayerObjectLayer);

            EnsureFolder("Assets/Art");
            EnsureFolder(PlaceholderFolder);
            EnsureFolder("Assets/Profiles");
            EnsureFolder("Assets/Scenes");

            Sprite playerSprite = GetOrCreatePlaceholderSprite("Player_Box", new Color32(79, 159, 255, 255));
            Sprite platformSprite = GetOrCreatePlaceholderSprite("Platform_Box", new Color32(96, 91, 83, 255));
            Sprite ladderSprite = GetOrCreatePlaceholderSprite("Ladder_Box", new Color32(201, 142, 69, 255));

            CharacterProfile profile = GetOrCreateProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Test";

            CreateCamera();
            CreatePlatform("Lower Platform", new Vector2(0f, -2.5f), new Vector2(9f, 0.5f), platformSprite);
            CreatePlatform("Upper Platform", new Vector2(2.75f, 1f), new Vector2(5.5f, 0.5f), platformSprite);
            CreateLadder("Ladder", new Vector2(2.75f, -0.5f), new Vector2(0.55f, 3.5f), ladderSprite);
            CreatePlayer(profile, playerSprite);

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
            profile.TryAddCharacter(new CharacterData("Apprentice", CharacterGender.Unspecified, 1, 5f));
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
        }

        private static void CreatePlatform(string name, Vector2 position, Vector2 size, Sprite sprite)
        {
            GameObject platform = CreateBox(name, position, size, sprite, new Color32(96, 91, 83, 255));
            SetSorting(platform, PlatformsSortingLayer, 0);
            SetLayer(platform, PlatformObjectLayer);
            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            platform.AddComponent<DropThroughPlatform>();
        }

        private static void CreateLadder(string name, Vector2 position, Vector2 size, Sprite sprite)
        {
            GameObject ladder = CreateBox(name, position, size, sprite, new Color32(201, 142, 69, 180));
            SetSorting(ladder, LaddersSortingLayer, 10);
            SetLayer(ladder, LadderObjectLayer);
            BoxCollider2D collider = ladder.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;
            ladder.AddComponent<LadderZone>();
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
            string path = PlaceholderFolder + "/" + name + ".asset";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.name = name + "_Texture";

            sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = name;
            AssetDatabase.CreateAsset(texture, path);
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.ImportAsset(path);
            return sprite;
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
