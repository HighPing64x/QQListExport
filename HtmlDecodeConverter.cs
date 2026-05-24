using System;
using System.Globalization;
using System.Windows.Data;
using System.Web;

namespace QQListExport
{
    /// <summary>
    /// Converts HTML-encoded strings (e.g. &amp;nbsp;) to plain text.
    /// </summary>
    public class HtmlDecodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                var decoded = HttpUtility.HtmlDecode(s);
                decoded = decoded.Replace("\u00A0", " ").Replace("\xA0", " ");
                // 把常见的&nbsp;替换成棍母
                decoded = decoded.Replace("&nbsp;", " ");
                return decoded;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
