using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;

namespace Consistency
{
    class Program
    {
        public static IConfiguration Config { get; set; }

        static string EndpointUrl;
        static string PrimaryKey;
        static string database;
        static string collection;
        private DocumentClient client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);
        public static void Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            Config = builder.Build();

            EndpointUrl = Config["EndpointUrl"];
            PrimaryKey = Config["PrimaryKey"];
            database = Config["Database"];
            collection = Config["Collection"];
            try
            {
                Program p = new Program();
                p.CompareConsistency().Wait();
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

        private async Task CompareConsistency()
        {
            var random = new Random((int)DateTime.Now.Ticks);
            var doc = new SampleDocument
            {
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

            //doc.Id = Guid.NewGuid().ToString();
            //var strongWriteResults = await this.CreateSampleDocument(database, collection, doc, ConsistencyLevel.Strong);
            //var strongReadResults = await this.ReadSampleDocument(strongWriteResults.Resource.Id, ConsistencyLevel.Strong);

            doc.Id = Guid.NewGuid().ToString();
            var boundedWriteResults = await this.CreateSampleDocument(database, collection, doc, ConsistencyLevel.BoundedStaleness);
            var boundedReadResults = await this.ReadSampleDocument(boundedWriteResults.Resource.Id, ConsistencyLevel.BoundedStaleness);

            doc.Id = Guid.NewGuid().ToString();
            var sessionWriteResults = await this.CreateSampleDocument(database, collection, doc, ConsistencyLevel.Session);
            var sessionReadResults = await this.ReadSampleDocument(sessionWriteResults.Resource.Id, ConsistencyLevel.Session);

            doc.Id = Guid.NewGuid().ToString();
            var eventualWriteResults = await this.CreateSampleDocument(database, collection, doc, ConsistencyLevel.Eventual);
            var eventualReadResults = await this.ReadSampleDocument(eventualWriteResults.Resource.Id, ConsistencyLevel.Eventual);

            
            //PrintDetails(strongWriteResults, "Strong write");
            PrintDetails(boundedWriteResults, "Bounded write");
            PrintDetails(sessionWriteResults, "Session write");
            PrintDetails(eventualWriteResults, "Eventual write");

            //PrintDetails(strongReadResults, "Strong read");
            PrintDetails(boundedReadResults, "Bounded read");
            PrintDetails(sessionReadResults, "Session read");
            PrintDetails(eventualReadResults, "Eventual read");

        }

        private void PrintDetails(ResourceResponse<Document> results, string consistencyType)
        {
            Console.WriteLine("{0} - {1:0.00} RU", consistencyType, results.RequestCharge);
        }

        private async Task<ResourceResponse<Document>> CreateSampleDocument(string databaseName, string collectionName, SampleDocument sample, ConsistencyLevel level)
        {
            var options = new RequestOptions() {ConsistencyLevel = level};

            var result = await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), sample, options);
            return result;
        }
        private async Task<ResourceResponse<Document>> ReadSampleDocument(string id, ConsistencyLevel level)
        {
            var options = new RequestOptions() {ConsistencyLevel = level};
            var docUri = UriFactory.CreateDocumentUri(database, collection, id);
            var result = await client.ReadDocumentAsync(docUri, options);
            return result;
        }
    }
}
