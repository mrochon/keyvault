using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using System.Net.Http;

namespace keyvaultdemo
{
    public static class GetAADData
    {
        private static readonly string keyId = "https://mrdemokeyvault.vault.azure.net/keys/func-cred-cert/c700b73e78fd471b9ecacdd2a27a4338";
        private static readonly string appId = "bc2961d0-6dbf-47cc-aa87-893ef0b41fd9";
        public static ILogger logger;
        [FunctionName("GetAADData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            logger = log;
            log.LogInformation("Starting.");

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            // string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            // OR
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            log.LogInformation("KV obtained");
            var aadToken = await kv.AcquireTokenAsync(
                keyId,
                "7d1abfb9-9f4e-4ec6-8280-722dd7bf9b50", // AAD tenant id
                "https://graph.microsoft.com",
                appId);
            return (ActionResult)new OkObjectResult(aadToken);
            log.LogInformation(aadToken);
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", aadToken);
            var resp = await http.GetStringAsync("https://graph.microsoft.com/v1.0/users");
            return (ActionResult)new OkObjectResult(resp);


            /*
            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult(resp)
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
                */
        }
    }
}
