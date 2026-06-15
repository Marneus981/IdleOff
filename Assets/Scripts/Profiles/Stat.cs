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
            public int statID;
            public int formulaParts;
            public float defaultValue;
            public float minValue;
            public List<int> validModifierIDs;
            public StatValues(int statID = 0, float defaultValue = 0f, float minValue = 0f, List<int> validModifierIDs = null, int formulaParts = 2)
            {
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
        [Min(0)]private int statID;
        [SerializeField, Min(0)] private float defaultValue;
        [SerializeField, Min(0)] private float minValue;

        public static Dictionary<string, StatValues> LoadStatsTable(string statsJsonPath)
        {
            var resolvedPath = ResolveStatsPath(statsJsonPath);
            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, StatValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            return (Dictionary<string, StatValues>)serializer.ReadObject(stream);
        }

        public static T CreateFromTable<T>(string statName, Dictionary<string, StatValues> statsTable) where T : Stat, new()
        {
            if (!statsTable.TryGetValue(statName, out var values))
            {
                throw new KeyNotFoundException($"Stat '{statName}' was not found in the stats table.");
            }

            var stat = new T();
            stat.Initialize(statName, values);
            return stat;
        }

        public void Initialize(string statName, StatValues values)
        {
            this.statName = statName;
            statID = values.statID;
            defaultValue = values.defaultValue;
            minValue = values.minValue;
            validModifierIDs = values.validModifierIDs ?? new List<int>();
            formulaParts = values.formulaParts;
            SetValue(defaultValue);
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
            foreach (var id in validModifierIDs)
            {
               var tuple = CharacterData.GetModifier(id).AppliedIncrease(statID);////TBA///CharacterData.GetModifier(id) gets modifier by ID
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
