using IdleOff.Profiles;

namespace IdleOff.Game
{
    public sealed class ProfileRecord
    {
        public string ProfileID { get; }
        public string ProfileName { get; set; }
        public CharacterProfile Profile { get; }

        public ProfileRecord(string profileID, string profileName, CharacterProfile profile)
        {
            ProfileID = profileID;
            ProfileName = string.IsNullOrWhiteSpace(profileName) ? "New Profile" : profileName;
            Profile = profile;
        }
    }
}
