using System.Collections.Generic;

namespace IdleOff.Profiles
{
    [System.Serializable]
    public sealed class ClassModifier : Modifier
    {
        public ClassModifier()
        {
        }

        public ClassModifier(List<string> tags)
        {
            SetTags(tags);
        }
    }
}
