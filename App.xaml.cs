using System.Windows;
using OfficeOpenXml;

namespace QQListExport
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ExcelPackage.License.SetNonCommercialPersonal("QQListExport User");
            base.OnStartup(e);
        }
    }
}
