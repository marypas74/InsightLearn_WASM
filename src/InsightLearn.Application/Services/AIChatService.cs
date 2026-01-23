using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using InsightLearn.Application.Interfaces;
using InsightLearn.Core.DTOs.AIChat;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.Services
{
    /// <summary>
    /// AI Chat service implementation with dynamic provider selection (OpenAI/Ollama).
    /// Uses IAIServiceFactory to select provider at runtime based on admin configuration.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class AIChatService : IAIChatService
    {
        private readonly IAIConversationRepository _conversationRepository;
        private readonly IAIServiceFactory _serviceFactory;
        private readonly IVideoTranscriptService? _transcriptService;
        private readonly ILogger<AIChatService> _logger;

        public AIChatService(
            IAIConversationRepository conversationRepository,
            IAIServiceFactory serviceFactory,
            ILogger<AIChatService> logger,
            IVideoTranscriptService? transcriptService = null)
        {
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transcriptService = transcriptService;
        }

        public async Task<AIChatResponseDto> SendMessageAsync(Guid? userId, AIChatMessageDto messageDto, CancellationToken ct = default)
        {
            _logger.LogInformation("[AI_CHAT] Processing message from user {UserId}, SessionId: {SessionId}, LessonId: {LessonId}",
                userId.HasValue ? userId.Value.ToString() : "anonymous", messageDto.SessionId, messageDto.LessonId);

            // 1. Get or create session
            Core.Entities.AIConversation conversation;

            if (messageDto.SessionId.HasValue)
            {
                var existing = await _conversationRepository.GetConversationMetadataAsync(messageDto.SessionId.Value, ct);
                if (existing == null)
                {
                    _logger.LogWarning("[AI_CHAT] Session {SessionId} not found, creating new session", messageDto.SessionId);
                    conversation = await _conversationRepository.CreateConversationAsync(
                        userId, messageDto.LessonId, messageDto.VideoTimestamp, ct);
                }
                // Allow access if:
                // - Both are null (anonymous user accessing anonymous conversation)
                // - Both have the same non-null value (authenticated user accessing own conversation)
                // - Conversation has null UserId (anonymous session, anyone can continue it)
                else if (!existing.UserId.HasValue || existing.UserId == userId)
                {
                    conversation = existing;
                }
                else
                {
                    _logger.LogWarning("[AI_CHAT] User {UserId} tried to access session {SessionId} owned by {OwnerId}",
                        userId, messageDto.SessionId, existing.UserId);
                    throw new UnauthorizedAccessException("You do not have access to this conversation");
                }
            }
            else
            {
                _logger.LogInformation("[AI_CHAT] Creating new session for user {UserId}", userId);
                conversation = await _conversationRepository.CreateConversationAsync(
                    userId, messageDto.LessonId, messageDto.VideoTimestamp, ct);
            }

            // 2. Store user message
            await _conversationRepository.AddMessageAsync(
                conversation.SessionId, "user", messageDto.Message, messageDto.VideoTimestamp, ct);

            // 3. Build context-enriched prompt
            var contextInfo = new AIContextInfoDto
            {
                LessonId = messageDto.LessonId,
                VideoTimestamp = messageDto.VideoTimestamp,
                TranscriptUsed = false,
                NotesUsed = false,
                TokensUsed = 0
            };

            var enrichedPrompt = await BuildEnrichedPromptAsync(messageDto, contextInfo, ct);

            // 4. Get AI response from configured provider (OpenAI or Ollama via factory)
            string aiResponse;
            try
            {
                // Build conversation history for context
                var history = await _conversationRepository.GetConversationHistoryAsync(conversation.SessionId, limit: 10, ct);
                var chatHistory = history?.Messages?
                    .Select(m => new ChatMessage { Role = m.Role, Content = m.Content })
                    .ToList();

                const string systemPrompt = "You are a helpful educational AI assistant focused on helping students learn effectively. " +
                                  "Answer questions based on video content, transcripts, and student notes. " +
                                  "Be concise, clear, and encouraging.";

                // Try to get OpenAI service first (primary provider based on config)
                var openAIService = await _serviceFactory.GetChatServiceAsync();
                if (openAIService != null)
                {
                    _logger.LogInformation("[AI_CHAT] Using OpenAI provider for chat");
                    aiResponse = await openAIService.SendMessageAsync(
                        userMessage: enrichedPrompt,
                        systemPrompt: systemPrompt,
                        conversationHistory: chatHistory,
                        operationType: "educational-chat",
                        ct: ct);
                    _logger.LogInformation("[AI_CHAT] OpenAI generated response with {Length} characters", aiResponse.Length);
                }
                else
                {
                    // Fall back to Ollama if available
                    var ollamaService = _serviceFactory.GetOllamaService();
                    if (ollamaService != null)
                    {
                        _logger.LogInformation("[AI_CHAT] Using Ollama provider for chat (OpenAI not available)");
                        aiResponse = await ollamaService.GenerateResponseAsync(
                            prompt: $"{systemPrompt}\n\n{enrichedPrompt}",
                            cancellationToken: ct);
                        _logger.LogInformation("[AI_CHAT] Ollama generated response with {Length} characters", aiResponse.Length);
                    }
                    else
                    {
                        _logger.LogWarning("[AI_CHAT] No AI provider available");
                        aiResponse = "I apologize, but no AI service is currently configured. Please contact the administrator.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CHAT] AI provider failed to generate response");
                aiResponse = "I apologize, but I'm having trouble processing your request right now. Please try again in a moment.";
            }

            // 5. Store assistant response
            await _conversationRepository.AddMessageAsync(
                conversation.SessionId, "assistant", aiResponse, messageDto.VideoTimestamp, ct);

            // 6. Return response
            return new AIChatResponseDto
            {
                SessionId = conversation.SessionId,
                Response = aiResponse,
                Timestamp = DateTime.UtcNow,
                Context = contextInfo
            };
        }

        public async Task<AIConversationHistoryDto?> GetHistoryAsync(Guid sessionId, int limit = 50, CancellationToken ct = default)
        {
            _logger.LogInformation("[AI_CHAT] Getting history for session {SessionId}, limit: {Limit}", sessionId, limit);
            return await _conversationRepository.GetConversationHistoryAsync(sessionId, limit, ct);
        }

        public async Task EndSessionAsync(Guid? userId, Guid sessionId, CancellationToken ct = default)
        {
            _logger.LogInformation("[AI_CHAT] Ending session {SessionId} for user {UserId}", sessionId,
                userId.HasValue ? userId.Value.ToString() : "anonymous");

            // Verify ownership
            var conversation = await _conversationRepository.GetConversationMetadataAsync(sessionId, ct);
            if (conversation == null)
            {
                throw new KeyNotFoundException($"Session {sessionId} not found");
            }

            // Allow access if:
            // - Conversation has null UserId (anonymous session)
            // - UserId matches the authenticated user
            if (conversation.UserId.HasValue && conversation.UserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have access to this conversation");
            }

            await _conversationRepository.EndConversationAsync(sessionId, ct);
        }

        public async Task<List<AISessionSummaryDto>> GetSessionsAsync(Guid? userId, Guid? lessonId = null, int limit = 50, CancellationToken ct = default)
        {
            _logger.LogInformation("[AI_CHAT] Getting sessions for user {UserId}, lessonId: {LessonId}",
                userId.HasValue ? userId.Value.ToString() : "anonymous", lessonId);

            // Anonymous users don't have persistent session tracking
            if (!userId.HasValue)
            {
                return new List<AISessionSummaryDto>();
            }

            List<Core.Entities.AIConversation> conversations;

            if (lessonId.HasValue)
            {
                // Get user's conversations for specific lesson
                var allLessonConversations = await _conversationRepository.GetLessonConversationsAsync(lessonId.Value, limit * 2, ct);
                conversations = allLessonConversations.Where(c => c.UserId == userId.Value).Take(limit).ToList();
            }
            else
            {
                conversations = await _conversationRepository.GetUserConversationsAsync(userId.Value, limit, ct);
            }

            return conversations.Select(c => new AISessionSummaryDto
            {
                SessionId = c.SessionId,
                LessonId = c.LessonId,
                LessonTitle = c.Lesson?.Title,
                MessageCount = c.MessageCount,
                CreatedAt = c.CreatedAt,
                LastMessageAt = c.LastMessageAt,
                IsActive = c.IsActive
            }).ToList();
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                // Check if any AI provider is available (OpenAI or Ollama)
                var openAIService = await _serviceFactory.GetChatServiceAsync();
                if (openAIService != null)
                {
                    return await openAIService.IsAvailableAsync();
                }

                var ollamaService = _serviceFactory.GetOllamaService();
                if (ollamaService != null)
                {
                    return await ollamaService.IsAvailableAsync();
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AI_CHAT] Error checking AI provider availability");
                return false;
            }
        }

        private async Task<string> BuildEnrichedPromptAsync(AIChatMessageDto messageDto, AIContextInfoDto contextInfo, CancellationToken ct)
        {
            var promptBuilder = new StringBuilder();

            // Add lesson/video context if available
            if (messageDto.LessonId.HasValue && _transcriptService != null)
            {
                try
                {
                    var transcript = await _transcriptService.GetTranscriptAsync(messageDto.LessonId.Value, ct);
                    if (transcript != null && transcript.Segments.Any())
                    {
                        // Get relevant transcript segments around the current timestamp
                        var relevantSegments = GetRelevantTranscriptSegments(transcript, messageDto.VideoTimestamp);

                        if (relevantSegments.Any())
                        {
                            promptBuilder.AppendLine("[Video Context]");
                            promptBuilder.AppendLine($"Current video timestamp: {FormatTimestamp(messageDto.VideoTimestamp ?? 0)}");
                            promptBuilder.AppendLine("Relevant transcript excerpts:");

                            foreach (var segment in relevantSegments)
                            {
                                promptBuilder.AppendLine($"  [{FormatTimestamp((int)segment.StartTime)}] {segment.Text}");
                            }

                            promptBuilder.AppendLine();
                            contextInfo.TranscriptUsed = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AI_CHAT] Failed to get transcript context for lesson {LessonId}", messageDto.LessonId);
                }
            }

            // Add the user's question
            promptBuilder.AppendLine("[User Question]");
            promptBuilder.AppendLine(messageDto.Message);

            var enrichedPrompt = promptBuilder.ToString();
            contextInfo.TokensUsed = enrichedPrompt.Length / 4; // Rough token estimate

            return enrichedPrompt;
        }

        private List<Core.DTOs.VideoTranscript.TranscriptSegmentDto> GetRelevantTranscriptSegments(
            Core.DTOs.VideoTranscript.VideoTranscriptDto transcript,
            int? currentTimestamp)
        {
            const int contextWindowSeconds = 60; // Get 60 seconds of context around current position
            const int maxSegments = 5;

            if (!currentTimestamp.HasValue || !transcript.Segments.Any())
            {
                // Return first few segments if no timestamp
                return transcript.Segments.Take(maxSegments).ToList();
            }

            double timestamp = currentTimestamp.Value;

            // Get segments within the context window
            return transcript.Segments
                .Where(s => s.StartTime >= timestamp - contextWindowSeconds
                         && s.StartTime <= timestamp + contextWindowSeconds)
                .OrderBy(s => Math.Abs(s.StartTime - timestamp))
                .Take(maxSegments)
                .OrderBy(s => s.StartTime)
                .ToList();
        }

        private static string FormatTimestamp(int seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0
                ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
