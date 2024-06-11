using Dapper.Contrib.Extensions;
using System;

namespace PrintAndScan4Ukraine.Model
{
	[Flags]
	public enum Access : short
	{
		None,
		SeePackages,
		EditSender,
		EditReceipient = 4,
		SeeSender = 8,
		AddNew = 16,
		Ship = 32,
		Arrive = 64,
		Deliver = 128,
		Export = 256,
		Print = 512,
		EditPackageID = 1024
	}

	[Table(Secrets.MySqlUserAccessTable)]
	public class Users
	{
        public int Id { get; set; }
		public string ComputerName { get; set; } = string.Empty;
		public Access Access { get; set; }
		public string Comment { get; init; } = string.Empty;
		public string LastConnectedVersion { get; set; } = string.Empty;
		public string Lang { get; set; } = string.Empty;
    }
}
