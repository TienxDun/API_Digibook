using Microsoft.AspNetCore.Mvc;

namespace API_DigiBook.Interfaces.Services
{
    public interface ITikiService
    {
        Task<byte[]> GetTikiDataAsync(string url);
        Task<string> GetTikiDataAsStringAsync(string url);
    }
}
