using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.IO;

using RestSharp;
using RestSharp.Authenticators;
using SQLite;

using Glasscubes.Drive.Model;
using Glasscubes.Drive.UI;
using Glasscubes.Drive.Util;

namespace Glasscubes.Drive
{
    class SyncMain
    {
        static System.Threading.Timer downloadTimer;
        static System.Threading.Timer consumerTimer;

        static DBHelper dbHelper;
        static DownloadMonitor downloadMonitor;
        static DiskMonitor diskMonitor;
        static GCActionConsumer consumer;

        [STAThread]
        static void Main(string[] args)
        {
            ConnectsToGC.Setup();
            dbHelper = new DBHelper();

            downloadMonitor = new DownloadMonitor(dbHelper.db);
            downloadMonitor.rootDir =  "C:\\test";
            downloadTimer = new System.Threading.Timer(DownloadCheck, null, 4000, Timeout.Infinite);
            consumerTimer = new System.Threading.Timer(ConsumerProcess, null, 4000, Timeout.Infinite);

            diskMonitor = new DiskMonitor(downloadMonitor.rootDir, dbHelper.db);
            consumer = new GCActionConsumer(dbHelper.db);
            consumer.rootDir = downloadMonitor.rootDir;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (NotifyIcon icon = new NotifyIcon())
            {
                icon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                icon.ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("About", (s, e) => {new MainForm().Show();}),
                    new MenuItem("Exit", (s, e) => { Application.Exit(); }),
                });
                icon.Visible = true;

                Application.Run();
                icon.Visible = false;
            }

           

        }

        private static void DownloadCheck(object state)
        {
            downloadMonitor.Monitor();
            downloadTimer.Change(4000, Timeout.Infinite);
        }

        private static void ConsumerProcess(object state)
        {
            consumer.Process();
            consumerTimer.Change(4000, Timeout.Infinite);
        }

    }
}
