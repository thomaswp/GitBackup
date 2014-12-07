using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace GitBackup
{
    public class Repository
    {
        public const string SETTINGS_FILE = ".gitbackup";

        public string Path { get; set; }

        private SettingsData settings;
        public SettingsData Settings { get { return settings; } }

        private string SettingsPath { get { return Path + SETTINGS_FILE; } }

        public string Name { get { return settings.Name; } }

        public readonly Git Git;

        public Repository(string path)
        {
            if (!path.EndsWith("\\")) path += "\\";
            Git = new Git(path);
            this.Path = path;
            string settingsPath = path + SETTINGS_FILE;
            if (File.Exists(settingsPath))
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<SettingsData>(File.ReadAllText(settingsPath));
                }
                catch { }
            }

            if (settings == null)
            {
                settings = new Repository.SettingsData();
                string[] parts = path.Split('\\');
                settings.Name = parts[parts.Length - 2];
            }
        }

        public bool Save()
        {
            try
            {
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(settings));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public class SettingsData
        {
            public string Name { get; set; }
            public bool Active { get; set; }
            public int Interval { get; set; }
            public List<String> IgnoredBranches { get; set; }

            public SettingsData()
            {
                Name = "";
                Active = true;
                Interval = 5;
                IgnoredBranches = new List<string>();
            }
        }
    }
}
