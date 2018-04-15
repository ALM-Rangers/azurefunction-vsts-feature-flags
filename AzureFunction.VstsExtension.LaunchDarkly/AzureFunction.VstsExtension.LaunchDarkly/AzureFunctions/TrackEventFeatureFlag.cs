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

namespace AzureFunction.VstsExtension.LaunchDarkly
{

    public static class TrackEventFeatureFlag
    {
        private static string key = TelemetryConfiguration.Active.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
        private static TelemetryClient telemetry = new TelemetryClient() { InstrumentationKey = key };
        private static LdClient _ldclient = new LdClient(System.Environment.GetEnvironmentVariable("LaunchDarkly_SDK_Key", EnvironmentVariableTarget.Process));

        [FunctionName("TrackEventFeatureFlag")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "TrackEventFeatureFlag")]HttpRequestMessage req, ExecutionContext context, TraceWriter log)
        {
            try
            {
                telemetry.Context.Operation.Id = context.InvocationId.ToString();
                telemetry.Context.Operation.Name = "TrackEventFeatureFlag";
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
                var customEvent = formValues["customevent"];
                var appSettingExtCert = (apiversion < 2) ? formValues["appsettingextcert"] : string.Empty; //"RollUpBoard_ExtensionCertificate"
                var ExtCertKey = (apiversion >= 2) ? formValues["extcertkey"] : string.Empty;
                bool useKeyVault = (apiversion >= 2);

                //get the token passed in the header request
                string tokenuserId = Helpers.TokenIsValid(req, useKeyVault, appSettingExtCert, ExtCertKey, log);

                if (tokenuserId != null)
                {
                    var userkey = LaunchDarklyServices.FormatUserKey(tokenuserId, account);
                    LaunchDarklyServices.TrackFeatureFlag(_ldclient, userkey, launchDarklySDKkey, customEvent);
                    return req.CreateResponse(HttpStatusCode.OK, "The custom event had be successfuly tracked");
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
