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

namespace RequestUnits
{
    class Program
    {
        public static IConfiguration Config { get; set; }

        static string EndpointUrl;
        static string PrimaryKey;
        static string database;
        static string collection;
        static DocumentClient client;

        static int newRU;
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

            // NOTE: RU must be a multiple of 100.
            // Also note: The RU range must be within the range of the collection type
            newRU = Int32.Parse( Config["RequestUnits"]) ;

            client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

            try
            {
                Program p = new Program();
                p.ChangeOffer().Wait();
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

        private async Task ChangeOffer()
        {
            await client.OpenAsync().ConfigureAwait(false);
 
            var originalRU = await this.SetCollectionRU(newRU);
            await this.SetCollectionRU(originalRU);
        }
        private async Task<int> SetCollectionRU(int targetRU)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(database, collection).ToString();

            // to query for a collection's offer, we first need the collection object
            // from the database, which will contain the (needed) self-link.
            DocumentCollection coll = await client.ReadDocumentCollectionAsync(collectionUri);

            // Note: The "Offer" class does not expose throughput as a property (it's a legacy
            // class). The new "OfferV2" class *does* expose it, and we can cast the returned Offer
            // class to OfferV2. This allows us to capture the collection's current throughput,
            // before scaling to a different number.
            var offer = (OfferV2) client.CreateOfferQuery()
                .Where(r => r.ResourceLink == coll.SelfLink)    
                .AsEnumerable()
                .SingleOrDefault();
    
            var currentRU = offer.Content.OfferThroughput;
            Console.WriteLine("Changing RU from {0} to {1}", currentRU, targetRU);

            // we cannot just adjust the current offer's throughput, as the property is
            // read-only. We have to create a new instance, for the purpose of scaling.
            offer = new OfferV2(offer, targetRU);
            await client.ReplaceOfferAsync(offer);
            return currentRU;
        }
    }
}
