using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PromptPalette
{
    [DataContract]
    internal sealed class AppSettings
    {
        public AppSettings()
        {
            AccentArgb = Theme.DefaultAccent.ToArgb();
        }

        [DataMember(Order = 1)]
        public int AccentArgb { get; set; }

        [DataMember(Order = 2)]
        public bool FavoriteFirst { get; set; }

        [DataMember(Order = 3)]
        public bool ShowAdult { get; set; }

        public Color Accent
        {
            get
            {
                Color color = Color.FromArgb(AccentArgb);
                return AccentArgb == 0 ? Theme.DefaultAccent : color;
            }
        }
    }

    internal static class SettingsStore
    {
        public static string DataFile
        {
            get
            {
                string directory = Path.GetDirectoryName(PromptStore.DataFile);
                return Path.Combine(directory, "settings.json");
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(DataFile)) return new AppSettings();
                using (FileStream stream = File.OpenRead(DataFile))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(AppSettings));
                    AppSettings settings = (AppSettings)serializer.ReadObject(stream);
                    if (settings == null || settings.AccentArgb == 0) return new AppSettings();
                    return settings;
                }
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            if (settings == null) return;
            string directory = Path.GetDirectoryName(DataFile);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string temp = DataFile + ".tmp";
            using (FileStream stream = File.Create(temp))
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(AppSettings));
                serializer.WriteObject(stream, settings);
            }
            File.Copy(temp, DataFile, true);
            File.Delete(temp);
        }
    }
}
