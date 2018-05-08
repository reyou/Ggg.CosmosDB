using todo.Models;

namespace todo
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public static class DocumentDBRepository<T> where T : class
    {
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["database"];
        private static readonly string CollectionId = ConfigurationManager.AppSettings["collection"];
        private static DocumentClient client;

        public static async Task<T> GetItemAsync(string id, string category)
        {
            try
            {
                // ReadDocumentAsync: Reads a Document as an asynchronous operation
                //in the Azure Cosmos DB service.
                // UriFactory: Helper class to assist in creating the various Uris
                // needed for use with the DocumentClient instance in the Azure
                // Cosmos DB service.
                Document document =
                    await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                return (T)(dynamic)document;
            }
            // The base class for client exceptions in the Azure Cosmos DB service
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            // IDocumentQuery: Provides methods to support query pagination and asynchronous
            // execution in the Azure Cosmos DB service
            // FeedOptions: Specifies the options associated with feed methods (enumeration operations)
            // in the Azure Cosmos DB service
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.client.feedoptions?view=azure-dotnet
            // AsDocumentQuery: Converts an IQueryable to an IDocumentQuery, which supports
            // pagination and asynchronous execution
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions
                {
                    MaxItemCount = -1,
                    EnableCrossPartitionQuery = true
                })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                // Executes the query and retrieves the next page of results
                // as dynamic objects in the Azure Cosmos DB service
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task<Document> CreateItemAsync(T item)
        {
            // Creates a document as an asychronous operation in the Azure Cosmos DB service
            /* A document is a structured JSON document. There is no set schema for the
             JSON documents, and a document may contain any number of custom properties 
             as well as an optional list of attachments. Document is an application 
             resource and can be authorized using the master key or resource keys.*/
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
        }

        public static async Task<Document> UpdateItemAsync(string id, T item)
        {
            // Replaces a document as an asynchronous operation in the Azure Cosmos DB service
            return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
        }

        public static async Task DeleteItemAsync(string id, string category)
        {
            // Delete a Document as an asynchronous operation in the Azure Cosmos DB service
            await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
        }

        public static void Initialize()
        {
            // DocumentClient: Provides a client-side logical representation for the Azure
            // Cosmos DB service. This client is used to configure and execute requests
            // against the service.
            client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["endpoint"]), ConfigurationManager.AppSettings["authKey"]);
            // <summary>Waits for the <see cref="T:System.Threading.Tasks.Task" /> to complete execution.</summary>
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                // Reads a Database as an asynchronous operation in the Azure Cosmos DB service
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Creates a database resource as an asychronous operation in the Azure Cosmos DB service
                    await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                // Reads a DocumentCollection as an asynchronous operation in the Azure Cosmos DB service
                // DocumentCollection: Represents a document collection in the Azure Cosmos DB service.
                // A collection is a named logical container for documents
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection
                        {
                            Id = CollectionId
                        },
                        new RequestOptions { OfferThroughput = 400 });
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
