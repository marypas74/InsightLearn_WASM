using Prometheus;
using System.Diagnostics;

namespace InsightLearn.Application.Services;

/// <summary>
/// Centralized metrics service for Prometheus monitoring
/// Provides application-level metrics for business logic tracking
/// </summary>
public class MetricsService
{
    private readonly ILogger<MetricsService> _logger;

    // ==================================================
    // COUNTERS - Monotonically increasing values
    // ==================================================

    /// <summary>
    /// Total API requests processed
    /// Labels: method (GET/POST/PUT/DELETE), endpoint (/api/courses), status (200/400/500)
    /// </summary>
    private static readonly Counter ApiRequestsTotal = Metrics.CreateCounter(
        "insightlearn_api_requests_total",
        "Total number of API requests processed",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "endpoint", "status" }
        });

    /// <summary>
    /// Total course enrollments created
    /// </summary>
    private static readonly Counter EnrollmentsTotal = Metrics.CreateCounter(
        "insightlearn_enrollments_total",
        "Total number of course enrollments created");

    /// <summary>
    /// Total payments processed
    /// Labels: status (pending/completed/failed/refunded), payment_method (stripe/paypal)
    /// </summary>
    private static readonly Counter PaymentsTotal = Metrics.CreateCounter(
        "insightlearn_payments_total",
        "Total number of payments processed",
        new CounterConfiguration
        {
            LabelNames = new[] { "status", "payment_method" }
        });

    /// <summary>
    /// Total payment revenue in USD
    /// Labels: status (completed/refunded), payment_method (stripe/paypal)
    /// </summary>
    private static readonly Counter PaymentRevenueTotal = Metrics.CreateCounter(
        "insightlearn_payment_revenue_total",
        "Total payment revenue in USD",
        new CounterConfiguration
        {
            LabelNames = new[] { "status", "payment_method" }
        });

    /// <summary>
    /// Total chatbot messages processed
    /// Labels: model (qwen2:0.5b/phi3:mini)
    /// </summary>
    private static readonly Counter ChatbotMessagesTotal = Metrics.CreateCounter(
        "insightlearn_chatbot_messages_total",
        "Total number of chatbot messages processed",
        new CounterConfiguration
        {
            LabelNames = new[] { "model" }
        });

    /// <summary>
    /// Total video uploads
    /// Labels: status (success/failed)
    /// </summary>
    private static readonly Counter VideoUploadsTotal = Metrics.CreateCounter(
        "insightlearn_video_uploads_total",
        "Total number of video uploads",
        new CounterConfiguration
        {
            LabelNames = new[] { "status" }
        });

    /// <summary>
    /// Total user registrations
    /// Labels: user_type (Student/Instructor/Admin)
    /// </summary>
    private static readonly Counter UserRegistrationsTotal = Metrics.CreateCounter(
        "insightlearn_user_registrations_total",
        "Total number of user registrations",
        new CounterConfiguration
        {
            LabelNames = new[] { "user_type" }
        });

    /// <summary>
    /// Total login attempts
    /// Labels: status (success/failed)
    /// </summary>
    private static readonly Counter LoginAttemptsTotal = Metrics.CreateCounter(
        "insightlearn_login_attempts_total",
        "Total number of login attempts",
        new CounterConfiguration
        {
            LabelNames = new[] { "status" }
        });

    /// <summary>
    /// Total transcript generation jobs
    /// Labels: status (success/failed/timeout)
    /// Part of Batch Video Transcription System v2.3.23-dev
    /// </summary>
    private static readonly Counter TranscriptJobsTotal = Metrics.CreateCounter(
        "insightlearn_transcript_jobs_total",
        "Total transcript generation jobs",
        new CounterConfiguration
        {
            LabelNames = new[] { "status" }
        });

    /// <summary>
    /// Total subtitle translation jobs
    /// Labels: translator (azure/ollama), target_language (es/fr/de/pt/etc), status (success/failed)
    /// Part of Phase 8: Multi-Language Subtitle Support - LinkedIn Learning parity (v2.3.24-dev)
    /// </summary>
    private static readonly Counter TranslationJobsTotal = Metrics.CreateCounter(
        "insightlearn_translation_jobs_total",
        "Total subtitle translation jobs",
        new CounterConfiguration
        {
            LabelNames = new[] { "translator", "target_language", "status" }
        });

    /// <summary>
    /// Total OpenAI tokens consumed
    /// Labels: operation_type (chat/translate/summary/concepts), model (gpt-4-turbo/gpt-3.5-turbo), token_type (prompt/completion/total)
    /// Tracks AI usage and costs for monitoring and billing
    /// </summary>
    private static readonly Counter OpenAITokensTotal = Metrics.CreateCounter(
        "insightlearn_openai_tokens_total",
        "Total OpenAI tokens consumed",
        new CounterConfiguration
        {
            LabelNames = new[] { "operation_type", "model", "token_type" }
        });

    // ==================================================
    // GAUGES - Current snapshot values (can go up/down)
    // ==================================================

    /// <summary>
    /// Number of currently active users (sessions active in last 15 minutes)
    /// </summary>
    private static readonly Gauge ActiveUsersGauge = Metrics.CreateGauge(
        "insightlearn_active_users",
        "Number of currently active users (last 15 minutes)");

    /// <summary>
    /// Number of active course enrollments
    /// </summary>
    private static readonly Gauge ActiveEnrollmentsGauge = Metrics.CreateGauge(
        "insightlearn_active_enrollments",
        "Number of active course enrollments");

    /// <summary>
    /// Number of courses available
    /// Labels: status (published/draft)
    /// </summary>
    private static readonly Gauge CoursesGauge = Metrics.CreateGauge(
        "insightlearn_courses",
        "Number of courses available",
        new GaugeConfiguration
        {
            LabelNames = new[] { "status" }
        });

    /// <summary>
    /// MongoDB video storage size in bytes
    /// </summary>
    private static readonly Gauge VideoStorageSizeBytes = Metrics.CreateGauge(
        "insightlearn_video_storage_bytes",
        "Total size of video storage in MongoDB GridFS (bytes)");

    /// <summary>
    /// Database connection pool active connections
    /// </summary>
    private static readonly Gauge DatabaseConnectionsGauge = Metrics.CreateGauge(
        "insightlearn_database_connections",
        "Number of active database connections");

    // ==================================================
    // HISTOGRAMS - Distribution of observed values
    // ==================================================

    /// <summary>
    /// API request duration in seconds
    /// Labels: method (GET/POST/PUT/DELETE), endpoint (/api/courses)
    /// Buckets: 1ms, 2ms, 5ms, 10ms, 25ms, 50ms, 100ms, 250ms, 500ms, 1s, 2.5s, 5s, 10s
    /// </summary>
    private static readonly Histogram ApiRequestDuration = Metrics.CreateHistogram(
        "insightlearn_api_request_duration_seconds",
        "API request duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "method", "endpoint" },
            Buckets = new[] { 0.001, 0.002, 0.005, 0.010, 0.025, 0.050, 0.100, 0.250, 0.500, 1.0, 2.5, 5.0, 10.0 }
        });

    /// <summary>
    /// Ollama AI inference duration in seconds
    /// Labels: model (qwen2:0.5b/phi3:mini)
    /// Buckets: 100ms, 250ms, 500ms, 1s, 2s, 5s, 10s, 30s
    /// </summary>
    private static readonly Histogram OllamaInferenceDuration = Metrics.CreateHistogram(
        "insightlearn_ollama_inference_duration_seconds",
        "Ollama AI inference duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "model" },
            Buckets = new[] { 0.1, 0.25, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0 }
        });

    /// <summary>
    /// Database query duration in seconds
    /// Labels: operation (select/insert/update/delete)
    /// </summary>
    private static readonly Histogram DatabaseQueryDuration = Metrics.CreateHistogram(
        "insightlearn_database_query_duration_seconds",
        "Database query duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "operation" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 12) // 1ms to ~4s
        });

    /// <summary>
    /// Transcript processing duration in seconds
    /// Labels: video_duration_minutes (5, 10, 15, 30, 60+)
    /// Buckets: 10s, 30s, 1m, 2m, 5m, 10m, 20m
    /// Part of Batch Video Transcription System v2.3.23-dev
    /// </summary>
    private static readonly Histogram TranscriptProcessingDuration = Metrics.CreateHistogram(
        "insightlearn_transcript_processing_duration_seconds",
        "Transcript processing duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "video_duration_minutes" },
            Buckets = new[] { 10.0, 30.0, 60.0, 120.0, 300.0, 600.0, 1200.0 } // 10s to 20 minutes
        });

    /// <summary>
    /// Translation processing duration in seconds
    /// Labels: translator (azure/ollama), target_language (es/fr/de/pt/etc)
    /// Buckets: 1s, 5s, 10s, 30s, 1m, 2m, 5m, 10m
    /// Part of Phase 8: Multi-Language Subtitle Support - LinkedIn Learning parity (v2.3.24-dev)
    /// </summary>
    private static readonly Histogram TranslationProcessingDuration = Metrics.CreateHistogram(
        "insightlearn_translation_processing_duration_seconds",
        "Translation processing duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "translator", "target_language" },
            Buckets = new[] { 1.0, 5.0, 10.0, 30.0, 60.0, 120.0, 300.0, 600.0 } // 1s to 10 minutes
        });

    // ==================================================
    // SUMMARIES - Statistical distribution (quantiles)
    // ==================================================

    /// <summary>
    /// Video upload size in bytes (p50, p90, p99)
    /// Labels: status (success/failed)
    /// </summary>
    private static readonly Summary VideoUploadSize = Metrics.CreateSummary(
        "insightlearn_video_upload_size_bytes",
        "Video upload size in bytes (p50, p90, p99)",
        new SummaryConfiguration
        {
            LabelNames = new[] { "status" },
            Objectives = new[]
            {
                new QuantileEpsilonPair(0.5, 0.05),  // p50 ±5%
                new QuantileEpsilonPair(0.9, 0.01),  // p90 ±1%
                new QuantileEpsilonPair(0.99, 0.001) // p99 ±0.1%
            }
        });

    /// <summary>
    /// Payment amount in USD (p50, p90, p99)
    /// Labels: payment_method (stripe/paypal)
    /// </summary>
    private static readonly Summary PaymentAmount = Metrics.CreateSummary(
        "insightlearn_payment_amount_usd",
        "Payment amount in USD (p50, p90, p99)",
        new SummaryConfiguration
        {
            LabelNames = new[] { "payment_method" },
            Objectives = new[]
            {
                new QuantileEpsilonPair(0.5, 0.05),
                new QuantileEpsilonPair(0.9, 0.01),
                new QuantileEpsilonPair(0.99, 0.001)
            }
        });

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
        _logger.LogInformation("[METRICS] MetricsService initialized - Prometheus metrics available at /metrics");
    }

    // ==================================================
    // PUBLIC METHODS - Counter increments
    // ==================================================

    /// <summary>
    /// Record an API request
    /// </summary>
    public void RecordApiRequest(string method, string endpoint, int statusCode)
    {
        try
        {
            ApiRequestsTotal.WithLabels(method, endpoint, statusCode.ToString()).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record API request metric");
        }
    }

    /// <summary>
    /// Record a course enrollment
    /// </summary>
    public void RecordEnrollment()
    {
        try
        {
            EnrollmentsTotal.Inc();
            _logger.LogDebug("[METRICS] Recorded enrollment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record enrollment metric");
        }
    }

    /// <summary>
    /// Record a payment transaction
    /// </summary>
    /// <param name="status">Payment status (pending/completed/failed/refunded)</param>
    /// <param name="paymentMethod">Payment method (stripe/paypal)</param>
    /// <param name="amount">Payment amount in USD (optional for revenue tracking)</param>
    public void RecordPayment(string status, string paymentMethod, decimal? amount = null)
    {
        try
        {
            PaymentsTotal.WithLabels(status, paymentMethod).Inc();

            // Track revenue for completed/refunded payments
            if (amount.HasValue && (status == "completed" || status == "refunded"))
            {
                var revenueAmount = status == "refunded" ? -Math.Abs((double)amount.Value) : (double)amount.Value;
                PaymentRevenueTotal.WithLabels(status, paymentMethod).Inc(revenueAmount);
            }

            _logger.LogDebug("[METRICS] Recorded payment: status={Status}, method={Method}, amount={Amount}",
                status, paymentMethod, amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record payment metric");
        }
    }

    /// <summary>
    /// Record a chatbot message
    /// </summary>
    public void RecordChatbotMessage(string model)
    {
        try
        {
            ChatbotMessagesTotal.WithLabels(model).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record chatbot message metric");
        }
    }

    /// <summary>
    /// Record a video upload
    /// </summary>
    public void RecordVideoUpload(string status, long sizeBytes)
    {
        try
        {
            VideoUploadsTotal.WithLabels(status).Inc();
            VideoUploadSize.WithLabels(status).Observe(sizeBytes);
            _logger.LogDebug("[METRICS] Recorded video upload: status={Status}, size={Size}MB",
                status, sizeBytes / 1024.0 / 1024.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record video upload metric");
        }
    }

    /// <summary>
    /// Record a user registration
    /// </summary>
    public void RecordUserRegistration(string userType)
    {
        try
        {
            UserRegistrationsTotal.WithLabels(userType).Inc();
            _logger.LogDebug("[METRICS] Recorded user registration: type={UserType}", userType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record user registration metric");
        }
    }

    /// <summary>
    /// Record a login attempt
    /// </summary>
    public void RecordLoginAttempt(bool success)
    {
        try
        {
            LoginAttemptsTotal.WithLabels(success ? "success" : "failed").Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record login attempt metric");
        }
    }

    /// <summary>
    /// Record a transcript generation job completion
    /// Part of Batch Video Transcription System v2.3.23-dev
    /// </summary>
    /// <param name="status">Job status (success/failed/timeout)</param>
    public void RecordTranscriptJob(string status)
    {
        try
        {
            TranscriptJobsTotal.WithLabels(status).Inc();
            _logger.LogDebug("[METRICS] Recorded transcript job: status={Status}", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record transcript job metric");
        }
    }

    /// <summary>
    /// Record a translation job completion
    /// Part of Phase 8: Multi-Language Subtitle Support - LinkedIn Learning parity (v2.3.24-dev)
    /// </summary>
    /// <param name="translator">Translator used (azure/ollama)</param>
    /// <param name="targetLanguage">Target language code (es/fr/de/pt/etc)</param>
    /// <param name="status">Job status (success/failed)</param>
    public void RecordTranslationJob(string translator, string targetLanguage, string status)
    {
        try
        {
            TranslationJobsTotal.WithLabels(translator, targetLanguage, status).Inc();
            _logger.LogDebug("[METRICS] Recorded translation job: translator={Translator}, language={Language}, status={Status}",
                translator, targetLanguage, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record translation job metric");
        }
    }

    /// <summary>
    /// Record OpenAI token usage
    /// Tracks tokens consumed for AI operations (chat, translation, summary, concept extraction)
    /// </summary>
    /// <param name="operationType">Operation type (chat/translate/summary/concepts)</param>
    /// <param name="model">OpenAI model used (gpt-4-turbo/gpt-4/gpt-3.5-turbo)</param>
    /// <param name="promptTokens">Number of tokens in the prompt</param>
    /// <param name="completionTokens">Number of tokens in the completion</param>
    /// <param name="totalTokens">Total tokens (prompt + completion)</param>
    public void RecordOpenAITokens(string operationType, string model, int promptTokens, int completionTokens, int totalTokens)
    {
        try
        {
            // Record prompt tokens
            OpenAITokensTotal.WithLabels(operationType, model, "prompt").Inc(promptTokens);

            // Record completion tokens
            OpenAITokensTotal.WithLabels(operationType, model, "completion").Inc(completionTokens);

            // Record total tokens
            OpenAITokensTotal.WithLabels(operationType, model, "total").Inc(totalTokens);

            _logger.LogDebug("[METRICS] Recorded OpenAI tokens: operation={Operation}, model={Model}, prompt={Prompt}, completion={Completion}, total={Total}",
                operationType, model, promptTokens, completionTokens, totalTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record OpenAI tokens metric");
        }
    }

    // ==================================================
    // PUBLIC METHODS - Gauge updates
    // ==================================================

    /// <summary>
    /// Update active users gauge
    /// </summary>
    public void SetActiveUsers(int count)
    {
        try
        {
            ActiveUsersGauge.Set(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to update active users gauge");
        }
    }

    /// <summary>
    /// Update active enrollments gauge
    /// </summary>
    public void SetActiveEnrollments(int count)
    {
        try
        {
            ActiveEnrollmentsGauge.Set(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to update active enrollments gauge");
        }
    }

    /// <summary>
    /// Update courses gauge
    /// </summary>
    public void SetCourses(string status, int count)
    {
        try
        {
            CoursesGauge.WithLabels(status).Set(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to update courses gauge");
        }
    }

    /// <summary>
    /// Update video storage size gauge
    /// </summary>
    public void SetVideoStorageSize(long bytes)
    {
        try
        {
            VideoStorageSizeBytes.Set(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to update video storage size gauge");
        }
    }

    /// <summary>
    /// Update database connections gauge
    /// </summary>
    public void SetDatabaseConnections(int count)
    {
        try
        {
            DatabaseConnectionsGauge.Set(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to update database connections gauge");
        }
    }

    // ==================================================
    // PUBLIC METHODS - Duration measurements (Histograms)
    // ==================================================

    /// <summary>
    /// Measure API request duration
    /// Usage: using (metricsService.MeasureApiDuration("GET", "/api/courses")) { ... }
    /// </summary>
    public IDisposable MeasureApiDuration(string method, string endpoint)
    {
        return ApiRequestDuration.WithLabels(method, endpoint).NewTimer();
    }

    /// <summary>
    /// Measure Ollama inference duration
    /// Usage: using (metricsService.MeasureOllamaInference("qwen2:0.5b")) { ... }
    /// </summary>
    public IDisposable MeasureOllamaInference(string model)
    {
        return OllamaInferenceDuration.WithLabels(model).NewTimer();
    }

    /// <summary>
    /// Measure database query duration
    /// Usage: using (metricsService.MeasureDatabaseQuery("select")) { ... }
    /// </summary>
    public IDisposable MeasureDatabaseQuery(string operation)
    {
        return DatabaseQueryDuration.WithLabels(operation).NewTimer();
    }

    /// <summary>
    /// Measure transcript processing duration
    /// Usage: using (metricsService.MeasureTranscriptProcessing(videoDurationMinutes)) { ... }
    /// Part of Batch Video Transcription System v2.3.23-dev
    /// </summary>
    /// <param name="videoDurationMinutes">Video duration in minutes for labeling</param>
    public IDisposable MeasureTranscriptProcessing(int videoDurationMinutes)
    {
        return TranscriptProcessingDuration.WithLabels(videoDurationMinutes.ToString()).NewTimer();
    }

    /// <summary>
    /// Measure translation processing duration
    /// Usage: using (metricsService.MeasureTranslationProcessing("azure", "es")) { ... }
    /// Part of Phase 8: Multi-Language Subtitle Support - LinkedIn Learning parity (v2.3.24-dev)
    /// </summary>
    /// <param name="translator">Translator used (azure/ollama)</param>
    /// <param name="targetLanguage">Target language code (es/fr/de/pt/etc)</param>
    public IDisposable MeasureTranslationProcessing(string translator, string targetLanguage)
    {
        return TranslationProcessingDuration.WithLabels(translator, targetLanguage).NewTimer();
    }

    /// <summary>
    /// Record a payment amount for summary statistics
    /// </summary>
    public void RecordPaymentAmount(string paymentMethod, decimal amount)
    {
        try
        {
            PaymentAmount.WithLabels(paymentMethod).Observe((double)amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to record payment amount");
        }
    }

    // ==================================================
    // UTILITY METHODS - Metric snapshots
    // ==================================================

    /// <summary>
    /// Get current metric snapshot (for debugging/testing)
    /// </summary>
    public string GetMetricsSnapshot()
    {
        try
        {
            // Prometheus metrics are exposed via /metrics endpoint
            // This method is for debugging only
            return $"[METRICS SNAPSHOT] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                   $"Metrics available at /metrics endpoint\n" +
                   $"Grafana dashboard: http://localhost:3000";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METRICS] Failed to get metrics snapshot");
            return "Error generating metrics snapshot";
        }
    }
}
