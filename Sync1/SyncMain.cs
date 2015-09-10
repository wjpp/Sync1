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
        static System.Threading.Timer timer;
        static DBHelper dbHelper;
        static DownloadMonitor downloadMonitor;
        static Monitor upMonitor;

        [STAThread]
        static void Main(string[] args)
        {

            dbHelper = new DBHelper();

            downloadMonitor = new DownloadMonitor(dbHelper.db);
            downloadMonitor.rootDir =  "C:\\test";
            timer = new System.Threading.Timer(DownloadCheck, null, 4000, Timeout.Infinite);

            upMonitor = new Monitor(downloadMonitor.rootDir, dbHelper.db);
            

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
            upMonitor.paused = true;
            downloadMonitor.Monitor();
            upMonitor.paused = false;
            timer.Change(4000, Timeout.Infinite);
        }


    }
}
