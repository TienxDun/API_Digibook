using API_DigiBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_DigiBook.Examples
{
    /// <summary>
    /// Example usage of LoggerService (Singleton Pattern)
    /// This is a reference implementation showing how to use the logger in your controllers
    /// </summary>
    public class LoggerUsageExample : ControllerBase
    {
        // Get the singleton instance
        private readonly LoggerService _logger = LoggerService.Instance;

        /// <summary>
        /// Example 1: Log a successful operation
        /// </summary>
        public async Task<IActionResult> ExampleLogSuccess()
        {
            // Perform some operation
            var bookId = "book123";
            
            // Log success
            await _logger.LogSuccessAsync(
                action: "CREATE_BOOK",
                detail: $"Book with ID {bookId} was created successfully",
                user: "admin@example.com"
            );

            return Ok();
        }

        /// <summary>
        /// Example 2: Log an error
        /// </summary>
        public async Task<IActionResult> ExampleLogError()
        {
            try
            {
                // Some operation that might fail
                throw new Exception("Database connection failed");
            }
            catch (Exception ex)
            {
                // Log error
                await _logger.LogErrorAsync(
                    action: "DATABASE_CONNECTION",
                    detail: $"Error: {ex.Message}",
                    user: "system"
                );

                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Example 3: Log a warning
        /// </summary>
        public async Task<IActionResult> ExampleLogWarning()
        {
            var stockQuantity = 5;

            if (stockQuantity < 10)
            {
                await _logger.LogWarningAsync(
                    action: "LOW_STOCK",
                    detail: $"Book stock is low: {stockQuantity} items remaining",
                    user: "system"
                );
            }

            return Ok();
        }

        /// <summary>
        /// Example 4: Log info
        /// </summary>
        public async Task<IActionResult> ExampleLogInfo()
        {
            await _logger.LogInfoAsync(
                action: "USER_LOGIN",
                detail: "User logged in from IP 192.168.1.1",
                user: "user@example.com"
            );

            return Ok();
        }

        /// <summary>
        /// Example 5: Use in a complete CRUD operation
        /// </summary>
        public async Task<IActionResult> ExampleCompleteOperation(string bookId)
        {
            try
            {
                // Log start of operation
                await _logger.LogInfoAsync(
                    "GET_BOOK",
                    $"Fetching book with ID: {bookId}",
                    "user@example.com"
                );

                // Simulate getting book
                if (string.IsNullOrEmpty(bookId))
                {
                    // Log validation error
                    await _logger.LogWarningAsync(
                        "VALIDATION_ERROR",
                        "Book ID is required but was not provided",
                        "user@example.com"
                    );

                    return BadRequest(new { message = "Book ID is required" });
                }

                // Simulate successful operation
                var book = new { Id = bookId, Title = "Sample Book" };

                // Log success
                await _logger.LogSuccessAsync(
                    "GET_BOOK",
                    $"Successfully retrieved book: {bookId}",
                    "user@example.com"
                );

                return Ok(book);
            }
            catch (Exception ex)
            {
                // Log error
                await _logger.LogErrorAsync(
                    "GET_BOOK",
                    $"Failed to get book {bookId}: {ex.Message}",
                    "user@example.com"
                );

                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Example 6: Log with custom status
        /// </summary>
        public async Task<IActionResult> ExampleCustomLog()
        {
            await _logger.LogAsync(
                action: "CUSTOM_ACTION",
                detail: "This is a custom log entry",
                status: "CUSTOM_STATUS",
                user: "admin"
            );

            return Ok();
        }
    }
}
