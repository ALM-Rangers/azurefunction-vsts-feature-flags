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
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    public static class GetUserFeatureFlag
    {
        private static string key = TelemetryConfiguration.Active.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
        private static TelemetryClient telemetry = new TelemetryClient() { InstrumentationKey = key };
        
        [FunctionName("GetUserFeatureFlag")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "GetUserFeatureFlag")]HttpRequestMessage req, ExecutionContext context, TraceWriter log)
        {
            try
            {
                telemetry.Context.Operation.Id = context.InvocationId.ToString();
                telemetry.Context.Operation.Name = "GetUserFeatureFlag";
                var startTime = DateTime.Now;
                var timer = System.Diagnostics.Stopwatch.StartNew();

                var data = await req.Content.ReadAsStringAsync(); //Gettings parameters in Body request     
                var formValues = data.Split('&')
                    .Select(value => value.Split('='))
                    .ToDictionary(pair => Uri.UnescapeDataString(pair[0]).Replace("+", " "),
                                  pair => Uri.UnescapeDataString(pair[1]).Replace("+", " "));

                #region display log for debug
                log.Info(data); //for debug
                #endregion

                var account = formValues["account"];
                var appSettingExtCert = formValues["appsettingextcert"]; //"RollUpBoard_ExtensionCertificate"
                var launchDarklySDKkey = formValues["ldkey"];
                
                //get the token passed in the header request
                string issuedToken = Helpers.GetUserTokenInRequest(req);


                string extcert = Helpers.GetExtCertificatEnvName(appSettingExtCert, Helpers.GetHeaderValue(req, "api-version"));
                var tokenuserId = CheckVSTSToken.checkTokenValidity(issuedToken, extcert); //Check the token, and compare with the VSTS UserId
                if (tokenuserId != null)
                {

                    //Configuration ldConfig = Configuration.Default(launchDarklySDKkey);
                    //LdClient ldClient = new LdClient(ldConfig);
                    //User user = User.WithKey(tokenuserId + ":" + account);
                    ////var flags = ldClient.AllFlags(user);
                    Dictionary<string, bool> userFlags = new Dictionary<string, bool>();
                    //userFlags.Add("enable-telemetry", ldClient.BoolVariation("enable-telemetry", user));
                    //userFlags.Add("display-logs", ldClient.BoolVariation("display-logs", user));
                    //ldClient.Dispose();

                    if (userFlags != null)
                    {
                        return req.CreateResponse(HttpStatusCode.OK, userFlags); //return the users flags
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
