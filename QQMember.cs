namespace QQListExport
{
    public class QQMember
    {
        public long Uin { get; set; }
        public string Nick { get; set; }
        public string Card { get; set; }        // 群名片 （没用到）
        public int Gender { get; set; }         // 0男 1女 -1未知 （没用到）
        public int QAge { get; set; }           // Q龄（年）（没用到）
        public int Role { get; set; }           // 0群主 1管理员 2成员
        public long JoinTime { get; set; }      // 入群时间戳（秒） （没用到）
        public long LastSpeakTime { get; set; } // 最后发言时间戳（秒） （没用到）
    }
}

