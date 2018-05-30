﻿using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace PPOBot
{
    public class UserSettings
    {
        private readonly SettingsCache _settings;

        public bool AutoReconnect
        {
            get => _settings.AutoReconnect;
            set
            {
                if (_settings.AutoReconnect == value) return;
                _settings.AutoReconnect = value;
                _settings.Save();
            }
        }

        public bool AutoEvolve
        {
            get => _settings.AutoEvolve;
            set
            {
                if (_settings.AutoEvolve == value) return;
                _settings.AutoEvolve = value;
                _settings.Save();
            }
        }

        public string LastScript
        {
            get => _settings.LastScript;
            set
            {
                if (_settings.LastScript == value) return;
                _settings.LastScript = value;
                _settings.Save();
            }
        }

        public UserSettings()
        {
            try
            {
                if (File.Exists("Settings.json"))
                {
                    string fileText = File.ReadAllText("Settings.json");
                    if (JsonConvert.DeserializeObject(fileText) is JObject json) _settings = JsonConvert.DeserializeObject<SettingsCache>(json.ToString());
                    return;
                }
            }
            catch {
                //ignore
                  }
            _settings = new SettingsCache();
        }

        private class SettingsCache
        {
            public bool AutoReconnect;
            public bool AutoEvolve = true;
            public string LastScript;

            public void Save()
            {
                var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.Indented
                });
                File.WriteAllText("Settings.json", json);
            }
        }
    }
}