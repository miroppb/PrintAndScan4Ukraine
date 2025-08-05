using CodingSeb.Localization;
using miroppb;
using MySqlConnector;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PrintAndScan4Ukraine.ViewModel
{
	public class SearchViewModel : ClosableViewModel, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public List<string> PackagesOnList = [];

		public DelegateCommand SearchCommand { get; }
		public DelegateCommand RadioSearchCheckedCommand { get; }
		public DelegateCommand CloseCommand { get; }

		public SearchViewModel()
		{
			SearchCommand = new DelegateCommand(ExecuteSearchCommand, () => true);
			RadioSearchCheckedCommand = new DelegateCommand(ExecuteRadioSearchCheckedCommand, () => true);
			CloseCommand = new DelegateCommand(ExecuteCloseCommand, () => true);
		}

		private void ExecuteRadioSearchCheckedCommand(object obj)
		{
			if (obj as string == "Package")
				WhatAreWeSearchingFor = SearchFor.PackageID;
			else
				WhatAreWeSearchingFor = SearchFor.Name;
		}

		private async void ExecuteSearchCommand(object obj)
		{
			bool Generating = true;
			//search for item(s) with DataProvider
			IPackageDataProvider _provider = new APIPackageDataProvider(new ApiService(Secrets.ApiKey));
			using MySqlConnection conn = Secrets.GetConnectionString();
			PreviousShipments.Clear();
			if (WhatAreWeSearchingFor == SearchFor.PackageID)
			{
				Libmiroppb.Log($"Searching for Package: {SearchParam}");
				TopText = $"Search Results for Package: {SearchParam}";
				var temp_packages = await _provider.GetPackageAsync(SearchParam, ArchiveChecked);
				var statuses = await _provider.GetStatusByPackage(SearchParam);
				if (temp_packages != null && statuses != null)
					PreviousShipments.AddDistinctRange(_provider.MapPackagesAndStatusesToLess(temp_packages, statuses));
				else if (temp_packages == null && statuses != null) //no package but statuses exist
				{
					List<Package_Status> s = statuses.Where(s => s.PackageId == SearchParam).ToList();
					List<string> so = new();
					s.ForEach(x => so.Add(x.ToString()));
					string status_string = so.Join(Environment.NewLine);

					MessageBox.Show(string.Format($"{Loc.Tr("PAS4U.SearchSelectionWindow.NoIDButStatus", "{0} isn't in the database, but there statuses:{1}")}",
						SearchParam, $"{Environment.NewLine}{Environment.NewLine}{status_string}"));
					Generating = false;
				}
			}
			else
			{
				Libmiroppb.Log($"Searching by Sender: {SearchParam}");
				TopText = $"Search Results for Sender: {SearchParam}";
				var temp_packages = await _provider.GetByNameAsync(SearchParam, ArchiveChecked);
				if (temp_packages != null && temp_packages.Any())
				{
					var statuses = await _provider.GetAllStatuses(temp_packages.Select(x => x.PackageId).ToList());
					if (statuses != null)
						PreviousShipments.AddDistinctRange(_provider.MapPackagesAndStatusesToLess(temp_packages, statuses));
					else
						MessageBox.Show($"{Loc.Tr("PAS4U.SearchSelectionWindow.Error", "An unexpected error occused. Please try again.")}");
				}
				else
				{
					MessageBox.Show(string.Format($"{Loc.Tr("PAS4U.SearchSelectionWindow.NoPackages", "{0} has no packages in the database")}", SearchParam));
					Generating = false;
				}
			}
			if (Generating)
			{
				Libmiroppb.Log($"Results found: {JsonConvert.SerializeObject(PreviousShipments)}");
				for (int a = 0; a < PreviousShipments.Count; a++)
				{
					int sender_length = PreviousShipments[a].Sender_Name!.Length;
					PreviousShipments[a].Recipient_Name = $"{PreviousShipments[a].Sender_Name}: {PreviousShipments[a].Recipient_Name}";
					PreviousShipments[a].Sender_Phone = $"{PreviousShipments[a].Sender_Phone!.PadRight(sender_length)}    :     {PreviousShipments[a].Recipient_Phone}";
				}

				//open Search Window with options
				SearchWindow searchWindow = new(this);
				searchWindow.ShowDialog();
			}
		}

		public void ExecuteCloseCommand(object obj)
		{
			OnClosingRequest();
		}

		private ObservableCollection<Package_less> _previousShipments = new();

		public ObservableCollection<Package_less> PreviousShipments
		{
			get => _previousShipments;
			set
			{
				_previousShipments = value;
				RaisePropertyChanged();
			}
		}

		private Package_less? _SelectedShipment = null;

		public Package_less? SelectedShipment
		{
			get => _SelectedShipment;
			set
			{
				_SelectedShipment = value;
				RaisePropertyChanged();
			}
		}

		private string _SearchParam = string.Empty;

		public string SearchParam
		{
			get => _SearchParam;
			set
			{
				_SearchParam = value;
				RaisePropertyChanged();
			}
		}

		private SearchFor _WhatAreWeSearchingFor = 0;

		public SearchFor WhatAreWeSearchingFor
		{
			get => _WhatAreWeSearchingFor;
			set
			{
				_WhatAreWeSearchingFor = value;
				RaisePropertyChanged();
			}
		}

		public enum SearchFor
		{
			PackageID,
			Name
		}

		private string _TopText = string.Empty;

		public string TopText
		{
			get => _TopText;
			set
			{
				_TopText = value;
				RaisePropertyChanged();
			}
		}

		private bool _ArchiveChecked = false;

		public bool ArchiveChecked
		{
			get => _ArchiveChecked;
			set
			{
				_ArchiveChecked = value;
				RaisePropertyChanged();
			}
		}
	}
}
