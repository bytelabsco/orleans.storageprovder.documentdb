namespace Orleans.StorageProvider.DocumentDb
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class DocumentDbOptionsTest
    {
        [Fact]
        public void OptionPropertiesShouldConvertToStringDictionary()
        {
            // Arrange
            var options = new DocumentDbOptions();

            // Act  
            var optionsDictionary = options.AsDictionary();

            // Assert 
            PropertyInfo[] properties = options.GetType().GetProperties();
            foreach(var property in properties)
            {
                var correctValue = property.GetValue(options).ToString();
                var checkValue = optionsDictionary[property.Name];

                Assert.Equal(correctValue, checkValue);
            }
        }

        [Fact]
        public void OptionsShouldBeBuiltFromStringDictionary()
        {
            // Arrange
            var optionsDictionary = new Dictionary<string, string>();
            optionsDictionary.Add(nameof(DocumentDbOptions.AccountName), "AccountNameTest");
            optionsDictionary.Add(nameof(DocumentDbOptions.AccountKey), "AccountKeyTest");
            optionsDictionary.Add(nameof(DocumentDbOptions.CollectionName), "CollectionNameTest");
            optionsDictionary.Add(nameof(DocumentDbOptions.DatabaseName), "DatabaseNameTest");
            optionsDictionary.Add(nameof(DocumentDbOptions.DropDatabaseOnInit), "True");
            optionsDictionary.Add(nameof(DocumentDbOptions.Throughput), "1000");
            optionsDictionary.Add(nameof(DocumentDbOptions.UseCollectionPartition), "True");

            // Act
            var options = DocumentDbOptions.FromDictionary(optionsDictionary);

            // Assert
            PropertyInfo[] properties = options.GetType().GetProperties();
            foreach(var key in optionsDictionary.Keys)
            {
                var correctValue = optionsDictionary[key];

                var checkProperty = properties.FirstOrDefault(p => p.Name.Equals(key));
                if(!Equals(checkProperty, default(PropertyInfo)))
                {
                    var checkValue = checkProperty.GetValue(options).ToString();
                    Assert.Equal(correctValue, checkValue);
                }
            }
        }

        [Fact]
        public void StringDictionaryShouldHaveAsManyValuesAsTheAreOptions()
        {
            // Arrange
            var options = new DocumentDbOptions();

            // Act
            var optionsDictionary = options.AsDictionary();

            // Assert
            var propertyCount = options.GetType().GetProperties().Length;
            var dictionaryCount = optionsDictionary.Keys.Count;

            Assert.Equal(propertyCount, dictionaryCount);
        }
    }
}
