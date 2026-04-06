using System;
using System.IO;
using System.Xml.Serialization;
using BotExchangeRateWinForms.Models;

namespace BotExchangeRateWinForms.Services
{
    public sealed class UserSettingsService
    {
        private readonly string _settingsDirectory;
        private readonly string _settingsFilePath;

        public UserSettingsService()
        {
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BotExchangeRateWinForms");
            _settingsFilePath = Path.Combine(_settingsDirectory, "user-settings.xml");
        }

        public UserSettings Load()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    return UserSettings.CreateDefault();
                }

                using (var stream = File.OpenRead(_settingsFilePath))
                {
                    var serializer = new XmlSerializer(typeof(UserSettings));
                    var settings = serializer.Deserialize(stream) as UserSettings;
                    return settings ?? UserSettings.CreateDefault();
                }
            }
            catch
            {
                return UserSettings.CreateDefault();
            }
        }

        public void Save(UserSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            Directory.CreateDirectory(_settingsDirectory);

            using (var stream = File.Create(_settingsFilePath))
            {
                var serializer = new XmlSerializer(typeof(UserSettings));
                serializer.Serialize(stream, settings);
            }
        }
    }
}
