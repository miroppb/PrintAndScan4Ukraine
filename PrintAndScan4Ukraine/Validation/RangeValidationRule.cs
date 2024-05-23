using System.Globalization;
using System.Windows.Controls;

namespace PrintAndScan4Ukraine
{
	public class RangeValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (int.TryParse(value?.ToString(), out int intValue))
			{
				if (intValue < 1000000 || intValue > 9999999)
				{
					return new ValidationResult(false, "Value must be between 1000000 and 9999999.");
				}
			}
			else
			{
				return new ValidationResult(false, "Invalid input.");
			}
			return ValidationResult.ValidResult;
		}
	}
}
