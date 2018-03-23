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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    public static class GetUserFeatureFlag
    {
        private static string key = TelemetryConfiguration.Active.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
        private static TelemetryClient telemetry = new TelemetryClient() { InstrumentationKey = key };
        private static LdClient _ldclient = new LdClient(System.Environment.GetEnvironmentVariable("LaunchDarkly_SDK_Key", EnvironmentVariableTarget.Process));


        [FunctionName("GetUserFeatureFlag")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "GetUserFeatureFlag")]HttpRequestMessage req, ExecutionContext context, TraceWriter log)
        {
            try
            {
                Task<HttpResponseMessage> response = null;

                telemetry.Context.Operation.Id = context.InvocationId.ToString();
                telemetry.Context.Operation.Name = "GetUserFeatureFlag";
                var startTime = DateTime.Now;
                var timer = System.Diagnostics.Stopwatch.StartNew();

                int apiversion = Helpers.GetHeaderValue(req, "api-version");

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
                string launchDarklySDKkey = (apiversion == 1) ? formValues["ldkey"] : string.Empty;
                string LDproject = (apiversion >= 2) ? formValues["ldproject"] : "roll-up-board";
                string LDenv = (apiversion >= 2) ? formValues["ldenv"] : "production";



                //get the token passed in the header request
                string issuedToken = Helpers.GetUserTokenInRequest(req);


                string extcert = Helpers.GetExtCertificatEnvName(appSettingExtCert, apiversion);
                var tokenuserId = CheckVSTSToken.checkTokenValidity(issuedToken, extcert); //Check the token, and compare with the VSTS UserId
                if (tokenuserId != null)
                {
                    var userkey = LaunchDarklyServices.FormatUserKey(tokenuserId, account);
                    Dictionary<string, bool> userFlags = new Dictionary<string, bool>();

                    // LD SDK performance review
                    if (apiversion == 2)
                    {
                        LaunchDarklyServices.GetUserFeatureFlags(_ldclient, userkey,ref userFlags);
                    }
                    else
                    {
                        userFlags = await LaunchDarklyServices.GetUserFeatureFlagsv1(LDproject, LDenv, userkey);
                    }
                    if (userFlags != null)
                    {
                        response = req.CreateResponse(HttpStatusCode.OK, userFlags); //return the users flags
                    }
                    else
                    {
                        telemetry.TrackTrace("flags is null");
                        response = req.CreateResponse(HttpStatusCode.InternalServerError, "User flags is null");
                    }
                    _ldclient.flush();
                }
                else
                {
                    telemetry.TrackTrace("The token is not valid");
                    response = req.CreateResponse(HttpStatusCode.Unauthorized, "The token is not valid");
                }
                return response;
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}
