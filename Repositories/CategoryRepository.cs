using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Repositories
{
    public class CategoryRepository : FirestoreRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ICacheService cache, ILogger<CategoryRepository> logger) 
            : base("categories", cache, logger)
        {
        }

        public async Task<Category?> GetByNameAsync(string name)
        {
            try
            {
                // Case-insensitive category name search
                var allCategories = await GetAllAsync();
                return allCategories.FirstOrDefault(c => 
                    string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting category by name {Name}", name);
                throw;
            }
        }
    }
}
