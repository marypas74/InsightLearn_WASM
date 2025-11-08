# Course Detail Page Specification
# `/courses/{id}` - Course Detail & Enrollment

**Priority:** CRITICAL (P0)
**Status:** Missing - Needs Implementation
**Route:** `/courses/{id}` or `/courses/{slug}`
**Page Type:** Public, High Conversion
**Estimated Effort:** 4-5 days

---

## Quick Reference

**Purpose:** Detailed course information and primary enrollment conversion page
**WOW Factor:** Video hero, sticky enrollment card, interactive curriculum, live counters
**Key Interactions:** Video play, curriculum expand, enroll/cart actions, review pagination
**API Endpoints:**
- `GET /api/courses/{id}` - Course details
- `GET /api/courses/{id}/reviews?page=1&limit=10` - Reviews
- `GET /api/courses/{id}/related` - Related courses
- `POST /api/cart/add` - Add to cart
- `GET /api/users/me/enrollments/{courseId}` - Check enrollment status

---

## Component Structure

```
Pages/CourseDetail.razor
├── Components/
│   ├── CourseVideoHero.razor (new)
│   ├── EnrollmentCard.razor (new)
│   ├── CourseCurriculum.razor (new)
│   ├── InstructorBio.razor (new)
│   ├── CourseReviews.razor (new)
│   └── RelatedCoursesCarousel.razor (new)
```

---

## Data Models

```csharp
public class CourseDetailDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string ShortDescription { get; set; }
    public string LongDescription { get; set; }
    public string VideoUrl { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Category { get; set; }
    public string Subcategory { get; set; }
    public string Level { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int? DiscountPercentage { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public int StudentCount { get; set; }
    public double TotalDuration { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? PromotionEndsAt { get; set; }

    public List<string> WhatYouWillLearn { get; set; }
    public List<string> Requirements { get; set; }
    public List<CourseSectionDto> Curriculum { get; set; }

    public InstructorDto Instructor { get; set; }

    public CourseIncludesDto Includes { get; set; }
}

public class CourseSectionDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public int LessonCount { get; set; }
    public double TotalDuration { get; set; }
    public List<LessonDto> Lessons { get; set; }
}

public class LessonDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public double Duration { get; set; }
    public bool IsFreePreview { get; set; }
    public string Type { get; set; } // video, article, quiz
}

public class InstructorDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }
    public string AvatarUrl { get; set; }
    public string Bio { get; set; }
    public double Rating { get; set; }
    public int TotalStudents { get; set; }
    public int CourseCount { get; set; }
    public List<SocialLinkDto> SocialLinks { get; set; }
}

public class CourseIncludesDto
{
    public bool LifetimeAccess { get; set; }
    public bool MobileAccess { get; set; }
    public bool Certificate { get; set; }
    public int DownloadableResources { get; set; }
    public int CodingExercises { get; set; }
    public bool ClosedCaptions { get; set; }
}

public class CourseReviewDto
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string UserAvatar { get; set; }
    public double Rating { get; set; }
    public string ReviewText { get; set; }
    public DateTime CreatedAt { get; set; }
    public int HelpfulCount { get; set; }
    public bool UserFoundHelpful { get; set; }
}
```

---

## Layout Implementation

### Full Page Structure

```razor
@page "/courses/{idOrSlug}"
@using InsightLearn.WebAssembly.Services
@inject ICourseService CourseService
@inject ICartService CartService
@inject NavigationManager Navigation
@inject IJSRuntime JS

<PageTitle>@(course?.Title ?? "Course") - InsightLearn</PageTitle>

@if (isLoading)
{
    <CourseDetailSkeleton />
}
else if (course == null)
{
    <div class="error-state">
        <h2>Course not found</h2>
        <a href="/courses" class="btn btn-primary">Browse Courses</a>
    </div>
}
else
{
    <!-- Breadcrumb -->
    <nav class="breadcrumb" aria-label="Breadcrumb">
        <a href="/">Home</a>
        <span class="separator">/</span>
        <a href="/categories">Categories</a>
        <span class="separator">/</span>
        <a href="/categories/@course.Category.ToLower()">@course.Category</a>
        <span class="separator">/</span>
        <span class="current">@course.Title</span>
    </nav>

    <!-- Video Hero -->
    <section class="course-hero" style="background-image: url('@course.ThumbnailUrl');">
        <div class="hero-overlay"></div>
        <div class="hero-content">
            @if (!videoPlaying)
            {
                <button class="play-button" @onclick="PlayVideo" aria-label="Play course preview">
                    <i class="fas fa-play"></i>
                </button>
            }
            else
            {
                <video controls autoplay class="course-video">
                    <source src="@course.VideoUrl" type="video/mp4" />
                    <track kind="captions" src="@course.CaptionsUrl" srclang="en" label="English" />
                    Your browser does not support the video tag.
                </video>
            }

            <div class="hero-info">
                <h1>@course.Title</h1>
                <p class="subtitle">@course.ShortDescription</p>
                <div class="meta-info">
                    <span class="instructor">
                        <img src="@course.Instructor.AvatarUrl" alt="@course.Instructor.Name" />
                        @course.Instructor.Name
                    </span>
                    <span class="rating">
                        <span class="stars">
                            @for (int i = 1; i <= 5; i++)
                            {
                                <i class="fas fa-star @(i <= course.Rating ? "filled" : "")"></i>
                            }
                        </span>
                        @course.Rating.ToString("0.0")
                        <span class="review-count">(@course.ReviewCount.ToString("N0") reviews)</span>
                    </span>
                    <span class="students">
                        <i class="fas fa-user-graduate"></i>
                        @course.StudentCount.ToString("N0") students
                    </span>
                    <span class="updated">
                        <i class="fas fa-sync-alt"></i>
                        Last updated @course.LastUpdated.ToString("MMMM yyyy")
                    </span>
                </div>
            </div>
        </div>
    </section>

    <!-- Main Content -->
    <div class="course-detail-container">
        <div class="course-detail-layout">
            <!-- Main Content (70%) -->
            <main class="course-main-content">
                <!-- What You'll Learn -->
                <section class="section what-you-learn" id="whatYouLearn">
                    <h2>What you'll learn</h2>
                    <div class="learning-outcomes">
                        @foreach (var outcome in course.WhatYouWillLearn)
                        {
                            <div class="outcome-item">
                                <i class="fas fa-check-circle"></i>
                                <span>@outcome</span>
                            </div>
                        }
                    </div>
                </section>

                <!-- Course Content / Curriculum -->
                <section class="section course-content" id="courseContent">
                    <h2>Course content</h2>
                    <div class="content-summary">
                        @course.Curriculum.Count sections •
                        @course.Curriculum.Sum(s => s.LessonCount) lectures •
                        @FormatDuration(course.TotalDuration) total length
                    </div>

                    <CourseCurriculum Sections="@course.Curriculum"
                                      OnPreviewClick="HandlePreviewClick" />
                </section>

                <!-- Requirements -->
                <section class="section requirements" id="requirements">
                    <h2>Requirements</h2>
                    <ul class="requirements-list">
                        @foreach (var requirement in course.Requirements)
                        {
                            <li>@requirement</li>
                        }
                    </ul>
                </section>

                <!-- Description -->
                <section class="section description" id="description">
                    <h2>Description</h2>
                    <div class="description-content @(descriptionExpanded ? "expanded" : "collapsed")">
                        @((MarkupString)course.LongDescription)
                    </div>
                    @if (course.LongDescription.Length > 500)
                    {
                        <button class="btn-ghost" @onclick="ToggleDescription">
                            @(descriptionExpanded ? "Show less" : "Read more")
                            <i class="fas fa-chevron-@(descriptionExpanded ? "up" : "down")"></i>
                        </button>
                    }
                </section>

                <!-- Instructor -->
                <section class="section instructor-section" id="instructor">
                    <h2>Instructor</h2>
                    <InstructorBio Instructor="@course.Instructor" />
                </section>

                <!-- Reviews -->
                <section class="section reviews-section" id="reviews">
                    <h2>Student reviews</h2>
                    <CourseReviews CourseId="@course.Id"
                                   AverageRating="@course.Rating"
                                   TotalReviews="@course.ReviewCount" />
                </section>
            </main>

            <!-- Sticky Sidebar (30%) -->
            <aside class="course-sidebar">
                <EnrollmentCard Course="@course"
                                IsEnrolled="@isEnrolled"
                                OnEnroll="HandleEnroll"
                                OnAddToCart="HandleAddToCart"
                                OnAddToWishlist="HandleAddToWishlist" />
            </aside>
        </div>
    </div>

    <!-- Related Courses -->
    <section class="section related-courses">
        <div class="container">
            <h2>Students also bought</h2>
            <RelatedCoursesCarousel CourseId="@course.Id" />
        </div>
    </section>
}

@code {
    [Parameter] public string IdOrSlug { get; set; } = string.Empty;

    private CourseDetailDto? course;
    private bool isLoading = true;
    private bool isEnrolled = false;
    private bool videoPlaying = false;
    private bool descriptionExpanded = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadCourseAsync();
    }

    private async Task LoadCourseAsync()
    {
        try
        {
            isLoading = true;
            course = await CourseService.GetCourseDetailAsync(IdOrSlug);

            // Check if user is enrolled
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                isEnrolled = await CourseService.IsEnrolledAsync(course.Id);
            }
        }
        catch (Exception ex)
        {
            // Log error and show error state
            Console.Error.WriteLine($"Error loading course: {ex.Message}");
            course = null;
        }
        finally
        {
            isLoading = false;
        }
    }

    private void PlayVideo()
    {
        videoPlaying = true;
    }

    private void ToggleDescription()
    {
        descriptionExpanded = !descriptionExpanded;
    }

    private async Task HandleEnroll()
    {
        if (course == null) return;

        if (course.Price == 0)
        {
            // Free course - enroll directly
            await CourseService.EnrollAsync(course.Id);
            Navigation.NavigateTo($"/my-courses");
        }
        else
        {
            // Paid course - go to checkout
            Navigation.NavigateTo($"/checkout?courseId={course.Id}");
        }
    }

    private async Task HandleAddToCart()
    {
        if (course == null) return;

        await CartService.AddToCartAsync(course.Id);

        // Show success toast
        await JS.InvokeVoidAsync("showToast", "Course added to cart", "success");
    }

    private async Task HandleAddToWishlist()
    {
        if (course == null) return;

        await CourseService.AddToWishlistAsync(course.Id);

        // Show success toast with animation
        await JS.InvokeVoidAsync("showToast", "Added to wishlist", "success");
    }

    private void HandlePreviewClick(string lessonId)
    {
        // Open lesson preview modal
        Navigation.NavigateTo($"/courses/{course?.Id}/preview/{lessonId}");
    }

    private string FormatDuration(double hours)
    {
        if (hours < 1)
            return $"{(int)(hours * 60)}m";
        else if (hours < 10)
            return $"{hours:0.0}h";
        else
            return $"{(int)hours}h";
    }
}
```

---

## EnrollmentCard Component

```razor
<!-- Components/EnrollmentCard.razor -->
<div class="enrollment-card" id="enrollmentCard">
    <div class="card-header">
        <div class="price-section">
            @if (Course.OriginalPrice.HasValue)
            {
                <div class="current-price">$@Course.Price.ToString("0.00")</div>
                <div class="original-price">$@Course.OriginalPrice.Value.ToString("0.00")</div>
                <div class="discount-badge">@Course.DiscountPercentage% off</div>
            }
            else
            {
                <div class="current-price">$@Course.Price.ToString("0.00")</div>
            }
        </div>

        @if (Course.PromotionEndsAt.HasValue)
        {
            <div class="urgency-banner">
                <i class="fas fa-clock"></i>
                <span id="countdown">@GetCountdownText()</span> left at this price
            </div>
        }
    </div>

    <div class="card-actions">
        @if (IsEnrolled)
        {
            <a href="/my-courses" class="btn btn-primary btn-lg">
                <i class="fas fa-play-circle"></i>
                Go to Course
            </a>
        }
        else if (Course.Price == 0)
        {
            <button @onclick="OnEnroll" class="btn btn-primary btn-lg">
                <i class="fas fa-gift"></i>
                Enroll for Free
            </button>
        }
        else
        {
            <button @onclick="OnEnroll" class="btn btn-primary btn-lg">
                Buy Now
            </button>
            <button @onclick="OnAddToCart" class="btn btn-secondary btn-lg">
                <i class="fas fa-shopping-cart"></i>
                Add to Cart
            </button>
        }

        <button @onclick="OnAddToWishlist" class="btn-ghost">
            <i class="@(isInWishlist ? "fas" : "far") fa-heart"></i>
            @(isInWishlist ? "Saved" : "Add to Wishlist")
        </button>
    </div>

    <div class="card-includes">
        <h4>This course includes:</h4>
        <ul>
            @if (Course.Includes.LifetimeAccess)
            {
                <li>
                    <i class="fas fa-infinity"></i>
                    Full lifetime access
                </li>
            }
            <li>
                <i class="fas fa-clock"></i>
                @Course.TotalDuration.ToString("0.0") hours on-demand video
            </li>
            @if (Course.Includes.DownloadableResources > 0)
            {
                <li>
                    <i class="fas fa-download"></i>
                    @Course.Includes.DownloadableResources downloadable resources
                </li>
            }
            @if (Course.Includes.MobileAccess)
            {
                <li>
                    <i class="fas fa-mobile-alt"></i>
                    Access on mobile and TV
                </li>
            }
            @if (Course.Includes.Certificate)
            {
                <li>
                    <i class="fas fa-certificate"></i>
                    Certificate of completion
                </li>
            }
        </ul>
    </div>

    <div class="card-guarantee">
        <i class="fas fa-shield-alt"></i>
        30-Day Money-Back Guarantee
    </div>
</div>

@code {
    [Parameter] public CourseDetailDto Course { get; set; } = null!;
    [Parameter] public bool IsEnrolled { get; set; }
    [Parameter] public EventCallback OnEnroll { get; set; }
    [Parameter] public EventCallback OnAddToCart { get; set; }
    [Parameter] public EventCallback OnAddToWishlist { get; set; }

    private bool isInWishlist = false;

    private string GetCountdownText()
    {
        if (!Course.PromotionEndsAt.HasValue) return "";

        var timeLeft = Course.PromotionEndsAt.Value - DateTime.UtcNow;

        if (timeLeft.TotalDays >= 1)
            return $"{(int)timeLeft.TotalDays} days";
        else if (timeLeft.TotalHours >= 1)
            return $"{(int)timeLeft.TotalHours} hours";
        else
            return $"{(int)timeLeft.TotalMinutes} minutes";
    }
}
```

---

## CSS Specifications

```css
/* Course Hero */
.course-hero {
    position: relative;
    height: 500px;
    background-size: cover;
    background-position: center;
    display: flex;
    align-items: flex-end;
    overflow: hidden;
}

.hero-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(
        180deg,
        rgba(0, 0, 0, 0.3) 0%,
        rgba(0, 0, 0, 0.7) 100%
    );
    z-index: 1;
}

.hero-content {
    position: relative;
    z-index: 2;
    width: 100%;
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 var(--space-6) var(--space-8);
}

.play-button {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    width: 120px;
    height: 120px;
    border-radius: 50%;
    background: var(--primary-red);
    color: white;
    border: none;
    font-size: var(--text-4xl);
    cursor: pointer;
    transition: all var(--transition-normal);
    box-shadow: 0 8px 24px rgba(220, 38, 38, 0.4);
}

.play-button:hover {
    transform: translate(-50%, -50%) scale(1.1);
    box-shadow: 0 12px 32px rgba(220, 38, 38, 0.6);
}

.course-video {
    width: 100%;
    max-width: 900px;
    margin: 0 auto;
    border-radius: var(--radius-xl);
    box-shadow: var(--shadow-2xl);
}

.hero-info h1 {
    color: white;
    font-size: var(--text-5xl);
    margin-bottom: var(--space-3);
    text-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
}

.hero-info .subtitle {
    color: white;
    font-size: var(--text-xl);
    margin-bottom: var(--space-4);
    text-shadow: 0 1px 4px rgba(0, 0, 0, 0.3);
}

.meta-info {
    display: flex;
    gap: var(--space-6);
    align-items: center;
    flex-wrap: wrap;
    color: white;
    font-size: var(--text-sm);
}

.meta-info .instructor {
    display: flex;
    align-items: center;
    gap: var(--space-2);
}

.meta-info .instructor img {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    border: 2px solid white;
}

.meta-info .stars .filled {
    color: var(--color-secondary);
}

/* Main Content Layout */
.course-detail-container {
    background: var(--bg-subtle);
}

.course-detail-layout {
    max-width: 1400px;
    margin: 0 auto;
    padding: var(--space-8) var(--space-6);
    display: grid;
    grid-template-columns: 1fr 380px;
    gap: var(--space-8);
    align-items: start;
}

/* Sections */
.section {
    background: white;
    border-radius: var(--radius-xl);
    padding: var(--space-8);
    margin-bottom: var(--space-6);
    box-shadow: var(--shadow-sm);
}

.section h2 {
    font-size: var(--text-3xl);
    font-weight: var(--font-bold);
    margin-bottom: var(--space-6);
    color: var(--gray-900);
}

/* What You'll Learn */
.learning-outcomes {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: var(--space-4);
}

.outcome-item {
    display: flex;
    gap: var(--space-3);
    align-items: flex-start;
}

.outcome-item i {
    color: var(--success);
    font-size: var(--text-lg);
    margin-top: 2px;
}

.outcome-item span {
    flex: 1;
    color: var(--gray-700);
    line-height: var(--leading-relaxed);
}

/* Enrollment Card */
.enrollment-card {
    background: white;
    border: 2px solid var(--gray-200);
    border-radius: var(--radius-xl);
    padding: var(--space-6);
    box-shadow: var(--shadow-lg);
    position: sticky;
    top: calc(var(--header-height) + var(--space-4));
}

.enrollment-card .card-header {
    margin-bottom: var(--space-6);
}

.price-section {
    display: flex;
    align-items: baseline;
    gap: var(--space-3);
    margin-bottom: var(--space-3);
}

.current-price {
    font-size: var(--text-4xl);
    font-weight: var(--font-bold);
    color: var(--gray-900);
}

.original-price {
    font-size: var(--text-xl);
    color: var(--gray-400);
    text-decoration: line-through;
}

.discount-badge {
    background: var(--primary-red-light);
    color: var(--primary-red-hover);
    font-size: var(--text-sm);
    font-weight: var(--font-bold);
    padding: var(--space-1) var(--space-3);
    border-radius: var(--radius-md);
}

.urgency-banner {
    display: flex;
    align-items: center;
    gap: var(--space-2);
    padding: var(--space-3);
    background: rgba(245, 158, 11, 0.1);
    border: 1px solid rgba(245, 158, 11, 0.3);
    border-radius: var(--radius-md);
    color: var(--warning);
    font-size: var(--text-sm);
    font-weight: var(--font-semibold);
}

.card-actions {
    display: flex;
    flex-direction: column;
    gap: var(--space-3);
    margin-bottom: var(--space-6);
}

.card-includes {
    border-top: 1px solid var(--gray-200);
    padding-top: var(--space-6);
    margin-bottom: var(--space-6);
}

.card-includes h4 {
    font-size: var(--text-base);
    font-weight: var(--font-semibold);
    margin-bottom: var(--space-4);
}

.card-includes ul {
    list-style: none;
    padding: 0;
    margin: 0;
}

.card-includes li {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    padding: var(--space-2) 0;
    color: var(--gray-700);
    font-size: var(--text-sm);
}

.card-includes i {
    color: var(--gray-500);
    width: 20px;
}

.card-guarantee {
    display: flex;
    align-items: center;
    gap: var(--space-2);
    padding: var(--space-3);
    background: var(--gray-50);
    border-radius: var(--radius-md);
    color: var(--gray-700);
    font-size: var(--text-sm);
    font-weight: var(--font-medium);
}

/* Responsive */
@media (max-width: 1024px) {
    .course-detail-layout {
        grid-template-columns: 1fr;
    }

    .enrollment-card {
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        border-radius: 0;
        border-left: none;
        border-right: none;
        border-bottom: none;
        z-index: 9998;
    }

    .learning-outcomes {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 768px) {
    .course-hero {
        height: 300px;
    }

    .hero-info h1 {
        font-size: var(--text-3xl);
    }

    .meta-info {
        font-size: var(--text-xs);
    }

    .play-button {
        width: 80px;
        height: 80px;
        font-size: var(--text-2xl);
    }
}
```

---

## Performance Optimizations

### Video Loading Strategy

```javascript
// wwwroot/js/course-video.js
window.courseVideo = {
    lazyLoadVideo: function(videoElement) {
        if ('IntersectionObserver' in window) {
            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const video = entry.target;
                        video.src = video.dataset.src;
                        video.load();
                        observer.unobserve(video);
                    }
                });
            });

            observer.observe(videoElement);
        } else {
            // Fallback for browsers without IntersectionObserver
            videoElement.src = videoElement.dataset.src;
            videoElement.load();
        }
    }
};
```

### Lazy Load Reviews

```csharp
// Only load reviews when scrolled into view
private bool reviewsLoaded = false;

private async Task OnReviewsSectionVisible()
{
    if (!reviewsLoaded)
    {
        await LoadReviewsAsync();
        reviewsLoaded = true;
    }
}
```

---

## Accessibility

### Video Accessibility

```html
<video controls
       aria-label="@course.Title course preview"
       preload="metadata">
    <source src="@course.VideoUrl" type="video/mp4">
    <track kind="captions"
           src="@course.CaptionsUrl"
           srclang="en"
           label="English"
           default>
    <track kind="descriptions"
           src="@course.DescriptionsUrl"
           srclang="en"
           label="Audio Descriptions">
</video>
```

### Keyboard Navigation

- Tab: Navigate through sections
- Enter/Space: Expand curriculum sections
- Arrow keys: Navigate curriculum items
- Escape: Close video player (if modal)

---

## Testing Checklist

### Functional
- [ ] Course details load correctly
- [ ] Video plays/pauses
- [ ] Curriculum expands/collapses
- [ ] Enroll button works
- [ ] Add to cart works
- [ ] Wishlist toggle works
- [ ] Reviews load and paginate
- [ ] Related courses display
- [ ] Enrollment status checked correctly

### Responsive
- [ ] Desktop: Full layout with sidebar
- [ ] Tablet: Sticky bottom enrollment bar
- [ ] Mobile: Single column, video adjusts

### Performance
- [ ] Lighthouse score >90
- [ ] Video lazy loads
- [ ] Images optimized
- [ ] No layout shifts

### Accessibility
- [ ] Video has captions
- [ ] Keyboard navigation works
- [ ] Screen reader announces correctly
- [ ] Focus indicators visible
- [ ] ARIA labels present

---

**Ready to Implement:** Yes
**Blockers:** Course detail API endpoint
**Dependencies:** CourseCard component, video player
