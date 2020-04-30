using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;

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

        public string[] ProtocolKeys
        {
            get => _settings.ProtocolKeys;
            set
            {
                if (_settings.ProtocolKeys == value) return;
                _settings.ProtocolKeys = value;
                _settings.Save();
            }
        }

        public Dictionary<string, string> ExtraHttpHeaders
        {
            get => _settings.ExtraHttpHeaders ?? (_settings.ExtraHttpHeaders = new Dictionary<string, string>());
            set
            {
                if (_settings.ExtraHttpHeaders == value) return;
                _settings.ExtraHttpHeaders = value;
                _settings.Save();
            }
        }

        public UserSettings()
        {
            try
            {
                if (File.Exists("Settings.json"))
                {
                    var fileText = File.ReadAllText("Settings.json");
                    if (JsonConvert.DeserializeObject(fileText) is JObject json) _settings = JsonConvert.DeserializeObject<SettingsCache>(json.ToString());
                    return;
                }
            }
            catch
            {
                //ignore
            }
            _settings = new SettingsCache();
            _settings.Save();
        }

        private class SettingsCache
        {
            public bool AutoReconnect;
            public bool AutoEvolve = true;
            public string LastScript;
            public string[] ProtocolKeys = new string[2] {
                "zzbjtdn2hdsfgsfcvbaegfsdafsss3tasdgta1235sdfz5b2", // kg1
                "zzbysdasfsgdsgfadfhdfrsadfs4easdfasdadadsgtz5b2" // kg2
            };
            public Dictionary<string, string> ExtraHttpHeaders = new Dictionary<string, string>();

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
