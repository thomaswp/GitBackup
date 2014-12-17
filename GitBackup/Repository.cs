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

        public bool CheckModified()
        {
            try
            {
                string data = JsonConvert.SerializeObject(settings);
                string saved = File.ReadAllText(SettingsPath);
                return data != saved;
            }
            catch { }
            return true;
        }

        public bool CanBackup()
        {
            string status = Git.TryExecute("status");
            string[] lines = status.Split(new char[] { '\n' });
            if (lines.Length < 4 || !lines[0].StartsWith("On branch ")) return false;
            string originalBranch = lines[0].Substring(10);
            if (originalBranch.Contains("-backup-")) return false;
            if (!lines[3].StartsWith("Changes not staged")) return false;

            return true;
        }

        public bool Backup()
        {
            if (Git.GitPath == null) return false;

            Git.TryExecute("config credential.helper store");


            string status = Git.TryExecute("status");
            string[] lines = status.Split(new char[] { '\n' });
            if (lines.Length < 4 || !lines[0].StartsWith("On branch ")) return false;
            string originalBranch = lines[0].Substring(10);

            if (originalBranch.Contains("-backup-"))
            {
                try
                {
                    originalBranch = originalBranch.Substring(0, originalBranch.IndexOf("-backup-"));
                    Git.Execute("reset " + originalBranch);
                    Git.Execute("checkout " + originalBranch);
                }
                catch (GitException) { return false; }
                return Backup();
            }

            if (!lines[3].StartsWith("Changes not staged")) return true;

            try
            {
                string hash = Git.Execute("rev-parse HEAD").Substring(0, 8);
                string branch = originalBranch + "-backup-" + hash;
                try
                {
                    string create = Git.Execute("checkout " + branch);
                }
                catch (GitException)
                {
                    Git.Execute("checkout -b " + branch);
                }
                Git.TryExecute("reset origin/" + branch);
                Git.Execute("add -A");
                Git.Execute("commit -m \"Autosave " + DateTime.Now.ToString() + "\"");
                Git.Execute("push --set-upstream origin " + branch);
            }
            catch
            {
                return false;
            }
            finally
            {
                Git.Execute("reset " + originalBranch);
                Git.Execute("checkout " + originalBranch);
            }

            return true;
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
