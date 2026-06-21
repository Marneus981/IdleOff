using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Maps
{
    public sealed class CharacterMapStateStore
    {
        [Serializable]
        private sealed class SaveData
        {
            public string characterID;
            public bool hasLastLocation;
            public int lastMapID;
            public float lastPositionX;
            public float lastPositionY;
            public Dictionary<string, MapRuntimeState> maps = new();
        }

        private readonly string savePath;
        private SaveData saveData;

        public CharacterMapStateStore(CharacterData character)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            savePath = GetSavePath(character.CharacterID);
            Load(character.CharacterID);
        }

        public MapRuntimeState GetOrCreateMapState(int mapID)
        {
            var key = mapID.ToString();
            if (!saveData.maps.TryGetValue(key, out var state) || state == null)
            {
                state = new MapRuntimeState { mapID = mapID };
                saveData.maps[key] = state;
            }

            state.mapID = mapID;
            return state;
        }

        public bool TryGetLastLocation(out int mapID, out Vector2 position)
        {
            mapID = default;
            position = default;
            if (saveData == null || !saveData.hasLastLocation || saveData.lastMapID <= 0)
            {
                return false;
            }

            mapID = saveData.lastMapID;
            position = new Vector2(saveData.lastPositionX, saveData.lastPositionY);
            return true;
        }

        public void SetLastLocation(int mapID, Vector2 position)
        {
            if (mapID <= 0)
            {
                return;
            }

            saveData.hasLastLocation = true;
            saveData.lastMapID = mapID;
            saveData.lastPositionX = position.x;
            saveData.lastPositionY = position.y;
        }

        public void Save()
        {
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = File.Create(savePath);
            var serializer = new DataContractJsonSerializer(
                typeof(SaveData),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            serializer.WriteObject(stream, saveData);
        }

        private void Load(string characterID)
        {
            if (!File.Exists(savePath))
            {
                saveData = new SaveData { characterID = characterID };
                return;
            }

            using var stream = File.OpenRead(savePath);
            var serializer = new DataContractJsonSerializer(
                typeof(SaveData),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            saveData = (SaveData)serializer.ReadObject(stream);
            saveData ??= new SaveData();
            saveData.characterID = characterID;
            saveData.maps ??= new Dictionary<string, MapRuntimeState>();
        }

        private static string GetSavePath(string characterID)
        {
            return Path.Combine(Application.persistentDataPath, "IdleOff", "MapStates", characterID + ".json");
        }

        public static void DeleteForCharacter(string characterID)
        {
            if (string.IsNullOrWhiteSpace(characterID))
            {
                return;
            }

            var path = GetSavePath(characterID);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
