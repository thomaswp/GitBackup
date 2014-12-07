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
            foreach (Repository repo in repos) repo.Save();
            this.Close();
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
