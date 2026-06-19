using UnityEngine;

namespace IdleOff.Combat
{
    public sealed class UnityRandomSource : IRandomSource
    {
        public static readonly UnityRandomSource Shared = new();

        public float Value => Random.value;

        public float Range(float minInclusive, float maxInclusive)
        {
            return Random.Range(minInclusive, maxInclusive);
        }
    }
}
