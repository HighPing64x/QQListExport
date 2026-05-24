namespace QQListExport
{
    public class QQFriend
    {
        public long Uin { get; set; }
        public string Name { get; set; }
        public string? Remark { get; set; }
        public string GroupName { get; set; }   // 好友所在群名(在导出群成员列表时有用)
        public string Groups { get; set; } = string.Empty;
    }
}

