using AzureFunction.VstsExtension.LaunchDarkly.AzureFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    public class Helpers
    {
        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        public static string GetHeaderValue(HttpRequestMessage request, string name)
        {
            IEnumerable<string> values;
            var found = request.Headers.TryGetValues(name, out values);
            if (found)
            {
                return values.FirstOrDefault();
            }

            return null;
        }

        public static string GetSecurityToken(AuthenticationHeaderValue value)
        {
            if (value?.Scheme != "Bearer")
            {
                return null;
            }
            else
            {
                return value.Parameter;
            }
        }

        public static string GetUserTokenInRequest(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            //string apiVersion = Helpers.GetHeaderValue(request, "api-version");

            string issuedToken;

            issuedToken = Helpers.GetSecurityToken(request.Headers.Authorization);

            if (string.IsNullOrEmpty(issuedToken))
            {
                throw new SecurityTokenException();
            }
            else
            {
                return issuedToken;
            }
        }

        public static string GetExtCertificatEnvName(string appSettingExtCert, string apiversion)
        {
            if (apiversion == "2")
                return appSettingExtCert;
            else
                return "RollUpBoard_ExtensionCertificate";
        }
    }
}