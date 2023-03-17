using System;

namespace PrintAndScan4Ukraine.Model
{
	[Flags]
	public enum Access : short
	{
		None,
		EditSender,
		EditReceipient,
		SeeSender = 4,
		AddNew = 8,
		Ship = 16,
		Arrive = 32,
		Deliver = 64,
		Print = 128
	}

	public class Users
	{
        public int Id { get; set; }
		public string ComputerName { get; set; } = string.Empty;
		public int Access { get; set; }
		public string Comment { get; init; } = string.Empty;
    }
}
