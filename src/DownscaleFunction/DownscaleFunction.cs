using CosmosClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DownscaleFunction
{
    public static class DownscaleFunction
    {
        [FunctionName("DownscaleFunction")]
        public static async Task Run([TimerTrigger("* * * * * *")]TimerInfo myTimer, ILogger log)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cosmosUrl = config["CosmosUrl"];
            var cosmosAuthKey = config["CosmosAuthKey"];

            var customClient = CustomDocumentClient.Of(cosmosUrl, cosmosAuthKey);

            await customClient.ScaleDownAll().ConfigureAwait(false);
        }
    }
}
