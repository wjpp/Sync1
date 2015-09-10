using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Glasscubes.Drive.Util;

namespace Glasscubes.Drive
{
    class Monitor
    {
        private FileSystemWatcher m_Watcher;

        public Monitor(string path)
        {
         
            MyFileSystemWatcher fsw = new MyFileSystemWatcher(path);
            fsw.Created += new System.IO.FileSystemEventHandler(fsw_Created);
            fsw.Changed += new System.IO.FileSystemEventHandler(fsw_Changed);
            fsw.Deleted += new System.IO.FileSystemEventHandler(fsw_Deleted);
            fsw.Renamed += new System.IO.RenamedEventHandler(fsw_Renamed);
            fsw.EnableRaisingEvents = true;

      
        }

        void fsw_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            Console.WriteLine("Renamed: FileName - {0}, ChangeType - {1}, Old FileName - {2}", e.Name, e.ChangeType, e.OldName);
        }

        void fsw_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            Console.WriteLine("Deleted: FileName - {0}, ChangeType - {1}", e.Name, e.ChangeType);
        }

        void fsw_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            Console.WriteLine("Changed: FileName - {0}, ChangeType - {1}", e.Name, e.ChangeType);
        }

        void fsw_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            Console.WriteLine("Created: FileName - {0}, ChangeType - {1}", e.Name, e.ChangeType);
        }

    }
}
