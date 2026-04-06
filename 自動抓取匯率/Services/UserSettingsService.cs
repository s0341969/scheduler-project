using System;
using System.IO;
using System.Xml.Serialization;
using BotExchangeRateWinForms.Models;

namespace BotExchangeRateWinForms.Services
{
    /// <summary>
    /// 負責將畫面設定序列化到本機 XML，並在啟動時載入。
    /// </summary>
    public sealed class UserSettingsService
    {
        private readonly string _settingsDirectory;
        private readonly string _settingsFilePath;

        /// <summary>
        /// 建立設定檔儲存路徑。
        /// </summary>
        public UserSettingsService()
        {
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BotExchangeRateWinForms");
            _settingsFilePath = Path.Combine(_settingsDirectory, "user-settings.xml");
        }

        /// <summary>
        /// 讀取本機設定；若檔案不存在或格式錯誤，回傳預設值。
        /// </summary>
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
                    if (settings == null)
                    {
                        return UserSettings.CreateDefault();
                    }

                    if (settings.WriteToDatabase && !settings.WriteChrname && !settings.WriteChrnameHistory)
                    {
                        settings.WriteChrname = true;
                        settings.WriteChrnameHistory = true;
                    }

                    return settings;
                }
            }
            catch
            {
                return UserSettings.CreateDefault();
            }
        }

        /// <summary>
        /// 將目前設定寫入本機 XML 檔案。
        /// </summary>
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
