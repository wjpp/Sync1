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

namespace Sync1
{
    class SyncMain
    {
        private RestClient client = new RestClient();
        const string key = "a06055a0-7605-4aee-9048-981aa6ef41a0";
        const string apiId = "11111";
        const string rootDir = "C:\\test";
        const string server = "http://home.glasscubesdev.com:8080/";
        private SQLiteConnection db;

        static void Main(string[] args)
        {
            SyncMain p = new SyncMain();
            p.Start();




 //               var request = new RestRequest();
 //             request.Resource = "/rest/sync/hello";

            //           IRestResponse response = client.Execute(request);

        }

        public void Start()
        {
            client.BaseUrl = new Uri(server);
            Init();

        }


        private void Init()
        {
            SetupDB("sync3.db");
            GetWorkspaces();
            GetInitFiles();
        }

        private void SetupDB(string DatabaseName)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            db = new SQLiteConnection(System.IO.Path.Combine(folder, DatabaseName));
            db.DropTable<Workspace>();
            db.DropTable<DiskItem>();
            db.DropTable<Globals>();

            db.CreateTable<Workspace>();
            db.CreateTable<DiskItem>();
            db.CreateTable<Globals>();

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
          
                    string path;
                    if (f.FolderId > 0)
                    {
                        DiskItem parent = db.Find<DiskItem>(f.FolderId);
                        path = parent.Path;
                    } else
                    {
                        Workspace w = db.Find<Workspace>(f.WorkspaceId);
                        path = Path.Combine(rootDir, w.Name);
                    }

                    using (var client = new WebClient())
                    {
                        string url = server + "/rest/sync/get?key=" + key + "&apiId=" + apiId + "&docId=" + f.Id;
                        client.DownloadFile(url, Path.Combine(path, f.FileName));
                    }


                }

            }

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
