using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Singleton;
using API_DigiBook.Services;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using API_DigiBook.Interfaces.Services;
using System.Text.RegularExpressions;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILogger<BooksController> _logger;
        private readonly LoggerService _systemLogger;
        private readonly ICacheService _cache;

        public BooksController(IBookRepository bookRepository, ILogger<BooksController> logger, ICacheService cache)
        {
            _bookRepository = bookRepository;
            _logger = logger;
            _systemLogger = LoggerService.Instance; 
            _cache = cache;
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var count = await _bookRepository.CountAsync();
                return Ok(new { success = true, message = "Firebase connection successful!", booksCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Firebase connection");
                return StatusCode(500, new { success = false, message = "Firebase connection failed", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBooks()
        {
            try
            {
                List<Book>? books;
                if (IsForceRefresh())
                {
                    books = (await _bookRepository.GetAllAsync()).ToList();
                }
                else
                {
                    var cacheKey = _cache.GetVersionedKey("books:all");
                    books = await _cache.GetOrSetAsync(cacheKey, async () => 
                    {
                        var all = await _bookRepository.GetAllAsync();
                        return all.ToList();
                    });
                }

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books");
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("isbn/{isbn}")]
        public async Task<IActionResult> GetBookByIsbn(string isbn)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:isbn:{isbn}");
                var book = await _cache.GetOrSetAsync(cacheKey, () => _bookRepository.GetByIsbnAsync(isbn));

                if (book == null)
                {
                    return NotFound(new { success = false, message = $"Book with ISBN '{isbn}' not found" });
                }

                return Ok(new { success = true, data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book by ISBN: {Isbn}", isbn);
                return StatusCode(500, new { success = false, message = "Error retrieving book", error = ex.Message });
            }
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBookBySlug(string slug)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:slug:{slug}");
                var book = await _cache.GetOrSetAsync(cacheKey, () => _bookRepository.GetBySlugAsync(slug));

                if (book == null)
                {
                    return NotFound(new { success = false, message = $"Book with slug '{slug}' not found" });
                }

                // Note: Incrementing views happens here for backward compatibility with direct slug calls
                await _bookRepository.IncrementViewCountAsync(book.Id);
                _cache.BumpVersion("books");
                return Ok(new { success = true, data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book by slug: {Slug}", slug);
                return StatusCode(500, new { success = false, message = "Error retrieving book", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBook([FromBody] Book book)
        {
            try
            {
                book = SanitizeBook(book);
                book.CreatedAt = Timestamp.GetCurrentTimestamp();
                book.UpdatedAt = Timestamp.GetCurrentTimestamp();

                var bookId = await _bookRepository.AddAsync(book, book.Id);
                book.Id = bookId;

                _cache.BumpVersion("books");

                return CreatedAtAction(nameof(GetBookByIsbn), new { isbn = book.Isbn }, new { success = true, message = "Book created successfully", data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                return StatusCode(500, new { success = false, message = "Error creating book", error = ex.Message });
            }
        }

        [HttpPut("isbn/{isbn}")]
        public async Task<IActionResult> UpdateBook(string isbn, [FromBody] Book book)
        {
            try
            {
                book = SanitizeBook(book);
                book.Isbn = isbn;
                book.UpdatedAt = Timestamp.GetCurrentTimestamp();
                var updated = await _bookRepository.UpdateByIsbnAsync(isbn, book);

                if (!updated)
                {
                    return NotFound(new { success = false, message = $"Book with ISBN '{isbn}' not found" });
                }

                _cache.BumpVersion("books");
                return Ok(new { success = true, message = "Book updated successfully", data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book with ISBN: {Isbn}", isbn);
                return StatusCode(500, new { success = false, message = "Error updating book", error = ex.Message });
            }
        }

        [HttpPost("by-ids")]
        public async Task<IActionResult> GetBooksByIds([FromBody] JsonElement body)
        {
            try
            {
                var bookIds = ExtractBookIds(body);
                if (bookIds == null || !bookIds.Any()) return BadRequest(new { success = false, message = "Book IDs are required" });

                var cacheKey = _cache.GetVersionedKey($"books:batch:{string.Join(",", bookIds.OrderBy(x => x))}");
                var books = await _cache.GetOrSetAsync(cacheKey, () => _bookRepository.GetByIdsAsync(bookIds));

                return Ok(new { success = true, count = books?.Count() ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books by IDs");
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("related")]
        public async Task<IActionResult> GetRelatedBooks(
            [FromQuery] string category, 
            [FromQuery] string currentBookId, 
            [FromQuery] string? author = null, 
            [FromQuery] int limit = 5)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:related:{currentBookId}:{category}:{author}:{limit}");
                var books = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var all = await _bookRepository.GetAllAsync();
                    var related = all.Where(b => b.Id != currentBookId && 
                        (string.Equals(b.Category, category, StringComparison.OrdinalIgnoreCase) || 
                         (!string.IsNullOrEmpty(author) && string.Equals(b.Author, author, StringComparison.OrdinalIgnoreCase))))
                        .Take(limit)
                        .ToList();
                    return related;
                });

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related books for: {Id}", currentBookId);
                return StatusCode(500, new { success = false, message = "Error retrieving related books", error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title)) return BadRequest(new { success = false, message = "Search title is required" });
                
                var books = await _bookRepository.SearchByTitleAsync(title);
                return Ok(new { success = true, count = books.Count(), data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching books by title: {Title}", title);
                return StatusCode(500, new { success = false, message = "Error searching books", error = ex.Message });
            }
        }

        [HttpGet("top-rated")]
        public async Task<IActionResult> GetTopRatedBooks([FromQuery] int count = 10)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:top-rated:{count}");
                var books = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var result = await _bookRepository.GetTopRatedAsync(count);
                    return result.ToList();
                });

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top rated books");
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("paginated")]
        public async Task<IActionResult> GetBooksPaginated(
            [FromQuery] string? limit = null,
            [FromQuery] string? offset = null,
            [FromQuery] string? category = null,
            [FromQuery] string? sortBy = null)
        {
            try
            {
                int limitValue = int.TryParse(limit, out var l) && l > 0 ? l : 10;
                int offsetValue = int.TryParse(offset, out var o) && o >= 0 ? o : 0;
                var sortValue = sortBy ?? "newest";

                var cacheKey = _cache.GetVersionedKey($"books:paginated:{category ?? "all"}:{sortValue}:{offsetValue}:{limitValue}");
                var page = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var allBooks = await _bookRepository.GetAllAsync();
                    var filtered = string.IsNullOrWhiteSpace(category) ? allBooks : allBooks.Where(b => string.Equals(b.Category, category, StringComparison.OrdinalIgnoreCase));
                    var sorted = sortValue switch
                    {
                        "price_asc" => filtered.OrderBy(b => b.Price),
                        "price_desc" => filtered.OrderByDescending(b => b.Price),
                        "rating" => filtered.OrderByDescending(b => b.Rating),
                        _ => filtered.OrderByDescending(b => GetTimestampOrMin(b.UpdatedAt, b.CreatedAt))
                    };
                    return sorted.Skip(offsetValue).Take(limitValue).ToList();
                });

                return Ok(new { success = true, count = page?.Count ?? 0, data = page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated books");
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookById(string id)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:id:{id}");
                var book = await _cache.GetOrSetAsync(cacheKey, () => _bookRepository.GetByIdAsync(id));

                if (book == null)
                {
                    return NotFound(new { success = false, message = $"Book with ID '{id}' not found" });
                }

                return Ok(new { success = true, data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book by ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving book", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBookById(string id, [FromBody] JsonElement updates)
        {
            try
            {
                var updateDict = JsonElementToDictionary(updates);
                var allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "title", "author", "authorId", "authorBio", "category", "price", "originalPrice",
                    "stockQuantity", "cover", "description", "isbn", "pages", "publisher", 
                    "publishYear", "language", "badge", "isAvailable", "slug", "viewCount",
                    "searchKeywords", "reviewCount", "quantitySold", "badges", "discountRate", "images", "dimensions", 
                    "translator", "bookLayout", "manufacturer"
                };

                var filteredUpdates = updateDict
                    .Where(kvp => allowedFields.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                filteredUpdates = SanitizeBookUpdates(filteredUpdates);

                if (filteredUpdates.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No valid fields provided for update" });
                }

                filteredUpdates["updatedAt"] = Timestamp.GetCurrentTimestamp();
                var updated = await _bookRepository.UpdateFieldsAsync(id, filteredUpdates);

                if (!updated)
                {
                    return NotFound(new { success = false, message = $"Book with ID '{id}' not found" });
                }

                _cache.BumpVersion("books");
                return Ok(new { success = true, message = "Book updated successfully", data = await _bookRepository.GetByIdAsync(id) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book by ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error updating book", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBookById(string id)
        {
            try
            {
                var deleted = await _bookRepository.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(new { success = false, message = $"Book with ID '{id}' not found" });
                }

                _cache.BumpVersion("books");
                return Ok(new { success = true, message = "Book deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error deleting book", error = ex.Message });
            }
        }

        [HttpDelete("isbn/{isbn}")]
        public async Task<IActionResult> DeleteBook(string isbn)
        {
            try
            {
                var deleted = await _bookRepository.DeleteByIsbnAsync(isbn);
                if (!deleted)
                {
                    return NotFound(new { success = false, message = $"Book with ISBN '{isbn}' not found" });
                }

                _cache.BumpVersion("books");
                return Ok(new { success = true, message = "Book deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book with ISBN: {Isbn}", isbn);
                return StatusCode(500, new { success = false, message = "Error deleting book", error = ex.Message });
            }
        }

        [HttpPost("{id}/increment-views")]
        public async Task<IActionResult> IncrementViewCount(string id)
        {
            try
            {
                var success = await _bookRepository.IncrementViewCountAsync(id);
                if (!success) return NotFound(new { success = false, message = "Book not found" });
                
                _cache.BumpVersion("books");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing view count: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error incrementing view count", error = ex.Message });
            }
        }

        [HttpGet("author/{name}")]
        public async Task<IActionResult> GetBooksByAuthor(string name)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:author:{name}");
                var books = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var all = await _bookRepository.GetAllAsync();
                    return all.Where(b => string.Equals(b.Author, name, StringComparison.OrdinalIgnoreCase)).ToList();
                });

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books by author: {Name}", name);
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetBooksByCategory(string category)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:category:{category}");
                var books = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var result = await _bookRepository.GetByCategoryAsync(category);
                    return result.ToList();
                });

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books by category: {Category}", category);
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }


        private static List<string> ExtractBookIds(JsonElement body)
        {
            var ids = new List<string>();
            if (body.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in body.EnumerateArray()) if (item.ValueKind == JsonValueKind.String) ids.Add(item.GetString()!);
            }
            else if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("bookIds", out var el) && el.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in el.EnumerateArray()) if (item.ValueKind == JsonValueKind.String) ids.Add(item.GetString()!);
            }
            return ids;
        }

        private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object?>();
            if (element.ValueKind != JsonValueKind.Object) return result;
            foreach (var property in element.EnumerateObject())
            {
                var val = ConvertJsonElement(property.Value);
                if (val != null) result[property.Name] = val;
            }
            return result;
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                _ => null
            };
        }

        private static DateTime GetTimestampOrMin(Timestamp updatedAt, Timestamp createdAt)
        {
            if (!updatedAt.Equals(default(Timestamp))) return updatedAt.ToDateTime();
            if (!createdAt.Equals(default(Timestamp))) return createdAt.ToDateTime();
            return DateTime.MinValue;
        }

        private static Book SanitizeBook(Book book)
        {
            book.Id = NormalizeWhitespace(book.Id);
            book.Title = NormalizeWhitespace(book.Title);
            book.Author = NormalizeWhitespace(book.Author);
            book.AuthorId = NormalizeWhitespace(book.AuthorId);
            book.AuthorBio = NormalizeWhitespace(book.AuthorBio);
            book.Category = NormalizeWhitespace(book.Category);
            book.Price = Math.Max(0, book.Price);
            book.OriginalPrice = Math.Max(book.Price, book.OriginalPrice);
            book.StockQuantity = Math.Max(0, book.StockQuantity);
            book.Rating = Math.Max(0, book.Rating);
            book.Cover = NormalizeWhitespace(book.Cover);
            book.Description = NormalizeDescription(book.Description);
            book.Isbn = NormalizeWhitespace(book.Isbn);
            book.Pages = Math.Max(0, book.Pages);
            book.Publisher = NormalizeWhitespace(book.Publisher);
            book.PublishYear = Math.Max(0, book.PublishYear);
            book.Language = string.IsNullOrWhiteSpace(book.Language) ? "Tiếng Việt" : NormalizeWhitespace(book.Language);
            book.Badge = NormalizeWhitespace(book.Badge);
            book.Slug = NormalizeWhitespace(book.Slug);
            book.ViewCount = Math.Max(0, book.ViewCount);
            book.ReviewCount = Math.Max(0, book.ReviewCount);
            book.DiscountRate = Math.Max(0, book.DiscountRate);
            book.Images = SanitizeStringList(book.Images);
            book.Dimensions = NormalizeWhitespace(book.Dimensions);
            book.Translator = NormalizeWhitespace(book.Translator);
            book.BookLayout = NormalizeWhitespace(book.BookLayout);
            book.Manufacturer = NormalizeWhitespace(book.Manufacturer);
            book.SearchKeywords = SanitizeStringList(book.SearchKeywords);

            if (book.QuantitySold != null)
            {
                book.QuantitySold.Text = NormalizeWhitespace(book.QuantitySold.Text);
                book.QuantitySold.Value = Math.Max(0, book.QuantitySold.Value);
                if (string.IsNullOrWhiteSpace(book.QuantitySold.Text) && book.QuantitySold.Value > 0)
                {
                    book.QuantitySold.Text = $"{book.QuantitySold.Value} đã bán";
                }
                if (string.IsNullOrWhiteSpace(book.QuantitySold.Text) && book.QuantitySold.Value <= 0)
                {
                    book.QuantitySold = null;
                }
            }

            book.Badges = (book.Badges ?? new List<BookBadge>())
                .Where(b => b != null)
                .Select(b => new BookBadge
                {
                    Code = NormalizeWhitespace(b.Code),
                    Text = NormalizeWhitespace(b.Text),
                    Type = NormalizeWhitespace(b.Type)
                })
                .Where(b => !string.IsNullOrWhiteSpace(b.Code) || !string.IsNullOrWhiteSpace(b.Text) || !string.IsNullOrWhiteSpace(b.Type))
                .GroupBy(b => $"{b.Code}|{b.Text}|{b.Type}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (string.IsNullOrWhiteSpace(book.Badge) && book.DiscountRate > 0)
            {
                book.Badge = $"-{Math.Round(book.DiscountRate)}%";
            }

            if (string.IsNullOrWhiteSpace(book.Cover) && book.Images.Any())
            {
                book.Cover = book.Images.First();
            }

            if (!book.Images.Any() && !string.IsNullOrWhiteSpace(book.Cover))
            {
                book.Images = new List<string> { book.Cover };
            }

            return book;
        }

        private static Dictionary<string, object?> SanitizeBookUpdates(Dictionary<string, object?> updates)
        {
            var sanitized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in updates)
            {
                switch (kvp.Key)
                {
                    case "title":
                    case "author":
                    case "authorId":
                    case "authorBio":
                    case "category":
                    case "cover":
                    case "description":
                    case "isbn":
                    case "publisher":
                    case "language":
                    case "badge":
                    case "slug":
                    case "dimensions":
                    case "translator":
                    case "bookLayout":
                    case "manufacturer":
                        sanitized[kvp.Key] = kvp.Key == "description"
                            ? NormalizeDescription(kvp.Value?.ToString() ?? string.Empty)
                            : NormalizeWhitespace(kvp.Value?.ToString() ?? string.Empty);
                        break;
                    case "price":
                    case "originalPrice":
                    case "rating":
                    case "discountRate":
                        sanitized[kvp.Key] = Math.Max(0, Convert.ToDouble(kvp.Value ?? 0));
                        break;
                    case "stockQuantity":
                    case "pages":
                    case "publishYear":
                    case "viewCount":
                    case "reviewCount":
                        sanitized[kvp.Key] = Math.Max(0, Convert.ToInt32(kvp.Value ?? 0));
                        break;
                    case "searchKeywords":
                    case "images":
                        sanitized[kvp.Key] = SanitizeObjectStringList(kvp.Value);
                        break;
                    case "quantitySold":
                        sanitized[kvp.Key] = SanitizeQuantitySold(kvp.Value);
                        break;
                    case "badges":
                        sanitized[kvp.Key] = SanitizeBadges(kvp.Value);
                        break;
                    default:
                        sanitized[kvp.Key] = kvp.Value;
                        break;
                }
            }

            if (sanitized.TryGetValue("price", out var priceObj) && sanitized.TryGetValue("originalPrice", out var originalPriceObj))
            {
                var price = Convert.ToDouble(priceObj ?? 0);
                var originalPrice = Convert.ToDouble(originalPriceObj ?? 0);
                sanitized["originalPrice"] = Math.Max(price, originalPrice);
            }

            if ((!sanitized.ContainsKey("badge") || string.IsNullOrWhiteSpace(sanitized["badge"]?.ToString()))
                && sanitized.TryGetValue("discountRate", out var discountRateObj)
                && Convert.ToDouble(discountRateObj ?? 0) > 0)
            {
                sanitized["badge"] = $"-{Math.Round(Convert.ToDouble(discountRateObj ?? 0))}%";
            }

            if (sanitized.TryGetValue("images", out var imagesObj)
                && imagesObj is List<string> images
                && images.Any()
                && (!sanitized.TryGetValue("cover", out var coverObj) || string.IsNullOrWhiteSpace(coverObj?.ToString())))
            {
                sanitized["cover"] = images.First();
            }

            return sanitized;
        }

        private static List<string> SanitizeStringList(List<string>? values)
        {
            return (values ?? new List<string>())
                .Select(NormalizeWhitespace)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> SanitizeObjectStringList(object? value)
        {
            if (value is IEnumerable<object> objectValues)
            {
                return objectValues
                    .Select(v => NormalizeWhitespace(v?.ToString() ?? string.Empty))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return new List<string>();
        }

        private static object? SanitizeQuantitySold(object? value)
        {
            if (value is Dictionary<string, object?> quantitySold)
            {
                var text = NormalizeWhitespace(quantitySold.TryGetValue("text", out var textObj) ? textObj?.ToString() ?? string.Empty : string.Empty);
                var quantity = Math.Max(0, Convert.ToInt32(quantitySold.TryGetValue("value", out var valueObj) ? valueObj ?? 0 : 0));
                if (string.IsNullOrWhiteSpace(text) && quantity > 0)
                {
                    text = $"{quantity} đã bán";
                }
                if (string.IsNullOrWhiteSpace(text) && quantity == 0)
                {
                    return null;
                }

                return new Dictionary<string, object?>
                {
                    ["text"] = text,
                    ["value"] = quantity
                };
            }

            return null;
        }

        private static object SanitizeBadges(object? value)
        {
            var sanitized = new List<Dictionary<string, object?>>();
            if (value is IEnumerable<object> badges)
            {
                foreach (var badge in badges)
                {
                    if (badge is not Dictionary<string, object?> badgeObject) continue;

                    var code = NormalizeWhitespace(badgeObject.TryGetValue("code", out var codeObj) ? codeObj?.ToString() ?? string.Empty : string.Empty);
                    var text = NormalizeWhitespace(badgeObject.TryGetValue("text", out var textObj) ? textObj?.ToString() ?? string.Empty : string.Empty);
                    var type = NormalizeWhitespace(badgeObject.TryGetValue("type", out var typeObj) ? typeObj?.ToString() ?? string.Empty : string.Empty);

                    if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(type))
                    {
                        continue;
                    }

                    sanitized.Add(new Dictionary<string, object?>
                    {
                        ["code"] = code,
                        ["text"] = text,
                        ["type"] = type
                    });
                }
            }

            return sanitized
                .GroupBy(badge => $"{badge["code"]}|{badge["text"]}|{badge["type"]}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private static string NormalizeDescription(string value)
        {
            var normalized = value ?? string.Empty;
            var boilerplateIndex = normalized.IndexOf("Giá sản phẩm trên Tiki đã bao gồm thuế", StringComparison.OrdinalIgnoreCase);
            if (boilerplateIndex >= 0)
            {
                normalized = normalized[..boilerplateIndex];
            }

            normalized = Regex.Replace(normalized, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, "</p>", "\n\n", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, "<[^>]*>", " ", RegexOptions.IgnoreCase);
            normalized = normalized
                .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
                .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
                .Replace("&lt;", "<", StringComparison.OrdinalIgnoreCase)
                .Replace("&gt;", ">", StringComparison.OrdinalIgnoreCase)
                .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase);
            normalized = Regex.Replace(normalized, @"[ \t]{2,}", " ");
            normalized = Regex.Replace(normalized, @"\n{3,}", "\n\n");
            return normalized.Trim();
        }

        private static string NormalizeWhitespace(string value)
        {
            return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
        }

        private bool IsForceRefresh()
        {
            return HttpContext.Request.Query.TryGetValue("force", out var forceValues)
                && bool.TryParse(forceValues.FirstOrDefault(), out var force)
                && force;
        }
    }
}
