using System;
using System.Globalization;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc.Reports
{
    public class PostBuildReportSectionStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var falseStatus =
                parameter is PostBuildReportSectionStatus status ? status : PostBuildReportSectionStatus.Error;
            return value is bool b && b ? PostBuildReportSectionStatus.OK : falseStatus;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is PostBuildReportSectionStatus status && status == PostBuildReportSectionStatus.OK;
        }
    }
}
