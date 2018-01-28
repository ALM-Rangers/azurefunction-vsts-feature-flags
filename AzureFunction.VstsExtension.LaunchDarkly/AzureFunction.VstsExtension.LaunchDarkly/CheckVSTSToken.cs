using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security.Tokens;
using System.IdentityModel.Tokens;
using Microsoft.Azure.WebJobs.Host;

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    public class CheckVSTSToken
    {
        public static string checkTokenValidity(string issuedToken, string nameExtensionCertificateKey)
        {
            try
            {
                string secret = Helpers.GetEnvironmentVariable(nameExtensionCertificateKey); // Load your extension's secret
                var validationParameters = new TokenValidationParameters()
                {
                    IssuerSigningTokens = new List<BinarySecretSecurityToken>()
                        {
                            new BinarySecretSecurityToken (System.Text.UTF8Encoding.UTF8.GetBytes(secret))
                        },
                    ValidateIssuer = false,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    ValidateActor = false
                };

                SecurityToken token = null;
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(issuedToken, validationParameters, out token);
                //compare the principal with the userId
                string principalUserId = principal.Claims.FirstOrDefault(q => string.Compare(q.Type, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", true) == 0).Value;

                return principalUserId;
            }
            catch
            {
                return null;
            }
        }

    }
}