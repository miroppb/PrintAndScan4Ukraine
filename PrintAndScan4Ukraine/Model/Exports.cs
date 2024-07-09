using System;

namespace PrintAndScan4Ukraine.Model
{
	public class Exports
	{
		public int Id { get; set; }
		public string Filename { get; set; } = string.Empty;
		public DateTime Datetime { get; set; }
		public byte[] Content { get; set; } = Array.Empty<byte>();
	}
}
