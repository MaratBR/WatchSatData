using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WatcherSatData_UI.Utils.Converters
{
    public class BooleanConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty TrueValueProperty =
            DependencyProperty.Register("TrueValue", typeof(object), typeof(BooleanConverter),
                new PropertyMetadata(null));

        public static readonly DependencyProperty FalseValueProperty =
            DependencyProperty.Register("FalseValue", typeof(object), typeof(BooleanConverter),
                new PropertyMetadata(null));


        public object TrueValue
        {
            get => GetValue(TrueValueProperty);
            set => SetValue(TrueValueProperty, value);
        }


        public object FalseValue
        {
            get => GetValue(FalseValueProperty);
            set => SetValue(FalseValueProperty, value);
        }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? TrueValue : FalseValue;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == TrueValue;
        }
    }
}