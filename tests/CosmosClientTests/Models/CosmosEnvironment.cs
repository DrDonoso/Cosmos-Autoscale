using Microsoft.Azure.Documents.Client;
using System.Collections.Generic;

namespace CosmosClientTests.Models
{
    public class CosmosEnvironment
    {
        public IEnumerable<CosmosDatabase> Databases { get; set; } = new List<CosmosDatabase>();
    }

    public class CosmosDatabase
    {
        public string Name { get; set; }
        public IEnumerable<CosmosCollection> Collections { get; set; } = new List<CosmosCollection>();
        public RequestOptions RequestOptions { get; set; }
    }

    public class CosmosCollection
    {
        public string Name { get; set; }
        public RequestOptions RequestOptions { get; set; }
    }
}