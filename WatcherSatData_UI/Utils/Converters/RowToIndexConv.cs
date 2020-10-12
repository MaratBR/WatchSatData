using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace WatcherSatData_UI.Utils.Converters
{
    public class RowToIndexConv : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var row = value as DataGridRow;
            return row.GetIndex() + 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}