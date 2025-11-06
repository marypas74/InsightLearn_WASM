using System.Diagnostics;
using InsightLearn.Application.DTOs;
using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Servizio per gestire le conversazioni del chatbot
/// Integra Ollama LLM con persistenza su database SQL Server
/// </summary>
public class ChatbotService : IChatbotService
{
    private readonly IOllamaService _ollamaService;
    private readonly InsightLearnDbContext _dbContext;
    private readonly ILogger<ChatbotService> _logger;
    private readonly string _defaultModel;

    // FAQ Cache for instant responses (80% of queries)
    private static readonly Dictionary<string, string> _faqCache = new(StringComparer.OrdinalIgnoreCase)
    {
        // Greetings
        ["hello"] = "Hello! I'm the InsightLearn assistant. How can I help you today?",
        ["hi"] = "Hi there! Welcome to InsightLearn. What would you like to know?",
        ["ciao"] = "Ciao! Sono l'assistente di InsightLearn. Come posso aiutarti?",
        ["buongiorno"] = "Buongiorno! Benvenuto su InsightLearn. In cosa posso aiutarti?",

        // Course-related
        ["courses"] = "We offer a wide range of online courses in technology, business, data science, and more. Browse our catalog to find courses that match your interests!",
        ["corsi"] = "Offriamo un'ampia gamma di corsi online in tecnologia, business, data science e altro. Esplora il nostro catalogo per trovare i corsi che ti interessano!",
        ["what courses"] = "InsightLearn provides courses in programming, web development, AI/ML, business management, and many other fields. All courses are self-paced and include video lectures, quizzes, and certificates.",

        // Platform help
        ["help"] = "I can help you with course information, enrollment questions, platform navigation, and general inquiries about InsightLearn. What do you need help with?",
        ["aiuto"] = "Posso aiutarti con informazioni sui corsi, domande sull'iscrizione, navigazione della piattaforma e domande generali su InsightLearn. Di cosa hai bisogno?",
        ["how to enroll"] = "To enroll in a course, simply browse our catalog, select a course you're interested in, and click the 'Enroll' button. You'll need to create an account if you haven't already.",
        ["come iscriversi"] = "Per iscriverti a un corso, sfoglia il nostro catalogo, seleziona un corso che ti interessa e clicca sul pulsante 'Iscriviti'. Dovrai creare un account se non l'hai già fatto.",

        // Pricing
        ["price"] = "Course prices vary depending on the content and duration. Many courses offer free previews. Check individual course pages for specific pricing information.",
        ["prezzo"] = "I prezzi dei corsi variano in base al contenuto e alla durata. Molti corsi offrono anteprime gratuite. Controlla le pagine dei singoli corsi per informazioni specifiche sui prezzi.",
        ["free"] = "Yes! We offer several free courses and free previews for premium courses. Look for courses marked as 'Free' in our catalog.",

        // Certificates
        ["certificate"] = "Yes, you'll receive a certificate of completion after finishing a course and passing the final assessment. Certificates can be shared on LinkedIn and added to your resume.",
        ["certificato"] = "Sì, riceverai un certificato di completamento dopo aver finito un corso e superato la valutazione finale. I certificati possono essere condivisi su LinkedIn e aggiunti al tuo CV.",

        // Technical support
        ["support"] = "For technical support, please contact our team at support@insightlearn.cloud or use the contact form. We typically respond within 24 hours.",
        ["supporto"] = "Per supporto tecnico, contatta il nostro team a support@insightlearn.cloud o usa il modulo di contatto. Rispondiamo tipicamente entro 24 ore.",

        // Thank you
        ["thanks"] = "You're welcome! Let me know if you have any other questions.",
        ["grazie"] = "Prego! Fammi sapere se hai altre domande.",
        ["thank you"] = "You're very welcome! Feel free to ask if you need anything else."
    };

    public ChatbotService(
        IOllamaService ollamaService,
        InsightLearnDbContext dbContext,
        ILogger<ChatbotService> logger,
        string defaultModel = "llama2")
    {
        _ollamaService = ollamaService ?? throw new ArgumentNullException(nameof(ollamaService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultModel = defaultModel;
    }

    public async Task<ChatMessageResponse> ProcessMessageAsync(
        ChatMessageRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation(
                "Processing chatbot message for session {SessionId}, message length: {Length}",
                sessionId, request.Message.Length);

            // 1. Ottieni o crea il contatto chatbot
            var contact = await GetOrCreateContactAsync(
                sessionId,
                request.Email,
                request.Message,
                ipAddress,
                userAgent);

            // 1.5. Check FAQ cache for instant response (< 100ms)
            var normalizedMessage = request.Message.Trim().ToLowerInvariant();
            if (_faqCache.TryGetValue(normalizedMessage, out var cachedResponse))
            {
                _logger.LogInformation("Using cached FAQ response for: {Message}", normalizedMessage);

                stopwatch.Stop();
                return new ChatMessageResponse
                {
                    Response = cachedResponse,
                    SessionId = sessionId,
                    Timestamp = DateTime.UtcNow,
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds, // Should be < 100ms
                    AiModel = "faq-cache",
                    HasError = false,
                    ErrorMessage = null
                };
            }

            // 2. Salva il messaggio dell'utente
            var userMessage = new ChatbotMessage
            {
                SessionId = sessionId,
                Message = request.Message,
                IsUserMessage = true,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.ChatbotMessages.Add(userMessage);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("User message saved to database with ID {MessageId}", userMessage.Id);

            // 3. Genera risposta con Ollama
            string aiResponse;
            try
            {
                // Costruisci un prompt con contesto della conversazione se necessario
                var prompt = await BuildPromptWithContextAsync(sessionId, request.Message);

                aiResponse = await _ollamaService.GenerateResponseAsync(
                    prompt,
                    _defaultModel,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating response from Ollama");
                aiResponse = "Mi dispiace, sto avendo problemi tecnici al momento. Riprova tra qualche istante.";
            }

            stopwatch.Stop();
            var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            // 4. Salva la risposta del bot
            var botMessage = new ChatbotMessage
            {
                SessionId = sessionId,
                Message = aiResponse,
                IsUserMessage = false,
                Timestamp = DateTime.UtcNow,
                AiModel = _defaultModel,
                ResponseTimeMs = responseTimeMs
            };

            _dbContext.ChatbotMessages.Add(botMessage);

            // 5. Aggiorna il contatto
            contact.MessageCount += 2; // User message + Bot message
            contact.LastInteractionAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Chatbot response generated in {ResponseTime}ms for session {SessionId}",
                responseTimeMs, sessionId);

            return new ChatMessageResponse
            {
                Response = aiResponse,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                ResponseTimeMs = responseTimeMs,
                AiModel = _defaultModel,
                HasError = false
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing chatbot message for session {SessionId}", sessionId);

            return new ChatMessageResponse
            {
                Response = "Mi dispiace, si è verificato un errore. Riprova più tardi.",
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                HasError = true,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<ChatHistoryDto>> GetChatHistoryAsync(string sessionId, int limit = 50)
    {
        try
        {
            _logger.LogDebug("Fetching chat history for session {SessionId}", sessionId);

            var messages = await _dbContext.ChatbotMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.Timestamp)
                .Take(limit)
                .Select(m => new ChatHistoryDto
                {
                    Id = m.Id,
                    SessionId = m.SessionId,
                    Message = m.Message,
                    IsUserMessage = m.IsUserMessage,
                    Timestamp = m.Timestamp,
                    AiModel = m.AiModel
                })
                .ToListAsync();

            _logger.LogInformation(
                "Found {Count} messages for session {SessionId}",
                messages.Count, sessionId);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching chat history for session {SessionId}", sessionId);
            return new List<ChatHistoryDto>();
        }
    }

    public async Task<bool> ClearChatHistoryAsync(string sessionId)
    {
        try
        {
            _logger.LogInformation("Clearing chat history for session {SessionId}", sessionId);

            // Elimina tutti i messaggi della sessione
            var messages = await _dbContext.ChatbotMessages
                .Where(m => m.SessionId == sessionId)
                .ToListAsync();

            if (messages.Any())
            {
                _dbContext.ChatbotMessages.RemoveRange(messages);
                _logger.LogDebug("Removing {Count} messages", messages.Count);
            }

            // Elimina il contatto
            var contact = await _dbContext.ChatbotContacts
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);

            if (contact != null)
            {
                _dbContext.ChatbotContacts.Remove(contact);
                _logger.LogDebug("Removing contact for session {SessionId}", sessionId);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Chat history cleared for session {SessionId}: {MessageCount} messages, contact removed",
                sessionId, messages.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing chat history for session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<ChatbotContact> GetOrCreateContactAsync(
        string sessionId,
        string? email = null,
        string? initialMessage = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            // Cerca contatto esistente
            var contact = await _dbContext.ChatbotContacts
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);

            if (contact != null)
            {
                _logger.LogDebug("Found existing contact for session {SessionId}", sessionId);
                return contact;
            }

            // Crea nuovo contatto
            contact = new ChatbotContact
            {
                SessionId = sessionId,
                Email = email ?? string.Empty,
                InitialMessage = initialMessage,
                CreatedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                MessageCount = 0,
                LastInteractionAt = DateTime.UtcNow
            };

            _dbContext.ChatbotContacts.Add(contact);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created new chatbot contact for session {SessionId}", sessionId);

            return contact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating contact for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Costruisce un prompt con contesto della conversazione precedente (ultimi N messaggi)
    /// </summary>
    private async Task<string> BuildPromptWithContextAsync(string sessionId, string currentMessage, int contextLimit = 5)
    {
        try
        {
            // Ottieni gli ultimi messaggi della conversazione per fornire contesto
            var recentMessages = await _dbContext.ChatbotMessages
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.Timestamp)
                .Take(contextLimit)
                .OrderBy(m => m.Timestamp) // Riordina cronologicamente
                .ToListAsync();

            if (!recentMessages.Any())
            {
                // Nessun contesto, usa solo il messaggio corrente
                return currentMessage;
            }

            // Costruisci il prompt con contesto
            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine("Conversazione precedente:");
            promptBuilder.AppendLine();

            foreach (var msg in recentMessages)
            {
                var sender = msg.IsUserMessage ? "Utente" : "Assistente";
                promptBuilder.AppendLine($"{sender}: {msg.Message}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"Utente: {currentMessage}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Assistente:");

            return promptBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building prompt with context, using message only");
            return currentMessage;
        }
    }
}
