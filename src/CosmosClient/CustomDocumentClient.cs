using CosmosClient.Extensions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosClient
{
    public class CustomDocumentClient : ICustomDocumentClient
    {
        private readonly IDocumentClient _documentClient;
        private readonly CustomConfiguration _config;
        
        private const string ExceptionMessage = "Request rate is large";
        
        private CustomDocumentClient(IDocumentClient documentClient, CustomConfiguration configuration)
        {
            _documentClient = documentClient;

            configuration.CheckConfiguration();
            _config = configuration;
        }

        public static CustomDocumentClient Of(string url, string authKey, CustomConfiguration configuration = null)
        {
            var uri = new Uri(url);
            if (configuration == null)
            {
                configuration = new CustomConfiguration();
            }

            return new CustomDocumentClient(new DocumentClient(uri, authKey), configuration);
        }

        public T Execute<T>(Func<IDocumentClient, T> func, string database, string collection, int retries = 0)
        {
            return Execute(func, database, collection, _config.MinThroughput, _config.MaxThroughput, _config.ScaleUpBatch, retries);
        }

        public T Execute<T>(Func<IDocumentClient, T> func, string database, string collection, int minThroughput, int maxThroughput, int scaleUpBatch,
            int retries = 0)
        {
            try
            {
                return func(_documentClient);
            }
            catch (DocumentClientException exception) when (exception.Message.Contains(ExceptionMessage))
            {
                retries++;
                _documentClient.ScaleAsync(database, collection, scaleUpBatch, minThroughput, maxThroughput).Wait();
                if (retries < _config.MaxRetries) return Execute(func, database, collection, minThroughput, maxThroughput, scaleUpBatch, retries);
                throw;
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<IDocumentClient, Task<T>> func, string database, string collection, int retries = 0)
        {
            return await ExecuteAsync(func, database, collection, _config.MinThroughput, _config.MaxThroughput, _config.ScaleUpBatch, retries).ConfigureAwait(false);
        }

        public async Task<T> ExecuteAsync<T>(Func<IDocumentClient, Task<T>> func, string database, string collection, int minThroughput, int maxThroughput, int scaleUpBatch, int retries = 0)
        {
            try
            {
                return await func(_documentClient).ConfigureAwait(false);
            }
            catch (DocumentClientException exception) when (exception.Message.Contains(ExceptionMessage))
            {
                retries++;
                await _documentClient.ScaleAsync(database, collection, scaleUpBatch, minThroughput, maxThroughput).ConfigureAwait(false);
                if (retries < _config.MaxRetries) return await ExecuteAsync(func, database, collection, minThroughput, maxThroughput, scaleUpBatch, retries).ConfigureAwait(false);
                throw;
            }
        }

        public async Task ScaleDownAll()
        {
            var databases = _documentClient.CreateDatabaseQuery().AsEnumerable().ToList();

            foreach (var database in databases)
            {
                var offer = await _documentClient.GetOfferFromSelfLinkAsync(database.SelfLink).ConfigureAwait(false);

                if (offer != null)
                {
                    await _documentClient.ScaleAsync(offer, _config.ScaleDownBatch, _config.MinThroughput, _config.MaxThroughput).ConfigureAwait(false);
                }
                else
                {
                    var collections = _documentClient.CreateDocumentCollectionQuery(database.SelfLink).ToList();
                    foreach (var collection in collections)
                    {
                        offer = await _documentClient.GetOfferFromSelfLinkAsync(collection.SelfLink).ConfigureAwait(false);
                        await _documentClient.ScaleAsync(offer, _config.ScaleDownBatch, _config.MinThroughput, _config.MaxThroughput);
                    }
                }
            }
        }
    }
}