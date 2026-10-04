using System;
using System.IO;
using System.Text.Json;

namespace DesktopCharacterPet.Models
{
    public class AppSettings
    {
        public double CharacterWidth { get; set; } = 320.0;
        public double CharacterHeight { get; set; } = 192.0;
        public double MovementSpeed { get; set; } = 2.5;
        public int UpdateIntervalMs { get; set; } = 20;
        public double? StartX { get; set; } = null;
        public double? StartY { get; set; } = null;
        public bool AutoStart { get; set; } = true;
        public string GifPath { get; set; } = "Assets/ironman_vs_thanos.gif";
        public string BattlePath { get; set; } = "AutoCycleAll";

        private static readonly string SettingsFileName = "settings.json";

        public static AppSettings Load()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
                // Fall back to default settings on error
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // Ignore save errors in restricted environments
            }
        }
    }
}
