namespace Orleans.StorageProvider.DocumentDb
{
    using System;
    using Orleans.Runtime.Configuration;

    public static class ExtensionMethods
    {
        public static void UseDocumentDbStore(this GlobalConfiguration configuration, string providerName, DocumentDbOptions options = null)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                providerName = "Default";
            }

            if (options == null)
            {
                options = new DocumentDbOptions();
            }

            configuration.RegisterStorageProvider<DocumentDbStorage>(providerName, options.AsDictionary());
        }
    }
}
