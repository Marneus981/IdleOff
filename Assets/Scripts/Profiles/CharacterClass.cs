using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class CharacterClass
    {
        [Serializable]
#pragma warning disable CS0649
        private struct ClassModifierValues
        {
            public string name;
            public string description;
            public int level;
            public int maxLevel;
            public Dictionary<string, Modifier.IndexIncrease> indexIncreaseByStatID;
        }
#pragma warning restore CS0649

        #region Variables
        [SerializeField] private string className = "Wandering Soul";
        [SerializeField] private string classModifiersPath = "Assets/Tables/ModifiersWanderingSoul.json";
        [SerializeField, Min(1)] private int levelNumber = 1;
        [SerializeField, Min(0f)] private float currentXP;
        [SerializeField, Min(0f)] private float maxXP = 100f;
        [SerializeField, Min(0)] private int BaseTalentPoints = 3;
        [SerializeField, Min(0)] private int ClassTalentPoints = 3;
        [SerializeField] private List<ClassModifier> ClassModifiers = new();
        private Dictionary<int, ClassModifier> classModifiersByID = new();
        [SerializeField] private Dictionary<int,Active> classActives = new();
        [NonSerialized] private CharacterData owner;
        ///public IReadOnlyList<Modifier> PassiveAbilities => ClassModifiers;
        ///public IReadOnlyList<Active> ActiveAbilities => classActives;
        #endregion
        #region Constructors and Class Creators
        public CharacterClass()
        {
        }
        private CharacterClass(string className, string path)
        {
            this.className = className;
            this.classModifiersPath = path;
            LoadClassModifiers();
        }

        public static CharacterClass CreateWanderingSoul()
        {
            return new CharacterClass("Wandering Soul", "Assets/Tables/ModifiersWanderingSoul.json");
        }

        public static CharacterClass CreateErrantKnight()
        {
            return new CharacterClass("Errant Knight","Assets/Tables/ModifiersErrantKnight.json");
        }

        public static CharacterClass CreateWildHunter()
        {
            return new CharacterClass("Wild Hunter", "Assets/Tables/ModifiersWildHunter.json");
        }

        public static CharacterClass CreateVoidWhisperer()
        {
            return new CharacterClass("Void Whisperer","Assets/Tables/ModifiersVoidWhisperer.json");
        }
        #endregion
        #region Set/Get
        public string GetClassName()
        {
            return className;
        }

        public void SetClassName(string value)
        {
            className = string.IsNullOrWhiteSpace(value) ? "Wandering Soul" : value;
        }

        public int GetLevelNumber()
        {
            return levelNumber;
        }

        public void SetLevelNumber(int value)
        {
            levelNumber = Mathf.Max(1, value);
        }

        public float GetCurrentXP()
        {
            return currentXP;
        }

        public void SetCurrentXP(float value)
        {
            currentXP = Mathf.Max(0f, value);
        }

        public void AddCurrentXP(float value)
        {
            SetCurrentXP(currentXP + value);
        }

        public float GetMaxXP()
        {
            return maxXP;
        }

        public void SetMaxXP(float value)
        {
            maxXP = Mathf.Max(0f, value);
        }

        public int GetBaseTalentPoints()
        {
            return BaseTalentPoints;
        }

        public void SetBaseTalentPoints(int value)
        {
            BaseTalentPoints = Mathf.Max(0, value);
        }

        public int GetClassTalentPoints()
        {
            return ClassTalentPoints;
        }

        public void SetClassTalentPoints(int value)
        {
            ClassTalentPoints = Mathf.Max(0, value);
        }

        public IReadOnlyList<ClassModifier> GetClassModifiers()
        {
            EnsureClassModifiersLoaded();
            return ClassModifiers;
        }
        #endregion
        public void LevelUp(CharacterData characterData)
        {
            if (currentXP < maxXP)
            {
                return;
            }

            var previousMaxXP = maxXP;
            var newLevel = levelNumber + 1;
            var overflowXP = currentXP - previousMaxXP;

            levelNumber = newLevel;
            maxXP += newLevel * 100f;
            currentXP = Mathf.Max(0f, overflowXP);
            BaseTalentPoints = BaseTalentPoints + 3;
            ClassTalentPoints= ClassTalentPoints + 3;
        }
        public void ChangeClass(string targetClass)
        {
            switch (NormalizeClassName(targetClass))
            {
                case "wanderingsoul":
                    ChangeToWanderingSoul();
                    break;
                case "errantknight":
                    ChangeToErrantKnight();
                    break;
                case "wildhunter":
                    ChangeToWildHunter();
                    break;
                case "voidwhisperer":
                    ChangeToVoidWhisperer();
                    break;
                default:
                    throw new ArgumentException($"Unknown class '{targetClass}'.", nameof(targetClass));
            }
        }

        private void ChangeToWanderingSoul()
        {
            ChangeToClass("Wandering Soul", "Assets/Tables/ModifiersWanderingSoul.json");
        }

        private void ChangeToErrantKnight()
        {
            ChangeToClass("Errant Knight", "Assets/Tables/ModifiersErrantKnight.json");
        }

        private void ChangeToWildHunter()
        {
            ChangeToClass("Wild Hunter", "Assets/Tables/ModifiersWildHunter.json");
        }

        private void ChangeToVoidWhisperer()
        {
            ChangeToClass("Void Whisperer", "Assets/Tables/ModifiersVoidWhisperer.json");
        }

        private void ChangeToClass(string targetClassName, string targetClassModifiersPath)
        {
            if (className == targetClassName && classModifiersPath == targetClassModifiersPath)
            {
                return;
            }

            EnsureClassModifiersLoaded();
            var previousModifiers = new Dictionary<int, ClassModifier>(classModifiersByID);
            className = targetClassName;
            classModifiersPath = targetClassModifiersPath;
            LoadClassModifiers();

            var refundedClassTalentPoints = 0;
            foreach (var previousModifier in previousModifiers)
            {
                if (classModifiersByID.TryGetValue(previousModifier.Key, out var loadedModifier))
                {
                    loadedModifier.level = previousModifier.Value.level;
                    continue;
                }

                refundedClassTalentPoints += Mathf.Max(0, previousModifier.Value.level);
            }

            ClassTalentPoints += refundedClassTalentPoints;
        }

        public void SetOwner(CharacterData owner)
        {
            this.owner = owner;
            foreach (var modifier in ClassModifiers)
            {
                modifier?.SetOwner(owner);
            }
        }

        public ClassModifier GetModifier(int modifierID)
        {
            if (modifierID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(modifierID), modifierID, "Modifier ID must be positive.");
            }

            EnsureClassModifiersLoaded();
            if (!classModifiersByID.TryGetValue(modifierID, out var modifier))
            {
                throw new KeyNotFoundException($"Modifier ID {modifierID} was not found in class '{className}'.");
            }

            return modifier;
        }

        private void LoadClassModifiers()
        {
            var resolvedPath = ResolveClassModifiersPath(classModifiersPath);
            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, ClassModifierValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedModifiers = (Dictionary<string, ClassModifierValues>)serializer.ReadObject(stream);

            ClassModifiers = new List<ClassModifier>();
            classModifiersByID = new Dictionary<int, ClassModifier>();
            foreach (var entry in serializedModifiers)
            {
                if (!int.TryParse(entry.Key, out var modifierID))
                {
                    throw new FormatException($"Class modifier table key '{entry.Key}' is not a valid modifier ID.");
                }

                var modifier = CreateClassModifier(modifierID, entry.Value);
                modifier.SetOwner(owner);
                ClassModifiers.Add(modifier);
                if (classModifiersByID.ContainsKey(modifierID))
                {
                    throw new InvalidOperationException($"Class modifier table '{classModifiersPath}' contains duplicate modifier ID {modifierID}.");
                }

                classModifiersByID.Add(modifierID, modifier);
            }
        }

        private void LoadActives()
        {
            ////TO BE IMPLEMENTED
        }

        private static string NormalizeClassName(string className)
        {
            return string.IsNullOrWhiteSpace(className)
                ? string.Empty
                : className.Replace(" ", string.Empty).ToLowerInvariant();
        }

        private static ClassModifier CreateClassModifier(int modifierID, ClassModifierValues values)
        {
            if (modifierID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(modifierID), modifierID, "Modifier ID must be positive.");
            }

            if (values.maxLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.maxLevel, $"Modifier ID {modifierID} has an invalid max level.");
            }

            if (values.level < 0 || values.level > values.maxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.level, $"Modifier ID {modifierID} has a level outside 0..maxLevel.");
            }

            var modifier = new ClassModifier
            {
                modifierID = modifierID,
                name = values.name,
                description = values.description,
                level = values.level,
                maxLevel = values.maxLevel,
                indexIncreaseByStatID = new Dictionary<int, Modifier.IndexIncrease>()
            };

            if (values.indexIncreaseByStatID == null)
            {
                return modifier;
            }

            foreach (var entry in values.indexIncreaseByStatID)
            {
                if (!int.TryParse(entry.Key, out var statID))
                {
                    throw new FormatException($"Modifier ID {modifierID} has stat key '{entry.Key}', which is not a valid stat ID.");
                }

                modifier.indexIncreaseByStatID.Add(statID, entry.Value);
            }

            return modifier;
        }

        private void EnsureClassModifiersLoaded()
        {
            if (ClassModifiers == null || ClassModifiers.Count == 0)
            {
                LoadClassModifiers();
                return;
            }

            RebuildClassModifierLookup();
        }

        private void RebuildClassModifierLookup()
        {
            classModifiersByID = new Dictionary<int, ClassModifier>();
            foreach (var modifier in ClassModifiers)
            {
                if (modifier == null)
                {
                    throw new InvalidOperationException($"Class '{className}' contains a null class modifier.");
                }

                if (modifier.modifierID <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(ClassModifiers), modifier.modifierID, $"Class '{className}' contains an invalid modifier ID.");
                }

                if (classModifiersByID.ContainsKey(modifier.modifierID))
                {
                    throw new InvalidOperationException($"Class '{className}' contains duplicate modifier ID {modifier.modifierID}.");
                }

                modifier.SetOwner(owner);
                classModifiersByID.Add(modifier.modifierID, modifier);
            }
        }

        private static string ResolveClassModifiersPath(string modifiersPath)
        {
            if (Path.IsPathRooted(modifiersPath))
            {
                return modifiersPath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), modifiersPath));
        }
    }

}
