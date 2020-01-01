using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Security.Claims;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace keyvaultdemo
{
    public static class KeyVaultExtensions
    {
        // https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow
        public static async Task<string> AcquireTokenAsync(
            this KeyVaultClient keyVault,
            string keyId,
            string tenantId, 
            string resourceId, 
            string appId
            )
        {
            var jwt = await keyVault.GetJWTUsingX509Async(keyId, tenantId, appId).ConfigureAwait(false);
            var body = $"scope={resourceId}/.default&clientId={appId}&client_assertion_type=urn%3Aietf%3Aparams%3Aoauth%3Aclient-assertion-type%3Ajwt-bearer&client_assertion={jwt}&grant_type=client_credentials";
            //return body;

            var http = new HttpClient();
            http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            var resp = await http.PostAsync(
                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var token = JObject.Parse(json)["access_token"].Value<string>();
                return token;
            }
            return null;
        }

        // https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-certificate-credentials
        private static async Task<string> GetJWTUsingX509Async(
            this KeyVaultClient keyVault,
            string keyId,
            string tenantId, 
            string appId)
        {
            try
            {
                var cert = await keyVault.GetCertificateAsync("https://mrdemokeyvault.vault.azure.net/certificates/func-cred-cert/c700b73e78fd471b9ecacdd2a27a4338").ConfigureAwait(false);
                var thumbprint = cert.X509Thumbprint.Aggregate(new StringBuilder(),
                               (sb, v) => sb.Append(v.ToString("X2"))).ToString();
                var kid = cert.Kid;
                var token = new JwtSecurityToken(
                    issuer: appId,
                    audience: $"https://login.microsoftonline.com/{tenantId}/oauth2/token",
                    claims: new Claim[]
                    {
                        new Claim("jti", Guid.NewGuid().ToString("D")),
                        new Claim("sub", appId)
                    },
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddMinutes(10)
                );
                var header = Base64UrlEncoder.Encode(JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    { JwtHeaderParameterNames.Alg, "RS256" },
                    { JwtHeaderParameterNames.X5t, "CM2UiOQMKph-SkcT5_Ejki2Kzik" }, // used B2C to get this value; see https://stackoverflow.microsoft.com/questions/179774
                    //{ JwtHeaderParameterNames.Kid, thumbprint },
                    { JwtHeaderParameterNames.Typ, "JWT" }
                }));

                var unsignedToken = $"{header}.{token.EncodedPayload}";
                var byteData = Encoding.UTF8.GetBytes(unsignedToken);
                var hasher = new SHA256CryptoServiceProvider();
                var digest = hasher.ComputeHash(byteData);
                var signature = await keyVault.SignAsync(keyId, "RS256", digest);
                return $"{unsignedToken}.{Base64UrlEncoder.Encode(signature.Result)}";
            } catch(Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
