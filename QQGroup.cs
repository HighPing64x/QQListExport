using System.Collections.Generic;

namespace QQListExport
{
    public class QQGroup
    {
        public long Gc { get; set; }            // 群号
        public string Gn { get; set; }          // 群名称
        public long Owner { get; set; }         // 群主QQ
        public int MyRole { get; set; }         // 当前用户在群中的角色 (0=群主,1=管理员,2=成员)
        public string MyRoleDisplay => MyRole switch { 0 => "群主", 1 => "管理员", 2 => "群员", _ => "未知" };
        public int MemberCount { get; set; }    // 成员总数(不知道为什么检测不到。)
        public List<QQMember> Members { get; set; } = new List<QQMember>();
    }
}

