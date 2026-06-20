using System;
using System.Collections.Generic;
using System.IO;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Game
{
    public sealed class ProfileManager
    {
        [Serializable]
        private sealed class ProfileSaveData
        {
            public string profileID;
            public string profileName;
            public int activeCharacterIndex;
            public List<CharacterSaveData> characters = new();
        }

        [Serializable]
        private sealed class CharacterSaveData
        {
            public string characterName;
            public CharacterGender gender;
            public string className;
            public int level = 1;
            public float currentXP;
            public int starSignModifierID = 3002;
            public int startingHatItemID = 5001;
        }

        private const string ProfilesFolderName = "Profiles";
        private readonly List<ProfileRecord> profiles = new();

        public IReadOnlyList<ProfileRecord> Profiles => profiles;

        public void LoadProfiles()
        {
            profiles.Clear();
            Directory.CreateDirectory(GetProfilesDirectory());
            foreach (var file in Directory.GetFiles(GetProfilesDirectory(), "*.json"))
            {
                var json = File.ReadAllText(file);
                var saveData = JsonUtility.FromJson<ProfileSaveData>(json);
                if (saveData == null)
                {
                    continue;
                }

                profiles.Add(CreateRecord(saveData));
            }
        }

        public ProfileRecord CreateProfile(string profileName)
        {
            var saveData = new ProfileSaveData
            {
                profileID = Guid.NewGuid().ToString("N"),
                profileName = string.IsNullOrWhiteSpace(profileName) ? "New Profile" : profileName,
                activeCharacterIndex = 0,
                characters = new List<CharacterSaveData>()
            };
            var record = CreateRecord(saveData);
            profiles.Add(record);
            SaveProfile(record);
            return record;
        }

        public void SaveProfile(ProfileRecord record)
        {
            if (record == null)
            {
                return;
            }

            Directory.CreateDirectory(GetProfilesDirectory());
            var saveData = new ProfileSaveData
            {
                profileID = record.ProfileID,
                profileName = record.ProfileName,
                activeCharacterIndex = record.Profile.ActiveCharacterIndex,
                characters = CreateCharacterSaveData(record.Profile.Characters)
            };
            File.WriteAllText(GetProfilePath(record.ProfileID), JsonUtility.ToJson(saveData, true));
        }

        public void DeleteProfile(ProfileRecord record)
        {
            if (record == null)
            {
                return;
            }

            profiles.Remove(record);
            var path = GetProfilePath(record.ProfileID);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static ProfileRecord CreateRecord(ProfileSaveData saveData)
        {
            var profile = ScriptableObject.CreateInstance<CharacterProfile>();
            if (saveData.characters != null)
            {
                foreach (var characterSave in saveData.characters)
                {
                    profile.TryAddCharacter(CreateCharacter(characterSave));
                }
            }

            profile.SetActiveCharacterIndex(saveData.activeCharacterIndex);
            return new ProfileRecord(
                string.IsNullOrWhiteSpace(saveData.profileID) ? Guid.NewGuid().ToString("N") : saveData.profileID,
                saveData.profileName,
                profile);
        }

        private static List<CharacterSaveData> CreateCharacterSaveData(IReadOnlyList<CharacterData> characters)
        {
            var saveData = new List<CharacterSaveData>();
            if (characters == null)
            {
                return saveData;
            }

            foreach (var character in characters)
            {
                if (character == null)
                {
                    continue;
                }

                saveData.Add(new CharacterSaveData
                {
                    characterName = character.CharacterName,
                    gender = character.Gender,
                    className = character.CharacterClass.GetClassName(),
                    level = character.Level,
                    currentXP = character.CharacterClass.GetCurrentXP(),
                    starSignModifierID = character.StarSignModifier?.modifierID ?? 3002,
                    startingHatItemID = GetEquippedHatID(character)
                });
            }

            return saveData;
        }

        private static CharacterData CreateCharacter(CharacterSaveData saveData)
        {
            var characterClass = CreateClass(saveData?.className);
            var character = new CharacterData(
                string.IsNullOrWhiteSpace(saveData?.characterName) ? "Wandering Soul" : saveData.characterName,
                saveData?.gender ?? CharacterGender.Unspecified,
                characterClass,
                saveData?.starSignModifierID > 0 ? saveData.starSignModifierID : 3002,
                saveData?.startingHatItemID >= 5001 && saveData.startingHatItemID <= 5004 ? saveData.startingHatItemID : 5001);
            character.CharacterClass.SetLevelNumber(Mathf.Max(1, saveData?.level ?? 1));
            character.CharacterClass.SetCurrentXP(Mathf.Max(0f, saveData?.currentXP ?? 0f));
            character.UpdateStats();
            return character;
        }

        private static CharacterClass CreateClass(string className)
        {
            return CharacterClass.NormalizeClassName(className) switch
            {
                "errantknight" => CharacterClass.CreateErrantKnight(),
                "wildhunter" => CharacterClass.CreateWildHunter(),
                "voidwhisperer" => CharacterClass.CreateVoidWhisperer(),
                _ => CharacterClass.CreateWanderingSoul()
            };
        }

        private static int GetEquippedHatID(CharacterData character)
        {
            GlobalItemCatalog.EnsureLoaded();
            foreach (var item in GlobalItemCatalog.Items.Values)
            {
                if (item.HasTag("hat") && character.Equipment.GetItemQuantity(item.itemID) > 0)
                {
                    return item.itemID;
                }
            }

            return 5001;
        }

        private static string GetProfilesDirectory()
        {
            return Path.Combine(Application.persistentDataPath, "IdleOff", ProfilesFolderName);
        }

        private static string GetProfilePath(string profileID)
        {
            return Path.Combine(GetProfilesDirectory(), profileID + ".json");
        }
    }
}
