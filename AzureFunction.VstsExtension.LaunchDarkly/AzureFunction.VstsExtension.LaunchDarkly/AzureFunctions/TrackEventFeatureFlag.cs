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

        [FunctionName("TrackEventFeatureFlag")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "TrackEventFeatureFlag")]HttpRequestMessage req, ExecutionContext context, TraceWriter log)
        {
            try
            {
                telemetry.Context.Operation.Id = context.InvocationId.ToString();
                telemetry.Context.Operation.Name = "TrackEventFeatureFlag";
                var startTime = DateTime.Now;
                var timer = System.Diagnostics.Stopwatch.StartNew();

                var data = req.Content.ReadAsStringAsync().Result; //Gettings parameters in Body request     
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
                var customEvent = formValues["customevent"];

                //get the token passed in the header request
                string issuedToken = Helpers.GetUserTokenInRequest(req);

                #region display log for debug
                log.Info(issuedToken);
                #endregion

                string extcert = Helpers.GetExtCertificatEnvName(appSettingExtCert, Helpers.GetHeaderValue(req, "api-version"));
                var tokenuserId = CheckVSTSToken.checkTokenValidity(issuedToken, extcert); //Check the token, and compare with the VSTS UserId
                if (tokenuserId != null)
                {

                    LdClient ldClient = new LdClient(launchDarklySDKkey);
                    User user = new User(tokenuserId + ":" + account);
                    ldClient.Track(customEvent, user, string.Empty);
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
