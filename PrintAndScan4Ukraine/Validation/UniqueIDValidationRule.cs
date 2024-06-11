using PrintAndScan4Ukraine.Data;
using System.Globalization;
using System.Windows.Controls;

namespace PrintAndScan4Ukraine
{
	public class UniqueIDValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (value is string newId)
			{
				IPackageDataProvider _provider = new PackageDataProvider();
				if (_provider.VerifyIfExists(newId))
					return new ValidationResult(false, "ID already exists.");
			}

			return ValidationResult.ValidResult;
		}
	}
}
