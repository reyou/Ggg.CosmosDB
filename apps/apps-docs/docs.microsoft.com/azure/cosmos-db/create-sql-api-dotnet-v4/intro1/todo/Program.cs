using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace todo
{
    /// <summary>
    /// https://github.com/reyou/Ggg.CosmosDB
    /// https://www.nuget.org/packages/Microsoft.Azure.Cosmos
    /// </summary>
    static class Program
    {
        // private const string EndpointUrl = "https://<your-account>.documents.azure.com:443/";
        private static readonly string EndpointUrl = File.ReadAllText("C:\\apikeys\\create-sql-api-dotnet-v4\\endpointurl.txt");
        private static readonly string AuthorizationKey = File.ReadAllText("C:\\apikeys\\create-sql-api-dotnet-v4\\authorizationkey.txt");
        private const string DatabaseId = "FamilyDatabase";
        private const string ContainerId = "FamilyContainer";
        static async Task Main()
        {
            Console.WriteLine("Hello World!");
            CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey);
            await CreateDatabaseAsync(cosmosClient);
            await CreateContainerAsync(cosmosClient);
            await AddItemsToContainerAsync(cosmosClient);
            await QueryItemsAsync(cosmosClient);
            await ReplaceFamilyItemAsync(cosmosClient);
            await DeleteFamilyItemAsync(cosmosClient);
            await DeleteDatabaseAndCleanupAsync(cosmosClient);
            Console.WriteLine("Bye World!");
            Console.ReadLine();
        }

        private static async Task DeleteDatabaseAndCleanupAsync(CosmosClient cosmosClient)
        {
            Database database = cosmosClient.GetDatabase(DatabaseId);
            DatabaseResponse databaseResourceResponse = await database.DeleteAsync();
            Console.WriteLine("Deleted Database: {0}\n", DatabaseId);
        }

        private static async Task DeleteFamilyItemAsync(CosmosClient cosmosClient)
        {
            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);
            string partitionKeyValue = "Wakefield"; string familyId = "Wakefield.7";
            // Delete an item. Note we must provide the partition key value and id of the item to delete
            ItemResponse<Family> wakefieldFamilyResponse = await container.DeleteItemAsync<Family>(familyId, new PartitionKey(partitionKeyValue));
            Console.WriteLine("Deleted Family [{0},{1}]\n", partitionKeyValue, familyId);
        }

        private static async ValueTask ReplaceFamilyItemAsync(CosmosClient cosmosClient)
        {
            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);
            ItemResponse<Family> wakefieldFamilyResponse = await container.ReadItemAsync<Family>("Wakefield.7", new PartitionKey("Wakefield"));
            Family itemBody = wakefieldFamilyResponse;
            // update registration status from false to true
            itemBody.IsRegistered = true;
            // update grade of child
            itemBody.Children[0].Grade = 6;
            // replace the item with the updated content
            wakefieldFamilyResponse = await container.ReplaceItemAsync<Family>(itemBody, itemBody.Id, new PartitionKey(itemBody.LastName));
            Console.WriteLine("Updated Family [{0},{1}].\n \tBody is now: {2}\n", itemBody.LastName, itemBody.Id, wakefieldFamilyResponse.Resource);

        }

        private static async Task QueryItemsAsync(CosmosClient cosmosClient)
        {
            string sqlQueryText = "SELECT * FROM c WHERE c.LastName = 'Andersen'";
            Console.WriteLine("Running query: {0}\n", sqlQueryText);
            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<Family> families = new List<Family>();
            FeedIterator<Family> itemQueryIterator = container.GetItemQueryIterator<Family>(queryDefinition);
            foreach (Family family in await itemQueryIterator.ReadNextAsync())
            {
                families.Add(family); Console.WriteLine("\tRead {0}\n", family);
            }
        }

        private static async Task AddItemsToContainerAsync(CosmosClient cosmosClient)
        {
            Family andersenFamily = new Family
            {
                Id = "Andersen.1",
                LastName = "Andersen",
                Parents = new Parent[]
                {
                    new Parent { FirstName = "Thomas" }, new Parent { FirstName = "Mary Kay" }
                },
                Children = new Child[]
                {
                    new Child
                    {
                        FirstName = "Henriette Thaulow", Gender = "female", Grade = 5, Pets = new Pet[] { new Pet
                        {
                            GivenName = "Fluffy"
                        }

                        }
                    }
                },
                Address = new Address { State = "WA", County = "King", City = "Seattle" },
                IsRegistered = false
            };
            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);
            try
            {
                ItemResponse<Family> andersenFamilyResponse = await container.ReadItemAsync<Family>(andersenFamily.Id, new PartitionKey(andersenFamily.LastName));
                Console.WriteLine("Item in database with id: {0} already exists\n", andersenFamilyResponse.Resource.Id);

            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ItemResponse<Family> andersenFamilyResponse = await container.CreateItemAsync<Family>(andersenFamily, new PartitionKey(andersenFamily.LastName));
                Console.WriteLine("Created item in database with id: {0}\n", andersenFamilyResponse.Resource.Id);
            }
            Family wakefieldFamily = new Family
            {
                Id = "Wakefield.7",
                LastName = "Wakefield",
                Parents = new Parent[]
                {
                    new Parent
                    {
                        FamilyName = "Wakefield", FirstName = "Robin"
                    }, new Parent { FamilyName = "Miller", FirstName = "Ben" }
                },
                Children = new Child[]        {
                    new Child            {                FamilyName = "Merriam",                FirstName = "Jesse",                Gender = "female",                Grade = 8,
                             Pets = new Pet[]                {                    new Pet { GivenName = "Goofy" },                    new Pet { GivenName = "Shadow" }                }            },            new Child            {                FamilyName = "Miller",                FirstName = "Lisa",                Gender = "female",                Grade = 1            }        },
                Address = new Address { State = "NY", County = "Manhattan", City = "NY" },
                IsRegistered = true
            };
            ItemResponse<Family> wakefieldFamilyResponse = await container.UpsertItemAsync<Family>(wakefieldFamily, new PartitionKey(wakefieldFamily.LastName));
            Console.WriteLine("Created item in database with id: {0}\n", wakefieldFamilyResponse.Resource.Id);
        }

        private static async Task CreateContainerAsync(CosmosClient cosmosClient)
        {
            // Create a new container
            Database database = cosmosClient.GetDatabase(DatabaseId);
            Container container = await database.CreateContainerIfNotExistsAsync(ContainerId, "/LastName");
            Console.WriteLine("Created Container: {0}\n", container.Id);
        }

        private static async Task CreateDatabaseAsync(CosmosClient cosmosClient)
        {
            // Create a new database
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
            Console.WriteLine("Created Database: {0}\n", database.Id);
        }
    }
}
