namespace Orleans.StorageProvider.DocumentDb
{
    using Newtonsoft.Json;

    /// <summary>
    /// Wrapping class to use one collection to store multiple document types
    /// </summary>
    /// <typeparam name="T">Type of the SubCollection, used as the partition</typeparam>
    public class StateDocument
    {
        public StateDocument(object state)
        {
            State = state;
        }

        // Json Property has to be here if we are extending the Azure Document
        [JsonProperty("id")]
        public string Id { get; set; }

        // Json Property has to be here if we are extending the Azure Document
        [JsonProperty("_etag")]
        public string ETag { get; private set; }

        // Json Property has to be here if we are extending the Azure Document
        [JsonProperty("partitionId")]
        public string PartitionId { get; internal set; }

        // Json Property has to be here if we are extending the Azure Document
        [JsonProperty("state")]
        public object State { get; private set; }
    }
}
