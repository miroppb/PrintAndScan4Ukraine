using PrintAndScan4Ukraine.Data;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PrintAndScan4Ukraine.Converter
{
	public class StatusToTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int)
			{
				return APIPackageDataProvider.StatusToText((int)value);
			}
			else
				return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
