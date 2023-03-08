using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrintAndScan4Ukraine.Model
{
	public class Package : INotifyPropertyChanged
	{
		public int? Id { get; set; }
		public string PackageId { get; set; } = string.Empty;
		public string? Sender_Name { get; set; } = string.Empty;
		public string? Sender_Address { get; set; } = string.Empty;
		public string? Sender_Phone { get; set; } = string.Empty;
		private string? _recipient_Name = string.Empty;

		public string? Recipient_Name
		{
			get => _recipient_Name;
			set
			{
				_recipient_Name = value;
				RaisePropertyChanged();
			}
		}

		private string? _recipient_Address = string.Empty;

		public string? Recipient_Address
		{
			get => _recipient_Address;
			set
			{
				_recipient_Address = value;
				RaisePropertyChanged();
			}
		}

		private string? _recipient_Phone = string.Empty;

		public string? Recipient_Phone
		{
			get => _recipient_Phone;
			set
			{
				_recipient_Phone = value;
				RaisePropertyChanged();
			}
		}


		private double? _Weight = null;

		public double? Weight
		{
			get => _Weight;
			set
			{
				_Weight = value;
				RaisePropertyChanged();
			}
		}


		private double? _Value = null;
		public double? Value
		{
			get => _Value;
			set
			{
				_Value = value;
				Total = ((double)value!).ToString("$0.00");
				RaisePropertyChanged();
			}
		}
		public string? Contents { get; set; } = string.Empty;
		private List<Contents> _recipient_contents = new() { };

		public List<Contents> Recipient_Contents
		{
			get => _recipient_contents;
			set
			{
				if (value == null)
					_recipient_contents = new List<Contents>();
				else
					_recipient_contents = value;
				RaisePropertyChanged();
			}
		}

		public DateTime? Date_Shipped { get; set; }
		public DateTime Date_Added { get; set; }
		private string? _Total = "$0.00";
		public string? Total
		{
			get => _Total;
			set
			{
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

		//private void CalcTotal() => Total = ((double)(Value! + Delivery! + Insurance! + Other!)).ToString("$0.00");
	}

	public class Package_less
	{
		public string PackageId { get; set; } = string.Empty;
		public string? Sender_Name { get; set; } = string.Empty;
		public string? Sender_Address { get; set; } = string.Empty;
		public string? Sender_Phone { get; set; } = string.Empty;
		[Description("Имя Получателя")]
		public string? Recipient_Name { get; set; } = string.Empty;
		[Description("Адрес Получателя")]
		public string? Recipient_Address { get; set; } = string.Empty;
		[Description("Телефон Получателя")]
		public string? Recipient_Phone { get; set; } = string.Empty;
		[Description("Наименование Вложений/Количество")]
		public string? Contents { get; set; } = string.Empty;
		[Description("Общий Вес")]
		public double? Weight { get; set; }
		[Description("Ценность")]
		public double? Value { get; set; }
		public DateTime? Date_Shipped { get; set; }
		public DateTime Date_Added { get; set; }
	}

	public class Contents
	{
		public string Name { get; set; } = string.Empty;
		public int Amount { get; set; }
	}
}
