namespace Orleans.StorageProvider.DocumentDb
{
    using System;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Orleans.Providers;
    using Orleans.Runtime;
    using Orleans.Storage;

    public class DocumentDbStorage : IStorageProvider
    {
        private const string NOT_FOUND_CODE = "NotFound";

        private DocumentDbOptions _options;
        private DocumentClient _client;
        private Uri _databaseUri;
        private Uri _collectionUri;

        public Logger Log { get; set; }

        public string Name { get; set; }

        public async Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            try
            {
                _options = DocumentDbOptions.FromDictionary(config.Properties);

                var serializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    TypeNameHandling = TypeNameHandling.All
                };

                _client = new DocumentClient(new Uri(_options.AccountName), _options.AccountKey, serializerSettings);

                _databaseUri = UriFactory.CreateDatabaseUri(_options.DatabaseName);
                _collectionUri = UriFactory.CreateDocumentCollectionUri(_options.DatabaseName, _options.CollectionName);

                if (_options.DropDatabaseOnInit)
                {
                    await _client.DeleteDatabaseAsync(_databaseUri);
                }

                await _client.CreateDatabaseIfNotExistsAsync(new Database() { Id = _options.DatabaseName });

                var collection = new DocumentCollection();
                collection.Id = _options.CollectionName;

                if (_options.UseCollectionPartition)
                {
                    var partitionName = nameof(StateDocument.PartitionId);
                    partitionName = Char.ToLowerInvariant(partitionName[0]) + partitionName.Substring(1);
                    collection.PartitionKey.Paths.Add($"/{partitionName}");
                }

                await _client.CreateDocumentCollectionIfNotExistsAsync(
                    _databaseUri,
                    collection,
                    new RequestOptions
                    {
                        OfferThroughput = _options.Throughput
                    });
            }
            catch (Exception e)
            {
                Log.Error(0, "Error in Init", e);
                throw e;
            }
        }

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var docUri = GetDocumentUri(grainReference.ToKeyString());

            var partitionKey = CleanUpName(grainType) ?? grainType;

            await _client.DeleteDocumentAsync(docUri, new RequestOptions() { PartitionKey = new PartitionKey(partitionKey) });
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            try
            {
                var documentUri = GetDocumentUri(grainReference.ToKeyString());

                var requestOptions = new RequestOptions();
                if (_options.UseCollectionPartition)
                {
                    var partitionKey = CleanUpName(grainType) ?? grainType;
                    requestOptions.PartitionKey = new PartitionKey(partitionKey);
                }

                var response = await _client.ReadDocumentAsync<StateDocument>(documentUri, requestOptions);
                var document = response.Document;

                if (document != null)
                {
                    grainState.ETag = document.ETag;
                    grainState.State = document.State;
                }
            }
            catch (DocumentClientException dce)
            {
                if (NOT_FOUND_CODE.Equals(dce?.Error?.Code))
                {
                    // State is new.  Just return
                    return;
                }

                Log.Error(0, "Error in ReadStateAsync", dce);
                throw dce;
            }
            catch (Exception e)
            {
                Log.Error(0, "Error in ReadStateAsync", e);
                throw e;
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            try
            {
                var document = new StateDocument(grainState.State);
                document.Id = grainReference.ToKeyString();

                var partitionKey = CleanUpName(grainType) ?? grainType;
                document.PartitionId = partitionKey;

                var requestOptions = new RequestOptions();

                if (grainState.ETag != null)
                {
                    var accessCondition = new AccessCondition { Condition = grainState.ETag, Type = AccessConditionType.IfMatch };
                    requestOptions.AccessCondition = accessCondition;
                }

                await _client.UpsertDocumentAsync(_collectionUri, document, requestOptions);
            }
            catch (Exception e)
            {
                Log.Error(0, "Error in WriteStateAsync", e);
                throw e;
            }
        }

        public Task Close()
        {
            if (_client != null)
            {
                _client.Dispose();
            }

            return Task.CompletedTask;
        }

        private Uri GetDocumentUri(string documentId)
        {
            return UriFactory.CreateDocumentUri(_options.DatabaseName, _options.CollectionName, documentId);
        }

        private static Type GetType(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }

        string nameTokensRegex = @"`[0-9]+\[(.*)]";
        private string CleanUpName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            var name = String.Empty;

            var splits = Regex.Split(typeName, nameTokensRegex);

            if (splits.Length > 1)
            {
                bool firstCleaned = false;
                bool bracketed = false;
                for (var i = 0; i < splits.Length; i++)
                {
                    var t = splits[i];

                    var cleanName = CleanUpName(t);
                    if (cleanName != null)
                    {

                        if (!firstCleaned)
                        {
                            firstCleaned = true;
                        }
                        else
                        {
                            if (!bracketed)
                            {
                                name = name + "<";
                                bracketed = true;
                            }
                            else
                            {
                                name = name + ", ";
                            }
                        }

                        name = name + cleanName;
                    }
                }

                if (bracketed)
                {
                    name = name + ">";
                }
            }
            else
            {
                name = splits[0];

                // Remove any collection brackets
                name = name.Replace("[", "");
                name = name.Replace("]", "");

                // Remove assembly info
                var commaIdx = name.IndexOf(',');
                if (commaIdx >= 0)
                {
                    name = name.Substring(0, commaIdx);
                }

                // Remove Namespace Info
                var lastPeriod = name.LastIndexOf('.');
                if (lastPeriod >= 0)
                {
                    name = name.Substring(lastPeriod + 1);
                }
            }

            return name;
        }
    }
}
