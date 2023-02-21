using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrintAndScan4Ukraine.Model
{
	public class Package : INotifyPropertyChanged
	{
        public int? Id { get; set; }
		public int PackageId { get; set; }
		public string? Sender_Name { get; set; } = string.Empty;
		public string? Sender_Address { get; set; } = string.Empty;
		public string? Sender_Phone { get; set; } = string.Empty;
		public string? Recipient_Name { get; set; } = string.Empty;
		public string? Recipient_Address { get; set; } = string.Empty;
		public string? Recipient_Phone { get; set; } = string.Empty;
		public double? Weight { get; set; }
		private double? _Cost = 0;
		public double? Cost
		{
			get => _Cost;
			set {
				_Cost = value;
				CalcTotal();
				RaisePropertyChanged();
			}
		}
		private double? _Delivery = 0;
		public double? Delivery
		{
			get => _Delivery;
			set {
				_Delivery = value;
				CalcTotal();
				RaisePropertyChanged();
			}
		}
		private double? _Insurance = 0;
		public double? Insurance
		{
			get => _Insurance;
			set {
				_Insurance = value;
				CalcTotal();
				RaisePropertyChanged();
			}
		}
		private double? _Other = 0;
		public double? Other
		{
			get => _Other;
			set {
				_Other = value;
				CalcTotal();
				RaisePropertyChanged();
			}
		}
		public string? Contents { get; set; } = string.Empty;
		private List<Contents> _recipient_contents = new() { };

		public List<Contents> Recipient_Contents
		{
			get => _recipient_contents;
			set {
				if (value == null)
					_recipient_contents = new List<Contents>();
				else
					_recipient_contents = value;
				RaisePropertyChanged();
			}
		}

		public DateTime? Date_Shipped { get; set; }
		public DateTime Date_Added { get; set; }
		private double? _Total = 0;
		public double? Total
		{
			get => _Total;
			set {
				_Total = value;
				RaisePropertyChanged();
			}
		}

		public bool Removed { get; set; } = false;

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void CalcTotal() => Total = (double)(Cost! + Delivery! + Insurance! + Other!);
	}

	public class Package_less
	{
		public int PackageId { get; set; }
		public string? Sender_Name { get; set; } = string.Empty;
		public string? Sender_Address { get; set; } = string.Empty;
		public string? Sender_Phone { get; set; } = string.Empty;
		public string? Recipient_Name { get; set; } = string.Empty;
		public string? Recipient_Address { get; set; } = string.Empty;
		public string? Recipient_Phone { get; set; } = string.Empty;
		public string? Contents { get; set; } = string.Empty;
		public double? Weight { get; set; }
		public double? Cost { get; set; }
		public double? Delivery { get; set; }
		public double? Insurance { get; set; }
		public double? Other { get; set; }
		public DateTime? Date_Shipped { get; set; }
		public DateTime Date_Added { get; set; }
		public double Total { get; set; }
	}

	public class Contents
	{
		public string Name { get; set; } = string.Empty;
		public int Amount { get; set; }
	}
}
