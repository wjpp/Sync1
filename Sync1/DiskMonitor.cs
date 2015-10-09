using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Glasscubes.Drive.Util;
using SQLite;
using Glasscubes.Drive.Model;

namespace Glasscubes.Drive
{
    class DiskMonitor
    {
        SQLiteConnection db;
        public bool paused
        {
            get; set;
        }

        public DiskMonitor(string path, SQLiteConnection dbIn)
        {
            this.db = dbIn;
            this.paused = false;

            MyFileSystemWatcher fsw = new MyFileSystemWatcher(path);
            fsw.Created += new System.IO.FileSystemEventHandler(fsw_Created);
            fsw.Changed += new System.IO.FileSystemEventHandler(fsw_Changed);
            fsw.Deleted += new System.IO.FileSystemEventHandler(fsw_Deleted);
            fsw.Renamed += new System.IO.RenamedEventHandler(fsw_Renamed);
            fsw.EnableRaisingEvents = true;

      
        }

        void fsw_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            if (paused) return;
            Console.WriteLine("Renamed: FileName - {0}, ChangeType - {1}, Old FileName - {2}", e.Name, e.ChangeType, e.OldName);

            GCAction a = new GCAction();
            a.Action = GCAction.RENAMED;
            a.Path = e.FullPath;
            db.Insert(a);

        }

        void fsw_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            if (paused) return;
            Console.WriteLine("Deleted: FileName - {0}, ChangeType - {1}", e.Name, e.ChangeType);
            
           

            GCAction a = new GCAction();
            a.Action = GCAction.DELETED;
            a.Path = e.FullPath;
            db.Insert(a);

        }

        void fsw_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            if (paused) return;
            Console.WriteLine("Changed: FileName - {0}, ChangeType - {1}", e.Name, e.ChangeType);


            GCAction a = new GCAction();
            a.Action = GCAction.CHANGED;
            a.Path = e.FullPath;
            db.Insert(a);
        }

        void fsw_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            if (paused) return;
            Console.WriteLine("Created: FileName - {0}, ChangeType - {1}", e.Name, e.ChangeType);

            // lets not worry about moves just yet

            GCAction a = new GCAction();
            a.Action = GCAction.NEW;
            a.Path = e.FullPath;
            db.Insert(a);

        }

    }
}
