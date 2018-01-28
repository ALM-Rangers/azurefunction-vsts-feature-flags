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

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    public static class GetHashKey
    {
        private static string key = TelemetryConfiguration.Active.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
        private static TelemetryClient telemetry = new TelemetryClient() { InstrumentationKey = key };

        [FunctionName("GetHashKey")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "GetHashKey")]HttpRequestMessage req, ExecutionContext context, TraceWriter log)
        {
            try
            {
                telemetry.Context.Operation.Id = context.InvocationId.ToString();
                telemetry.Context.Operation.Name = "GetHashKey";


                var data = req.Content.ReadAsStringAsync().Result; //Gettings parameters in Body request     
                var formValues = data.Split('&')
                    .Select(value => value.Split('='))
                    .ToDictionary(pair => Uri.UnescapeDataString(pair[0]).Replace("+", " "),
                                  pair => Uri.UnescapeDataString(pair[1]).Replace("+", " "));

                #region display log for debug
                log.Info(data); //for debug
                #endregion

                var account = formValues["account"];

                string issuedToken = Helpers.GetUserTokenInRequest(req);

                #region display log for debug
                log.Info(issuedToken);
                #endregion

                var tokenuserId = CheckVSTSToken.checkTokenValidity(issuedToken, "RollUpBoard_ExtensionCertificate"); //Check the token, and compare with the VSTS UserId
                if (tokenuserId != null)
                {
                    string hash = LaunchDarklyServices.GetHashKey(tokenuserId + ":" + account); //hash the User Key
                    return req.CreateResponse(HttpStatusCode.OK, hash); //return the hash key
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
