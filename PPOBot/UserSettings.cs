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
            get
            {
                if (_settings.ProtocolKeys == null || _settings.ProtocolKeys.Length <= 0)
                {
                    _settings.ProtocolKeys = new[] 
                    {
                        "zzbjtdn2hdsfgsfcvbaegfsdafsss3tasdgta1235sdfz5", // kg1
                        "zzbysdasfsgdsgfadfhdfrsadfs4easdfasdadadsgtz5" // kg2
                    };
                    _settings.Save();
                }
                return _settings.ProtocolKeys;
            }
            set
            {
                if (_settings.ProtocolKeys == value) return;
                _settings.ProtocolKeys = value;
                _settings.Save();
            }
        }

        public Dictionary<string, string> ExtraHttpHeaders
        {
            get
            {
                if (_settings.ExtraHttpHeaders == null || _settings.ExtraHttpHeaders.Count <= 0)
                {
                    _settings.ExtraHttpHeaders = new Dictionary<string, string>()
                    {
                        /*{ "Cookie", "__cfduid=d9a043d8b9b2392c5bae537410cb8afb11581154517; cf_clearance=ecd4cfa25be6117e98cf49d1ce5eab6a4ec1d454-1581157110-0-250" },
                        { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.79 Safari/537.36" }*/
                    };
                    _settings.Save();
                }
                return _settings.ExtraHttpHeaders;
            }
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
            public string[] ProtocolKeys;
            public Dictionary<string, string> ExtraHttpHeaders;

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
