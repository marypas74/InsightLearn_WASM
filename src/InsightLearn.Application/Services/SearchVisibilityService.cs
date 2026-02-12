using System.Text.Json;
using Prometheus;

namespace InsightLearn.Application.Services;

/// <summary>
/// Search Visibility Service - Monitors real SEO visibility metrics
/// Uses Google Search indexing check + PageSpeed for InsightLearn itself
/// Exposes Prometheus metrics for Grafana dashboard
/// </summary>
public class SearchVisibilityService
{
    private readonly ILogger<SearchVisibilityService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private static DateTime _lastUpdate = DateTime.MinValue;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(4);

    private const string SiteUrl = "https://www.insightlearn.cloud";

    // Pages to check indexation for
    private static readonly List<string> CriticalPages = new()
    {
        "/",
        "/courses",
        "/about",
        "/faq",
        "/categories",
        "/pricing",
        "/contact",
        "/blog",
        "/instructors",
        "/privacy-policy",
        "/terms-of-service",
        "/search"
    };

    // --- Prometheus Gauges ---
    private static readonly Gauge IndexedPages = Metrics.CreateGauge(
        "insightlearn_seo_indexed_pages",
        "Number of pages indexed by Google");

    private static readonly Gauge TotalPagesInSitemap = Metrics.CreateGauge(
        "insightlearn_seo_total_sitemap_pages",
        "Total pages in sitemap.xml");

    private static readonly Gauge IndexationRate = Metrics.CreateGauge(
        "insightlearn_seo_indexation_rate",
        "Percentage of sitemap pages indexed by Google (0-100)");

    private static readonly Gauge SitePerformanceScore = Metrics.CreateGauge(
        "insightlearn_seo_site_performance_score",
        "Google PageSpeed performance score for InsightLearn (0-100)");

    private static readonly Gauge SiteLcpMs = Metrics.CreateGauge(
        "insightlearn_seo_site_lcp_ms",
        "Largest Contentful Paint for InsightLearn in milliseconds");

    private static readonly Gauge SiteInpMs = Metrics.CreateGauge(
        "insightlearn_seo_site_inp_ms",
        "Interaction to Next Paint for InsightLearn in milliseconds");

    private static readonly Gauge SiteCls = Metrics.CreateGauge(
        "insightlearn_seo_site_cls",
        "Cumulative Layout Shift for InsightLearn");

    private static readonly Gauge PageIndexStatus = Metrics.CreateGauge(
        "insightlearn_seo_page_indexed",
        "Whether a specific page is indexed by Google (1=yes, 0=no)",
        new GaugeConfiguration { LabelNames = new[] { "page" } });

    private static readonly Gauge DomainAuthority = Metrics.CreateGauge(
        "insightlearn_seo_domain_authority_estimate",
        "Estimated domain authority based on indexed pages and backlinks (0-100)");

    private static readonly Gauge SnapshotCoverage = Metrics.CreateGauge(
        "insightlearn_seo_snapshot_coverage",
        "Percentage of sitemap pages with SEO snapshots (0-100)");

    public SearchVisibilityService(
        ILogger<SearchVisibilityService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task UpdateVisibilityMetricsAsync()
    {
        if (DateTime.UtcNow - _lastUpdate < UpdateInterval)
        {
            _logger.LogDebug("[VISIBILITY] Cache still valid, skipping update");
            return;
        }

        _logger.LogInformation("[VISIBILITY] Starting visibility metrics update...");

        try
        {
            // 1. Check Google indexation for critical pages
            var indexedCount = await CheckGoogleIndexationAsync();

            // 2. Get PageSpeed data for our own site
            await FetchOwnPageSpeedAsync();

            // 3. Calculate derived metrics
            var totalSitemapPages = 60; // From sitemap analysis
            TotalPagesInSitemap.Set(totalSitemapPages);
            IndexedPages.Set(indexedCount);
            IndexationRate.Set(totalSitemapPages > 0 ? (indexedCount * 100.0 / totalSitemapPages) : 0);

            // 4. Calculate snapshot coverage
            var snapshotPages = 12; // Current snapshot count
            SnapshotCoverage.Set(totalSitemapPages > 0 ? (snapshotPages * 100.0 / totalSitemapPages) : 0);

            // 5. Estimate domain authority (simplified formula)
            var daEstimate = CalculateDomainAuthorityEstimate(indexedCount, totalSitemapPages);
            DomainAuthority.Set(daEstimate);

            _lastUpdate = DateTime.UtcNow;
            _logger.LogInformation("[VISIBILITY] Update completed: {Indexed}/{Total} pages indexed, DA estimate: {DA}",
                indexedCount, totalSitemapPages, daEstimate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VISIBILITY] Failed to update visibility metrics");
        }
    }

    private async Task<int> CheckGoogleIndexationAsync()
    {
        var indexedCount = 0;

        foreach (var page in CriticalPages)
        {
            try
            {
                var fullUrl = $"{SiteUrl}{page}";
                var searchUrl = $"https://www.google.com/search?q=site:{Uri.EscapeDataString(fullUrl)}&num=1";

                using var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                request.Headers.Add("Accept", "text/html");
                request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                // Check if Google returned results (not "did not match any documents")
                var isIndexed = !content.Contains("did not match any documents") &&
                                !content.Contains("No results found") &&
                                content.Contains("insightlearn.cloud");

                PageIndexStatus.WithLabels(page).Set(isIndexed ? 1 : 0);

                if (isIndexed)
                {
                    indexedCount++;
                    _logger.LogDebug("[VISIBILITY] Page {Page} IS indexed", page);
                }
                else
                {
                    _logger.LogDebug("[VISIBILITY] Page {Page} NOT indexed", page);
                }

                // Rate limit - don't hammer Google
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[VISIBILITY] Failed to check indexation for {Page}", page);
                PageIndexStatus.WithLabels(page).Set(0);
            }
        }

        return indexedCount;
    }

    private async Task FetchOwnPageSpeedAsync()
    {
        try
        {
            var apiKey = _configuration["GooglePageSpeedApiKey"] ?? "";
            var apiUrl = $"https://www.googleapis.com/pagespeedonline/v5/runPagespeed?url={Uri.EscapeDataString(SiteUrl)}&strategy=mobile&category=performance";

            if (!string.IsNullOrEmpty(apiKey))
                apiUrl += $"&key={apiKey}";

            var response = await _httpClient.GetStringAsync(apiUrl);
            var json = JsonDocument.Parse(response);

            if (json.RootElement.TryGetProperty("lighthouseResult", out var lighthouse))
            {
                if (lighthouse.TryGetProperty("categories", out var categories) &&
                    categories.TryGetProperty("performance", out var performance) &&
                    performance.TryGetProperty("score", out var score))
                {
                    SitePerformanceScore.Set((int)(score.GetDouble() * 100));
                }

                if (lighthouse.TryGetProperty("audits", out var audits))
                {
                    if (audits.TryGetProperty("largest-contentful-paint", out var lcp) &&
                        lcp.TryGetProperty("numericValue", out var lcpValue))
                        SiteLcpMs.Set((int)lcpValue.GetDouble());

                    if (audits.TryGetProperty("cumulative-layout-shift", out var cls) &&
                        cls.TryGetProperty("numericValue", out var clsValue))
                        SiteCls.Set(clsValue.GetDouble());

                    if (audits.TryGetProperty("total-blocking-time", out var tbt) &&
                        tbt.TryGetProperty("numericValue", out var tbtValue))
                        SiteInpMs.Set((int)tbtValue.GetDouble());
                }

                _logger.LogInformation("[VISIBILITY] Own PageSpeed: Perf={Perf}",
                    SitePerformanceScore.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VISIBILITY] Failed to fetch own PageSpeed data");
        }
    }

    private double CalculateDomainAuthorityEstimate(int indexedPages, int totalPages)
    {
        // Simplified DA estimate based on:
        // - Indexation ratio (40%)
        // - Number of indexed pages (30%)
        // - Site age factor (30%) - assume new site
        var indexationScore = totalPages > 0 ? (indexedPages * 100.0 / totalPages) : 0;
        var pageCountScore = Math.Min(indexedPages * 2.0, 100); // 50 indexed pages = 100
        var ageScore = 15; // New site - low age factor

        return Math.Round(indexationScore * 0.4 + pageCountScore * 0.3 + ageScore * 0.3);
    }
}