using CosmosClient;
using CosmosClient.Extensions;
using CosmosClientTests.Helpers;
using CosmosClientTests.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace CosmosClientTests
{
    [TestClass]
    public class CustomDocumentClientTests
    {
        private readonly ICustomDocumentClient _customDocumentClient;
        private readonly IDocumentClient _documentClient;
        private readonly DatabaseHelper _databaseHelper;
        private readonly DocumentClientException _documentClientException;

        private const string CosmosUrl = "https://localhost:8081";
        private const string CosmosAuthKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private const int InitialThroughput = 1000;

        public CustomDocumentClientTests()
        {
            _documentClient = new DocumentClient(new Uri(CosmosUrl), CosmosAuthKey);
            _customDocumentClient = CustomDocumentClient.Of(CosmosUrl, CosmosAuthKey);
            _databaseHelper = DatabaseHelper.Of(CosmosUrl, CosmosAuthKey);

            var type = typeof(DocumentClientException);
            var documentClientExceptionInstance = type.Assembly.CreateInstance(type.FullName,
                false, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { new Error
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = "429",
                    Message = "Request rate is large"
                }, (HttpResponseHeaders)null, HttpStatusCode.TooManyRequests }, null, null);
            _documentClientException = (DocumentClientException) documentClientExceptionInstance;

        }

        [TestInitialize]
        public async Task TestInitializer()
        {
            await InitializeDatabase().ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow("Library", "Books")]
        [DataRow("Videoclub", "Movies")]
        public async Task ExecuteAsync_ShouldScaleIfThrowTooLargeException(string database, string collection)
        {
            var funcAsync = new Mock<Func<IDocumentClient, Task<ResourceResponse<Document>>>>();
            funcAsync.Setup(x => x.Invoke(It.IsAny<IDocumentClient>())).Throws(_documentClientException);
            var previousOffer = await _documentClient.GetOfferAsync(database, collection).ConfigureAwait(false);

            try
            {
                await _customDocumentClient.ExecuteAsync(funcAsync.Object, database, collection).ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            var newOffer = await _documentClient.GetOfferAsync(database, collection).ConfigureAwait(false);

            Assert.IsTrue(newOffer.Content.OfferThroughput > previousOffer.Content.OfferThroughput);
        }

        [TestMethod]
        [DataRow("Library", "Comics")]
        [DataRow("Videoclub", "TV Shows")]
        public async Task Execute_ShouldScaleIfThrowTooLargeException(string database, string collection)
        {
            var func = new Mock<Func<IDocumentClient, ResourceResponse<Document>>>();
            func.Setup(x => x.Invoke(It.IsAny<IDocumentClient>())).Throws(_documentClientException);
            var previousOffer = await _documentClient.GetOfferAsync(database, collection).ConfigureAwait(false);

            try
            {
                _customDocumentClient.Execute(func.Object, database, collection);
            }
            catch
            {
                // ignored
            }

            var newOffer = await _documentClient.GetOfferAsync(database, collection).ConfigureAwait(false);

            Assert.IsTrue(newOffer.Content.OfferThroughput > previousOffer.Content.OfferThroughput);
        }

        [TestMethod]
        [DataRow("Library", "Books")]
        [DataRow("Library", "Comics")]
        [DataRow("Videoclub", "Movies")]
        public async Task ScaleDown_ShouldScaleDownAll(string database, string collection)
        {
            var previousOffer = await _documentClient.GetOfferAsync(database, collection).ConfigureAwait(false);

            await _customDocumentClient.ScaleDownAll().ConfigureAwait(false);

            var newOffer = await _documentClient.GetOfferAsync(database, collection).ConfigureAwait(false);

            Assert.IsTrue(newOffer.Content.OfferThroughput < previousOffer.Content.OfferThroughput);
        }

        private async Task InitializeDatabase()
        {
            var env = new CosmosEnvironment
            {
                Databases = new List<CosmosDatabase>
                {
                    new CosmosDatabase
                    {
                        Name = "Library",
                        Collections = new List<CosmosCollection>
                        {
                            new CosmosCollection
                                {Name = "Books", RequestOptions = new RequestOptions {OfferThroughput = InitialThroughput}},
                            new CosmosCollection
                                {Name = "Comics", RequestOptions = new RequestOptions {OfferThroughput = InitialThroughput}}
                        }
                    },
                    new CosmosDatabase
                    {
                        Name = "Videoclub",
                        Collections = new List<CosmosCollection>
                        {
                            new CosmosCollection {Name = "Movies"},
                            new CosmosCollection {Name = "TV Shows"}
                        },
                        RequestOptions = new RequestOptions {OfferThroughput = InitialThroughput}
                    }
                }
            };
            await _databaseHelper.Initialize(env).ConfigureAwait(false);
        }
    }
}