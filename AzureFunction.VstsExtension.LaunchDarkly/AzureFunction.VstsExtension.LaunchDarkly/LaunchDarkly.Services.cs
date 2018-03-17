using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    public class LaunchDarklyServices
    {

        public static string FormatUserKey(string userid, string account)
        {
            return string.Format("{0}:{1}", userid, account);
        }

        //Hash the secure userkey
        public static string GetHashKey(string userkey)
        {
            if (string.IsNullOrEmpty(userkey))
            {
                return null;
            }
            UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] keyBytes = encoding.GetBytes(Helpers.GetEnvironmentVariable("LaunchDarkly_SDK_Key"));

            HMACSHA256 hmacSha256 = new HMACSHA256(keyBytes);
            byte[] hashedMessage = hmacSha256.ComputeHash(encoding.GetBytes(userkey));
            return BitConverter.ToString(hashedMessage).Replace("-", "").ToLower();
        }

        public static async Task<HttpResponseMessage> UpdateUserFlag(string ldproject, string ldenv, string userkey, string feature, bool active)
        {
            string ldUri = string.Concat("https://app.launchdarkly.com/api/v2/users/"+ ldproject + "/"+ ldenv + "/" + userkey + "/flags/" + feature);
            Dictionary<string, bool> dic = new Dictionary<string, bool>();
            dic.Add("setting", active);
            string json = JsonConvert.SerializeObject(dic);
            var requestData = new StringContent(json,Encoding.UTF8, "application/json");
          
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Helpers.GetEnvironmentVariable("LaunchDarkly_API_Key"));
                requestData.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = await client.PutAsync(ldUri, requestData);
                return response;
            }

        }

        private static async Task<HttpResponseMessage> GetUserFlags(string ldproject, string ldenv, string userkey)
        {
            string ldUri = string.Concat("https://app.launchdarkly.com/api/v2/users/" + ldproject + "/" + ldenv + "/" + userkey + "/flags");
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Helpers.GetEnvironmentVariable("LaunchDarkly_API_Key"));
                var response = await client.GetAsync(ldUri);
                return response;
            }

        }

        public static async Task<Dictionary<string, bool>> GetUserFeatureFlags(string LDproject, string LDenv, string userkey)
        {
            HttpResponseMessage getStatusResponse = await GetUserFlags(LDproject, LDenv, userkey);
            var getflagsusers = getStatusResponse.Content.ReadAsStringAsync().Result;
            Dictionary<string, bool> userFlags = new Dictionary<string, bool>();
            dynamic jobj = JObject.Parse(getflagsusers);
            foreach (JProperty prop in jobj["items"])
            {
                string ffname = prop.Name;
                string ffvalue = prop.Value["_value"].ToString();
                userFlags.Add(ffname, Convert.ToBoolean(ffvalue));
            }

            return userFlags;
        }
    }
}