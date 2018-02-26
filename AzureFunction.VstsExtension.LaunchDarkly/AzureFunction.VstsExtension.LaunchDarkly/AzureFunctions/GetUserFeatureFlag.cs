using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System.IdentityModel.Tokens;
using LaunchDarkly.Client;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Collections.Generic;

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    public static class GetUserFeatureFlag
    {
        private static string key = TelemetryConfiguration.Active.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
        private static TelemetryClient telemetry = new TelemetryClient() { InstrumentationKey = key };


        [FunctionName("GetUserFeatureFlag")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "GetUserFeatureFlag")]HttpRequestMessage req, ExecutionContext context, TraceWriter log)
        {
            try
            {
                telemetry.Context.Operation.Id = context.InvocationId.ToString();
                telemetry.Context.Operation.Name = "GetUserFeatureFlag";
                var startTime = DateTime.Now;
                var timer = System.Diagnostics.Stopwatch.StartNew();
                int apiversion = Helpers.GetHeaderValue(req, "api-version");

                var data = req.Content.ReadAsStringAsync().Result; //Gettings parameters in Body request     
                var formValues = data.Split('&')
                    .Select(value => value.Split('='))
                    .ToDictionary(pair => Uri.UnescapeDataString(pair[0]).Replace("+", " "),
                                  pair => Uri.UnescapeDataString(pair[1]).Replace("+", " "));

                #region display log for debug
                log.Info(data); //for debug
                #endregion

                var account = formValues["account"];
                var launchDarklySDKkey = formValues["ldkey"];
                var appSettingExtCert = (apiversion < 2) ? formValues["appsettingextcert"] : string.Empty; //"RollUpBoard_ExtensionCertificate"
                var ExtCertKey = (apiversion >= 2) ? formValues["extcertkey"] : string.Empty;
                bool useKeyVault = (apiversion >= 2);

                //get the token passed in the header request
                string tokenuserId = Helpers.TokenIsValid(req, useKeyVault , appSettingExtCert, ExtCertKey, log);


                //Check the token, and compare with the VSTS UserId
                if (tokenuserId != null)
                {
                    IDictionary<string, Newtonsoft.Json.Linq.JToken> flags = LaunchDarklyServices.GetAllUserFlags(account, launchDarklySDKkey, tokenuserId);
                    if (flags != null)
                    {
                        return req.CreateResponse(HttpStatusCode.OK, flags); //return the users flags
                    }
                    else
                    {
                        telemetry.TrackTrace("flags is null");
                        return req.CreateResponse(HttpStatusCode.InternalServerError, "User flags is null");
                    }
                }
                else
                {
                    telemetry.TrackTrace("The token is not valid");
                    return req.CreateResponse(HttpStatusCode.Unauthorized, "The token is not valid");
                }
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


    }
}
