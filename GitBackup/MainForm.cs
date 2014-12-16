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
using System.Resources;

namespace GitBackup
{
    public partial class MainForm : Form
    {

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

            this.listBoxFolders.DataSource = repos;
            this.listBoxFolders.DisplayMember = "Name";
            string github = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\GitHub";
            if (Directory.Exists(github))
            {
                this.folderBrowserDialog.SelectedPath = github;
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.GitPath.Length == 0)
            {
                if (!Git.FindGitPath())
                {
                    MessageBox.Show("Please locate your git executable");
                    setGitPath();
                }
            }
            else
            {
                Git.GitPath = Properties.Settings.Default.GitPath;
            }

            repos.Clear();
            foreach (string path in Properties.Settings.Default.Repositories)
            {
                repos.Add(new Repository(path));
            }
            this.listBoxFolders.ClearSelected();
            if (this.listBoxFolders.Items.Count > 0) this.listBoxFolders.SelectedIndex = 0;
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

        private void set()
        {
            if (currentRepo != null)
            {
                currentRepo.Settings.Name = this.textBoxName.Text;
                currentRepo.Settings.Active = this.checkBoxActive.Checked;
                currentRepo.Settings.Interval = (int) this.nudInterval.Value;
                currentRepo.Settings.IgnoredBranches.Clear();
                for (int i = 0; i < this.checkedListBoxBranches.Items.Count; i++)
                {
                    if (this.checkedListBoxBranches.CheckedIndices.IndexOf(i) == -1) 
                    {
                        currentRepo.Settings.IgnoredBranches.Add((string)checkedListBoxBranches.Items[i]);
                    }
                }
            }
        }

        private void read()
        {
            if (currentRepo == null) return;
            this.checkBoxActive.Checked = currentRepo.Settings.Active;
            this.nudInterval.Value = currentRepo.Settings.Interval;
            this.textBoxName.Text = currentRepo.Settings.Name;
            string[] branches = currentRepo.Git.execute("branch").Split('\n');
            this.checkedListBoxBranches.Items.Clear();
            this.textBoxPath.Text = currentRepo.Path;
            int index = 0;
            foreach (string branch in branches)
            {
                string b = branch.Replace("*", "").Trim();
                if (b.Length == 0) continue;
                if (b.Contains("-backup-")) continue;
                this.checkedListBoxBranches.Items.Add(b);
                if (!currentRepo.Settings.IgnoredBranches.Contains(b))
                {
                    this.checkedListBoxBranches.SetItemChecked(index, true);
                }
                index++;
            }

        }

        private void listBoxFolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            set();

            currentRepo = (Repository) this.listBoxFolders.SelectedItem;
            this.textBoxName.Enabled = 
                this.checkBoxActive.Enabled = 
                this.checkedListBoxBranches.Enabled = 
                this.nudInterval.Enabled = 
                currentRepo != null;

            read();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            set();
            if (Git.GitPath != null) Properties.Settings.Default.GitPath = Git.GitPath;
            Properties.Settings.Default.Repositories = new System.Collections.Specialized.StringCollection();
            foreach (Repository repo in repos)
            {
                Properties.Settings.Default.Repositories.Add(repo.Path);
                repo.Save();
            }
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void buttonGitPath_Click(object sender, EventArgs e)
        {
            setGitPath();
        }

        private void setGitPath()
        {
            if (Git.GitPath != null)
            {
                this.openFileDialogGit.InitialDirectory = Git.GitPath;
            }
            if (this.openFileDialogGit.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String path = this.openFileDialogGit.FileName;
                path = path.Substring(0, path.Length - 7);
                if (!Directory.Exists(path) || !File.Exists(path + "\\git.exe"))
                {
                    MessageBox.Show("Invlaid git path!");
                    return;
                }
                Git.GitPath = path;
            }
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
