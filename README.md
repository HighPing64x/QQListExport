2# QQ 群管理助手 (QQ Group Manager)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-6.0%2B-blue)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-brightgreen)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)

一个基于 WPF 的 QQ 群管理工具，支持扫码登录 / Cookie 登录，可导出群列表、好友列表、群成员信息为 JSON、XML、Excel、CSV、图片等格式。

## ✨ 功能特性

- **登录方式**  
  - 内嵌 WebView2 扫码登录  
  - 直接粘贴 Cookie 字符串登录（快速切换账号）

- **数据获取**  
  - 获取用户加入、创建、管理的所有群列表  
  - 获取指定群的完整成员列表（支持分页，自动翻页）  
  - 获取好友列表（含分组信息）

- **数据导出**  
  - JSON / XML / CSV / Excel (.xlsx) / PNG 截图  
  - Excel 支持多工作表：群汇总 + 每个群一个成员工作表 + 好友列表  
  - CSV 和图片导出当前视图（群汇总或好友列表）

- **交互体验**  
  - 实时状态栏提示  
  - DataGrid 支持多选、复制选中行、复制 QQ 号  
  - 群成员抓取支持并发限制（默认最多 15 个群，防封控）
    
## 🛠️ 技术栈

- **框架**: .NET 6 / 7 / 8 (WPF)
- **UI 嵌入浏览器**: Microsoft.Web.WebView2
- **HTTP 请求**: System.Net.Http
- **JSON 处理**: Newtonsoft.Json
- **Excel 导出**: EPPlus (非商业许可证)
- **图片渲染**: RenderTargetBitmap + PngBitmapEncoder

## 🚀 快速开始

### 环境要求

- Windows 10 / 11 (WebView2 Runtime 已预装，或可[手动安装](https://developer.microsoft.com/zh-cn/microsoft-edge/webview2/))
- [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) 或更高版本

### 从源码编译
```
git clone https://github.com/HighPing64x/QQListExport.git
cd QQListExport
dotnet restore
dotnet build
```

#### 使用说明
1.扫码登录

点击「打开登录页」，在弹出的 WebView2 中使用手机 QQ 扫码。

登录成功后 WebView2 自动隐藏，界面恢复。

2.Cookie 登录

从浏览器开发者工具复制完整 Cookie 字符串（需包含 skey 或 p_skey）。

点击「Cookie登录」，粘贴字符串并确定。

3.获取数据

点击「获取群列表」拉取所有群。

在群汇总表格中多选群（Ctrl/Shift），点击「获取群内成员列表」抓取成员。

点击「获取好友列表」拉取好友。

4.导出数据

切换上方「显示」下拉框，选择要导出的视图（群汇总 / 好友列表）。

点击对应格式按钮（JSON、XML、CSV、Excel、图片）保存文件。

📂 项目结构
```
QQListExport/
├── Models/
│   ├── QQGroup.cs
│   ├── QQMember.cs
│   └── QQFriend.cs
├── Services/
│   ├── QqLoginHelper.cs      # WebView2 登录与 Cookie 提取
│   ├── QqApiService.cs       # QQ 接口调用与 bkn 计算
│   └── ExportHelper.cs       # 导出 JSON/XML/Excel/PNG 等
├── Converters/
│   └── HtmlDecodeConverter.cs # XAML 值转换器（HTML 解码）
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── App.xaml
└── App.xaml.cs
```

📄 许可证
本项目采用 MIT 许可证。
你可以自由使用、修改、分发，但需保留原始版权声明。

MIT License

Copyright (c) 2025 highping (QQ:3948185609)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

⚠️ 免责声明
本工具仅供学习交流，请勿用于非法用途。

使用本工具即表示您自行承担可能的风险（如账号封禁）。

作者不对任何因使用本软件造成的损失负责。

🤝 贡献
欢迎提交 Issue 和 Pull Request。
建议提交前先通过 Issue 讨论功能或 bug。

作者: highping (QQ:3948185609)
项目主页: [QQListExport](https://github.com/HighPing64x/QQListExport/)
