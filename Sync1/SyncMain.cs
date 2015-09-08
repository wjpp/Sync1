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



namespace Glasscubes.Drive
{
    class SyncMain
    {
        static System.Threading.Timer timer;
        static DownloadMonitor downloadMonitor;

        [STAThread]
        static void Main(string[] args)
        {
            // SyncMain p = new SyncMain();
            // p.Start();

            downloadMonitor = new DownloadMonitor();
            timer = new System.Threading.Timer(DownloadCheck, null, 4000, Timeout.Infinite);


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
            timer.Change(4000, Timeout.Infinite);
        }


    }
}
