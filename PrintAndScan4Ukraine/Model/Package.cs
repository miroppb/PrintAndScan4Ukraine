using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrintAndScan4Ukraine.Model
{
	public class Package : INotifyPropertyChanged
	{
		public int Id { get; set; }
		public string PackageId { get; set; } = string.Empty;

		private string _Sender_Name = string.Empty;

		public string Sender_Name
		{
			get => _Sender_Name;
			set
			{
				if (_Sender_Name != string.Empty && _Sender_Name != value)
					Modified = true;

				_Sender_Name = value;
				RaisePropertyChanged();
			}
		}


		private string _Sender_Address = string.Empty;

		public string Sender_Address
		{
			get => _Sender_Address;
			set
			{
				if (_Sender_Address != string.Empty && _Sender_Address != value)
					Modified = true;

				_Sender_Address = value;
				RaisePropertyChanged();
			}
		}


		private string _Sender_Phone = string.Empty;

		public string Sender_Phone
		{
			get => _Sender_Phone;
			set
			{
				if (_Sender_Phone != string.Empty && _Sender_Phone != value)
					Modified = true;

				_Sender_Phone = value;
				RaisePropertyChanged();
			}
		}

		private string? _Recipient_Name = string.Empty;

		public string? Recipient_Name
		{
			get => _Recipient_Name;
			set
			{
				if (_Recipient_Name != string.Empty && _Recipient_Name != value)
					Modified = true;

				_Recipient_Name = value;
				RaisePropertyChanged();
			}
		}

		private string? _Recipient_Address = string.Empty;

		public string? Recipient_Address
		{
			get => _Recipient_Address;
			set
			{
				if (_Recipient_Address != string.Empty && _Recipient_Address != value)
					Modified = true;

				_Recipient_Address = value;
				RaisePropertyChanged();
			}
		}

		private string? _Recipient_Phone = string.Empty;

		public string? Recipient_Phone
		{
			get => _Recipient_Phone;
			set
			{
				if (_Recipient_Phone != string.Empty && _Recipient_Phone != value)
					Modified = true;

				_Recipient_Phone = value;
				RaisePropertyChanged();
			}
		}

		private double? _Weight = null;

		public double? Weight
		{
			get => _Weight;
			set
			{
				if (_Weight != null && _Weight != value)
					Modified = true;

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
				if (_Value != null && _Value != value)
					Modified = true;

				_Value = value;
				if (value != null)
					Total = ((double)value!).ToString("$0.00");
				RaisePropertyChanged();
			}
		}
		public string? Contents { get; set; } = string.Empty;
		private List<Contents> _recipient_contents = new() { };

		[Write(false)]
		[Computed]
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

		private string? _Total = "$0.00";

		[Write(false)]
		[Computed]
		public string? Total
		{
			get => _Total;
			set
			{
				_Total = value;
				RaisePropertyChanged();
			}
		}


		private bool _Delivery;

		public bool Delivery
		{
			get => _Delivery;
			set
			{
				_Delivery = value;
				RaisePropertyChanged();
			}
		}


		public bool Removed { get; set; } = false;

		[Write(false)]
		[Computed]
		public bool Modified { get; internal set; } = false;

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

		private string _Delivery = string.Empty;
		[Description("Доставка")]
		public string? Delivery
		{
			get => _Delivery;
			set
			{
				if (value != null)
				{
					_Delivery = (value.ToLower() == "true" ? "✔" : "");
				}
			}
		}
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
		[Description("Статусы")]
		public string? Statuses { get; set; }
	}

	public class Contents
	{
		public string Name { get; set; } = string.Empty;
		public int Amount { get; set; }
	}
}
