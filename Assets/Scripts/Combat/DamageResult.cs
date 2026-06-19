namespace IdleOff.Combat
{
    public readonly struct DamageResult
    {
        public DamageResult(
            ICombatant attacker,
            ICombatant defender,
            bool hit,
            float hitChance,
            float rawDamage,
            float finalDamage,
            int critCount)
        {
            Attacker = attacker;
            Defender = defender;
            Hit = hit;
            HitChance = hitChance;
            RawDamage = rawDamage;
            FinalDamage = finalDamage;
            CritCount = critCount;
        }

        public ICombatant Attacker { get; }
        public ICombatant Defender { get; }
        public bool Hit { get; }
        public float HitChance { get; }
        public float RawDamage { get; }
        public float FinalDamage { get; }
        public int CritCount { get; }
    }
}
