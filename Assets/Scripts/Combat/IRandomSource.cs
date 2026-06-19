namespace IdleOff.Combat
{
    public interface IRandomSource
    {
        float Value { get; }
        float Range(float minInclusive, float maxInclusive);
    }
}
