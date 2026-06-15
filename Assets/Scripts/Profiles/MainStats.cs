using System;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class MainStats
    {
        [SerializeField] private string statsJsonPath = "Assets/Tables/Stats.json";
        [SerializeField] private Str str;
        [SerializeField] private Agi agi;
        [SerializeField] private Wis wis;
        [SerializeField] private Luck luck;

        public bool IsLoaded()
        {
            return str != null
                && agi != null
                && wis != null
                && luck != null;
        }

        public void Update()
        {
            if (!IsLoaded())
            {
                LoadMainStats();
            }

            str.UpdateStat();
            agi.UpdateStat();
            wis.UpdateStat();
            luck.UpdateStat();
        }
        public void LoadMainStats()
        {
            var statsTable = Stat.LoadStatsTable(statsJsonPath);
            str = Stat.CreateFromTable<Str>("str", statsTable);
            agi = Stat.CreateFromTable<Agi>("agi", statsTable);
            wis = Stat.CreateFromTable<Wis>("wis", statsTable);
            luck = Stat.CreateFromTable<Luck>("luck", statsTable);
        }
    }
}
