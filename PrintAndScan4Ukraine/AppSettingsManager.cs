using System;
using System.IO;
using System.Text.Json;

namespace PrintAndScan4Ukraine
{
    internal static class AppSettingsManager
    {
        private static readonly string AppSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        private class AppSettings
        {
            public string? Language { get; set; }
        }

        public static string? GetLanguage()
        {
            try
            {
                if (!File.Exists(AppSettingsPath))
                    return null;
                var json = File.ReadAllText(AppSettingsPath);
                if (string.IsNullOrWhiteSpace(json))
                    return null;
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings?.Language;
            }
            catch
            {
                return null;
            }
        }

        public static void SetLanguage(string? language)
        {
            try
            {
                var settings = new AppSettings { Language = language };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AppSettingsPath, json);
            }
            catch
            {
                // swallow
            }
        }
    }
}
