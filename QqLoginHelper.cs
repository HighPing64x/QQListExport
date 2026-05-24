using Microsoft.Web.WebView2.Wpf;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QQListExport
{
    // 获取神秘登录cookie。WebView
    public class QqLoginHelper
    {
        /// <summary>
        /// Try to extract skey, p_skey, uin and build cookie string from the WebView2 cookie manager.
        /// Returns nullable skey/p_skey/uin; cookieString contains concatenated cookies for requests.
        /// </summary>
        public async Task<(bool success, string cookieString, string? skey, string? p_skey, string? uin)> TryExtractCookiesAsync(Microsoft.Web.WebView2.Wpf.WebView2 webView)
        {
            try
            {
                if (webView?.CoreWebView2 == null)
                {
                    return (false, string.Empty, null, null, null);
                }

                var cm = webView.CoreWebView2.CookieManager;
                // Get cookies for qun.qq.com and qq.com
                var list1 = await cm.GetCookiesAsync("https://qun.qq.com/");
                var list2 = await cm.GetCookiesAsync("https://qq.com/");
                var cookies = list1.Concat(list2).ToList();

                if (cookies == null || cookies.Count == 0)
                {
                    return (false, string.Empty, null, null, null);
                }

                string? skey = cookies.FirstOrDefault(c => c.Name == "skey")?.Value;
                string? p_skey = cookies.FirstOrDefault(c => c.Name == "p_skey")?.Value;
                string? uin = cookies.FirstOrDefault(c => c.Name == "uin")?.Value
                              ?? cookies.FirstOrDefault(c => c.Name == "o_cookie")?.Value;

                // Build cookie string including all cookies returned for the domains - more robust
                var cookieParts = cookies.Select(c => $"{c.Name}={c.Value}").Distinct().ToList();

                var cookieString = string.Join("; ", cookieParts);

                var success = !string.IsNullOrEmpty(cookieString) && (skey != null || p_skey != null || uin != null);
                return (success, cookieString, skey, p_skey, uin);
            }
            catch
            {
                return (false, string.Empty, null, null, null);
            }
        }
    }
}
