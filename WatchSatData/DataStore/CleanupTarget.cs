using System.Runtime.Serialization;

namespace WatchSatData.DataStore
{
    [DataContract]
    public enum CleanupTarget
    {
        [EnumMember] All,

        [EnumMember] Files,

        [EnumMember] Directories
    }
}