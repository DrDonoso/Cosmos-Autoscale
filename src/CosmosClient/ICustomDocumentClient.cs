using Microsoft.Azure.Documents;
using System;
using System.Threading.Tasks;

namespace CosmosClient
{
    public interface ICustomDocumentClient
    {
        T Execute<T>(Func<IDocumentClient, T> func, string database, string collection, int retries = 0);
        T Execute<T>(Func<IDocumentClient, T> func, string database, string collection, int minThroughput,
            int maxThroughput, int scaleUpBatch, int retries = 0);

        Task<T> ExecuteAsync<T>(Func<IDocumentClient, Task<T>> func, string database, string collection, int retries = 0);
        Task<T> ExecuteAsync<T>(Func<IDocumentClient, Task<T>> func, string database, string collection, int minThroughput,
            int maxThroughput, int scaleUpBatch, int retries = 0);


        Task ScaleDownAll();
    }
}