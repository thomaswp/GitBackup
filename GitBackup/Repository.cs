using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitBackup
{
    public class Repository
    {
        public string Path { get; set; }
        public string Name { get; set; }

        public Repository(string path)
        {
            this.Path = path;
            this.Name = path;
        }
    }
}
