using API_DigiBook.Models;

namespace API_DigiBook.Interfaces.Services
{
    public interface IMembershipService
    {
        Task<User?> RefreshMembershipAsync(string userId);
        Task<int> SyncAllUsersMembershipAsync();
    }
}
