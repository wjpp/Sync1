using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glasscubes.Drive.Util
{
    class ConnectsToGC
    {
        protected RestClient client = new RestClient();
        //glasscubes://auth-callback=3271f319-911b-4217-b696-897517643535&accountId=54036&apiId=222&accountName=wp2
        protected const string key = "3271f319-911b-4217-b696-897517643535";
        protected const string apiId = "222";
        protected const string server = "http://home.glasscubesdev.com:8080/";


        public ConnectsToGC()
        {
            client.BaseUrl = new Uri(server);
            
        }

        protected void SetUpRequest(RestRequest request)
        {
            request.AddParameter("apiId", apiId);
            request.AddParameter("key", key);
        }

        protected RestRequest newReq(string url)
        {
            return new RestRequest(url)  { DateFormat = "yyyyMMdd:HH:mm:ss:fff:zzz" } ;
        }

        protected RestRequest newReq(string url, Method m)
        {
            return new RestRequest(url, m) { DateFormat = "yyyyMMdd:HH:mm:ss:fff:zzz" };
        }

    }



}
