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
    /// AI Chat service implementation with Ollama integration and context enrichment.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class AIChatService : IAIChatService
    {
        private readonly IAIConversationRepository _conversationRepository;
        private readonly IOllamaService _ollamaService;
        private readonly IVideoTranscriptService? _transcriptService;
        private readonly ILogger<AIChatService> _logger;

        public AIChatService(
            IAIConversationRepository conversationRepository,
            IOllamaService ollamaService,
            ILogger<AIChatService> logger,
            IVideoTranscriptService? transcriptService = null)
        {
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _ollamaService = ollamaService ?? throw new ArgumentNullException(nameof(ollamaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transcriptService = transcriptService;
        }

        public async Task<AIChatResponseDto> SendMessageAsync(Guid userId, AIChatMessageDto messageDto, CancellationToken ct = default)
        {
            _logger.LogInformation("[AI_CHAT] Processing message from user {UserId}, SessionId: {SessionId}, LessonId: {LessonId}",
                userId, messageDto.SessionId, messageDto.LessonId);

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
                else if (existing.UserId != userId)
                {
                    _logger.LogWarning("[AI_CHAT] User {UserId} tried to access session {SessionId} owned by {OwnerId}",
                        userId, messageDto.SessionId, existing.UserId);
                    throw new UnauthorizedAccessException("You do not have access to this conversation");
                }
                else
                {
                    conversation = existing;
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

            // 4. Get AI response from Ollama
            string aiResponse;
            try
            {
                aiResponse = await _ollamaService.GenerateResponseAsync(enrichedPrompt, cancellationToken: ct);
                _logger.LogInformation("[AI_CHAT] Ollama generated response with {Length} characters", aiResponse.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CHAT] Ollama failed to generate response");
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

        public async Task EndSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
        {
            _logger.LogInformation("[AI_CHAT] Ending session {SessionId} for user {UserId}", sessionId, userId);

            // Verify ownership
            var conversation = await _conversationRepository.GetConversationMetadataAsync(sessionId, ct);
            if (conversation == null)
            {
                throw new KeyNotFoundException($"Session {sessionId} not found");
            }

            if (conversation.UserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have access to this conversation");
            }

            await _conversationRepository.EndConversationAsync(sessionId, ct);
        }

        public async Task<List<AISessionSummaryDto>> GetSessionsAsync(Guid userId, Guid? lessonId = null, int limit = 50, CancellationToken ct = default)
        {
            _logger.LogInformation("[AI_CHAT] Getting sessions for user {UserId}, lessonId: {LessonId}", userId, lessonId);

            List<Core.Entities.AIConversation> conversations;

            if (lessonId.HasValue)
            {
                // Get user's conversations for specific lesson
                var allLessonConversations = await _conversationRepository.GetLessonConversationsAsync(lessonId.Value, limit * 2, ct);
                conversations = allLessonConversations.Where(c => c.UserId == userId).Take(limit).ToList();
            }
            else
            {
                conversations = await _conversationRepository.GetUserConversationsAsync(userId, limit, ct);
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
                return await _ollamaService.IsAvailableAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AI_CHAT] Error checking Ollama availability");
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
