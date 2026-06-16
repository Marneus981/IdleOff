using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;


namespace IdleOff.Profiles
{
    [Serializable]
    public class Stat
    {
        public struct StatValues
        {
            public string name;
            public int statID;
            public int formulaParts;
            public float defaultValue;
            public float minValue;
            public List<int> validModifierIDs;
            public StatValues(string name = "", int statID = 0, float defaultValue = 0f, float minValue = 0f, List<int> validModifierIDs = null, int formulaParts = 2)
            {
                this.name = name;
                this.statID = statID;
                this.defaultValue = defaultValue;
                this.minValue = minValue;
                this.validModifierIDs = validModifierIDs;
                this.formulaParts = formulaParts;
            }
        }
        [SerializeField] private string statName =  "default"; /*Every MainStat, SubStat will have its own different statName set here 
                                                (for example, subclasss critChance will have the "critChance" statName)*/
        [SerializeField] private List<int> validModifierIDs = new List<int>();
        [SerializeField] private int formulaParts;/*Every MainStat, SubStat will have its own different formulaParts set here 
                                    ; these will affect the magnitude and order of applied modifiers inside their own Formula*/
        [SerializeField] private float value;
        [SerializeField, Min(0)] private int statID;
        [SerializeField, Min(0)] private float defaultValue;
        [SerializeField, Min(0)] private float minValue;
        [NonSerialized] private CharacterData owner;

        public static Dictionary<int, StatValues> LoadStatsTable(string statsJsonPath)
        {
            var resolvedPath = ResolveStatsPath(statsJsonPath);
            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, StatValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedStatsTable = (Dictionary<string, StatValues>)serializer.ReadObject(stream);
            var statsTable = new Dictionary<int, StatValues>();

            foreach (var entry in serializedStatsTable)
            {
                if (!int.TryParse(entry.Key, out var statID))
                {
                    throw new FormatException($"Stat table key '{entry.Key}' is not a valid stat ID.");
                }

                var values = entry.Value;
                values.statID = statID;
                if (statsTable.ContainsKey(statID))
                {
                    throw new InvalidOperationException($"Stats table contains duplicate stat ID {statID}.");
                }

                statsTable.Add(statID, values);
            }

            return statsTable;
        }

        public static T CreateFromTable<T>(int statID, Dictionary<int, StatValues> statsTable, CharacterData owner = null) where T : Stat, new()
        {
            if (statID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(statID), statID, "Stat ID must be positive.");
            }

            if (!statsTable.TryGetValue(statID, out var values))
            {
                throw new KeyNotFoundException($"Stat ID '{statID}' was not found in the stats table.");
            }

            var stat = new T();
            stat.Initialize(values);
            stat.SetOwner(owner);
            return stat;
        }

        public void Initialize(StatValues values)
        {
            if (values.statID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.statID, "Stat values must contain a positive stat ID.");
            }

            if (values.formulaParts < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.formulaParts, $"Stat ID {values.statID} must define at least 2 formula parts.");
            }

            statName = values.name;
            statID = values.statID;
            defaultValue = values.defaultValue;
            minValue = values.minValue;
            validModifierIDs = values.validModifierIDs ?? new List<int>();
            formulaParts = values.formulaParts;
            SetValue(defaultValue);
        }

        public void SetOwner(CharacterData owner)
        {
            this.owner = owner;
        }

        public int GetStatID()
        {
            return statID;
        }
        
        public float GetValue()
        {
            return value;
        }
        public void SetValue(float value)
        {
            this.value = Mathf.Max(this.minValue, value);
        }
        public void AddValue(float value)
        {
            SetValue(this.value  + value);
        }

        public virtual float Formula(List<float> formulaValuesList)
        {
            //Standard formula, can be changed inside each subclass
            float flat = formulaValuesList[0];
            float percentile = formulaValuesList[1];
            float result = (defaultValue + flat) * percentile;
            return result;
        }
        public void UpdateStat()
        {
            //Update gets recalculated on every stat change, if this gets heavy I will implement a better more surgical update system
            var formulaValuesList = new List<float>();
            for (int i = 0; i < formulaParts; i++) formulaValuesList.Add(0f);
            formulaValuesList[1] = 1f;

            foreach (var id in validModifierIDs)
            {
               if (id <= 0)
               {
                   throw new ArgumentOutOfRangeException(nameof(validModifierIDs), id, $"Stat ID {statID} contains an invalid modifier ID.");
               }

               if (owner == null)
               {
                   throw new InvalidOperationException($"Stat ID {statID} is not bound to a character and cannot resolve modifier ID {id}.");
               }

               var tuple = owner.GetModifier(id).AppliedIncrease(statID);
               if (tuple.Item1 < 0 || tuple.Item1 >= formulaValuesList.Count)
               {
                   throw new IndexOutOfRangeException($"Modifier ID {id} returned formula index {tuple.Item1} for stat ID {statID}, but this stat has {formulaParts} formula parts.");
               }

               formulaValuesList[tuple.Item1] = formulaValuesList[tuple.Item1] + tuple.Item2;
            }
            SetValue(Formula(formulaValuesList));

        }

        private static string ResolveStatsPath(string statsJsonPath)
        {
            if (Path.IsPathRooted(statsJsonPath))
            {
                return statsJsonPath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), statsJsonPath));
        }
    }
}
