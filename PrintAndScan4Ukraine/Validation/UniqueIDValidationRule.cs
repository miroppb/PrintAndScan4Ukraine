using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.ViewModel;
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
				if (PackagesViewModel.StaticSelectedPackage?.PackageId != newId && _provider.VerifyIfExists(newId))
					return new ValidationResult(false, "ID already exists.");
			}

			return ValidationResult.ValidResult;
		}
	}
}
