using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace bulk_import_demos
{
    class Program
    {
        private const string EndpointUrl = "https://<your-account>.documents.azure.com:443/";
        private const string AuthorizationKey = "<your-account-key>";
        private const string DatabaseName = "bulk-tutorial";
        private const string ContainerName = "items";
        private const int ItemsToInsert = 300000;
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey, new CosmosClientOptions()
            {
                AllowBulkExecution = true
            });
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(Program.DatabaseName);
            await database.DefineContainer(Program.ContainerName, "/pk")
                .WithIndexingPolicy()
                .WithIndexingMode(IndexingMode.Consistent)
                .WithIncludedPaths()
                .Attach()
                .WithExcludedPaths()
                .Path("/*")
                .Attach()
                .Attach().CreateAsync(50000);
            Dictionary<PartitionKey, Stream> itemsToInsert = new Dictionary<PartitionKey, Stream>(ItemsToInsert);
            foreach (Item item in GetItemsToInsert())
            {
                MemoryStream stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, item);
                itemsToInsert.Add(new PartitionKey(item.pk), stream);
            }
            Container container = database.GetContainer(ContainerName);
            List<Task> tasks = new List<Task>(ItemsToInsert);
            foreach (KeyValuePair<PartitionKey, Stream> item in itemsToInsert)
            {
                tasks.Add(container.CreateItemStreamAsync(item.Value, item.Key).ContinueWith(
                    task =>
                    {
                        using ResponseMessage response = task.Result;
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Received {response.StatusCode} ({response.ErrorMessage}) status codefor operation {response.RequestMessage.RequestUri}.");
                        }
                    }));
            }
            // Wait until all are done
            await Task.WhenAll(tasks);
            Console.WriteLine("Bye World!");
            Console.ReadLine();
        }

        private static IReadOnlyCollection<Item> GetItemsToInsert()
        {
            List<Item> generate = new Bogus.Faker<Item>().StrictMode(true)
                .RuleFor(o => o.id, f => Guid.NewGuid().ToString())
                .RuleFor(o => o.username, f => f.Internet.UserName())
                .RuleFor(o => o.pk, (f, o) => o.id)
                .Generate(ItemsToInsert);
            return generate;
        }
    }
}
