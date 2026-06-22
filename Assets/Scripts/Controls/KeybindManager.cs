using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using IdleOff.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleOff.Controls
{
    public static class KeybindManager
    {
        [Serializable]
#pragma warning disable CS0649
        private struct KeybindValues
        {
            public List<string> keys;
        }
#pragma warning restore CS0649

        private const string DefaultKeybindsPath = "Assets/Tables/Keybinds.json";

        public static Dictionary<string, Keybind> Keybinds { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadBeforeScene()
        {
            LoadKeybinds();
        }

        public static void EnsureLoaded()
        {
            if (Keybinds == null || Keybinds.Count == 0)
            {
                LoadKeybinds();
            }
        }

        public static void LoadKeybinds(string keybindsPath = DefaultKeybindsPath)
        {
            var resolvedPath = ResolveKeybindsPath(keybindsPath);
            if (!File.Exists(resolvedPath))
            {
                Keybinds = CreateDefaultKeybinds();
                SaveKeybinds(keybindsPath);
                return;
            }

            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, KeybindValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedKeybinds = (Dictionary<string, KeybindValues>)serializer.ReadObject(stream);

            Keybinds = new Dictionary<string, Keybind>();
            foreach (var entry in serializedKeybinds)
            {
                Keybinds[entry.Key] = new Keybind
                {
                    actionName = entry.Key,
                    keys = entry.Value.keys ?? new List<string>()
                };
            }
        }

        public static void SaveKeybinds(string keybindsPath = DefaultKeybindsPath)
        {
            EnsureLoaded();
            var serializedKeybinds = new Dictionary<string, KeybindValues>();
            foreach (var entry in Keybinds)
            {
                serializedKeybinds[entry.Key] = new KeybindValues { keys = entry.Value.keys ?? new List<string>() };
            }

            var resolvedPath = ResolveKeybindsPath(keybindsPath);
            var directory = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = File.Create(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, KeybindValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            serializer.WriteObject(stream, serializedKeybinds);
        }

        public static bool IsPressed(string actionName)
        {
            EnsureLoaded();
            return Keybinds.TryGetValue(actionName, out var keybind) && keybind.IsPressed();
        }

        public static bool WasPressedThisFrame(string actionName)
        {
            EnsureLoaded();
            return Keybinds.TryGetValue(actionName, out var keybind) && keybind.WasPressedThisFrame();
        }

        public static void SetKeys(string actionName, IEnumerable<Key> keys)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                throw new ArgumentException("Action name cannot be empty.", nameof(actionName));
            }

            EnsureLoaded();
            var keyNames = new List<string>();
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    keyNames.Add(key.ToString());
                }
            }

            Keybinds[actionName] = new Keybind { actionName = actionName, keys = keyNames };
        }

        public static void AddKey(string actionName, Key key)
        {
            EnsureLoaded();
            if (!Keybinds.TryGetValue(actionName, out var keybind))
            {
                keybind = new Keybind { actionName = actionName };
                Keybinds.Add(actionName, keybind);
            }

            var keyName = key.ToString();
            if (!keybind.keys.Contains(keyName))
            {
                keybind.keys.Add(keyName);
            }
        }

        private static Dictionary<string, Keybind> CreateDefaultKeybinds()
        {
            return new Dictionary<string, Keybind>
            {
                [KeybindActions.MoveLeft] = Create(KeybindActions.MoveLeft, Key.A, Key.LeftArrow),
                [KeybindActions.MoveRight] = Create(KeybindActions.MoveRight, Key.D, Key.RightArrow),
                [KeybindActions.MoveUp] = Create(KeybindActions.MoveUp, Key.W, Key.UpArrow),
                [KeybindActions.MoveDown] = Create(KeybindActions.MoveDown, Key.S, Key.DownArrow),
                [KeybindActions.Jump] = Create(KeybindActions.Jump, Key.Space),
                [KeybindActions.AttackPrimary] = Create(KeybindActions.AttackPrimary, Key.LeftCtrl),
                [KeybindActions.Interact] = Create(KeybindActions.Interact, Key.E)
            };
        }

        private static Keybind Create(string actionName, params Key[] keys)
        {
            var keyNames = new List<string>();
            foreach (var key in keys)
            {
                keyNames.Add(key.ToString());
            }

            return new Keybind { actionName = actionName, keys = keyNames };
        }

        private static string ResolveKeybindsPath(string keybindsPath)
        {
            return TablePathResolver.Resolve(keybindsPath);
        }
    }
}
