using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace GitBackup
{
    public class Git
    {
        public static string GitPath { get; set; }

        public static bool FindGitPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string github = appData + "\\GitHub";
            if (Directory.Exists(github))
            {
                foreach (string folder in Directory.EnumerateDirectories(github))
                {
                    string dirName = Path.GetFileName(folder);
                    if (dirName.StartsWith("PortableGit"))
                    {
                        string git = folder + "\\bin\\";
                        if (File.Exists(git + "git.exe"))
                        {
                            GitPath = git;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private string dir;

        public Git(string dir)
        {
            this.dir = dir;
        }

        public string TryExecute(string command)
        {
            try
            {
                return Execute(command);
            }
            catch (GitException)
            {
                return "";
            }
        }

        public string Execute(string command)
        {
            if (GitPath == null) return "";
            Process p = new Process();
            p.StartInfo.FileName = GitPath + "git.exe";
            p.StartInfo.Arguments = command;
            p.StartInfo.WorkingDirectory = dir;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            string message = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();
            if (message.Length == 0 && error.Length > 0) throw new GitException(error);
            return message;
        }
    }

    public class GitException : Exception
    {
        public GitException(string message) : base(message) { }
    }
}
