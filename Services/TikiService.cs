using API_DigiBook.Interfaces.Services;
using System.Net.Http;

namespace API_DigiBook.Services
{
    public class TikiService : ITikiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TikiService> _logger;

        public TikiService(IHttpClientFactory httpClientFactory, ILogger<TikiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> GetTikiDataAsStringAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Url is required.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri))
                throw new ArgumentException("Invalid target url.");

            if (!string.Equals(targetUri.Host, "tiki.vn", StringComparison.OrdinalIgnoreCase) && 
                !targetUri.Host.EndsWith(".tiki.vn", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Only tiki.vn is allowed.");
            }

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, targetUri);
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 DigiBook/1.0");
            request.Headers.TryAddWithoutValidation("Accept", "application/json,text/plain,*/*");
            request.Headers.TryAddWithoutValidation("Referer", "https://tiki.vn/");

            using var response = await client.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Tiki proxy failed with status {StatusCode} for {TargetUrl}", (int)response.StatusCode, targetUri);
                throw new HttpRequestException($"Failed to fetch Tiki data. Status: {response.StatusCode}", null, response.StatusCode);
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<byte[]> GetTikiDataAsync(string url)
        {
            var content = await GetTikiDataAsStringAsync(url);
            return System.Text.Encoding.UTF8.GetBytes(content);
        }
    }
}
