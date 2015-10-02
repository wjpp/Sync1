using Glasscubes.Drive.Model;
using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Glasscubes.Drive.Util
{
    class ConnectsToGC
    {
        protected RestClient client = new RestClient();
        //glasscubes://auth-callback=3271f319-911b-4217-b696-897517643535&accountId=54036&apiId=222&accountName=wp2
        protected static string key = "3271f319-911b-4217-b696-897517643535";
        protected const string apiId = "222";
        protected const string server = "http://home.glasscubesdev.com:8080/";
        protected const string appId = "gcdrive";

        static public void Setup()
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri(server);
            var request = newReq("/rest/sync/login");
            request.AddParameter("apiId", apiId);
            request.AddParameter("appId", appId);
            request.AddParameter("email", "test1@test.com");
            string hash = CreateMD5("test");
            request.AddParameter("pw", hash);

            var response = client.Execute<LoginInfo>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                LoginInfo info = response.Data;
                key = info.User.ApiKey;

            }
            else
            {
                //TO DO
                throw new NotImplementedException();
            }
        }

        public ConnectsToGC()
        {
            client.BaseUrl = new Uri(server);   
        }

        protected void SetUpRequest(RestRequest request)
        {
            request.AddParameter("apiId", apiId);
            request.AddParameter("key", key);
        }

        protected static RestRequest newReq(string url)
        {
            return new RestRequest(url)  { DateFormat = "yyyyMMdd:HH:mm:ss:fff:zzz" } ;
        }

        protected RestRequest newReq(string url, Method m)
        {
            return new RestRequest(url, m) { DateFormat = "yyyyMMdd:HH:mm:ss:fff:zzz" };
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

    }



}
