using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace QQListExport
{
    public class QqApiService
    {
        private readonly HttpClient _httpClient;

        public QqApiService()
        {
            var handler = new HttpClientHandler { UseCookies = false };
            _httpClient = new HttpClient(handler, disposeHandler: true);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        private int ComputeBkn(string skey)
        {
            if (skey == null) return 0;
            int hash = 5381;
            foreach (var c in skey)
            {
                hash += (hash << 5) + c;
            }
            return hash & 0x7fffffff;
        }

        private async Task<JObject> PostFormAsync(string url, string cookieString, Dictionary<string, string> form)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Referrer = new Uri("https://qun.qq.com/");
            req.Headers.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
            req.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            if (!string.IsNullOrEmpty(cookieString))
            {
                req.Headers.TryAddWithoutValidation("Cookie", cookieString);
            }
            req.Content = new FormUrlEncodedContent(form);

            var resp = await _httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var s = await resp.Content.ReadAsStringAsync();

            try
            {
                var firstBrace = s.IndexOf('{');
                var lastBrace = s.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace >= firstBrace)
                {
                    var json = s.Substring(firstBrace, lastBrace - firstBrace + 1);
                    return JObject.Parse(json);
                }
                return JObject.Parse(s);
            }
            catch
            {
                return new JObject();
            }
        }

        private static string CleanHtml(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            s = System.Web.HttpUtility.HtmlDecode(s);
            s = s.Replace('\u00A0', ' ');
            return s;
        }

        public async Task<List<QQGroup>> GetGroupListAsync(string cookieString, string skey, string? currentUin = null)
        {
            var bkn = ComputeBkn(skey);
            var url = "https://qun.qq.com/cgi-bin/qun_mgr/get_group_list";
            var form = new Dictionary<string, string> { { "bkn", bkn.ToString() } };

            var jo = await PostFormAsync(url, cookieString, form);
            var groups = new List<QQGroup>();

            var roleMap = new Dictionary<string, int>
            {
                { "create", 0 },
                { "manage", 1 },
                { "join", 2 }
            };

            foreach (var arrName in new[] { "create", "manage", "join" })
            {
                var arr = jo[arrName] as JArray;
                if (arr == null) continue;

                int roleFromArrName = roleMap.ContainsKey(arrName) ? roleMap[arrName] : 2;

                foreach (var item in arr)
                {
                    try
                    {
                        var gc = item.Value<long?>("gc") ?? item.Value<long?>("gid") ?? 0;
                        var gn = CleanHtml(item.Value<string>("gn") ?? item.Value<string>("name") ?? string.Empty);
                        var owner = item.Value<long?>("owner") ?? item.Value<long?>("owner_uin") ?? 0;

                        int myRole = roleFromArrName;
                        var roleField = item.Value<int?>("myrole") ?? item.Value<int?>("my_role") ?? item.Value<int?>("role");
                        if (roleField != null)
                        {
                            myRole = roleField.Value;
                        }
                        else if (!string.IsNullOrEmpty(currentUin))
                        {
                            if (long.TryParse(currentUin, out var cu) && owner == cu)
                            {
                                myRole = 0;
                            }
                        }

                        var memberCount = item.Value<int?>("member_num")
                                          ?? item.Value<int?>("member_count")
                                          ?? item.Value<int?>("max_member_num")
                                          ?? item.Value<int?>("members")
                                          ?? item.Value<int?>("count")
                                          ?? 0;

                        var qg = new QQGroup
                        {
                            Gc = gc,
                            Gn = gn,
                            Owner = owner,
                            MyRole = myRole,
                            MemberCount = memberCount,
                            Members = new List<QQMember>()
                        };
                        groups.Add(qg);
                    }
                    catch { }
                }
            }

            groups = groups
                .GroupBy(g => g.Gc)
                .Select(g => g.OrderBy(x => x.MyRole).First())
                .ToList();
            return groups;
        }

        public async Task<List<QQFriend>> GetFriendListAsync(string cookieString, string skey)
        {
            var bkn = ComputeBkn(skey);
            var url = "https://qun.qq.com/cgi-bin/qun_mgr/get_friend_list";
            var form = new Dictionary<string, string> { { "bkn", bkn.ToString() } };

            var jo = await PostFormAsync(url, cookieString, form);
            var list = new List<QQFriend>();
            var result = jo["result"] as JObject ?? jo["resultlist"] as JObject ?? jo["friends"] as JObject;

            if (result != null)
            {
                foreach (var prop in result.Properties())
                {
                    var groupName = CleanHtml(prop.Name);
                    var groupObj = prop.Value as JObject;
                    var mems = groupObj?["mems"] as JArray;
                    if (mems == null) continue;
                    foreach (var m in mems)
                    {
                        try
                        {
                            var uin = m.Value<long?>("uin") ?? 0;
                            var name = CleanHtml(m.Value<string>("name") ?? string.Empty);
                            var remark = CleanHtml(m.Value<string>("remark") ?? m.Value<string>("markname") ?? m.Value<string>("memo"));
                            list.Add(new QQFriend { Uin = uin, Name = name, Remark = remark, GroupName = groupName });
                        }
                        catch { }
                    }
                }
            }

            return list;
        }

        public async Task<List<QQMember>> GetGroupMembersAsync(string cookieString, string skey, long gc)
        {
            var bkn = ComputeBkn(skey);
            var url = "https://qun.qq.com/cgi-bin/qun_mgr/search_group_members";
            var members = new List<QQMember>();

            int pageSize = 20;
            int st = 0;
            int totalCount = 0;      // 记录总成员数，用于分页终止
            bool hasMore = true;

            while (hasMore)
            {
                var end = st + pageSize - 1;
                var form = new Dictionary<string, string>
        {
            { "gc", gc.ToString() },
            { "st", st.ToString() },
            { "end", end.ToString() },
            { "sort", "0" },
            { "bkn", bkn.ToString() }
        };

                JObject jo = null;
                try
                {
                    jo = await PostFormAsync(url, cookieString, form);
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception($"网络请求失败: {ex.Message}");
                }

                var ec = jo.Value<int?>("ec") ?? -1;
                if (ec != 0)
                {
                    if (ec == 7)  // 访问被拒绝 / 频率限制
                        throw new Exception("接口访问被限制，请稍后再试或减少并发请求 (ec=7)");
                    else
                        throw new Exception($"API 返回错误，ec={ec}");
                }

                // 获取总数（首次请求时获得）
                if (totalCount == 0)
                {
                    totalCount = jo.Value<int?>("search_count") ?? jo.Value<int?>("count") ?? 0;
                    if (totalCount == 0)
                        break; // 没有成员
                }

                var mems = jo["mems"] as JArray;
                if (mems == null || mems.Count == 0)
                    break;

                foreach (var m in mems)
                {
                    try
                    {
                        var uin = m.Value<long?>("uin") ?? 0;
                        var nick = CleanHtml(m.Value<string>("nick") ?? string.Empty);
                        var card = CleanHtml(m.Value<string>("card") ?? string.Empty);
                        var join_time = m.Value<long?>("join_time") ?? 0;
                        var last_speak_time = m.Value<long?>("last_speak_time") ?? 0;
                        var role = m.Value<int?>("role") ?? 2;
                        var qage = m.Value<int?>("qage") ?? 0;
                        var g = m.Value<int?>("g") ?? -1;

                        members.Add(new QQMember
                        {
                            Uin = uin,
                            Nick = nick,
                            Card = card,
                            Gender = g,
                            QAge = qage,
                            Role = role,
                            JoinTime = join_time,
                            LastSpeakTime = last_speak_time
                        });
                    }
                    catch { }
                }

                // 更新起始索引
                st += pageSize;
                // 判断是否还有更多数据
                hasMore = st < totalCount;
            }

            return members;
        }
    }
}