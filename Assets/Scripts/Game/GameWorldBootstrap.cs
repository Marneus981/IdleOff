using IdleOff.Actions;
using IdleOff.CameraTools;
using IdleOff.Combat;
using IdleOff.Drops;
using IdleOff.Maps;
using IdleOff.Player;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Game
{
    public static class GameWorldBootstrap
    {
        public const int HubMapID = 1000;
        private const float GameplayCameraOrthographicSize = 5.75f;
        private static readonly Vector2 GameplayCameraOffset = new(0f, 2f);

        public static void EnterHub(CharacterProfile profile)
        {
            if (profile == null || profile.ActiveCharacter == null)
            {
                throw new System.InvalidOperationException("A profile with an active character is required before entering the hub map.");
            }

            var player = EnsurePlayer(profile);
            EnsureCamera(player.transform);
            GameplayHud.EnsureExists();
            EnsureWorldDropSpawner();

            var mapManager = EnsureMapManager();
            mapManager.Configure(
                profile,
                MapManager.HubMapID,
                CreateRuntimeSprite(new Color32(96, 91, 83, 255)),
                CreateRuntimeSprite(new Color32(201, 142, 69, 255)),
                CreateRuntimeSprite(new Color32(174, 116, 216, 255)),
                CreateRuntimeSprite(new Color32(130, 130, 130, 255)),
                CreateRuntimeSprite(new Color32(70, 210, 120, 255)));
            if (mapManager.TryGetLastSavedLocation(out var lastMapID, out var lastPosition))
            {
                mapManager.LoadMap(lastMapID, lastPosition);
                return;
            }

            mapManager.LoadMap(MapManager.HubMapID);
        }

        private static PlayerCombatant EnsurePlayer(CharacterProfile profile)
        {
            var player = Object.FindFirstObjectByType<PlayerCombatant>();
            if (player == null)
            {
                var playerObject = new GameObject("Player");
                playerObject.transform.position = Vector3.zero;
                var renderer = playerObject.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateRuntimeSprite(new Color32(82, 166, 255, 255));
                renderer.sortingOrder = 10;

                var collider = playerObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.75f, 1.5f);

                var body = playerObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 3f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;

                player = playerObject.AddComponent<PlayerCombatant>();
            }

            var movement = EnsureComponent<PlayerMovement2D>(player.gameObject);
            var actionController = EnsureComponent<ActionController>(player.gameObject);
            var actionDriver = EnsureComponent<PlayerActionDriver>(player.gameObject);
            var interactDriver = EnsureComponent<PlayerInteractDriver>(player.gameObject);
            _ = actionController;
            _ = interactDriver;

            player.SetProfile(profile);
            movement.SetProfile(profile);
            actionDriver.SetProfile(profile);
            return player;
        }

        private static void EnsureCamera(Transform playerTarget)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = GameplayCameraOrthographicSize;
            var follow = EnsureComponent<CameraFollow2D>(camera.gameObject);
            follow.SetTarget(playerTarget);
            follow.SetOffset(GameplayCameraOffset);
        }

        private static void EnsureWorldDropSpawner()
        {
            var spawner = WorldDropSpawner.Instance;
            if (spawner == null)
            {
                spawner = Object.FindFirstObjectByType<WorldDropSpawner>();
            }

            if (spawner == null)
            {
                spawner = new GameObject("World Drop Spawner").AddComponent<WorldDropSpawner>();
            }

            spawner.Configure(
                CreateRuntimeSprite(new Color32(255, 238, 128, 255)),
                CreateRuntimeSprite(new Color32(255, 196, 74, 255)));
        }

        private static MapManager EnsureMapManager()
        {
            var mapManager = MapManager.Instance;
            if (mapManager == null)
            {
                mapManager = Object.FindFirstObjectByType<MapManager>();
            }

            return mapManager != null
                ? mapManager
                : new GameObject("Map Manager").AddComponent<MapManager>();
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent<T>(out var component)
                ? component
                : gameObject.AddComponent<T>();
        }

        private static Sprite CreateRuntimeSprite(Color32 color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
