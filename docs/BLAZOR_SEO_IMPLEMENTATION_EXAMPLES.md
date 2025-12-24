# Blazor WebAssembly SEO Implementation Examples

**Platform**: .NET 8 Blazor WebAssembly
**Purpose**: Practical code examples for implementing SEO features in InsightLearn LMS

---

## Table of Contents

1. [Dynamic Meta Tags with HeadOutlet](#dynamic-meta-tags-with-headoutlet)
2. [Pre-rendering Configuration](#pre-rendering-configuration)
3. [Structured Data Component](#structured-data-component)
4. [Dynamic Sitemap Generation API](#dynamic-sitemap-generation-api)
5. [SEO-Friendly Routing](#seo-friendly-routing)

---

## Dynamic Meta Tags with HeadOutlet

### 1. Update index.html

Replace static meta tags with dynamic placeholders:

```html
<!-- src/InsightLearn.WebAssembly/wwwroot/index.html -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />

    <!-- Dynamic meta tags will be injected here by HeadOutlet -->
    <component type="typeof(HeadOutlet)" render-mode="WebAssemblyPrerendered" />

    <!-- Default fallback title (for crawlers if JS disabled) -->
    <title>InsightLearn - Online Learning Platform</title>

    <!-- Existing CSS/JS links -->
    ...
</head>
<body>
    <div id="app"></div>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

---

### 2. Create SEO Metadata Component

```csharp
// src/InsightLearn.WebAssembly/Components/SeoMetadata.razor

@using Microsoft.AspNetCore.Components.Web

<PageTitle>@Title</PageTitle>
<HeadContent>
    <!-- Primary Meta Tags -->
    <meta name="title" content="@Title" />
    <meta name="description" content="@Description" />

    @if (!string.IsNullOrEmpty(Keywords))
    {
        <meta name="keywords" content="@Keywords" />
    }

    <!-- Canonical URL -->
    <link rel="canonical" href="@CanonicalUrl" />

    <!-- Robots -->
    <meta name="robots" content="@RobotsContent" />

    <!-- Open Graph / Facebook -->
    <meta property="og:type" content="@OgType" />
    <meta property="og:url" content="@CanonicalUrl" />
    <meta property="og:title" content="@Title" />
    <meta property="og:description" content="@Description" />

    @if (!string.IsNullOrEmpty(ImageUrl))
    {
        <meta property="og:image" content="@ImageUrl" />
    }

    <meta property="og:site_name" content="InsightLearn" />

    <!-- Twitter Card -->
    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:url" content="@CanonicalUrl" />
    <meta name="twitter:title" content="@Title" />
    <meta name="twitter:description" content="@Description" />

    @if (!string.IsNullOrEmpty(ImageUrl))
    {
        <meta name="twitter:image" content="@ImageUrl" />
    }
</HeadContent>

@code {
    [Parameter] public string Title { get; set; } = "InsightLearn - Online Learning Platform";
    [Parameter] public string Description { get; set; } = "Access expert-led online courses in technology, business, design, and more.";
    [Parameter] public string? Keywords { get; set; }
    [Parameter] public string CanonicalUrl { get; set; } = string.Empty;
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public string OgType { get; set; } = "website";
    [Parameter] public string RobotsContent { get; set; } = "index, follow";

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (string.IsNullOrEmpty(CanonicalUrl))
        {
            CanonicalUrl = NavigationManager.Uri;
        }
    }
}
```

---

### 3. Use SEO Component in Pages

#### Homepage Example

```razor
@* src/InsightLearn.WebAssembly/Pages/Index.razor *@
@page "/"

<SeoMetadata
    Title="InsightLearn - Online Courses & Professional Learning Platform"
    Description="Access 500+ expert-led online courses in technology, business, design, and more. Learn at your own pace with interactive video lessons."
    Keywords="online courses, e-learning platform, professional development, video tutorials"
    CanonicalUrl="https://www.insightlearn.cloud/"
    ImageUrl="https://www.insightlearn.cloud/images/og-homepage.jpg"
    OgType="website" />

<div class="homepage-content">
    <!-- Your existing homepage content -->
</div>
```

#### Course Detail Page Example

```razor
@* src/InsightLearn.WebAssembly/Pages/Courses/Detail.razor *@
@page "/courses/{CourseId:guid}"

@if (course != null)
{
    <SeoMetadata
        Title="@($"{course.Title} - {course.InstructorName} | InsightLearn")"
        Description="@GetCourseMetaDescription()"
        Keywords="@GetCourseKeywords()"
        CanonicalUrl="@($"https://www.insightlearn.cloud/courses/{CourseId}")"
        ImageUrl="@course.ThumbnailUrl"
        OgType="article" />

    <!-- Course JSON-LD Structured Data -->
    <CourseStructuredData Course="@course" />
}

<div class="course-detail-container">
    <!-- Your existing course detail content -->
</div>

@code {
    [Parameter] public Guid CourseId { get; set; }
    private CourseDto? course;

    private string GetCourseMetaDescription()
    {
        if (course == null) return string.Empty;

        return $"{course.ShortDescription} Learn {course.Title} from expert {course.InstructorName}. " +
               $"{FormatDuration(course.EstimatedDurationMinutes)}h video • {course.EnrollmentCount:N0} students • {course.AverageRating:0.0}★ rating.";
    }

    private string GetCourseKeywords()
    {
        if (course == null) return string.Empty;

        return $"{course.Title}, {course.CategoryName}, {course.InstructorName}, online course, video tutorial";
    }

    private string FormatDuration(int minutes)
    {
        return (minutes / 60.0).ToString("0.#");
    }
}
```

---

## Structured Data Component

### Course Structured Data Component

```razor
@* src/InsightLearn.WebAssembly/Components/CourseStructuredData.razor *@
@using System.Text.Json
@using System.Text.Json.Serialization

<HeadContent>
    <script type="application/ld+json">
        @((MarkupString)GetCourseSchema())
    </script>
</HeadContent>

@code {
    [Parameter] public CourseDto Course { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private string GetCourseSchema()
    {
        var schema = new
        {
            context = "https://schema.org",
            type = "Course",
            name = Course.Title,
            description = Course.Description,
            provider = new
            {
                type = "Organization",
                name = "InsightLearn",
                url = "https://www.insightlearn.cloud",
                logo = "https://www.insightlearn.cloud/icon-192.png"
            },
            instructor = new
            {
                type = "Person",
                name = Course.InstructorName,
                image = Course.InstructorImageUrl
            },
            image = Course.ThumbnailUrl,
            educationalLevel = Course.SkillLevel?.ToString() ?? "All Levels",
            courseCode = Course.Id.ToString(),
            hasCourseInstance = new
            {
                type = "CourseInstance",
                courseMode = "online",
                courseWorkload = $"PT{Course.EstimatedDurationMinutes}M"
            },
            offers = new
            {
                type = "Offer",
                price = Course.Price.ToString("F2"),
                priceCurrency = "EUR",
                availability = "https://schema.org/InStock",
                url = $"https://www.insightlearn.cloud/courses/{Course.Id}",
                validFrom = Course.CreatedAt.ToString("yyyy-MM-dd")
            },
            aggregateRating = new
            {
                type = "AggregateRating",
                ratingValue = Course.AverageRating.ToString("F1"),
                reviewCount = Course.ReviewCount,
                bestRating = "5",
                worstRating = "1"
            },
            inLanguage = Course.Language ?? "en",
            timeRequired = $"PT{Course.EstimatedDurationMinutes}M"
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(schema, options);
    }
}
```

---

### Organization Structured Data (For MainLayout)

```razor
@* src/InsightLearn.WebAssembly/Shared/MainLayout.razor *@

<HeadContent>
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "EducationalOrganization",
        "name": "InsightLearn",
        "url": "https://www.insightlearn.cloud",
        "logo": "https://www.insightlearn.cloud/icon-192.png",
        "description": "Modern online learning platform offering 500+ courses in technology, business, and design.",
        "sameAs": [
            "https://www.linkedin.com/company/insightlearn"
        ],
        "contactPoint": {
            "@type": "ContactPoint",
            "contactType": "Customer Support",
            "email": "support@insightlearn.cloud"
        }
    }
    </script>

    <!-- SearchAction for site search -->
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "WebSite",
        "url": "https://www.insightlearn.cloud",
        "potentialAction": {
            "@type": "SearchAction",
            "target": {
                "@type": "EntryPoint",
                "urlTemplate": "https://www.insightlearn.cloud/search?q={search_term_string}"
            },
            "query-input": "required name=search_term_string"
        }
    }
    </script>
</HeadContent>

<!-- Rest of MainLayout -->
<div class="page">
    @Body
</div>
```

---

## Pre-rendering Configuration

### Enable Pre-rendering in .NET 8 Blazor WASM

**Step 1**: Update Program.cs (Backend API)

```csharp
// src/InsightLearn.Application/Program.cs

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure pre-rendering
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(InsightLearn.WebAssembly._Imports).Assembly);

app.Run();
```

**Step 2**: Update App.razor

```razor
@* src/InsightLearn.WebAssembly/App.razor *@
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />

    <!-- SEO Components will inject meta tags here -->
    <HeadOutlet @rendermode="@RenderModeForPage" />
</head>
<body>
    <Routes @rendermode="@RenderModeForPage" />

    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>

@code {
    private IComponentRenderMode RenderModeForPage =>
        RenderMode.InteractiveWebAssembly;
}
```

**Step 3**: Create Pre-render Service

```csharp
// src/InsightLearn.Application/Services/IPrerenderService.cs

public interface IPrerenderService
{
    Task<string> RenderPageAsync(string path);
}

public class PrerenderService : IPrerenderService
{
    private readonly IServiceProvider _serviceProvider;

    public PrerenderService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<string> RenderPageAsync(string path)
    {
        // Use Blazor's rendering pipeline to generate static HTML
        // This is a simplified example - real implementation is more complex

        var htmlRenderer = _serviceProvider.GetRequiredService<IHtmlRenderer>();
        var result = await htmlRenderer.RenderComponentAsync<App>(
            ParameterView.FromDictionary(new Dictionary<string, object?>())
        );

        return result.ToHtmlString();
    }
}
```

**Note**: Full pre-rendering in Blazor WASM requires a hybrid hosting model. For best SEO results, consider:
1. Using Blazor Server for SEO-critical pages (courses, categories)
2. Using Blazor WASM for authenticated pages (dashboard, learning interface)
3. Implementing a prerendering service that generates static HTML at build time

---

## Dynamic Sitemap Generation API

### Backend API Endpoint

```csharp
// src/InsightLearn.Application/Controllers/SitemapController.cs

using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml.Linq;

[ApiController]
[Route("api/[controller]")]
public class SitemapController : ControllerBase
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICategoryRepository _categoryRepository;

    public SitemapController(
        ICourseRepository courseRepository,
        ICategoryRepository categoryRepository)
    {
        _courseRepository = courseRepository;
        _categoryRepository = categoryRepository;
    }

    [HttpGet("sitemap.xml")]
    [Produces("application/xml")]
    public async Task<IActionResult> GetSitemap()
    {
        var baseUrl = "https://www.insightlearn.cloud";

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XNamespace imageNs = "http://www.google.com/schemas/sitemap-image/1.1";

        var urlset = new XElement(ns + "urlset",
            new XAttribute(XNamespace.Xmlns + "image", imageNs));

        // Static pages
        AddStaticUrls(urlset, ns, baseUrl);

        // Dynamic course pages
        var courses = await _courseRepository.GetAllPublishedCoursesAsync();
        foreach (var course in courses)
        {
            var courseUrl = new XElement(ns + "url",
                new XElement(ns + "loc", $"{baseUrl}/courses/{course.Id}"),
                new XElement(ns + "lastmod", course.UpdatedAt?.ToString("yyyy-MM-dd") ?? course.CreatedAt.ToString("yyyy-MM-dd")),
                new XElement(ns + "changefreq", "weekly"),
                new XElement(ns + "priority", "0.8")
            );

            // Add course thumbnail as image
            if (!string.IsNullOrEmpty(course.ThumbnailUrl))
            {
                courseUrl.Add(new XElement(imageNs + "image",
                    new XElement(imageNs + "loc", course.ThumbnailUrl),
                    new XElement(imageNs + "title", course.Title)
                ));
            }

            urlset.Add(courseUrl);
        }

        // Dynamic category pages
        var categories = await _categoryRepository.GetAllAsync();
        foreach (var category in categories)
        {
            urlset.Add(new XElement(ns + "url",
                new XElement(ns + "loc", $"{baseUrl}/courses?category={category.Id}"),
                new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                new XElement(ns + "changefreq", "daily"),
                new XElement(ns + "priority", "0.7")
            ));
        }

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            urlset
        );

        var xml = document.ToString();
        return Content(xml, "application/xml", Encoding.UTF8);
    }

    private void AddStaticUrls(XElement urlset, XNamespace ns, string baseUrl)
    {
        var staticPages = new[]
        {
            new { Url = "/", Priority = "1.0", ChangeFreq = "daily" },
            new { Url = "/courses", Priority = "0.9", ChangeFreq = "daily" },
            new { Url = "/categories", Priority = "0.8", ChangeFreq = "weekly" },
            new { Url = "/search", Priority = "0.7", ChangeFreq = "weekly" }
        };

        foreach (var page in staticPages)
        {
            urlset.Add(new XElement(ns + "url",
                new XElement(ns + "loc", $"{baseUrl}{page.Url}"),
                new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                new XElement(ns + "changefreq", page.ChangeFreq),
                new XElement(ns + "priority", page.Priority)
            ));
        }
    }
}
```

### Register Endpoint in Program.cs

```csharp
// src/InsightLearn.Application/Program.cs

app.MapGet("/sitemap.xml", async (ICourseRepository courseRepo, ICategoryRepository catRepo) =>
{
    var controller = new SitemapController(courseRepo, catRepo);
    var result = await controller.GetSitemap();
    return Results.Content(
        ((ContentResult)result).Content,
        "application/xml",
        Encoding.UTF8
    );
});
```

---

## SEO-Friendly Routing

### URL Slug Generation

```csharp
// src/InsightLearn.Core/Utilities/SlugGenerator.cs

public static class SlugGenerator
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Remove special characters
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\s-]", "");

        // Replace spaces with hyphens
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "-").Trim('-');

        // Remove consecutive hyphens
        text = System.Text.RegularExpressions.Regex.Replace(text, @"-+", "-");

        return text;
    }
}
```

### Implement SEO-Friendly URLs

**Before**: `/courses/123e4567-e89b-12d3-a456-426614174000`
**After**: `/courses/master-react-typescript-123e4567`

```csharp
// Update Course entity to include Slug
public class Course
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // NEW
    // ...
}

// Generate slug on course creation
public async Task<Course> CreateCourseAsync(CreateCourseDto dto)
{
    var slug = SlugGenerator.GenerateSlug(dto.Title);

    // Ensure uniqueness
    var existingSlug = await _context.Courses
        .FirstOrDefaultAsync(c => c.Slug == slug);

    if (existingSlug != null)
    {
        slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 8)}";
    }

    var course = new Course
    {
        Title = dto.Title,
        Slug = slug,
        // ...
    };

    await _context.Courses.AddAsync(course);
    await _context.SaveChangesAsync();

    return course;
}
```

### Update Routing

```razor
@* Before *@
@page "/courses/{CourseId:guid}"

@* After - SEO-friendly *@
@page "/courses/{CourseSlug}"

@code {
    [Parameter] public string CourseSlug { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        // Fetch course by slug instead of GUID
        course = await CourseService.GetCourseBySlugAsync(CourseSlug);
    }
}
```

---

## Monitoring & Analytics

### Google Search Console Integration

```html
<!-- Add to index.html <head> -->
<meta name="google-site-verification" content="YOUR_VERIFICATION_CODE_HERE" />
```

### Google Analytics 4 Integration

```html
<!-- Add to index.html before closing </head> -->
<script async src="https://www.googletagmanager.com/gtag/js?id=G-XXXXXXXXXX"></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', 'G-XXXXXXXXXX');
</script>
```

---

## Testing SEO Implementation

### 1. Rich Results Test
```bash
# Test structured data
https://search.google.com/test/rich-results
# Enter: https://www.insightlearn.cloud/courses/your-course-slug
```

### 2. Mobile-Friendly Test
```bash
https://search.google.com/test/mobile-friendly
```

### 3. PageSpeed Insights
```bash
https://pagespeed.web.dev/
# Enter: https://www.insightlearn.cloud
```

### 4. Schema Markup Validator
```bash
https://validator.schema.org/
# Paste your Course JSON-LD
```

---

## Deployment Checklist

### Pre-Deployment
- [ ] All pages have unique meta titles and descriptions
- [ ] Structured data implemented for courses
- [ ] robots.txt uploaded to wwwroot
- [ ] sitemap.xml generated and accessible
- [ ] Canonical URLs set on all pages
- [ ] Pre-rendering configured for public pages

### Post-Deployment
- [ ] Submit sitemap to Google Search Console
- [ ] Submit sitemap to Bing Webmaster Tools
- [ ] Verify Google Analytics tracking
- [ ] Test all structured data with Rich Results Test
- [ ] Monitor Core Web Vitals in PageSpeed Insights
- [ ] Set up rank tracking for target keywords

---

## Troubleshooting

### Issue: Meta tags not updating

**Solution**: Ensure `HeadOutlet` is properly configured and components use `<HeadContent>`.

### Issue: Structured data not recognized

**Solution**: Validate JSON-LD with Schema.org validator. Ensure proper escaping of special characters.

### Issue: Blazor pages not indexed

**Solution**: Implement pre-rendering or switch to Blazor Server for SEO-critical pages.

---

**Document Version**: 1.0
**Last Updated**: 2025-12-02
**Framework**: .NET 8 Blazor WebAssembly
