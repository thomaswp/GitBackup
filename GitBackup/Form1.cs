using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace GitBackup
{
    public partial class MainForm : Form
    {
        private const string GIT_PATH = @"C:\Users\Thomas\AppData\Local\GitHub\PortableGit_ed44d00daa128db527396557813e7b68709ed0e2\bin\";
        private const string DIR = @"C:\Users\Thomas\Documents\GitHub\Test";

        private BindingList<Repository> repos = new BindingList<Repository>();

        private Repository currentRepo;

        public bool Showing
        {
            get { return Visible; }
            set {
                ShowInTaskbar = value;
                Visible = value;
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Showing = false;
            repos.Add(new Repository(@"C:\Users\Thomas\Documents\GitHub\Test\"));
            this.listBoxFolders.DataSource = repos;
            this.listBoxFolders.DisplayMember = "Name";
            this.folderBrowserDialog.SelectedPath = @"C:\Users\Thomas\Documents\GitHub";
        }

        private void backup()
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

        private string tryExecute(string command)
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

        private string execute(string command)
        {
            Process p = new Process();
            p.StartInfo.FileName = GIT_PATH + "git.exe";
            p.StartInfo.Arguments = command;
            p.StartInfo.WorkingDirectory = DIR;
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

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Showing = !Showing;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Showing && e.CloseReason == CloseReason.UserClosing)
            {
                Showing = false;
                e.Cancel = true;
            }
        }

        private void buttonAddFolder_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = this.folderBrowserDialog.SelectedPath;
                if (!Directory.Exists(path + "\\.git"))
                {
                    MessageBox.Show("This is not a git repository");
                    return;
                }
                repos.Add(new Repository(this.folderBrowserDialog.SelectedPath));
                
            }
        }

        private void listBoxFolders_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        //private struct GitMessage
        //{
        //    public readonly string message, error;

        //    public bool HasError { get { return error.Length > 0; } }

        //    public GitMessage(string message, string error)
        //    {
        //        this.message = message;
        //        this.error = error;
        //    }
        //}
    }
}
