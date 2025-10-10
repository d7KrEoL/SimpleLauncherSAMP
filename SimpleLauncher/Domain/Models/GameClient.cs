using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleLauncher.Domain.Models
{
    public class GameClient
    {
        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Path { get; private set; }
        public GameClient(string name, 
            string version, 
            string path)
        {
            Name = name;
            Version = version;
            Path = path;
        }
    }
}
