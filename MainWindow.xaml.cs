using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace QQListExport
{
    public partial class MainWindow : Window
    {
        private QqLoginHelper? _loginHelper;
        private QqApiService? _apiService;

        private string? _cookieString;
        private string? _skey;

        private List<QQGroup> _groups = new List<QQGroup>();
        private List<QQFriend> _friends = new List<QQFriend>();

        public MainWindow()
        {
            InitializeComponent();

            _loginHelper = new QqLoginHelper();
            _apiService = new QqApiService();

            SetupDefaultGridForGroups();
        }

        private void SetStatus(string text)
        {
            Dispatcher.Invoke(() => StatusText.Text = text);
        }

        private void BtnOpenLogin_Click(object sender, RoutedEventArgs e)
        {
            WebView.Visibility = Visibility.Visible;
            DataGridMain.Visibility = Visibility.Collapsed;
            BtnOpenLogin.IsEnabled = false;
            SetStatus("加载登录页面...");

            _ = InitializeAndNavigateWebViewAsync();
        }

        private async Task InitializeAndNavigateWebViewAsync()
        {
            try
            {
                if (WebView.CoreWebView2 == null)
                {
                    await WebView.EnsureCoreWebView2Async();
                }

                WebView.CoreWebView2.Navigate("https://xui.ptlogin2.qq.com/cgi-bin/xlogin?appid=715030901&daid=73&style=33&hide_close_icon=1&s_url=https://qun.qq.com/member.html");
                SetStatus("请在弹出的页面扫码登录...");
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化 WebView2 失败: " + ex.Message);
                SetStatus("WebView2 初始化失败");
                BtnOpenLogin.IsEnabled = true;
            }
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                var uri = WebView.Source?.ToString() ?? WebView.CoreWebView2?.Source;
                if (string.IsNullOrEmpty(uri))
                {
                    return;
                }

                if (uri.Contains("qun.qq.com/member.html", StringComparison.OrdinalIgnoreCase) || uri.Contains("qun.qq.com"))
                {
                    SetStatus("检测到跳转，尝试提取登录 Cookie...");

                    var result = await _loginHelper!.TryExtractCookiesAsync(WebView);
                    if (result.success)
                    {
                        _cookieString = result.cookieString;
                        _skey = result.skey ?? result.p_skey;

                        WebView.Visibility = Visibility.Collapsed;
                        DataGridMain.Visibility = Visibility.Visible;
                        BtnGetGroups.IsEnabled = true;
                        BtnGetFriends.IsEnabled = true;
                        BtnExportJson.IsEnabled = true;
                        BtnExportXml.IsEnabled = true;
                        BtnExportCsv.IsEnabled = true;
                        BtnExportExcel.IsEnabled = true;
                        BtnExportPng.IsEnabled = true;

                        SetStatus("登录成功");
                    }
                    else
                    {
                        SetStatus("尚未登录或无法提取 Cookie，继续等待...");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理导航完成时出错: " + ex.Message);
            }
            finally
            {
                BtnOpenLogin.IsEnabled = true;
            }
        }

        private async void BtnGetGroups_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_cookieString) || string.IsNullOrEmpty(_skey))
                {
                    MessageBox.Show("请先登录并确保已提取 Cookie。");
                    return;
                }

                SetStatus("获取群列表中...");
                BtnGetGroups.IsEnabled = false;

                _groups = await _apiService!.GetGroupListAsync(_cookieString, _skey);

                SetStatus("群列表获取完成（仅获取群信息）。请在表格中选择要抓取成员的群，最多 15 个，然后点击 '获取所选群成员'。");

                SetupDefaultGridForGroups();
                DataGridMain.ItemsSource = _groups;
                BtnFetchSelectedMembers.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取群列表出错: " + ex.Message);
                SetStatus("错误");
            }
            finally
            {
                BtnGetGroups.IsEnabled = true;
            }
        }

        private async void BtnGetFriends_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_cookieString) || string.IsNullOrEmpty(_skey))
                {
                    MessageBox.Show("请先登录并确保已提取 Cookie。");
                    return;
                }

                SetStatus("获取好友列表中...");
                BtnGetFriends.IsEnabled = false;

                _friends = await _apiService!.GetFriendListAsync(_cookieString, _skey);

                SetStatus("好友列表获取完成");

                SetupGridForFriends();
                DataGridMain.ItemsSource = _friends;
                BtnExportJson.IsEnabled = true;
                BtnExportXml.IsEnabled = true;
                BtnExportExcel.IsEnabled = true;
                BtnExportPng.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取好友列表出错: " + ex.Message);
                SetStatus("错误");
            }
            finally
            {
                BtnGetFriends.IsEnabled = true;
            }
        }

        private HtmlDecodeConverter _htmlDecode = new HtmlDecodeConverter();

        private void SetupDefaultGridForGroups()
        {
            if (DataGridMain == null) return;

            DataGridMain.Columns.Clear();
            DataGridMain.Columns.Add(new DataGridTextColumn { Header = "群号", Binding = new System.Windows.Data.Binding("Gc") });
            DataGridMain.Columns.Add(new DataGridTextColumn { Header = "群名称", Binding = new System.Windows.Data.Binding("Gn") { Converter = _htmlDecode } });
            DataGridMain.Columns.Add(new DataGridTextColumn { Header = "群主QQ", Binding = new System.Windows.Data.Binding("Owner") });
            DataGridMain.Columns.Add(new DataGridTextColumn { Header = "我的角色", Binding = new System.Windows.Data.Binding("MyRoleDisplay") });
        }

        private void SetupGridForFriends()
        {
            DataGridMain.Columns.Clear();
            DataGridMain.Columns.Add(new DataGridTextColumn { Header = "QQ号", Binding = new System.Windows.Data.Binding("Uin") });
            DataGridMain.Columns.Add(new DataGridTextColumn { Header = "昵称", Binding = new System.Windows.Data.Binding("Name") { Converter = _htmlDecode } });
            DataGridMain.Columns.Add(new DataGridTextColumn { Header = "分组", Binding = new System.Windows.Data.Binding("GroupName") { Converter = _htmlDecode } });
            DataGridMain.Columns.Add(new DataGridTextColumn { Header = "所在群", Binding = new System.Windows.Data.Binding("Groups") { Converter = _htmlDecode } });
        }

        private void MenuItemCopySelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var item in DataGridMain.SelectedItems)
                {
                    if (item is QQGroup g)
                        sb.AppendLine($"{g.Gc}\t{g.Gn}\t{g.Owner}\t{g.MyRoleDisplay}");
                    else if (item is QQFriend f)
                        sb.AppendLine($"{f.Uin}\t{f.Name}\t{f.GroupName}\t{f.Groups}");
                }
                if (sb.Length > 0)
                {
                    Clipboard.SetText(sb.ToString());
                    SetStatus("已复制选中行到剪贴板");
                }
            }
            catch { }
        }

        private void MenuItemCopyUin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uins = new List<string>();
                foreach (var item in DataGridMain.SelectedItems)
                {
                    if (item is QQGroup g)
                        uins.Add(g.Gc.ToString());
                    else if (item is QQFriend f)
                        uins.Add(f.Uin.ToString());
                }
                if (uins.Count > 0)
                {
                    Clipboard.SetText(string.Join(", ", uins));
                    SetStatus($"已复制 {uins.Count} 个QQ号到剪贴板");
                }
            }
            catch { }
        }

        private void ViewSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridMain == null) return;

            if (ViewSelector.SelectedItem is ComboBoxItem item)
            {
                var content = item.Content?.ToString();
                if (content == "群汇总")
                {
                    SetupDefaultGridForGroups();
                    DataGridMain.ItemsSource = _groups;
                }
                else
                {
                    SetupGridForFriends();
                    DataGridMain.ItemsSource = _friends;
                }
            }
        }

        private string GetCurrentViewType()
        {
            if (ViewSelector.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? "群汇总";
            return "群汇总";
        }

        private async void BtnExportJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentView = GetCurrentViewType();
                if (currentView == "群汇总")
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "JSON 文件|*.json", FileName = "groups.json" };
                    if (dlg.ShowDialog() == true)
                    {
                        SetStatus("导出 JSON 中...");
                        await ExportHelper.ExportJsonAsync(dlg.FileName, _groups, null);
                        SetStatus("导出 JSON 完成");
                        MessageBox.Show("导出成功: " + dlg.FileName);
                    }
                }
                else
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "JSON 文件|*.json", FileName = "friends.json" };
                    if (dlg.ShowDialog() == true)
                    {
                        SetStatus("导出 JSON 中...");
                        await ExportHelper.ExportJsonAsync(dlg.FileName, null, _friends);
                        SetStatus("导出 JSON 完成");
                        MessageBox.Show("导出成功: " + dlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出 JSON 出错: " + ex.Message);
                SetStatus("错误");
            }
        }

        private async void BtnExportXml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentView = GetCurrentViewType();
                if (currentView == "群汇总")
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "XML 文件|*.xml", FileName = "groups.xml" };
                    if (dlg.ShowDialog() == true)
                    {
                        SetStatus("导出 XML 中...");
                        await ExportHelper.ExportXmlAsync(dlg.FileName, _groups, null);
                        SetStatus("导出 XML 完成");
                        MessageBox.Show("导出成功: " + dlg.FileName);
                    }
                }
                else
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "XML 文件|*.xml", FileName = "friends.xml" };
                    if (dlg.ShowDialog() == true)
                    {
                        SetStatus("导出 XML 中...");
                        await ExportHelper.ExportXmlAsync(dlg.FileName, null, _friends);
                        SetStatus("导出 XML 完成");
                        MessageBox.Show("导出成功: " + dlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出 XML 出错: " + ex.Message);
                SetStatus("错误");
            }
        }

        private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentView = GetCurrentViewType();
                if (currentView == "群汇总")
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel 文件|*.xlsx", FileName = "groups.xlsx" };
                    if (dlg.ShowDialog() == true)
                    {
                        SetStatus("导出 Excel 中...");
                        await ExportHelper.ExportExcelAsync(dlg.FileName, _groups, null);
                        SetStatus("导出 Excel 完成");
                        MessageBox.Show("导出成功: " + dlg.FileName);
                    }
                }
                else
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel 文件|*.xlsx", FileName = "friends.xlsx" };
                    if (dlg.ShowDialog() == true)
                    {
                        SetStatus("导出 Excel 中...");
                        await ExportHelper.ExportExcelAsync(dlg.FileName, null, _friends);
                        SetStatus("导出 Excel 完成");
                        MessageBox.Show("导出成功: " + dlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出 Excel 出错: " + ex.Message);
                SetStatus("错误");
            }
        }

        private async void BtnExportPng_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "PNG 文件|*.png", FileName = "datagrid.png" };
                if (dlg.ShowDialog() == true)
                {
                    SetStatus("导出图片中...");
                    await ExportHelper.ExportDataGridToPngAsync(DataGridMain, dlg.FileName);
                    SetStatus("导出图片完成");
                    MessageBox.Show("导出成功: " + dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出图片出错: " + ex.Message);
                SetStatus("错误");
            }
        }

        private async void BtnFetchSelectedMembers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_cookieString) || string.IsNullOrEmpty(_skey))
                {
                    MessageBox.Show("请先登录并确保已提取 Cookie。");
                    return;
                }

                var selected = DataGridMain.SelectedItems.Cast<QQGroup>().ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("请在群列表中多选（Ctrl/Shift）要抓取的群。");
                    return;
                }
                if (selected.Count > 15)
                {
                    MessageBox.Show("最多只能选择 15 个群以避免被接口限流。请减少选择数量。");
                    return;
                }

                BtnFetchSelectedMembers.IsEnabled = false;
                int idx = 0;
                int successCount = 0;
                int failCount = 0;

                foreach (var g in selected)
                {
                    idx++;
                    SetStatus($"[{idx}/{selected.Count}] 获取群 {g.Gc} 成员...");
                    try
                    {
                        var members = await _apiService!.GetGroupMembersAsync(_cookieString, _skey, g.Gc);
                        g.Members = members;
                        g.MemberCount = members?.Count ?? 0;
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        MessageBox.Show($"获取群 {g.Gc} 成员失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    // 随机延迟 1-2 秒，避免频率限制
                    await Task.Delay(new Random().Next(1000, 2000));
                }

                SetStatus($"完成: 成功 {successCount} 个群，失败 {failCount} 个群");

                if (successCount == 0)
                    return;

                // 从成员列表构建好友视图
                _friends.Clear();
                foreach (var g in selected.Where(g => g.Members != null))
                {
                    var gname = string.IsNullOrEmpty(g.Gn) ? g.Gc.ToString() : g.Gn;
                    foreach (var m in g.Members!)
                    {
                        var existing = _friends.FirstOrDefault(x => x.Uin == m.Uin);
                        if (existing != null)
                        {
                            var list = (existing.Groups ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                            if (!list.Contains(gname)) list.Add(gname);
                            existing.Groups = string.Join("; ", list);
                        }
                        else
                        {
                            _friends.Add(new QQFriend
                            {
                                Uin = m.Uin,
                                Name = !string.IsNullOrEmpty(m.Card) ? m.Card : m.Nick,
                                GroupName = string.Empty,
                                Groups = gname
                            });
                        }
                    }
                }

                // 切换到好友列表视图
                ViewSelector.SelectedIndex = 1;
                SetupGridForFriends();
                DataGridMain.ItemsSource = _friends;
                MessageBox.Show($"成功获取 {successCount} 个群的成员，共 {_friends.Count} 个不同的用户。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取所选群成员时发生错误: " + ex.Message);
                SetStatus("错误");
            }
            finally
            {
                BtnFetchSelectedMembers.IsEnabled = true;
            }
        }

        private async void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentView = GetCurrentViewType();
                if (currentView == "群汇总")
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "CSV 文件|*.csv", FileName = "groups.csv" };
                    if (dlg.ShowDialog() != true) return;

                    SetStatus("导出 CSV 中...");
                    await Task.Run(() =>
                    {
                        using var sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8);
                        sw.WriteLine("群号,群名称,群主QQ,我的角色,成员数");
                        string Escape(string s) => s?.Replace("\"", "\"\"") ?? string.Empty;
                        foreach (var g in _groups)
                        {
                            sw.WriteLine($"{g.Gc},\"{Escape(g.Gn)}\",{g.Owner},\"{Escape(g.MyRoleDisplay)}\",{g.MemberCount}");
                        }
                    });
                    SetStatus("导出 CSV 完成");
                    MessageBox.Show("导出成功: " + dlg.FileName);
                }
                else if (currentView == "好友列表")
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "CSV 文件|*.csv", FileName = "friends.csv" };
                    if (dlg.ShowDialog() != true) return;

                    SetStatus("导出 CSV 中...");
                    await Task.Run(() =>
                    {
                        using var sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8);
                        sw.WriteLine("QQ号,昵称,分组,所在群");
                        string Escape(string s) => s?.Replace("\"", "\"\"") ?? string.Empty;
                        foreach (var f in _friends)
                        {
                            sw.WriteLine($"{f.Uin},\"{Escape(f.Name)}\",\"{Escape(f.GroupName)}\",\"{Escape(f.Groups)}\"");
                        }
                    });
                    SetStatus("导出 CSV 完成");
                    MessageBox.Show("导出成功: " + dlg.FileName);
                }
                else
                {
                    MessageBox.Show("请先切换到要导出的视图（群汇总或好友列表）。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出 CSV 出错: " + ex.Message);
                SetStatus("错误");
            }
        }

        // ==================== Cookie 登录新增方法 ====================

        private async void BtnCookieLogin_Click(object sender, RoutedEventArgs e)
        {
            string cookieInput = ShowInputDialog("请输入 Cookie 字符串", "Cookie 登录", "uin=o123456; skey=@xxx; p_skey=yyy;");
            if (string.IsNullOrWhiteSpace(cookieInput))
                return;

            SetStatus("正在验证 Cookie...");

            var (success, cookieString, skey, p_skey) = ParseCookieString(cookieInput);
            if (!success || string.IsNullOrEmpty(skey))
            {
                MessageBox.Show("Cookie 无效或缺少 skey，请确保复制了完整的 Cookie 字符串。", "登录失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                SetStatus("Cookie 解析失败");
                return;
            }

            _cookieString = cookieString;
            _skey = skey ?? p_skey;

            var apiService = new QqApiService();
            bool isValid = await ValidateCookieAsync(apiService, _cookieString, _skey);
            if (!isValid)
            {
                MessageBox.Show("Cookie 已过期或无效，请重新获取。", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Cookie 无效");
                return;
            }

            WebView.Visibility = Visibility.Collapsed;
            DataGridMain.Visibility = Visibility.Visible;
            BtnGetGroups.IsEnabled = true;
            BtnGetFriends.IsEnabled = true;
            BtnExportJson.IsEnabled = true;
            BtnExportXml.IsEnabled = true;
            BtnExportCsv.IsEnabled = true;
            BtnExportExcel.IsEnabled = true;
            BtnExportPng.IsEnabled = true;

            SetStatus("Cookie 登录成功");
        }

        private (bool success, string cookieString, string skey, string p_skey) ParseCookieString(string input)
        {
            try
            {
                var parts = input.Split(';');
                var dict = new Dictionary<string, string>();
                foreach (var part in parts)
                {
                    var kv = part.Split(new[] { '=' }, 2);
                    if (kv.Length == 2)
                    {
                        var key = kv[0].Trim();
                        var val = kv[1].Trim();
                        dict[key] = val;
                    }
                }

                if (!dict.ContainsKey("skey") && !dict.ContainsKey("p_skey"))
                    return (false, string.Empty, string.Empty, string.Empty);

                var cookieBuilder = new StringBuilder();
                foreach (var kv in dict)
                    cookieBuilder.Append($"{kv.Key}={kv.Value}; ");
                string cookieString = cookieBuilder.ToString();

                dict.TryGetValue("skey", out string? skey);
                dict.TryGetValue("p_skey", out string? p_skey);
                return (true, cookieString, skey ?? string.Empty, p_skey ?? string.Empty);
            }
            catch
            {
                return (false, string.Empty, string.Empty, string.Empty);
            }
        }

        private async Task<bool> ValidateCookieAsync(QqApiService api, string cookieString, string skey)
        {
            try
            {
                int hash = 5381;
                foreach (char c in skey)
                    hash += (hash << 5) + c;
                int bkn = hash & 0x7fffffff;

                var url = $"https://qun.qq.com/cgi-bin/qunwelcome/myinfo?bkn={bkn}";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("Cookie", cookieString);
                req.Headers.Referrer = new Uri("https://qun.qq.com/");
                req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                using var client = new HttpClient();
                var resp = await client.SendAsync(req);
                var content = await resp.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                return json.Value<int>("retcode") == 0;
            }
            catch
            {
                return false;
            }
        }

        private string ShowInputDialog(string message, string title, string defaultValue = "")
        {
            var inputWindow = new Window
            {
                Title = title,
                Width = 500,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBlock = new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            var textBox = new TextBox { Text = defaultValue, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "确定", Width = 75, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "取消", Width = 75 };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            inputWindow.Content = grid;
            string? result = null;

            okButton.Click += (s, ev) => { result = textBox.Text; inputWindow.Close(); };
            cancelButton.Click += (s, ev) => inputWindow.Close();
            inputWindow.ShowDialog();

            return result ?? string.Empty;
        }
    }
}