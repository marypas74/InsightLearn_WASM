using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using InsightLearn.Infrastructure.Data;

namespace InsightLearn.Application.Services;

public class ChatbotCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatbotCleanupBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour
    private readonly int _maxMessages = 10000; // Max total messages before cleanup

    public ChatbotCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ChatbotCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Chatbot cleanup background service started (max messages: {MaxMessages})", _maxMessages);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupCheckAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Chatbot cleanup background service was cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during chatbot cleanup check");
                // Wait 5 minutes before retrying after an error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Chatbot cleanup background service stopped");
    }

    private async Task PerformCleanupCheckAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightLearnDbContext>();

            // Check total chatbot messages count
            var totalMessages = await dbContext.ChatbotMessages.CountAsync(stoppingToken);

            _logger.LogInformation("Current chatbot messages count: {Count}/{Max}", totalMessages, _maxMessages);

            if (totalMessages > _maxMessages)
            {
                var messagesToDelete = totalMessages - _maxMessages;
                _logger.LogWarning("Chatbot messages limit exceeded! Current: {Total}, Max: {Max}, Deleting oldest: {ToDelete}",
                    totalMessages, _maxMessages, messagesToDelete);

                await CleanupOldestMessagesAsync(dbContext, messagesToDelete, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform chatbot cleanup check");
            throw;
        }
    }

    private async Task CleanupOldestMessagesAsync(
        InsightLearnDbContext dbContext,
        int messagesToDelete,
        CancellationToken stoppingToken)
    {
        try
        {
            // Get oldest sessions to delete
            var oldestSessions = await dbContext.ChatbotMessages
                .OrderBy(m => m.Timestamp)
                .Take(messagesToDelete)
                .Select(m => m.SessionId)
                .Distinct()
                .ToListAsync(stoppingToken);

            _logger.LogInformation("Found {Count} sessions to clean up", oldestSessions.Count);

            foreach (var sessionId in oldestSessions)
            {
                // Delete all messages for this session
                var messages = await dbContext.ChatbotMessages
                    .Where(m => m.SessionId == sessionId)
                    .ToListAsync(stoppingToken);

                dbContext.ChatbotMessages.RemoveRange(messages);

                // Delete contact record
                var contact = await dbContext.ChatbotContacts
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId, stoppingToken);

                if (contact != null)
                {
                    dbContext.ChatbotContacts.Remove(contact);
                }

                _logger.LogInformation("Deleted session {SessionId} ({MessageCount} messages, Email: {Email})",
                    sessionId,
                    messages.Count,
                    contact?.Email ?? "unknown");
            }

            await dbContext.SaveChangesAsync(stoppingToken);

            var finalCount = await dbContext.ChatbotMessages.CountAsync(stoppingToken);
            _logger.LogInformation("Chatbot cleanup completed successfully. Final count: {Count}", finalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old chatbot messages");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Chatbot cleanup background service is stopping");
        await base.StopAsync(stoppingToken);
    }
}
