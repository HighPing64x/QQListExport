using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace QQListExport
{
    public static class ExportHelper
    {
        public static Task ExportJsonAsync(string filePath, List<QQGroup> groups, List<QQFriend> friends)
        {
            return Task.Run(() =>
            {
                var obj = new { Groups = groups ?? new List<QQGroup>(), Friends = friends ?? new List<QQFriend>() };
                var s = JsonConvert.SerializeObject(obj, Formatting.Indented);
                File.WriteAllText(filePath, s, System.Text.Encoding.UTF8);
            });
        }

        public static Task ExportXmlAsync(string filePath, List<QQGroup> groups, List<QQFriend> friends)
        {
            return Task.Run(() =>
            {
                var doc = new XDocument(new XElement("QQData",
                    new XElement("Groups",
                        from g in (groups ?? new List<QQGroup>())
                        select new XElement("Group",
                            new XElement("Gc", g.Gc),
                            new XElement("Gn", g.Gn),
                            new XElement("Owner", g.Owner),
                            new XElement("MyRole", g.MyRole),
                            new XElement("MemberCount", g.MemberCount),
                            new XElement("Members",
                                from m in (g.Members ?? new List<QQMember>())
                                select new XElement("Member",
                                    new XElement("Uin", m.Uin),
                                    new XElement("Nick", m.Nick),
                                    new XElement("Card", m.Card),
                                    new XElement("Gender", m.Gender),
                                    new XElement("QAge", m.QAge),
                                    new XElement("Role", m.Role),
                                    new XElement("JoinTime", m.JoinTime),
                                    new XElement("LastSpeakTime", m.LastSpeakTime)
                                )
                            )
                        )
                    ),
                    new XElement("Friends",
                        from f in (friends ?? new List<QQFriend>())
                        select new XElement("Friend",
                            new XElement("Uin", f.Uin),
                            new XElement("Name", f.Name),
                            new XElement("GroupName", f.GroupName),
                            new XElement("Groups", f.Groups ?? string.Empty)
                        )
                    )
                ));

                doc.Save(filePath);
            });
        }

        public static Task ExportExcelAsync(string filePath, List<QQGroup> groups, List<QQFriend> friends)
        {
            return Task.Run(() =>
            {
                using var pkg = new ExcelPackage();

                var groupList = groups ?? new List<QQGroup>();
                var friendList = friends ?? new List<QQFriend>();

                if (groupList.Count > 0)
                {
                    var wsSummary = pkg.Workbook.Worksheets.Add("群汇总");
                    wsSummary.Cells[1, 1].Value = "群号";
                    wsSummary.Cells[1, 2].Value = "群名称";
                    wsSummary.Cells[1, 3].Value = "群主QQ";
                    wsSummary.Cells[1, 4].Value = "我的角色";
                    wsSummary.Cells[1, 5].Value = "成员数";

                    int r = 2;
                    foreach (var g in groupList)
                    {
                        wsSummary.Cells[r, 1].Value = g.Gc;
                        wsSummary.Cells[r, 2].Value = g.Gn;
                        wsSummary.Cells[r, 3].Value = g.Owner;
                        wsSummary.Cells[r, 4].Value = g.MyRoleDisplay;
                        wsSummary.Cells[r, 5].Value = g.MemberCount;
                        r++;
                    }
                    if (wsSummary.Dimension != null)
                        wsSummary.Cells[wsSummary.Dimension.Address].AutoFitColumns();

                    // Each group worksheet
                    foreach (var g in groupList)
                    {
                        var sheetName = $"群_{g.Gc}";
                        if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);
                        var ws = pkg.Workbook.Worksheets.Add(sheetName);
                        ws.Cells[1, 1].Value = "QQ号";
                        ws.Cells[1, 2].Value = "昵称";
                        ws.Cells[1, 3].Value = "群名片";
                        ws.Cells[1, 4].Value = "性别";
                        ws.Cells[1, 5].Value = "Q龄";
                        ws.Cells[1, 6].Value = "角色";
                        ws.Cells[1, 7].Value = "入群时间";
                        ws.Cells[1, 8].Value = "最后发言时间";

                        int rr = 2;
                        foreach (var m in (g.Members ?? new List<QQMember>()))
                        {
                            ws.Cells[rr, 1].Value = m.Uin;
                            ws.Cells[rr, 2].Value = m.Nick;
                            ws.Cells[rr, 3].Value = m.Card;
                            ws.Cells[rr, 4].Value = m.Gender;
                            ws.Cells[rr, 5].Value = m.QAge;
                            ws.Cells[rr, 6].Value = m.Role;
                            ws.Cells[rr, 7].Value = m.JoinTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(m.JoinTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty;
                            ws.Cells[rr, 8].Value = m.LastSpeakTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(m.LastSpeakTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty;
                            rr++;
                        }
                        if (ws.Dimension != null)
                            ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    }
                }

                if (friendList.Count > 0)
                {
                    var wsFriends = pkg.Workbook.Worksheets.Add("好友列表");
                    wsFriends.Cells[1, 1].Value = "QQ号";
                    wsFriends.Cells[1, 2].Value = "昵称";
                    wsFriends.Cells[1, 3].Value = "分组";
                    wsFriends.Cells[1, 4].Value = "所在群";
                    int rf = 2;
                    foreach (var f in friendList)
                    {
                        wsFriends.Cells[rf, 1].Value = f.Uin;
                        wsFriends.Cells[rf, 2].Value = f.Name;
                        wsFriends.Cells[rf, 3].Value = f.GroupName;
                        wsFriends.Cells[rf, 4].Value = f.Groups ?? string.Empty;
                        rf++;
                    }
                    if (wsFriends.Dimension != null)
                        wsFriends.Cells[wsFriends.Dimension.Address].AutoFitColumns();
                }

                // Save
                var fi = new FileInfo(filePath);
                pkg.SaveAs(fi);
            });
        }

        public static Task ExportDataGridToPngAsync(DataGrid grid, string filePath)
        {
            return Task.Run(() =>
            {
                grid.Dispatcher.Invoke(() =>
                {
                    var actualHeight = (int)grid.ActualHeight;
                    var actualWidth = (int)grid.ActualWidth;
                    if (actualHeight == 0 || actualWidth == 0)
                    {
                        // Try measure
                        grid.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                        grid.Arrange(new System.Windows.Rect(grid.DesiredSize));
                        actualHeight = (int)grid.ActualHeight;
                        actualWidth = (int)grid.ActualWidth;
                        if (actualHeight == 0) actualHeight = (int)grid.DesiredSize.Height;
                        if (actualWidth == 0) actualWidth = (int)grid.DesiredSize.Width;
                    }

                    if (actualHeight <= 0) actualHeight = 600;
                    if (actualWidth <= 0) actualWidth = 800;

                    var rtb = new RenderTargetBitmap(actualWidth, actualHeight, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(grid);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    using var fs = File.OpenWrite(filePath);
                    encoder.Save(fs);
                });
            });
        }
    }
}

