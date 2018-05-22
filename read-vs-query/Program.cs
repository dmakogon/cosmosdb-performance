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

namespace ReadsAndQueries
{
    class Program
    {

        const int runs = 10;

        public static IConfiguration Config { get; set; }

        static string EndpointUrl;
        static string PrimaryKey;
        static string database;
        static string collection;
        static DocumentClient client;
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

            client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

            try
            {
                Program p = new Program();
                p.CompareReadVsQuery().Wait();
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

        private async Task CompareReadVsQuery()
        {
            await client.OpenAsync().ConfigureAwait(false);
            var random = new Random((int)DateTime.Now.Ticks);
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
 
            await this.CreateSampleDocument(database, collection, doc);
            double readRU = await this.ReadSampleDocument(doc.Id);
            Console.WriteLine("Read cost: {0} RU", readRU);

            double queryRU = await this.QuerySampleDocument(doc.Id);
            Console.WriteLine("Query cost: {0} RU", queryRU);

            
            double queryRangeRU = await this.QuerySampleDocumentByRange(doc.Id);
            Console.WriteLine("Query cost: {0} RU", queryRangeRU);
        }

        private async Task<double> CreateSampleDocument(string databaseName, string collectionName, SampleDocument sample)
        {

            var result = await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), sample);
            return result.RequestCharge;

        }
        private async Task<double> ReadSampleDocument(string id)
        {
            var docUri = UriFactory.CreateDocumentUri(database, collection, id);
            var result = await client.ReadDocumentAsync(docUri);
            return result.RequestCharge;
        }

        private async Task<double> QuerySampleDocument(string id)
        {
            var RU = 0.0;
            var collectionUri = UriFactory.CreateDocumentCollectionUri(database, collection);
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = 1 };

            var queryable = client.CreateDocumentQuery(collectionUri,
                String.Format("SELECT * FROM docs WHERE docs.Id = '{0}'", id), queryOptions).AsDocumentQuery();
            while (queryable.HasMoreResults)
            {
                FeedResponse<dynamic> queryResponse = await queryable.ExecuteNextAsync<dynamic>();
                RU += queryResponse.RequestCharge;
            }
            return RU;
        }

        private async Task<double> QuerySampleDocumentByRange(string id)
        {
            var RU = 0.0;
            var collectionUri = UriFactory.CreateDocumentCollectionUri(database, collection);
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = 1 };

            var queryable = client.CreateDocumentQuery(collectionUri,
                String.Format("SELECT top 1 * FROM docs WHERE docs.prop10 > 50", id), queryOptions).AsDocumentQuery();
            while (queryable.HasMoreResults)
            {
                FeedResponse<dynamic> queryResponse = await queryable.ExecuteNextAsync<dynamic>();
                RU += queryResponse.RequestCharge;
            }
            return RU;
        }
    }
}
