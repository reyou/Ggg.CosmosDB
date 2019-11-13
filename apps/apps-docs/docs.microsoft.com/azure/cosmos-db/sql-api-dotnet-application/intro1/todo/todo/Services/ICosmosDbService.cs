using System.Collections.Generic;
using System.Threading.Tasks;
using todo.Models;

namespace todo.Services
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<Item>> GetItemsAsync(string selectFromC);
        Task AddItemAsync(Item item);
        Task UpdateItemAsync(string itemId, Item item);
        Task<Item> GetItemAsync(string id);
        Task DeleteItemAsync(string id);
    }
}