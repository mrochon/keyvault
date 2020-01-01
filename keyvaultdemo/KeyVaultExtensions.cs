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
            GetAADData.logger.LogInformation("AcquireTokenAsync");
            GetAADData.logger.LogInformation(keyId);
            GetAADData.logger.LogInformation(tenantId);
            GetAADData.logger.LogInformation(resourceId);
            GetAADData.logger.LogInformation(appId);
            var jwt = await keyVault.GetJWTUsingX509Async(keyId, tenantId, appId).ConfigureAwait(false);
            var body = $"scope={resourceId}/.default&clientId={appId}&client_assertion_type=urn%3Aietf%3Aparams%3Aoauth%3Aclient-assertion-type%3Ajwt-bearer&client_assertion={jwt}&grant_type=client_credentials";
            return body;
            GetAADData.logger.LogInformation(body);
            var http = new HttpClient();
            http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            var resp = await http.PostAsync(
                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                new StringContent(jwt, Encoding.UTF8, "application/x-www-form-urlencoded")).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsAsync<string>();
                var token = JObject.Parse(json)["access_token"].Value<string>();
                return token;
            }
            return null;
        }
        //HACK: get thumbprint using key id!!!
        private static readonly string thumbprint = "08CD9488E40C2A987E4A4713E7F123922D8ACE29";
        // https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-certificate-credentials
        private static async Task<string> GetJWTUsingX509Async(
            this KeyVaultClient keyVault,
            string keyId,
            string tenantId, 
            string appId)
        {
            GetAADData.logger.LogInformation("GetJWTUsingX509");
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
                //, signingCredentials: new SigningCredentials(new KeyVaultSecurityKey(keyId, , "RS256")
                //{
                //    CryptoProviderFactory = new CryptoProviderFactory() { CustomCryptoProvider = new KeyVaultCryptoProvider() }
                //}
            );
            GetAADData.logger.LogInformation("Raw token created");
            var header = Base64UrlEncoder.Encode(JsonConvert.SerializeObject(new Dictionary<string, string>()
            {
                { JwtHeaderParameterNames.Alg, "RS256" },
                //{ JwtHeaderParameterNames.Kid, (await keyVault.GetCertificateAsync(keyId).ConfigureAwait(false)).Kid},
                { JwtHeaderParameterNames.X5t, thumbprint },
                { JwtHeaderParameterNames.Typ, "JWT" }
            }));
            GetAADData.logger.LogInformation("Header created");
            var byteData = Encoding.UTF8.GetBytes(header + "." + token.EncodedPayload);
            var hasher = new SHA256CryptoServiceProvider();
            var digest = hasher.ComputeHash(byteData);
            GetAADData.logger.LogInformation("Signing...");
            var signature = await keyVault.SignAsync(keyId, "RS256", digest);
            GetAADData.logger.LogInformation("Signed");

            GetAADData.logger.LogInformation($"Header: {header}");
            GetAADData.logger.LogInformation($"Payload: {token.EncodedPayload}");
            GetAADData.logger.LogInformation($"Signature: {Base64UrlEncoder.Encode(signature.Result)}");
            return $"{header}.{token.EncodedPayload}.{Base64UrlEncoder.Encode(signature.Result)}";
        }
    }
}
