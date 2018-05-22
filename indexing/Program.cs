using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace Indexing
{
    class Program
    {

        const int runs = 50;
        private DocumentClient client;

        public static IConfiguration Config { get; set; }
        public static void Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            Config = builder.Build();

            try
            {
                Program p = new Program();
                p.WriteToCollection().Wait();
            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
            }
        }

        private async Task WriteToCollection()
        {
            string EndpointUrl = Config["EndpointUrl"];
            string PrimaryKey = Config["PrimaryKey"];
            string database = Config["Database"];
            string defaultIndexCollection = Config["DefaultIndexCollection"];
            string customIndexCollection = Config["CustomIndexCollection"];

            var random = new Random((int)DateTime.Now.Ticks);
            this.client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

            var totalDefaultRU = 0.0;
            var totalCustomRU = 0.0;
        

            for (int i = 0; i < runs; i++)
            {
                var doc = new SampleDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    prop1 = Guid.NewGuid().ToString(),
                    prop2 = random.Next(100),
                    prop3 = Guid.NewGuid().ToString(),
                    prop4 = random.Next(100),
                    prop5 = DateTime.UtcNow.ToLongTimeString(),
                    prop6 = random.Next(100),
                    prop7 = Guid.NewGuid().ToString(),
                    prop8 = random.Next(100),
                    prop9 = Guid.NewGuid().ToString(),
                    prop10 = random.Next(100),
                    prop11 = Guid.NewGuid().ToString(),
                    prop12 = random.Next(100),
                    prop13 = Guid.NewGuid().ToString(),
                    prop14 = random.Next(100),
                    prop15 = Guid.NewGuid().ToString(),
                    prop16 = random.Next(100)
                };

                totalDefaultRU += await this.CreateSampleDocument(database, defaultIndexCollection, doc);
                totalCustomRU += await this.CreateSampleDocument(database, customIndexCollection, doc);
            }
            Console.WriteLine("Cost with default indexing: {0}", totalDefaultRU / runs);
            Console.WriteLine("Cost with custom indexing: {0}", totalCustomRU / runs);
        }
        private async Task<double> CreateSampleDocument(string databaseName, string collectionName, SampleDocument sample)
        {

            var result = await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), sample);
            return result.RequestCharge;

        }
    }
}
