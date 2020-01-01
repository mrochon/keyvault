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
        [FunctionName("GetAADData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Starting.");

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            // string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            // OR
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            var aadToken = await kv.AcquireTokenAsync(
                Environment.GetEnvironmentVariable("signingKeyId", EnvironmentVariableTarget.Process),
                Environment.GetEnvironmentVariable("tenantid", EnvironmentVariableTarget.Process), 
                "https://graph.microsoft.com",
                Environment.GetEnvironmentVariable("appId", EnvironmentVariableTarget.Process));
            return (ActionResult)new OkObjectResult(aadToken);
        }
    }
}
