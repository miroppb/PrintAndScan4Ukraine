using PrintAndScan4Ukraine.Data;
using System;

namespace PrintAndScan4Ukraine.Model
{
	public class Package_Status
	{
		public int? Id { get; set; }
		public string PackageId { get; set; } = string.Empty;
		public DateTime CreatedDate { get; set; }
		public int Status { get; set; }
		public override string ToString()
		{
			return $"{PackageDataProvider.StatusToText(Status)}: {CreatedDate.ToShortDateString()} {CreatedDate.ToShortTimeString()}";
		}
	}
}
