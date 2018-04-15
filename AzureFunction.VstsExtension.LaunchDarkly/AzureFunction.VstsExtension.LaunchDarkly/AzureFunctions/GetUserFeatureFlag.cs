using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using LaunchDarkly.Client;
using System.Threading.Tasks;
using System.Collections.Generic;


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

                string account = formValues["account"];
                string appSettingExtCert = (apiversion == 2) ? formValues["appsettingextcert"] : string.Empty; //"RollUpBoard_ExtensionCertificate"
                string LDproject = (apiversion == 2) ? formValues["ldproject"] : "roll-up-board";
                string LDenv = (apiversion == 2) ? formValues["ldenv"] : "production";
                string ExtCertKey = (apiversion >= 3) ? formValues["extcertkey"] : string.Empty;
                bool useKeyVault = (apiversion >= 3);

                //get the token passed in the header request
                string issuedToken = Helpers.GetUserTokenInRequest(req);


                string extcert = Helpers.GetExtCertificatEnvName(appSettingExtCert, apiversion);
                //get the token passed in the header request
                string tokenuserId = Helpers.TokenIsValid(req, useKeyVault, appSettingExtCert, ExtCertKey, log);
                if (tokenuserId != null)
                {
                    var userkey = LaunchDarklyServices.FormatUserKey(tokenuserId, account);
                    Dictionary<string, bool> userFlags = new Dictionary<string, bool>();

                    LaunchDarklyServices.GetUserFeatureFlags(_ldclient, userkey, ref userFlags);

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
