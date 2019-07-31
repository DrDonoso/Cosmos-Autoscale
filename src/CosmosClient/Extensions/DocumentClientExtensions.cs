using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace CosmosClient.Extensions
{
    public static class DocumentClientExtensions
    {
        public static async Task ScaleAsync(this IDocumentClient documentClient, string database, string collection, int scaleBatch, int minThroughput, int maxThroughput)
        {
            var offer = await documentClient.GetOfferAsync(database, collection);
            await documentClient.ScaleAsync(offer, scaleBatch, minThroughput, maxThroughput);
        }

        public static async Task ScaleAsync(this IDocumentClient documentClient, OfferV2 offer, int scaleBatch, int minThroughput, int maxThroughput)
        {
            var currentThroughput = offer.Content.OfferThroughput;

            var newThroughput = currentThroughput + scaleBatch;
            if (newThroughput < minThroughput)
            {
                newThroughput = minThroughput;
            }

            if (newThroughput > maxThroughput)
            {
                newThroughput = maxThroughput;
            }
            var updatedOffer = new OfferV2(offer, newThroughput);
            await documentClient.ReplaceOfferAsync(updatedOffer);
        }

        public static async Task<OfferV2> GetOfferAsync(this IDocumentClient documentClient, string databaseName, string collectionName = null)
        {
            if (string.IsNullOrEmpty(collectionName))
            {
                return await documentClient.GetDatabaseOfferAsync(databaseName);
            }
            return await documentClient.GetCollectionOfferAsync(databaseName, collectionName) ??
                    await documentClient.GetDatabaseOfferAsync(databaseName);
        }

        public static async Task<OfferV2> GetOfferFromSelfLinkAsync(this IDocumentClient documentClient, string selfLink)
        {
            return (await documentClient.CreateOfferQuery()
                .Where(o => o.ResourceLink == selfLink)
                .AsDocumentQuery()
                .ExecuteNextAsync<OfferV2>()).FirstOrDefault();
        }

        private static async Task<OfferV2> GetDatabaseOfferAsync(this IDocumentClient documentClient, string databaseName)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(databaseName);
            var database = (await documentClient.ReadDatabaseAsync(databaseUri)).Resource;

            return await documentClient.GetOfferFromSelfLinkAsync(database.SelfLink);
        }

        private static async Task<OfferV2> GetCollectionOfferAsync(this IDocumentClient documentClient, string databaseName, string collectionName)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseName, collectionName);
            var collection = (await documentClient.ReadDocumentCollectionAsync(collectionUri)).Resource;

            return await documentClient.GetOfferFromSelfLinkAsync(collection.SelfLink);
        }
    }
}