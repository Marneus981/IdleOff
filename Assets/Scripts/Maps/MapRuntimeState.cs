using System;
using System.Collections.Generic;
using IdleOff.Mobs;

namespace IdleOff.Maps
{
    [Serializable]
    public sealed class MapRuntimeState
    {
        public int mapID;
        public int totalMobKills;
        public List<int> defeatedBossMobIDs = new();
        public List<string> unlockedInteractableIDs = new();
        public List<MapPickupState> presentPickups = new();
        public long lastSavedUtcTicks;

        public int MapID => mapID;
        public int TotalMobKills => totalMobKills;

        public void RecordMobKilled(MobTemplate template)
        {
            if (template == null)
            {
                return;
            }

            totalMobKills++;
            if (template.mobType == MobType.Boss && !defeatedBossMobIDs.Contains(template.mobID))
            {
                defeatedBossMobIDs.Add(template.mobID);
            }
        }

        public bool HasBossDefeated(int bossMobID)
        {
            return bossMobID > 0 && defeatedBossMobIDs.Contains(bossMobID);
        }

        public bool IsInteractableUnlocked(string instanceID)
        {
            return !string.IsNullOrWhiteSpace(instanceID) && unlockedInteractableIDs.Contains(instanceID);
        }

        public void MarkInteractableUnlocked(string instanceID)
        {
            if (!string.IsNullOrWhiteSpace(instanceID) && !unlockedInteractableIDs.Contains(instanceID))
            {
                unlockedInteractableIDs.Add(instanceID);
            }
        }
    }
}
