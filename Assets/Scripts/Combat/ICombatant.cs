using System.Collections.Generic;

namespace IdleOff.Combat
{
    public interface ICombatant
    {
        string DisplayName { get; }
        bool IsAlive { get; }
        bool IsPlayerControlled { get; }
        IReadOnlyList<string> Tags { get; }

        float GetStatValueByID(int statID);
        void ReceiveDamage(DamageResult result);
    }
}
