﻿using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.ViewModel
{
	public class MainViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

        
		private readonly IMainDataProvider _mainDataProvider;

		public DelegateCommand PrintCommand { get; }
		public DelegateCommand ScanCommand { get; }

		public MainViewModel(IMainDataProvider mainDataProvider)
		{
			_mainDataProvider = mainDataProvider;
			PrintCommand = new DelegateCommand(ClickPrint, () => CanPrint);
			ScanCommand = new DelegateCommand(ClickScan, () => CanScan);
		}

		private void ClickScan()
		{
			isVisible = false;
			ScanWindow win = new ScanWindow(CurrentUserAccess);
			win.ShowDialog();
			isVisible = true;
		}

		private void ClickPrint()
		{
			throw new NotImplementedException();
		}

		private bool _IsOnline = true;
		public bool IsOnline
		{
			get => _IsOnline;
			set
			{
				_IsOnline = value;
				RaisePropertyChanged();
			}
		}


		private bool _isVisible = true;

		public bool isVisible
		{
			get => _isVisible;
			set
			{
				_isVisible = value;
				RaisePropertyChanged();
			}
		}


		private Access _CurrentUserAccess = Access.None;

		public Access CurrentUserAccess
		{
			get => _CurrentUserAccess;
			set
			{
				_CurrentUserAccess = value;
				PrintCommand.RaiseCanExecuteChanged();
				ScanCommand.RaiseCanExecuteChanged();
				RaisePropertyChanged();
			}
		}

		public bool CanPrint => CurrentUserAccess.HasFlag(Access.Print);

		public bool CanScan => CurrentUserAccess.HasFlag(Access.SeeSender | Access.EditSender | Access.EditReceipient | Access.AddNew | Access.Ship | Access.Deliver | Access.Arrive);

		public async Task GetAccess()
		{
			if (IsOnline)
				CurrentUserAccess = await _mainDataProvider.GetAccessFromComputerNameAsync(Environment.MachineName);
			else
				CurrentUserAccess = Access.None;
		}

	}
}