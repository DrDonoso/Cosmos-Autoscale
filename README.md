# CosmosDB Autoscale

Tool for autoscaling cosmos. 


# Usage
First of all initialize CustomDocumentClient with Url and Authkey.
```c#
var customDocumentClient = CustomDocumentClient.Of("Url", "Authkey");
```

If you initialize it like this, you will have default settings if you use the common executeasync. To Initialize with settings:

```c#
var customConfiguration = new CustomConfiguration {
	MaxRetries = 10,
	MinThroughput = 1000,
	MaxThroughput = 10000,
	ScaleUpBatch = 200,
	ScaleDownBatch = 100
};
var customDocumentClient = CustomDocumentClient.Of("Url", "Authkey", customConfiguration);
```

## Client (Scale up)
Once you have the client initialized, you should use Execute or ExecuteAsync for all the requests.
There is two methods of Execute in order to use the common configuration or sending params if you have your environment configured.
```c#
Task<T> ExecuteAsync<T>(Func<IDocumentClient, Task<T>> func, string database, string collection, int retries = 0);
Task<T> ExecuteAsync<T>(Func<IDocumentClient, Task<T>> func, string database, string collection, int minThroughput, int maxThroughput, int scaleUpBatch, int retries = 0);
```

### Example
```c#
await _customDocumentClient.ExecuteAsync(x => x.UpsertDocumentAsync(collectionUri, data), database, collection).ConfigureAwait(false);
```

## Azure function (Scale down)

For now, there is only one method for scale down all the collections with the current configuration (Working on it).

```c#
await customClient.ScaleDownAll().ConfigureAwait(false);
```
