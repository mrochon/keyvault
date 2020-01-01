using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace keyvaultdemo
{
    public class KeyVaultTokenProvider
    {
        readonly KeyVaultClient _kvClient;
        readonly string _kvName;
        readonly string _signingKeyId;
        public KeyVaultTokenProvider(string kvName, string signingKeyId)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            // string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            // OR
            _kvClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            _kvName = kvName;
            _signingKeyId = signingKeyId;
        }
        public async Task<string> AcquireTokenAsync(string tenantId, string resourceId, string appId)
        {
            var jwt = await GetClientAssertionAsync(tenantId, appId).ConfigureAwait(false);
            var body = $"scope={resourceId}/.default&clientId={appId}&client_assertion_type=urn%3Aietf%3Aparams%3Aoauth%3Aclient-assertion-type%3Ajwt-bearer&client_assertion={jwt}&grant_type=client_credentials";
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
            else
                return null;
        }
        // https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-certificate-credentials
        public async Task<string> GetClientAssertionAsync(string tenantId, string appId)
        {
            try
            {
                var cert = await _kvClient.GetCertificateAsync($"https://{_kvName}.vault.azure.net/certificates/func-cred-cert/{_signingKeyId}").ConfigureAwait(false);
                //var thumbprint = cert.X509Thumbprint.Aggregate(new StringBuilder(),
                //               (sb, v) => sb.Append(v.ToString("X2"))).ToString();
                var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(cert.Cer);
                var jwk = JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(x509));
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
                    { JwtHeaderParameterNames.X5t, jwk.X5t }, // "CM2UiOQMKph-SkcT5_Ejki2Kzik"; initially, used B2C to get this value; see https://stackoverflow.microsoft.com/questions/179774
                    { JwtHeaderParameterNames.Typ, "JWT" }
                }));

                var unsignedToken = $"{header}.{token.EncodedPayload}";
                var byteData = Encoding.UTF8.GetBytes(unsignedToken);
                var hasher = new SHA256CryptoServiceProvider();
                var digest = hasher.ComputeHash(byteData);
                var signature = await _kvClient.SignAsync($"https://{_kvName}.vault.azure.net/keys/func-cred-cert/{_signingKeyId}", "RS256", digest);
                return $"{unsignedToken}.{Base64UrlEncoder.Encode(signature.Result)}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
