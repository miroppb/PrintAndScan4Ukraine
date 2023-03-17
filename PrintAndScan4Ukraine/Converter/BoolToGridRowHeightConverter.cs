using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace PrintAndScan4Ukraine.Converter
{
	public class BoolToGridRowHeightConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((bool)value == true) ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
