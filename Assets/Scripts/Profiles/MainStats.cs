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
        [NonSerialized] private CharacterData owner;

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
            LoadMainStats(owner);
        }

        public void LoadMainStats(CharacterData owner)
        {
            var statsTable = Stat.LoadStatsTable(statsJsonPath);
            this.owner = owner;
            str = Stat.CreateFromTable<Str>(1001, statsTable, owner);
            agi = Stat.CreateFromTable<Agi>(1002, statsTable, owner);
            wis = Stat.CreateFromTable<Wis>(1003, statsTable, owner);
            luck = Stat.CreateFromTable<Luck>(1004, statsTable, owner);
        }

        public void SetOwner(CharacterData owner)
        {
            this.owner = owner;
            str?.SetOwner(owner);
            agi?.SetOwner(owner);
            wis?.SetOwner(owner);
            luck?.SetOwner(owner);
        }

        public bool UpdateByStatID(int statID)
        {
            if (!IsLoaded())
            {
                LoadMainStats(owner);
            }

            switch (statID)
            {
                case 1001:
                    str.UpdateStat();
                    return true;
                case 1002:
                    agi.UpdateStat();
                    return true;
                case 1003:
                    wis.UpdateStat();
                    return true;
                case 1004:
                    luck.UpdateStat();
                    return true;
                default:
                    return false;
            }
        }
    }
}
