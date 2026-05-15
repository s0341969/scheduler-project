using System;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using AutoBubble.Core.Configuration;

namespace AutoBubble.AutoCAD.Services
{
    internal sealed class SettingsLoader
    {
        public AutoBubbleSettings Load()
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var baseDirectory = Path.GetDirectoryName(assemblyPath);
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                return AutoBubbleSettings.Sanitize(new AutoBubbleSettings());
            }

            var settingsPath = Path.Combine(baseDirectory, "autobubble.settings.json");
            if (!File.Exists(settingsPath))
            {
                return AutoBubbleSettings.Sanitize(new AutoBubbleSettings());
            }

            try
            {
                var json = File.ReadAllText(settingsPath);
                var serializer = new JavaScriptSerializer();
                var settings = serializer.Deserialize<AutoBubbleSettings>(json);
                return AutoBubbleSettings.Sanitize(settings);
            }
            catch
            {
                return AutoBubbleSettings.Sanitize(new AutoBubbleSettings());
            }
        }
    }
}
