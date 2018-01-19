namespace Orleans.StorageProvider.DocumentDb
{
    using System.Collections.Generic;

    public class DocumentDbOptions
    {
        // Defaults go against the CosmosDB Emulator
        // More Info Here:  https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator

        public string AccountName { get; set; } = "https://localhost:8081";
        public string AccountKey { get; set; } = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        public string DatabaseName { get; set; } = "Database";
        public string CollectionName { get; set; } = "Collection";

        public bool UseCollectionPartition { get; set; } = false;

        public int Throughput { get; set; } = 400;

        public bool DropDatabaseOnInit { get; set; } = false;

        public IDictionary<string, string> AsDictionary()
        {
            var dictionary = new Dictionary<string, string>();

            dictionary.Add(nameof(AccountName), AccountName ?? string.Empty);
            dictionary.Add(nameof(AccountKey), AccountKey ?? string.Empty);
            dictionary.Add(nameof(DatabaseName), DatabaseName ?? string.Empty);
            dictionary.Add(nameof(UseCollectionPartition), UseCollectionPartition.ToString());
            dictionary.Add(nameof(CollectionName), CollectionName ?? string.Empty);
            dictionary.Add(nameof(Throughput), Throughput.ToString());
            dictionary.Add(nameof(DropDatabaseOnInit), DropDatabaseOnInit.ToString());

            return dictionary;
        }

        public static DocumentDbOptions FromDictionary(IDictionary<string, string> dictionary)
        {
            var options = new DocumentDbOptions();

            foreach (var key in dictionary.Keys)
            {
                var value = dictionary[key];

                switch (key)
                {
                    case nameof(AccountName):
                        options.AccountName = value;
                        break;

                    case nameof(AccountKey):
                        options.AccountKey = value;
                        break;

                    case nameof(DatabaseName):
                        options.DatabaseName = value;
                        break;

                    case nameof(UseCollectionPartition):
                        if (bool.TryParse(value, out bool useCollectionPartition))
                        {
                            options.UseCollectionPartition = useCollectionPartition;
                        }
                        break;

                    case nameof(CollectionName):
                        options.CollectionName = value;
                        break;

                    case nameof(Throughput):
                        if (int.TryParse(value, out int throughput))
                        {
                            options.Throughput = throughput;
                        }
                        break;

                    case nameof(DropDatabaseOnInit):
                        if (bool.TryParse(value, out bool dropDatabaseOnInit))
                        {
                            options.DropDatabaseOnInit = dropDatabaseOnInit;
                        }
                        break;
                }
            }

            return options;
        }
    }
}
