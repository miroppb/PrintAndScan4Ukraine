using Dapper.Contrib.Extensions;
using PrintAndScan4Ukraine.Data;
using System;

namespace PrintAndScan4Ukraine.Model
{
	[Table(Secrets.MySqlPackageStatusTable)]
	public class Package_Status
	{
		[Key]
		public int? Id { get; set; }
		public int? Createdbyuser { get; set; }
		public string PackageId { get; set; } = string.Empty;
		public DateTime CreatedDate { get; set; }
		public int Status { get; set; }
        public override string ToString() => $"{APIPackageDataProvider.StatusToText(Status)}: {CreatedDate.ToShortDateString()} {CreatedDate.ToShortTimeString()}";
    }
}
