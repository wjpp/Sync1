using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RestSharp;
using RestSharp.Authenticators;
using System.IO;
using SQLite;
using System.Diagnostics;
using System.Net;
using System.Threading;

using Glasscubes.Drive.Model;

namespace Glasscubes.Drive
{
    class SyncMain
    {
        private RestClient client = new RestClient();
        const string key = "a06055a0-7605-4aee-9048-981aa6ef41a0";
        const string apiId = "11111";
        const string rootDir = "C:\\test";
        const string server = "http://home.glasscubesdev.com:8080/";
        const string dbName = "sync3.db";
        private SQLiteConnection db;

        static void Main(string[] args)
        {
            SyncMain p = new SyncMain();
            p.Start();

        }

        public void Start()
        {
            client.BaseUrl = new Uri(server);
            
            DbConnect(dbName);
            Globals global = null;
            try {
                global = db.Find<Globals>(1);
            }
            catch (SQLiteException e)
            {
            }
            if (global == null)
            {
                Init();
            } 

            while (true)
            {

                Thread.Sleep(2000);
                CheckForChanges();
            }
        }

        private void CheckForChanges()
        {
            var global = db.Find<Globals>(1);
            var request = new RestRequest();
            request.Resource = "/rest/sync/changes";
            SetUpRequest(request);
            request.AddParameter("rev", global.CurrentRevision);

            var response = client.Execute<SyncList>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                SyncList list = response.Data;
                List<DiskItem> items = list.Items;

                if (list.LatestRevision > global.CurrentRevision)
                {
                    global.CurrentRevision = list.LatestRevision;
                    db.Update(global);

                    foreach (DiskItem i in items)
                    {
                        switch(i.Action)
                        {
                            case "ADD":
                                NewFileAdded(i);
                                break;
                            case "DELETE":
                                DeleteDiskItem(i);
                                break;
                            case "UPDATED":
                                UpdateDiskItem(i);
                                break;
                            case "RENAME":
                                DiskItemRenamed(i);
                                break;
                        }
                    }
                }

            }
        }

        private void DiskItemRenamed(DiskItem i)
        {
            try {
                DiskItem orig = db.Find<DiskItem>(i.Id);
                string root = GetRootPath(orig);
                File.Move(Path.Combine(root,orig.FileName), Path.Combine(root,i.FileName));
                db.Update(i);
            }
            catch (SQLiteException e)
            {
                //TODO
            }
        }

        private void UpdateDiskItem(DiskItem i)
        {
            DiskItem original = db.Find<DiskItem>(i.Id);
            DeleteDiskItem(original);
            NewFileAdded(i);
        }

        private void NewFileAdded(DiskItem i)
        {
            if (i.Folder)
            {
                string newPath = Path.Combine(GetRootPath(i), i.FileName);
                System.IO.Directory.CreateDirectory(newPath);
                i.Path = newPath;
                db.Insert(i);
            } else
            {
                db.Insert(i);
                DownLoadFile(i);
            }
        }

        private void DeleteDiskItem(DiskItem i)
        {
            db.Delete(i);
            var path = Path.Combine(GetRootPath(i), i.FileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void Init()
        {
            SetupDB(dbName);
            GetWorkspaces();
            GetInitFiles();
        }

        private void SetupDB(string databaseName)
        {
           
            db.DropTable<Workspace>();
            db.DropTable<DiskItem>();
            db.DropTable<Globals>();

            db.CreateTable<Workspace>();
            db.CreateTable<DiskItem>();
            db.CreateTable<Globals>();

        }

        private void DbConnect(string databaseName)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            db = new SQLiteConnection(System.IO.Path.Combine(folder, databaseName));
        }

        private void GetInitFiles()
        {
            // curl "http://localhost:8080/rest/sync/changes?key=a06055a0-7605-4aee-9048-981aa6ef41a0&apiId=11111&rev=0"
            var request = new RestRequest();
            request.Resource = "/rest/sync/changes";
            SetUpRequest(request);
            request.AddParameter("rev", 0);

            var response = client.Execute<SyncList>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                SyncList list = response.Data;
                List<DiskItem> items = list.Items;

                Globals g = new Globals();
                g.Id = 1;
                g.CurrentRevision = list.LatestRevision;
                db.Insert(g);
                
                foreach (DiskItem i in items)
                {
                    db.Insert(i);
                }

                var queryWork = db.Table<Workspace>();
                var workspaces = queryWork.Cast<Workspace>().ToDictionary(o => o.Id, o => o);

                // Create all folders
                var toplevel = db.Query<DiskItem>("select * from DiskItem where FolderId = ? and Folder = ?", 0, true);

                foreach (var item in toplevel)
                {
                    Workspace w = workspaces[item.WorkspaceId];
                    string path = Path.Combine( rootDir , w.Name , item.FileName);
                    System.IO.Directory.CreateDirectory(path);
                    item.Path = path;
                    db.Update(item);

                    createChildFolders(item, path);
                }
                

                //Download all files

                var files = db.Query<DiskItem>("select * from DiskItem where  Folder = ?", false);
                foreach(DiskItem f in files)
                {
                    DownLoadFile(f);
                }

            }

        }

        private void DownLoadFile(DiskItem f)
        {
            var path = GetRootPath(f);
            var fullpath = Path.Combine(path, f.FileName);

            // conflict?
            if (File.Exists(fullpath))
            {
                bool conflict = true;
                int count = 1;
                while (conflict)
                {
                    string name = Path.GetFileNameWithoutExtension(f.FileName);
                    name = name + "(" + count + ")" + Path.GetExtension(f.FileName);
                    if (File.Exists(Path.Combine(path, name)))
                    {
                        count++;
                    } else
                    {
                        fullpath = Path.Combine(path, name);
                        conflict = false;
                        f.FileName = name;
                        db.Update(f); // save new name
                    }
                }
            }


            using (var client = new WebClient())
            {
                string url = server + "rest/sync/get?key=" + key + "&apiId=" + apiId + "&docId=" + f.Id;
                client.DownloadFile(url, fullpath);
            }
        }

        private string GetRootPath(DiskItem f)
        {
            string path;
            if (f.FolderId > 0)
            {
                DiskItem parent = db.Find<DiskItem>(f.FolderId);
                path = parent.Path;
            }
            else
            {
                Workspace w = db.Find<Workspace>(f.WorkspaceId);
                path = Path.Combine(rootDir, w.Name);
            }

            return path;
        }

        private void createChildFolders(DiskItem item, string path)
        {
            var folders = db.Query<DiskItem>("select * from DiskItem where FolderId = ? and Folder = ?", item.Id, true);
            foreach (var f in folders)
            {
                string newPath = Path.Combine (path,f.FileName);
                System.IO.Directory.CreateDirectory(newPath);
                f.Path = newPath;
                db.Update(f);

                createChildFolders(f, newPath);
            }
        }

    

        protected void GetWorkspaces()
        {
            //  curl    "http://localhost:8080/rest/sync/workspaces?key=a06055a0-7605-4aee-9048-981aa6ef41a0&apiId=11111"                                                                                                                             
            var request = new RestRequest();               
            request.Resource = "/rest/sync/workspaces";                            
            SetUpRequest(request);

            var response = client.Execute<List<Workspace>>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                List<Workspace> items = response.Data;
                foreach (Workspace w in items)
                {
                    System.IO.Directory.CreateDirectory(rootDir + "\\" + w.Name);
                    db.Insert(w);
                }
            }

        }

        protected void SetUpRequest(RestRequest request)
        {
            request.AddParameter("apiId", apiId);
            request.AddParameter("key", key);
        }

        protected List<DiskItem> GetWorkspaceItems(int workspaceId)
        {
            //curl "http://localhost:8080/rest/sync/list?apiId=12345&key=3b0193dd-ebed-44e1-b30c-ba6430c15e78&workspaceId=115317"


            var request = new RestRequest();
            request.Resource = "/rest/sync/list";

            SetUpRequest(request);
            request.AddParameter("workspaceId", workspaceId);

            var response = client.Execute<List<DiskItem>>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                List<DiskItem> items = response.Data;
                return items;
            }

            return null;
        }
    }
}
