using RestSharp;
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

        protected void SetUpRequest(RestRequest request)
        {
            request.AddParameter("apiId", apiId);
            request.AddParameter("key", key);
        }
    }



}
