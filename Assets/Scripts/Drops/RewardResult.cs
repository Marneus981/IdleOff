using System.Collections.Generic;

namespace IdleOff.Drops
{
    public sealed class RewardResult
    {
        public int XpReward { get; set; }
        public List<WorldDropPayload> Drops { get; } = new();
    }
}
