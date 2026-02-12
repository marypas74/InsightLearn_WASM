using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.Interfaces;
using InsightLearn.Application.Services;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using InsightLearn.Core.DTOs.VideoTranscript;

namespace InsightLearn.Application.BackgroundJobs
{
    /// <summary>
    /// Hangfire background job for multi-language subtitle translation.
    /// Translates English transcripts to target languages using Azure Translator or Ollama.
    /// Part of Phase 8: Multi-Language Subtitle Support - LinkedIn Learning parity (v2.3.24-dev).
    /// </summary>
    public class TranslationJob
    {
        private readonly IAzureTranslatorService _azureTranslator;
        private readonly IVideoTranscriptRepository _transcriptRepository;
        private readonly IMongoVideoStorageService _mongoStorage;
        private readonly InsightLearnDbContext _dbContext;
        private readonly ILogger<TranslationJob> _logger;
        private readonly MetricsService _metricsService;

        public TranslationJob(
            IAzureTranslatorService azureTranslator,
            IVideoTranscriptRepository transcriptRepository,
            IMongoVideoStorageService mongoStorage,
            InsightLearnDbContext dbContext,
            ILogger<TranslationJob> logger,
            MetricsService metricsService)
        {
            _azureTranslator = azureTranslator ?? throw new ArgumentNullException(nameof(azureTranslator));
            _transcriptRepository = transcriptRepository ?? throw new ArgumentNullException(nameof(transcriptRepository));
            _mongoStorage = mongoStorage ?? throw new ArgumentNullException(nameof(mongoStorage));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        }

        /// <summary>
        /// Execute translation job for a specific lesson and target language.
        /// Enqueued by VideoTranscriptService or scheduled batch translation.
        /// </summary>
        /// <param name="lessonId">Lesson ID to translate</param>
        /// <param name="targetLanguage">Target language code (ISO 639-1, e.g., "es", "fr", "de")</param>
        /// <param name="translator">Translation service: "azure" or "ollama"</param>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })] // Retry: 1 min, 5 min, 15 min
        [DisableConcurrentExecution(timeoutInSeconds: 3600)] // Prevent duplicate translation jobs
        public async Task ExecuteAsync(Guid lessonId, string targetLanguage, string translator = "ollama")
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("[TRANSLATION-JOB] Starting translation job for lesson {LessonId}, target language {TargetLanguage}, translator {Translator}",
                lessonId, targetLanguage, translator);

            // Step 1: Find or create VideoTranscriptTranslation metadata record
            var translationRecord = await _dbContext.VideoTranscriptTranslations
                .FirstOrDefaultAsync(t => t.LessonId == lessonId && t.TargetLanguage == targetLanguage);

            if (translationRecord == null)
            {
                // Create new translation record
                translationRecord = new Core.Entities.VideoTranscriptTranslation
                {
                    Id = Guid.NewGuid(),
                    LessonId = lessonId,
                    SourceLanguage = "en",
                    TargetLanguage = targetLanguage,
                    Status = "Processing",
                    QualityTier = translator == "azure" ? "Auto/Azure" : "Auto/Ollama",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.VideoTranscriptTranslations.Add(translationRecord);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("[TRANSLATION-JOB] Created translation record {TranslationId}", translationRecord.Id);
            }
            else if (translationRecord.Status == "Completed")
            {
                _logger.LogWarning("[TRANSLATION-JOB] Translation already completed for lesson {LessonId}, language {TargetLanguage}. Skipping.",
                    lessonId, targetLanguage);
                return;
            }
            else
            {
                // Update status to Processing
                translationRecord.Status = "Processing";
                translationRecord.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            try
            {
                // Step 2: Fetch English transcript from database
                _logger.LogInformation("[TRANSLATION-JOB] Fetching English transcript for lesson {LessonId}", lessonId);
                var englishTranscript = await _transcriptRepository.GetTranscriptAsync(lessonId);

                if (englishTranscript == null || !englishTranscript.Segments.Any())
                {
                    throw new InvalidOperationException($"No English transcript found for lesson {lessonId}. Cannot translate.");
                }

                _logger.LogInformation("[TRANSLATION-JOB] Fetched {SegmentCount} transcript segments", englishTranscript.Segments.Count);

                // Step 3: Translate segments based on translator type
                List<TranslatedSegment> translatedSegments;
                decimal estimatedCost = 0m;
                int totalCharacters = 0;

                if (translator == "azure")
                {
                    // Use Azure Translator API
                    _logger.LogInformation("[TRANSLATION-JOB] Translating with Azure Translator API");

                    using (_metricsService.MeasureTranslationProcessing(translator, targetLanguage))
                    {
                        translatedSegments = await TranslateWithAzureAsync(lessonId, englishTranscript.Segments, targetLanguage);

                        // Calculate cost: Azure charges $10 per 1M characters
                        totalCharacters = englishTranscript.Segments.Sum(s => s.Text.Length);
                        estimatedCost = (totalCharacters / 1_000_000m) * 10m;
                    }
                }
                else
                {
                    // Use Ollama (free, local translation)
                    _logger.LogInformation("[TRANSLATION-JOB] Translating with Ollama (qwen2:0.5b)");

                    using (_metricsService.MeasureTranslationProcessing(translator, targetLanguage))
                    {
                        translatedSegments = await TranslateWithOllamaAsync(englishTranscript.Segments, targetLanguage);

                        totalCharacters = englishTranscript.Segments.Sum(s => s.Text.Length);
                        estimatedCost = 0m; // Ollama is free
                    }
                }

                _logger.LogInformation("[TRANSLATION-JOB] Translation completed: {SegmentCount} segments, {TotalCharacters} characters, estimated cost ${EstimatedCost:F4}",
                    translatedSegments.Count, totalCharacters, estimatedCost);

                // Step 4: Store translated segments in MongoDB
                var mongoDocument = new BsonDocument
                {
                    { "lessonId", lessonId.ToString() },
                    { "sourceLanguage", "en" },
                    { "targetLanguage", targetLanguage },
                    { "translator", translator },
                    { "segments", new BsonArray(translatedSegments.Select(s => new BsonDocument
                        {
                            { "index", s.Index },
                            { "startSeconds", s.StartSeconds },
                            { "endSeconds", s.EndSeconds },
                            { "originalText", s.OriginalText },
                            { "translatedText", s.TranslatedText },
                            { "confidenceScore", s.ConfidenceScore.HasValue ? (BsonValue)s.ConfidenceScore.Value : BsonNull.Value }
                        }))
                    },
                    { "metadata", new BsonDocument
                        {
                            { "totalSegments", translatedSegments.Count },
                            { "totalCharacters", totalCharacters },
                            { "estimatedCost", (double)estimatedCost },
                            { "translatorVersion", translator == "azure" ? "Azure Translator v3.0" : "Ollama qwen2:0.5b" },
                            { "processingTime", (DateTime.UtcNow - startTime).TotalSeconds },
                            { "processedAt", DateTime.UtcNow }
                        }
                    },
                    { "qualityTier", translationRecord.QualityTier },
                    { "createdAt", DateTime.UtcNow },
                    { "updatedAt", DateTime.UtcNow }
                };

                var mongoDocumentId = await _mongoStorage.StoreTranslatedSubtitlesAsync(mongoDocument);
                _logger.LogInformation("[TRANSLATION-JOB] Stored translated subtitles in MongoDB: {MongoDocumentId}", mongoDocumentId);

                // Step 5: Update SQL Server metadata with success
                translationRecord.MongoDocumentId = mongoDocumentId;
                translationRecord.Status = "Completed";
                translationRecord.SegmentCount = translatedSegments.Count;
                translationRecord.TotalCharacters = totalCharacters;
                translationRecord.EstimatedCost = estimatedCost;
                translationRecord.CompletedAt = DateTime.UtcNow;
                translationRecord.UpdatedAt = DateTime.UtcNow;
                translationRecord.ErrorMessage = null;
                await _dbContext.SaveChangesAsync();

                // Record success metric
                _metricsService.RecordTranslationJob(translator, targetLanguage, "success");

                _logger.LogInformation("[TRANSLATION-JOB] Translation job completed successfully for lesson {LessonId}, language {TargetLanguage}, duration {Duration}s",
                    lessonId, targetLanguage, (DateTime.UtcNow - startTime).TotalSeconds);
            }
            catch (Exception ex)
            {
                // Update record with failure status
                translationRecord.Status = "Failed";
                translationRecord.ErrorMessage = ex.Message.Length > 1000 ? ex.Message.Substring(0, 1000) : ex.Message;
                translationRecord.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                // Record failure metric
                _metricsService.RecordTranslationJob(translator, targetLanguage, "failed");

                _logger.LogError(ex, "[TRANSLATION-JOB] Translation job failed for lesson {LessonId}, language {TargetLanguage}",
                    lessonId, targetLanguage);

                throw; // Re-throw to trigger Hangfire retry
            }
        }

        /// <summary>
        /// Translate transcript segments using Azure Translator API.
        /// </summary>
        private async Task<List<TranslatedSegment>> TranslateWithAzureAsync(Guid lessonId, List<TranscriptSegmentDto> segments, string targetLanguage)
        {
            // Convert TranscriptSegmentDto to TranscriptSegment for Azure API
            var transcriptSegments = segments.Select((s, i) => new Core.Interfaces.TranscriptSegment
            {
                Index = i,
                StartSeconds = s.StartTime,
                EndSeconds = s.EndTime,
                Text = s.Text
            }).ToList();

            // Call Azure Translator batch API
            var result = await _azureTranslator.TranslateBatchAsync(
                lessonId,
                transcriptSegments,
                "en",
                targetLanguage,
                CancellationToken.None);

            // Convert AzureTranslatedSegment to TranslatedSegment
            var translatedSegments = result.Segments.Select(s => new TranslatedSegment
            {
                Index = s.Index,
                StartSeconds = s.StartSeconds,
                EndSeconds = s.EndSeconds,
                OriginalText = s.OriginalText,
                TranslatedText = s.TranslatedText,
                ConfidenceScore = s.ConfidenceScore
            }).ToList();

            return translatedSegments;
        }

        /// <summary>
        /// Translate transcript segments using Ollama with batch processing.
        /// Phase 8.6 (v2.3.24-dev): Optimized batch translation using TranslateGemma model.
        /// Sends 30 segments per batch to reduce API calls from N to N/30.
        /// Reference: https://github.com/rockbenben/subtitle-translator
        /// </summary>
        private async Task<List<TranslatedSegment>> TranslateWithOllamaAsync(List<TranscriptSegmentDto> segments, string targetLanguage)
        {
            var translatedSegments = new List<TranslatedSegment>();
            const int batchSize = 30; // Optimal batch size for subtitle translation

            // Language name mapping for better translation quality
            var languageNames = new Dictionary<string, string>
            {
                { "es", "Spanish" }, { "fr", "French" }, { "de", "German" },
                { "pt", "Portuguese" }, { "it", "Italian" }, { "ru", "Russian" },
                { "zh", "Chinese" }, { "ja", "Japanese" }, { "ko", "Korean" },
                { "ar", "Arabic" }, { "hi", "Hindi" }, { "nl", "Dutch" },
                { "pl", "Polish" }, { "tr", "Turkish" }, { "vi", "Vietnamese" },
                { "th", "Thai" }, { "sv", "Swedish" }, { "no", "Norwegian" },
                { "da", "Danish" }, { "en", "English" }
            };
            var targetLangName = languageNames.GetValueOrDefault(targetLanguage, targetLanguage);

            _logger.LogInformation("[TRANSLATION-JOB] Starting batch translation: {SegmentCount} segments, batch size {BatchSize}, target {Language}",
                segments.Count, batchSize, targetLangName);

            // Process segments in batches
            for (int batchStart = 0; batchStart < segments.Count; batchStart += batchSize)
            {
                var batch = segments.Skip(batchStart).Take(batchSize).ToList();
                var batchNumber = (batchStart / batchSize) + 1;
                var totalBatches = (int)Math.Ceiling(segments.Count / (double)batchSize);

                _logger.LogDebug("[TRANSLATION-JOB] Processing batch {BatchNum}/{TotalBatches} ({Count} segments)",
                    batchNumber, totalBatches, batch.Count);

                try
                {
                    // Build batch prompt with numbered segments for easy parsing
                    var batchPrompt = BuildBatchTranslationPrompt(batch, targetLangName);

                    // Call Ollama with TranslateGemma model for batch translation
                    var response = await _azureTranslator.TranslateBatchWithOllamaAsync(
                        batchPrompt,
                        "en",
                        targetLanguage,
                        CancellationToken.None);

                    // Parse batch response back to individual segments
                    var translatedTexts = ParseBatchResponse(response, batch.Count);

                    // Map results to TranslatedSegment objects
                    for (int i = 0; i < batch.Count; i++)
                    {
                        var segment = batch[i];
                        var globalIndex = batchStart + i;
                        var translatedText = i < translatedTexts.Count ? translatedTexts[i] : segment.Text;

                        translatedSegments.Add(new TranslatedSegment
                        {
                            Index = globalIndex,
                            StartSeconds = segment.StartTime,
                            EndSeconds = segment.EndTime,
                            OriginalText = segment.Text,
                            TranslatedText = translatedText,
                            ConfidenceScore = null
                        });
                    }

                    _logger.LogDebug("[TRANSLATION-JOB] Batch {BatchNum} completed successfully", batchNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[TRANSLATION-JOB] Batch {BatchNum} failed, falling back to segment-by-segment", batchNumber);

                    // Fallback: translate segments individually in this batch
                    foreach (var segment in batch)
                    {
                        var globalIndex = batchStart + batch.IndexOf(segment);
                        try
                        {
                            var translatedText = await _azureTranslator.TranslateSingleAsync(
                                segment.Text, "en", targetLanguage, CancellationToken.None);

                            translatedSegments.Add(new TranslatedSegment
                            {
                                Index = globalIndex,
                                StartSeconds = segment.StartTime,
                                EndSeconds = segment.EndTime,
                                OriginalText = segment.Text,
                                TranslatedText = translatedText,
                                ConfidenceScore = null
                            });
                        }
                        catch
                        {
                            // Ultimate fallback: keep original text
                            translatedSegments.Add(new TranslatedSegment
                            {
                                Index = globalIndex,
                                StartSeconds = segment.StartTime,
                                EndSeconds = segment.EndTime,
                                OriginalText = segment.Text,
                                TranslatedText = segment.Text,
                                ConfidenceScore = 0.0
                            });
                        }
                    }
                }
            }

            _logger.LogInformation("[TRANSLATION-JOB] Batch translation completed: {TranslatedCount}/{TotalCount} segments",
                translatedSegments.Count, segments.Count);

            return translatedSegments;
        }

        /// <summary>
        /// Build a batch translation prompt with numbered segments.
        /// Format designed for easy parsing of LLM response.
        /// </summary>
        private string BuildBatchTranslationPrompt(List<TranscriptSegmentDto> segments, string targetLanguage)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"Translate the following subtitle segments from English to {targetLanguage}.");
            sb.AppendLine("IMPORTANT: Output ONLY the translations, one per line, maintaining the same order.");
            sb.AppendLine("Do NOT include numbers, explanations, or any other text.");
            sb.AppendLine();
            sb.AppendLine("=== SEGMENTS TO TRANSLATE ===");

            for (int i = 0; i < segments.Count; i++)
            {
                sb.AppendLine($"[{i + 1}] {segments[i].Text}");
            }

            sb.AppendLine();
            sb.AppendLine("=== TRANSLATIONS ===");

            return sb.ToString();
        }

        /// <summary>
        /// Parse batch translation response back to individual segment texts.
        /// Handles various LLM output formats robustly.
        /// </summary>
        private List<string> ParseBatchResponse(string response, int expectedCount)
        {
            var results = new List<string>();

            if (string.IsNullOrWhiteSpace(response))
                return results;

            // Split by newlines and clean up
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            foreach (var line in lines)
            {
                var cleaned = line;

                // Remove common prefixes: [1], 1., 1), (1), etc.
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^\s*[\[\(]?\d+[\]\)\.]?\s*", "");

                // Remove markdown formatting
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^\*\*|\*\*$", "");

                // Skip header/footer lines
                if (cleaned.StartsWith("===") || cleaned.StartsWith("---") ||
                    cleaned.ToLower().Contains("translation") && cleaned.Length < 30)
                    continue;

                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    results.Add(cleaned);
                }

                // Stop if we have enough results
                if (results.Count >= expectedCount)
                    break;
            }

            return results;
        }

        /// <summary>
        /// Schedule translation job to run immediately.
        /// </summary>
        public static string Enqueue(Guid lessonId, string targetLanguage, string translator = "ollama")
        {
            return BackgroundJob.Enqueue<TranslationJob>(
                job => job.ExecuteAsync(lessonId, targetLanguage, translator));
        }

        /// <summary>
        /// Schedule translation job to run after a delay.
        /// </summary>
        public static string Schedule(Guid lessonId, string targetLanguage, string translator, TimeSpan delay)
        {
            return BackgroundJob.Schedule<TranslationJob>(
                job => job.ExecuteAsync(lessonId, targetLanguage, translator),
                delay);
        }
    }

    /// <summary>
    /// Internal DTO for translated subtitle segments.
    /// </summary>
    internal class TranslatedSegment
    {
        public int Index { get; set; }
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public double? ConfidenceScore { get; set; }
    }
}
