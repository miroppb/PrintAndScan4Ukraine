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
		Print = 512
	}

	public class Users
	{
        public int Id { get; set; }
		public string ComputerName { get; set; } = string.Empty;
		public int Access { get; set; }
		public string Comment { get; init; } = string.Empty;
    }
}
