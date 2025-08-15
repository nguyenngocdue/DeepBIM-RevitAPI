// DeepBIM/Helpers/SettingsManager.cs
using System;
using System.IO;
using Newtonsoft.Json;

namespace DeepBIM.Helpers
{
    public class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeepBIM", "align-settings.json");

        static SettingsManager()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
        }

        public static double GetMinGap()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonConvert.DeserializeObject<Settings>(json);
                    return settings?.Data?.minGap ?? 0 ;
                }
            }
            catch { }
            return 0; // mặc định
        }

        public static void SaveMinGap(double minGap)
        {
            var settings = new Settings { Data = new SettingsData { minGap = minGap } };
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }
    }

    public class Settings
    {
        public Settings()
        {
            Data = new SettingsData(); // Khởi tạo giá trị mặc định
        }

        public SettingsData Data { get; set; }
    }

    public class SettingsData
    {
        public double minGap { get; set; }
    }
}