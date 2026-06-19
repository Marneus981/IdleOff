using IdleOff.Mobs;

namespace IdleOff.Combat
{
    public interface IMobCombatant : ICombatant
    {
        int MobID { get; }
        MobType MobType { get; }
        float AC { get; }
        MobRuntimeData RuntimeData { get; }
    }
}
