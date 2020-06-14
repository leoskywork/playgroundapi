
namespace LeoMaster6.Common
{
    public class Constants
    {
        public const string MessageSeparator = "//lsk//";
        public const string DevSessionId = "dev001abc";
        public const string DevUserId = "u086001";
        public const string DevUpdateUserId = "u086100";
        public const string DevDeleteUserId = "u086200";

        public const string HeaderSessionId = "lsk-session-id";

        public const string LskjsonPrefix = "lskjson-";
        public const string LskjsonIndexFilePrefix = "lskjson-index-";
        public const string LsktextPrefix = "lsktxt-";
        public const string LskArchived = "archived";

        public const int LskjsonIndexFileAgeInYears = 10;
        public const int LskMaxReturnNoteCount = 50;
        public const int LskMaxDBFileSizeKB = 5 * 1024;
        public const int LskMaxPasscodeLength = 128;
        public const int LskFulfillmentArchiveUnit = 10;
        public const int LskFulfillmentActiveRecords = 20;
    }
}