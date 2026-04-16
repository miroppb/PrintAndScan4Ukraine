namespace PrintAndScan4Ukraine.Model
{
    public class PackageGroupHeader
    {
        public string SenderName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public int Count { get; set; }

        public string Display => $"{SenderName} → {RecipientPhone}  ({Count} packages)";
    }
}
