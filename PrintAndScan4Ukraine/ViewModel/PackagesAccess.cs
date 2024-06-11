using PrintAndScan4Ukraine.Model;
using System.ComponentModel;
using System.Windows.Controls;

namespace PrintAndScan4Ukraine.ViewModel
{
	public partial class PackagesViewModel : INotifyPropertyChanged
	{
		public bool AccessToSeePackages => CurrentUser.Access.HasFlag(Access.SeePackages);
		public bool AccessToSeeSender => CurrentUser.Access.HasFlag(Access.SeeSender);
		public bool AccessToEditSender => !CurrentUser.Access.HasFlag(Access.EditSender); //needs to be opposite :shrug:
		public bool AccessToEditReceipient => !CurrentUser.Access.HasFlag(Access.EditReceipient);
		public bool AccessToAddNew => CurrentUser.Access.HasFlag(Access.AddNew);
		public bool AccessToShip => CurrentUser.Access.HasFlag(Access.Ship);
		public bool AccessToArrive => CurrentUser.Access.HasFlag(Access.Arrive);
		public bool AccessToDeliver => CurrentUser.Access.HasFlag(Access.Deliver);
		public bool AccessToExport => CurrentUser.Access.HasFlag(Access.Export);

		public bool CanSave => SelectedPackage != null && IsOnline && SelectedPackage.PackageIdValid;
		public bool CanShowHistory => SelectedPackage != null && IsOnline;
		public bool CanShip => CurrentUser.Access.HasFlag(Access.Ship) && IsOnline;
		public bool CanAddNew => CurrentUser.Access.HasFlag(Access.AddNew) && IsOnline;
		public bool CanArrive => CurrentUser.Access.HasFlag(Access.Arrive) && IsOnline;
		public bool CanDeliver => CurrentUser.Access.HasFlag(Access.Deliver) && IsOnline;
		public bool CanEditPackageID => CurrentUser.Access.HasFlag(Access.EditPackageID) && IsOnline;
	}
}
