using IdleOff.Profiles;

namespace IdleOff.Game
{
    public static class GameSession
    {
        public static ProfileRecord ActiveProfileRecord { get; private set; }
        public static CharacterProfile ActiveProfile => ActiveProfileRecord?.Profile;
        public static CharacterData ActiveCharacter => ActiveProfile?.ActiveCharacter;

        public static void SetActiveProfile(ProfileRecord record)
        {
            ActiveProfileRecord = record;
        }

        public static void SaveActiveProfile()
        {
            ProfileManager.SaveRecord(ActiveProfileRecord);
        }

        public static void Clear()
        {
            ActiveProfileRecord = null;
        }
    }
}
