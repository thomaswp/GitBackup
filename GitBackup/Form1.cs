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
    public partial class Form1 : Form
    {
        private const string GIT_PATH = @"C:\Users\Thomas\AppData\Local\GitHub\PortableGit_ed44d00daa128db527396557813e7b68709ed0e2\bin\";
        private const string DIR = @"C:\Users\Thomas\Documents\GitHub\Test";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            execute("config credential.helper store");
            string status = execute("status");
            string[] lines = status.Split(new char[] { '\n' });
            if (lines.Length < 4 || !lines[0].StartsWith("On branch ") || !lines[3].StartsWith("Changes not staged"))
            {
            }
            else
            {
                string originalBranch = lines[0].Substring(10);
                string hash = execute("rev-parse HEAD").Substring(0, 8);
                string branch = originalBranch + "-backup-" + hash;
                string error;
                string create = execute("checkout " + branch);
                if (create.StartsWith("error"))
                {
                    error = execute("checkout -b " + branch);
                }
                error = execute("reset origin/" + branch);
                error = execute("add -A");
                error = execute("commit -m \"Autosave " + DateTime.Now.ToString() + "\"");
                error = execute("push --set-upstream origin " + branch);
                error = execute("reset " + originalBranch);
                error = execute("checkout " + originalBranch);
            }

            Application.Exit();
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
            string output = p.StandardOutput.ReadToEnd();
            if (output.Length == 0) output = p.StandardError.ReadToEnd();
            return output;
        }
    }
}
