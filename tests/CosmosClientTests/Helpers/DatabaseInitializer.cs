using CosmosClientTests.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosClientTests.Helpers
{
    public class DatabaseHelper
    {
        private readonly IDocumentClient _documentClient;

        private DatabaseHelper(IDocumentClient documentClient)
        {
            _documentClient = documentClient;
        }

        public static DatabaseHelper Of(string url, string authKey)
        {
            var documentClient = new DocumentClient(new Uri(url), authKey);

            return new DatabaseHelper(documentClient);
        }

        public async Task Initialize(CosmosEnvironment cosmosEnvironment)
        {
            await TruncateEnvironment();
            foreach (var database in cosmosEnvironment.Databases)
            {
                await CreateDatabase(database.Name, database.RequestOptions);
                foreach (var collection in database.Collections)
                {
                    await CreateCollection(database.Name, collection.Name, collection.RequestOptions);
                }
            }
        }

        private async Task TruncateEnvironment()
        {
            var databases = _documentClient.CreateDatabaseQuery().AsEnumerable().ToList();
            foreach (var database in databases)
            {
                await _documentClient.DeleteDatabaseAsync(database.SelfLink);
            }
        }

        private async Task CreateDatabase(string database, RequestOptions requestOptions = null)
        {
            await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = database }, requestOptions);
        }

        private async Task CreateCollection(string database, string collection, RequestOptions requestOptions = null)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(database);

            var documentCollection = new DocumentCollection { Id = collection };
            documentCollection.PartitionKey.Paths.Add("/Language");

            await _documentClient.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection, requestOptions);
        }
    }
}