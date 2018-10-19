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
            public string Versions = "game628.swf:163"; //Default value...
            // lilililililililllililililililliilillliliililiiiiiilllliililil;
            private string kg1
            {
                get { return "25basdhgfoiusdfgasdfdo89uifgasdilfgvs9231gfoiugbv3dsfh4"; }
            }

            private string kg2(string FirstLi, string SecondLi)
            {
                return "25h678yr9e32yfdsdhfgf8d32iu1dgikasjgvbasdkfuvh34w" + FirstLi + SecondLi;
            }

            public string ProtocolKeys
            {
                get
                {
                    return kg1 + ":" + kg2("lilililililililllililililililliilillliliililiiiiiilllliililil",
                               "illililililillililililililliiiiilililililillililililillilililililiililililililililililililililili");
                }
            }// I actually don't know what to call it but it is needed for protocol stuff...
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