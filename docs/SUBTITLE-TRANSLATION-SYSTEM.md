# Real-time Subtitle Translation System

**Version**: v2.2.0-dev
**Status**: ✅ Backend Complete, Frontend Pending
**Last Updated**: 2025-12-15

## Overview

The subtitle translation system provides real-time, AI-powered translation of video subtitles using the existing Ollama LLM (qwen2:0.5b). Translations are cached in MongoDB for performance and reuse.

## Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Video Player (Blazor)                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Subtitle Menu: [Original] [Auto-translate ▼]       │   │
│  │    • English                                         │   │
│  │    • Italiano                                        │   │
│  │    • Español                                         │   │
│  │    • ... (20 languages)                              │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                             │
                             │ GET /api/subtitles/{lessonId}/translate/{lang}
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                   Translation Service                       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  1. Check MongoDB cache                              │   │
│  │  2. If not found, get source subtitles from SQL     │   │
│  │  3. Download & parse WebVTT file                    │   │
│  │  4. Translate with context-aware Ollama calls       │   │
│  │  5. Cache result in MongoDB                         │   │
│  │  6. Return translated WebVTT                        │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
         │                     │                     │
         ▼                     ▼                     ▼
   ┌─────────┐          ┌──────────┐        ┌────────────┐
   │ MongoDB │          │  Ollama  │        │ SQL Server │
   │ (Cache) │          │ qwen2:0.5b│        │ (Metadata) │
   └─────────┘          └──────────┘        └────────────┘
```

## Backend Implementation

### Files Created

1. **Service Interface**: `/src/InsightLearn.Application/Interfaces/ISubtitleTranslationService.cs`
   - `TranslateSubtitlesAsync()` - Translate entire WebVTT file
   - `GetOrCreateTranslatedSubtitlesAsync()` - Get from cache or translate
   - `GetSupportedLanguages()` - Get available languages (20 languages)
   - `TranslationExistsAsync()` - Check cache
   - `DeleteTranslationsForLessonAsync()` - Clear cache

2. **Service Implementation**: `/src/InsightLearn.Application/Services/SubtitleTranslationService.cs`
   - Context-aware translation (uses 3 previous cues for context)
   - WebVTT parsing & generation
   - MongoDB caching with upsert
   - Ollama integration for translation
   - Error handling with fallback

3. **API Endpoints**: `/src/InsightLearn.Application/Program.cs` (lines 2247-2414)
   - `GET /api/subtitles/{lessonId}/translate/{targetLanguage}` - Translate & return WebVTT
   - `GET /api/subtitles/translate/languages` - List supported languages
   - `GET /api/subtitles/{lessonId}/translate/{targetLanguage}/exists` - Check cache
   - `DELETE /api/subtitles/{lessonId}/translate` - Clear cached translations (admin/instructor)

### Supported Languages (20 total)

| Code | Language | Native Name |
|------|----------|-------------|
| `it` | Italian | Italiano |
| `en` | English | English |
| `es` | Spanish | Español |
| `fr` | French | Français |
| `de` | German | Deutsch |
| `pt` | Portuguese | Português |
| `ru` | Russian | Русский |
| `zh` | Chinese | 中文 |
| `ja` | Japanese | 日本語 |
| `ko` | Korean | 한국어 |
| `ar` | Arabic | العربية |
| `hi` | Hindi | हिन्दी |
| `nl` | Dutch | Nederlands |
| `pl` | Polish | Polski |
| `tr` | Turkish | Türkçe |
| `vi` | Vietnamese | Tiếng Việt |
| `th` | Thai | ไทย |
| `sv` | Swedish | Svenska |
| `no` | Norwegian | Norsk |
| `da` | Danish | Dansk |

## MongoDB Schema

### Collection: `TranslatedSubtitles`

```javascript
{
  "_id": ObjectId("..."),
  "lessonId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890", // UUID as string
  "targetLanguage": "es",                             // ISO 639-1 code
  "translatedVtt": "WEBVTT\n\n1\n00:00:01.000 --> 00:00:04.500\nHola, bienvenido...",
  "createdAt": ISODate("2025-12-15T10:30:00Z"),
  "cueCount": 42,                                     // Number of subtitle cues
  "fileSize": 5243                                    // Bytes
}
```

### Indexes (recommended)

```javascript
// Compound index for fast lookups
db.TranslatedSubtitles.createIndex({ "lessonId": 1, "targetLanguage": 1 }, { unique: true })

// TTL index to auto-delete old translations after 30 days (optional)
db.TranslatedSubtitles.createIndex({ "createdAt": 1 }, { expireAfterSeconds: 2592000 })
```

## Translation Algorithm

### Context-Aware Translation

The service uses a **sliding context window** approach:

1. **Parse WebVTT**: Extract all subtitle cues (timestamp + text)
2. **For each cue**:
   - Build context: Include previous 3 cues as reference
   - Construct prompt:
     ```
     You are a professional subtitle translator. Translate from English to Español.

     CONTEXT (previous subtitles):
     [Previous]: Hello, welcome to this course.
     [Previous]: In this lesson, we will learn...
     [Previous]: Let's start with the basics.

     TEXT TO TRANSLATE:
     The first step is to understand the fundamentals.

     Provide ONLY the Español translation:
     ```
   - Call Ollama API for translation
   - Clean response (remove markdown, extra text)
   - Store translated cue
   - Delay 100ms (rate limiting)
3. **Rebuild WebVTT**: Combine translated cues into WebVTT format
4. **Cache in MongoDB**: Upsert with lesson ID + target language

### Performance Characteristics

- **First translation**: ~10-20 seconds for 50 cues (Ollama processing)
- **Cached translation**: < 500ms (MongoDB read)
- **Memory**: ~5KB per translated subtitle file
- **Ollama load**: 1 request per cue (rate-limited to 10 req/sec)

## API Examples

### 1. Translate Subtitles

```bash
# Request
GET /api/subtitles/a1b2c3d4-e5f6-7890-abcd-ef1234567890/translate/es

# Response (200 OK, Content-Type: text/vtt; charset=utf-8)
WEBVTT

1
00:00:01.000 --> 00:00:04.500
Hola, bienvenido a este curso sobre desarrollo web.

2
00:00:05.000 --> 00:00:08.500
En esta lección, aprenderemos los fundamentos de HTML.

...
```

### 2. Get Supported Languages

```bash
# Request
GET /api/subtitles/translate/languages

# Response (200 OK)
{
  "count": 20,
  "languages": [
    { "code": "it", "name": "Italiano" },
    { "code": "en", "name": "English" },
    { "code": "es", "name": "Español" },
    ...
  ]
}
```

### 3. Check Cache Status

```bash
# Request
GET /api/subtitles/a1b2c3d4-e5f6-7890-abcd-ef1234567890/translate/fr/exists

# Response (200 OK)
{
  "lessonId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "targetLanguage": "fr",
  "exists": true,
  "cacheHit": true
}
```

### 4. Clear Cached Translations (Admin/Instructor)

```bash
# Request
DELETE /api/subtitles/a1b2c3d4-e5f6-7890-abcd-ef1234567890/translate?userId=admin-user-guid

# Response (200 OK)
{
  "message": "Cached translations deleted successfully"
}
```

## Frontend Integration (To Be Implemented)

### VideoPlayer.razor Updates

#### 1. Add Auto-Translate Button in Subtitle Menu

```razor
<!-- Existing subtitle tracks (uploaded) -->
@foreach (var track in SubtitleTracks)
{
    <button class="subtitle-option" @onclick="() => SelectTrack(track.Language)">
        @track.Label
    </button>
}

<!-- NEW: Auto-translate divider -->
<div class="subtitle-divider">Auto-translate</div>

<!-- NEW: Auto-translate language options -->
@foreach (var lang in TranslateLanguages)
{
    <button class="subtitle-option translate-option"
            @onclick="() => SelectTranslatedTrack(lang.Code)">
        <span class="translate-icon">🌐</span>
        @lang.Name
        @if (TranslationCache.ContainsKey(lang.Code))
        {
            <span class="cache-indicator">●</span>
        }
    </button>
}
```

#### 2. Add Translation Service Call

```csharp
// VideoPlayer.razor.cs

private Dictionary<string, string> TranslationCache = new();
private List<(string Code, string Name)> TranslateLanguages = new();
private bool IsTranslating = false;

protected override async Task OnInitializedAsync()
{
    // Load supported languages from API
    var response = await Http.GetFromJsonAsync<LanguagesResponse>(
        "/api/subtitles/translate/languages");

    TranslateLanguages = response.Languages
        .Select(l => (l.Code, l.Name))
        .ToList();
}

private async Task SelectTranslatedTrack(string languageCode)
{
    // Check if already cached in memory
    if (TranslationCache.TryGetValue(languageCode, out var cachedVtt))
    {
        ApplySubtitleTrack(cachedVtt, languageCode);
        return;
    }

    // Show loading indicator
    IsTranslating = true;
    StateHasChanged();

    try
    {
        // Fetch translated subtitles from API
        var vttContent = await Http.GetStringAsync(
            $"/api/subtitles/{LessonId}/translate/{languageCode}");

        // Cache in memory
        TranslationCache[languageCode] = vttContent;

        // Apply to video player
        ApplySubtitleTrack(vttContent, languageCode);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Translation error: {ex.Message}");
        // Show error toast to user
    }
    finally
    {
        IsTranslating = false;
        StateHasChanged();
    }
}

private void ApplySubtitleTrack(string vttContent, string language)
{
    // Create blob URL from VTT content
    var blobUrl = CreateBlobUrl(vttContent, "text/vtt");

    // Call JavaScript to add/switch track
    JSRuntime.InvokeVoidAsync("videoPlayer.addSubtitleTrack",
        VideoElementId, blobUrl, language);
}
```

#### 3. Add CSS Styles

```css
/* css/learning-space.css */

.subtitle-divider {
    padding: 8px 16px;
    font-size: 12px;
    color: var(--text-muted);
    background: var(--bg-secondary);
    border-top: 1px solid var(--border-color);
    font-weight: 600;
}

.translate-option {
    display: flex;
    align-items: center;
    gap: 8px;
}

.translate-icon {
    font-size: 16px;
}

.cache-indicator {
    margin-left: auto;
    color: var(--success-color);
    font-size: 8px;
}

.subtitle-loading {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: rgba(0, 0, 0, 0.8);
    padding: 20px;
    border-radius: 8px;
    color: white;
}

.subtitle-loading-spinner {
    width: 40px;
    height: 40px;
    border: 3px solid rgba(255, 255, 255, 0.3);
    border-top-color: white;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

@keyframes spin {
    to { transform: rotate(360deg); }
}
```

## Error Handling

### Common Errors & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| **404 Not Found** | No source subtitles for lesson | Upload original subtitles first |
| **400 Bad Request** | Unsupported language code | Check `/api/subtitles/translate/languages` |
| **408 Request Timeout** | Translation takes > 30s | Reduce subtitle count or increase timeout |
| **500 Internal Error** | Ollama service down | Check Ollama pod status in Kubernetes |

### Fallback Strategy

```csharp
try
{
    var translated = await translationService.GetOrCreateTranslatedSubtitlesAsync(...);
}
catch (Exception ex)
{
    // Log error
    logger.LogError(ex, "Translation failed");

    // Fallback: Return original subtitles with error marker
    var originalVtt = await GetOriginalSubtitles(lessonId);
    return Results.Content(
        $"WEBVTT\n\nNOTE Translation error: {ex.Message}\n\n{originalVtt}",
        "text/vtt");
}
```

## Performance Optimization

### 1. Caching Strategy

- **Memory Cache**: Store last 10 translations in-memory (VideoPlayer component)
- **MongoDB Cache**: Persistent cache with 30-day TTL
- **Pre-translation**: Background job to pre-translate popular lessons

### 2. Batch Translation

For lessons with many subtitles (> 100 cues), consider:
- Batch multiple cues in single Ollama request
- Parallel translation (5 concurrent requests)

### 3. Quality Improvements

- **Model Upgrade**: Switch to larger Ollama models (qwen2:1.5b or llama3:8b) for better quality
- **Post-processing**: Add spell-check and grammar correction
- **Human Review**: Allow instructors to edit auto-translations

## Security Considerations

### Authorization

- **Read**: Any authenticated user can request translations
- **Delete Cache**: Only course instructor or admin can clear translations
- **Rate Limiting**: 10 translation requests per minute per user (to prevent Ollama abuse)

### Input Validation

- Language code: Must be in supported list (20 languages)
- Lesson ID: Must be valid GUID
- VTT content: Max 100KB per file

## Future Enhancements

1. **Streaming Translation**: Use Server-Sent Events (SSE) to stream translations as they're generated
2. **Translation Quality Feedback**: Allow users to rate translation quality
3. **Custom Glossaries**: Allow instructors to define technical term translations
4. **Multi-model Support**: Allow selection between different LLM models (fast vs. quality)
5. **WebVTT Styling**: Preserve color, position, and formatting from source subtitles

## Testing

### Manual Testing

```bash
# 1. Upload source subtitles (English)
curl -X POST http://localhost:31081/api/subtitles/upload \
  -F "lessonId=LESSON_GUID" \
  -F "userId=USER_GUID" \
  -F "language=en" \
  -F "label=English" \
  -F "subtitle=@english.vtt"

# 2. Translate to Spanish
curl http://localhost:31081/api/subtitles/LESSON_GUID/translate/es

# 3. Check cache
curl http://localhost:31081/api/subtitles/LESSON_GUID/translate/es/exists

# 4. Verify Ollama logs
kubectl logs -n insightlearn ollama-0 -f | grep "generate"
```

### Unit Tests (To Be Implemented)

```csharp
[Fact]
public async Task TranslateSubtitles_WithValidInput_ReturnsTranslatedVtt()
{
    // Arrange
    var service = new SubtitleTranslationService(...);
    var sourceVtt = "WEBVTT\n\n1\n00:00:01.000 --> 00:00:04.500\nHello world";

    // Act
    var result = await service.TranslateSubtitlesAsync(
        sourceVtt, "en", "es", CancellationToken.None);

    // Assert
    Assert.Contains("Hola mundo", result);
    Assert.StartsWith("WEBVTT", result);
}
```

## Deployment

### Kubernetes ConfigMap

No additional configuration needed - uses existing Ollama service and MongoDB.

### Environment Variables

None required (uses existing `MONGODB_CONNECTION_STRING`).

### Monitoring

Add Prometheus metrics:
- `subtitle_translations_total{language}` - Total translations per language
- `subtitle_translation_duration_seconds{language}` - Translation time
- `subtitle_cache_hits_total` - Cache hit rate
- `subtitle_ollama_errors_total` - Ollama API errors

## Documentation References

- **Ollama API**: https://github.com/ollama/ollama/blob/main/docs/api.md
- **WebVTT Format**: https://developer.mozilla.org/en-US/docs/Web/API/WebVTT_API
- **MongoDB Upsert**: https://www.mongodb.com/docs/manual/reference/method/db.collection.replaceOne/
- **ISO 639-1 Language Codes**: https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes

---

**Implementation Status**:
- ✅ Backend Service (ISubtitleTranslationService + SubtitleTranslationService)
- ✅ API Endpoints (4 endpoints added to Program.cs)
- ✅ Service Registration (DI configured)
- ✅ Build Verification (0 errors, 45 warnings)
- ⏳ Frontend Integration (VideoPlayer.razor updates pending)
- ⏳ Testing (Manual + automated tests pending)

**Next Steps**:
1. Update `VideoPlayer.razor` with auto-translate UI
2. Test with real subtitle files (English → Spanish/Italian)
3. Add Prometheus metrics for monitoring
4. Create unit tests for translation service
5. Performance testing with large subtitle files (100+ cues)
