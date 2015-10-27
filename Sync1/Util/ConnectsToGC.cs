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
      

        //Using account https://wp2.glasscubes.com/auth?appId=dc&apiId=10001
        // wayne.p@glasscubes.com / test
        //glasscubes://auth-callback=50926ddc-ef9c-4bda-a800-32984305370e&accountId=54366&apiId=10001&accountName=wp2

        protected static string key = "50926ddc-ef9c-4bda-a800-32984305370e";
        protected const string apiId = "10001";
        protected const string server = "https://wp2.glasscubes.com/";
        protected const string appId = "gcdrive";
        protected const string email = "synctest@test.com";
        protected const string password = "test";

        static public void Setup()
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri(server);
            var request = newReq("/rest/sync/login");
            request.AddParameter("apiId", apiId);
            request.AddParameter("appId", appId);
            request.AddParameter("email", email);
            string hash = CreateMD5(password);
            request.AddParameter("pw", hash);

            var response = client.Execute<LoginInfo>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                //TO DO support multiple companies for same email

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
