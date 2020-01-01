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

            var oauthClient = new KeyVaultTokenProvider(
                kvName: Environment.GetEnvironmentVariable("keyVaultName", EnvironmentVariableTarget.Process),
                signingKeyId: Environment.GetEnvironmentVariable("signingKeyId", EnvironmentVariableTarget.Process));
            var aadToken = await oauthClient.AcquireTokenAsync(
                Environment.GetEnvironmentVariable("tenantid", EnvironmentVariableTarget.Process),
                Environment.GetEnvironmentVariable("resourceid", EnvironmentVariableTarget.Process),
                Environment.GetEnvironmentVariable("appId", EnvironmentVariableTarget.Process));
            return (ActionResult)new OkObjectResult(aadToken);
        }
    }
}
