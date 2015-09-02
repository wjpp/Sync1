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

namespace Sync1
{
    class Program
    {
        private RestClient client = new RestClient();
        const string key = "a06055a0-7605-4aee-9048-981aa6ef41a0";
        const string apiId = "11111";
        const string rootDir = "C:\\test";
        private SQLiteAsyncConnection db;

        static void Main(string[] args)
        {
            Program p = new Program();
            p.Start();




 //               var request = new RestRequest();
 //             request.Resource = "/rest/sync/hello";

            //           IRestResponse response = client.Execute(request);

        }

        public void Start()
        {
            client.BaseUrl = new Uri("http://home.glasscubesdev.com:8080/");
            SetupDB();
           


            GetWorkspaces();
          //  List<DiskItem> items = GetWorkspaceItems(115317);
          //  if (items == null) return;
          //  Download(items, rootDir);
        }

        private void SetupDB()
        {
            db = new SQLiteAsyncConnection("sync1");
            db.CreateTableAsync<Workspace>().ContinueWith((results) =>
            {
                Debug.WriteLine("Workspace created!");
            });
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
                    db.InsertAsync(w).ContinueWith((t) =>
                    {
                        Debug.WriteLine("Workspace ID: {0}", w.Id );
                    });
                }
            }

        }


        protected void Download(List<DiskItem> items, string path)
        {
            foreach (DiskItem item in items)
            {
                var request = new RestRequest();
                request.Resource = "/rest/sync/list";
                SetUpRequest(request);
                request.AddParameter("docId", item.Id);                

                var writer = File.OpenWrite(path + "\\" + item.FileName);
                request.ResponseWriter = (responseStream) => responseStream.CopyTo(writer);
                var response = client.DownloadData(request);

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
