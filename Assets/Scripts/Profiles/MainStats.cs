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
            str = Stat.CreateFromTable<Str>(1001, statsTable);
            agi = Stat.CreateFromTable<Agi>(1002, statsTable);
            wis = Stat.CreateFromTable<Wis>(1003, statsTable);
            luck = Stat.CreateFromTable<Luck>(1004, statsTable);
        }
    }
}
