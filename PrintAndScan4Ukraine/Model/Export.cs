using System;

namespace PrintAndScan4Ukraine.Model
{
    public class Export
    {
        public int Id { get; set; }
        public string Filename { get; set; } = string.Empty;
        public DateTime Datetime { get; set; }
        public string Content { get; set; } = string.Empty; // base64 string
    }

}
