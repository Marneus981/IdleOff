using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class CharacterData
    {
        [SerializeField] private string characterName = "New Character";
        [SerializeField] private CharacterGender gender = CharacterGender.Unspecified;
        [SerializeField] private CharacterClass characterClass = CharacterClass.CreateWanderingSoul();
        [SerializeField] private MainStats mainStats = new();
        [SerializeField] private SecondaryStats secondaryStats = new();
        [SerializeField] private int starSignModifierID = 3002;
        [SerializeField] private StarSignModifier starSignModifier;
        [SerializeField] private FamilyBonusModifier familyBonusModifier;
        [NonSerialized] private CharacterProfile parentProfile;

        public string CharacterName => characterName;
        public CharacterGender Gender => gender;
        public CharacterClass CharacterClass => GetCharacterClass();
        public CharacterProfile ParentProfile => parentProfile;
        public int Level => GetCharacterClass().GetLevelNumber();
        public StarSignModifier StarSignModifier => GetStarSignModifier();
        public FamilyBonusModifier FamilyBonusModifier => GetFamilyModifier();
        public MainStats MainStats
        {
            get
            {
                LoadStats();
                return mainStats;
            }
        }

        public SecondaryStats SecondaryStats
        {
            get
            {
                LoadStats();
                return secondaryStats;
            }
        }

        public Speed Speed
        {
            get
            {
                LoadStats();
                return secondaryStats.Speed;
            }
        }

        public CharacterData()
        {
            LoadStats();
        }

        public CharacterData(string characterName, CharacterGender gender, int level)
            : this(characterName, gender, level, 3002)
        {
        }

        public CharacterData(string characterName, CharacterGender gender, int level, int starSignModifierID)
        {
            this.characterName = characterName;
            this.gender = gender;
            this.starSignModifierID = starSignModifierID;
            GetCharacterClass().SetLevelNumber(level);
            LoadStats();
        }

        public CharacterData(string characterName, CharacterGender gender, CharacterClass characterClass)
            : this(characterName, gender, characterClass, 3002)
        {
        }

        public CharacterData(string characterName, CharacterGender gender, CharacterClass characterClass, int starSignModifierID)
        {
            this.characterName = characterName;
            this.gender = gender;
            this.characterClass = characterClass ?? CharacterClass.CreateWanderingSoul();
            this.starSignModifierID = starSignModifierID;
            LoadStats();
        }

        public void UpdateStats()
        {
            LoadStats(); //may reset saved values for the character
            mainStats.Update();
            secondaryStats.Update();
        }

        internal void SetParentProfile(CharacterProfile profile)
        {
            // Character-owned modifiers can query account-wide character data through this profile reference.
            parentProfile = profile;
            familyBonusModifier?.SetOwner(this);
        }

        private CharacterClass GetCharacterClass()
        {
            characterClass ??= CharacterClass.CreateWanderingSoul();
            characterClass.SetOwner(this);
            return characterClass;
        }

        public Modifier GetModifier(int modifierID)
        {
            if (modifierID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(modifierID), modifierID, "Modifier ID must be positive.");
            }

            // Stat updates still ask CharacterData for modifier IDs; this range split routes those IDs to the right source.
            if (modifierID == 3001)
            {
                return GetFamilyModifier();
            }

            if (modifierID >= 3002 && modifierID < 4000)
            {
                return GetStarSignModifier(modifierID);
            }

            if (modifierID >= 4001)
            {
                GlobalModifierCatalog.EnsureLoaded();
                if (GlobalModifierCatalog.ItemBonuses.TryGetValue(modifierID, out var itemModifier))
                {
                    return itemModifier;
                }

                throw new KeyNotFoundException($"Item modifier ID {modifierID} was not found.");
            }

            return GetCharacterClass().GetModifier(modifierID);
        }        

        public StarSignModifier GetStarSignModifier()
        {
            EnsureCharacterModifiersLoaded();
            return starSignModifier;
        }

        private StarSignModifier GetStarSignModifier(int modifierID)
        {
            var modifier = GetStarSignModifier();
            if (modifier.modifierID != modifierID)
            {
                throw new KeyNotFoundException($"Character is using star sign modifier ID {modifier.modifierID}, not requested ID {modifierID}.");
            }

            return modifier;
        }

        public FamilyBonusModifier GetFamilyModifier()
        {
            EnsureCharacterModifiersLoaded();
            return familyBonusModifier;
        }

        public void UpdateByStatID(int statID)
        {
            if (statID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(statID), statID, "Stat ID must be positive.");
            }

            LoadStats();
            if (mainStats.UpdateByStatID(statID) || secondaryStats.UpdateByStatID(statID))
            {
                return;
            }

            throw new KeyNotFoundException($"Stat ID {statID} was not found in main or secondary stats.");
        }

        private void LoadStats()
        {
            mainStats ??= new MainStats();
            secondaryStats ??= new SecondaryStats();
            GetCharacterClass().SetOwner(this);
            EnsureCharacterModifiersLoaded();
            mainStats.SetOwner(this);
            secondaryStats.SetOwner(this);

            if (!mainStats.IsLoaded())
            {
                mainStats.LoadMainStats(this);
            }

            if (!secondaryStats.IsLoaded())
            {
                secondaryStats.LoadSecondaryStats(this);
            }
        }

        private void EnsureCharacterModifiersLoaded()
        {
            GlobalModifierCatalog.EnsureLoaded();

            if (familyBonusModifier == null)
            {
                familyBonusModifier = new FamilyBonusModifier();
            }

            familyBonusModifier.SetOwner(this);

            if (starSignModifier == null || starSignModifier.modifierID != starSignModifierID)
            {
                starSignModifier = CreateCharacterStarSignModifier(starSignModifierID);
            }

            starSignModifier.SetOwner(this);
        }

        private static StarSignModifier CreateCharacterStarSignModifier(int modifierID)
        {
            if (!GlobalModifierCatalog.StarSignBonuses.TryGetValue(modifierID, out var source))
            {
                throw new KeyNotFoundException($"Star sign modifier ID {modifierID} was not found.");
            }

            // Characters get their own modifier instance so owner-bound updates do not mutate the global catalog object.
            var modifier = new StarSignModifier
            {
                modifierID = source.modifierID,
                name = source.name,
                description = source.description,
                level = source.level,
                maxLevel = source.maxLevel,
                indexIncreaseByStatID = new Dictionary<int, Modifier.IndexIncrease>(source.indexIncreaseByStatID)
            };
            modifier.SetTags(new List<string>(source.GetTags()));
            return modifier;
        }
        public float GetStatValueByID(int statID)
        {
            if (statID < 1005)
            {
                return MainStats.GetStatValueByID(statID);
            }
            return SecondaryStats.GetStatValueByID(statID);
        }
    }
}
