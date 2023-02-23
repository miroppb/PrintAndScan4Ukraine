using System;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace PrintAndScan4Ukraine.Converter
{
	public class InvertedBooleanToVisibilityConverter : IValueConverter
	{
		private object GetVisibility(object value)
		{
			if (!(value is bool))
				return Visibility.Collapsed;
			bool objValue = (bool)value;
			if (objValue)
				return Visibility.Collapsed;
			else
				return Visibility.Visible;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return GetVisibility(value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
