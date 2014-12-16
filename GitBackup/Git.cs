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

        public void backup()
        {
            tryExecute("config credential.helper store");

            string status = tryExecute("status");
            string[] lines = status.Split(new char[] { '\n' });
            if (lines.Length < 4 || !lines[0].StartsWith("On branch ")) return;
            string originalBranch = lines[0].Substring(10);

            if (originalBranch.Contains("-backup-"))
            {
                try
                {
                    originalBranch = originalBranch.Substring(0, originalBranch.IndexOf("-backup-"));
                    execute("reset " + originalBranch);
                    execute("checkout " + originalBranch);
                }
                catch (GitException) { return; }
                backup();
                return;
            }

            if (!lines[3].StartsWith("Changes not staged")) return;

            try
            {
                string hash = execute("rev-parse HEAD").Substring(0, 8);
                string branch = originalBranch + "-backup-" + hash;
                try
                {
                    string create = execute("checkout " + branch);
                }
                catch (GitException)
                {
                    execute("checkout -b " + branch);
                }
                tryExecute("reset origin/" + branch);
                execute("add -A");
                execute("commit -m \"Autosave " + DateTime.Now.ToString() + "\"");
                execute("push --set-upstream origin " + branch);
            }
            catch { }
            finally
            {
                execute("reset " + originalBranch);
                execute("checkout " + originalBranch);
            }
        }

        public string tryExecute(string command)
        {
            try
            {
                return execute(command);
            }
            catch (GitException)
            {
                return "";
            }
        }

        public string execute(string command)
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

        private class GitException : Exception
        {
            public GitException(string message) : base(message) { }
        }
    }
}
