using System;

namespace PrintAndScan4Ukraine.Model
{
	public class Package_Status
	{
		public int? Id { get; set; }
		public string PackageId { get; set; } = string.Empty;
		public DateTime CreatedDate { get; set; }
		public string Status { get; set; } = string.Empty;
		public override string ToString()
		{
			return $"{Status}: {CreatedDate}";
		}
	}
}
