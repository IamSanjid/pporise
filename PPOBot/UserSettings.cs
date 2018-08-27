using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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

        public string Versions => _settings.Versions;
        public string ProtocolKeys => _settings.ProtocolKeys;
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
            public string Versions = "game593.swf:163"; //Default value...
            public string ProtocolKeys = "ksdfih9230din2ndosf0asdbnfhasdf2431hbasdfm:k6m2nmsb6kgyfu7ibast1ggf02n9dujhid40zplpghm0cjl"; // I actually don't know what to call it but it is needed for protocol stuffs...
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