# Browse Courses Page Specification
# `/courses` - Main Course Catalog

**Priority:** CRITICAL (P0)
**Status:** Missing - Needs Implementation
**Route:** `/courses`
**Page Type:** Public, High Traffic
**Estimated Effort:** 3-4 days

---

## Quick Reference

**Purpose:** Primary course discovery and browsing page
**WOW Factor:** Masonry grid, live counters, smooth skeleton loading, filter sidebar
**Key Interactions:** Filter/sort, infinite scroll, quick preview on hover
**API Endpoint:** `GET /api/courses?page=1&limit=12&category=&level=&price=`

---

## Technical Implementation

### Component Structure

```
Pages/Courses.razor
├── Components/
│   ├── CourseCard.razor (reuse existing)
│   ├── CourseFilterSidebar.razor (new)
│   ├── CourseGrid.razor (new)
│   └── CourseSkeletonLoader.razor (new)
```

### Data Models

```csharp
public class CourseFilterOptions
{
    public List<string> Categories { get; set; }
    public List<string> Levels { get; set; }
    public PriceRange PriceRange { get; set; }
    public int MinRating { get; set; }
    public DurationRange Duration { get; set; }
}

public class CourseListResponse
{
    public List<CourseDto> Courses { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
```

### State Management

```csharp
@code {
    private List<CourseDto> courses = new();
    private CourseFilterOptions filters = new();
    private int currentPage = 1;
    private bool isLoading = true;
    private bool hasMore = true;
    private string sortBy = "popular";

    protected override async Task OnInitializedAsync()
    {
        await LoadCoursesAsync();
    }

    private async Task LoadCoursesAsync()
    {
        isLoading = true;
        var response = await CourseService.GetCoursesAsync(
            page: currentPage,
            filters: filters,
            sortBy: sortBy
        );

        if (currentPage == 1)
            courses = response.Courses;
        else
            courses.AddRange(response.Courses);

        hasMore = response.HasMore;
        isLoading = false;
    }

    private async Task ApplyFiltersAsync()
    {
        currentPage = 1;
        courses.Clear();
        await LoadCoursesAsync();
    }

    private async Task LoadMoreAsync()
    {
        if (hasMore && !isLoading)
        {
            currentPage++;
            await LoadCoursesAsync();
        }
    }
}
```

---

## Layout Specifications

### Hero Section (280px height)

```html
<section class="courses-hero">
    <div class="container">
        <h1 class="text-5xl font-bold gray-900">Explore 1,200+ Expert-Led Courses</h1>
        <p class="text-xl gray-600">Learn from industry professionals at your own pace</p>

        <!-- Large search bar -->
        <div class="search-bar-large">
            <input type="search"
                   placeholder="Search courses, topics, instructors..."
                   @bind="searchQuery"
                   @oninput="OnSearchInput" />
            <button class="btn btn-primary">
                <i class="fas fa-search"></i>
            </button>
        </div>

        <!-- Quick filter pills -->
        <div class="filter-pills">
            <button class="pill @(selectedCategory == "all" ? "active" : "")"
                    @onclick='() => SelectCategory("all")'>
                All Courses
            </button>
            <button class="pill @(selectedCategory == "development" ? "active" : "")"
                    @onclick='() => SelectCategory("development")'>
                💻 Development
            </button>
            <!-- More category pills... -->
        </div>
    </div>
</section>
```

### Main Content Layout

```html
<div class="courses-page-container">
    <div class="courses-layout">
        <!-- Sidebar (280px) -->
        <aside class="filter-sidebar" id="filterSidebar">
            <div class="sidebar-header">
                <h3>Filters</h3>
                <button @onclick="ClearFilters" class="btn-ghost">Clear All</button>
            </div>

            <!-- Filter sections -->
            <CourseFilterSidebar @bind-Filters="filters"
                                 OnFilterChanged="ApplyFiltersAsync" />
        </aside>

        <!-- Main content area -->
        <main class="courses-main">
            <!-- Results header -->
            <div class="results-header">
                <div class="results-count">
                    Showing @courses.Count of @totalCount courses
                </div>
                <div class="results-controls">
                    <select @bind="sortBy" @onchange="OnSortChanged">
                        <option value="popular">Most Popular</option>
                        <option value="rating">Highest Rated</option>
                        <option value="newest">Newest First</option>
                        <option value="price-low">Price: Low to High</option>
                        <option value="price-high">Price: High to Low</option>
                    </select>

                    <div class="view-toggle">
                        <button @onclick='() => viewMode = "grid"'
                                class="@(viewMode == "grid" ? "active" : "")">
                            <i class="fas fa-th"></i>
                        </button>
                        <button @onclick='() => viewMode = "list"'
                                class="@(viewMode == "list" ? "active" : "")">
                            <i class="fas fa-list"></i>
                        </button>
                    </div>
                </div>
            </div>

            <!-- Course grid -->
            @if (isLoading && courses.Count == 0)
            {
                <CourseSkeletonLoader Count="12" />
            }
            else if (courses.Count == 0)
            {
                <div class="empty-state">
                    <img src="/images/empty-courses.svg" alt="No courses found" />
                    <h3>No courses found</h3>
                    <p>Try adjusting your filters or search terms</p>
                    <button @onclick="ClearFilters" class="btn btn-primary">
                        Clear All Filters
                    </button>
                </div>
            }
            else
            {
                <div class="courses-grid @viewMode-view">
                    @foreach (var course in courses)
                    {
                        <CourseCard Course="@course" />
                    }
                </div>

                @if (hasMore)
                {
                    <div class="load-more-section">
                        <button @onclick="LoadMoreAsync"
                                class="btn btn-secondary btn-lg"
                                disabled="@isLoading">
                            @if (isLoading)
                            {
                                <span class="spinner-sm"></span>
                                <span>Loading...</span>
                            }
                            else
                            {
                                <span>Load More Courses</span>
                            }
                        </button>
                    </div>
                }
            }
        </main>
    </div>
</div>
```

---

## CSS Specifications

### Hero Section Styles

```css
.courses-hero {
    background: linear-gradient(180deg, var(--gray-50) 0%, white 100%);
    padding: var(--space-16) 0 var(--space-12);
    border-bottom: 1px solid var(--gray-200);
}

.courses-hero h1 {
    text-align: center;
    margin-bottom: var(--space-4);
}

.courses-hero p {
    text-align: center;
    margin-bottom: var(--space-8);
}

.search-bar-large {
    max-width: 600px;
    margin: 0 auto var(--space-6);
    display: flex;
    gap: var(--space-2);
}

.search-bar-large input {
    flex: 1;
    padding: var(--space-4) var(--space-6);
    font-size: var(--text-lg);
    border: 2px solid var(--gray-300);
    border-radius: var(--radius-lg);
    transition: border-color var(--transition-fast);
}

.search-bar-large input:focus {
    outline: none;
    border-color: var(--primary-red);
    box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.1);
}

.filter-pills {
    display: flex;
    justify-content: center;
    gap: var(--space-3);
    flex-wrap: wrap;
}

.filter-pills .pill {
    padding: var(--space-3) var(--space-5);
    border: 2px solid var(--gray-300);
    border-radius: var(--radius-full);
    background: white;
    font-size: var(--text-sm);
    font-weight: var(--font-semibold);
    cursor: pointer;
    transition: all var(--transition-fast);
}

.filter-pills .pill:hover {
    border-color: var(--gray-400);
    background: var(--gray-50);
}

.filter-pills .pill.active {
    border-color: var(--primary-red);
    background: var(--primary-red);
    color: white;
}
```

### Layout Styles

```css
.courses-page-container {
    background: var(--bg-subtle);
    min-height: calc(100vh - var(--header-height));
}

.courses-layout {
    max-width: 1400px;
    margin: 0 auto;
    padding: var(--space-8) var(--space-6);
    display: grid;
    grid-template-columns: 280px 1fr;
    gap: var(--space-8);
}

/* Filter Sidebar */
.filter-sidebar {
    background: white;
    border: 1px solid var(--gray-200);
    border-radius: var(--radius-lg);
    padding: var(--space-6);
    height: fit-content;
    position: sticky;
    top: calc(var(--header-height) + var(--space-4));
}

.sidebar-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: var(--space-6);
    padding-bottom: var(--space-4);
    border-bottom: 1px solid var(--gray-200);
}

/* Results Header */
.results-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: var(--space-6);
    padding: var(--space-4) 0;
}

.results-count {
    font-size: var(--text-base);
    color: var(--gray-600);
    font-weight: var(--font-medium);
}

.results-controls {
    display: flex;
    gap: var(--space-4);
    align-items: center;
}

.results-controls select {
    padding: var(--space-2) var(--space-4);
    border: 1px solid var(--gray-300);
    border-radius: var(--radius-md);
    font-size: var(--text-sm);
    background: white;
}

.view-toggle {
    display: flex;
    gap: var(--space-1);
    background: var(--gray-100);
    border-radius: var(--radius-md);
    padding: var(--space-1);
}

.view-toggle button {
    padding: var(--space-2) var(--space-3);
    border: none;
    background: transparent;
    border-radius: var(--radius-sm);
    cursor: pointer;
    color: var(--gray-600);
    transition: all var(--transition-fast);
}

.view-toggle button:hover {
    color: var(--gray-900);
}

.view-toggle button.active {
    background: white;
    color: var(--primary-red);
    box-shadow: var(--shadow-sm);
}

/* Course Grid */
.courses-grid {
    display: grid;
    gap: var(--space-6);
    margin-bottom: var(--space-8);
}

.courses-grid.grid-view {
    grid-template-columns: repeat(3, 1fr);
}

.courses-grid.list-view {
    grid-template-columns: 1fr;
}

/* Empty State */
.empty-state {
    text-align: center;
    padding: var(--space-16) var(--space-8);
}

.empty-state img {
    max-width: 300px;
    margin-bottom: var(--space-8);
}

.empty-state h3 {
    font-size: var(--text-3xl);
    color: var(--gray-900);
    margin-bottom: var(--space-3);
}

.empty-state p {
    font-size: var(--text-lg);
    color: var(--gray-600);
    margin-bottom: var(--space-6);
}

/* Load More */
.load-more-section {
    text-align: center;
    padding: var(--space-8) 0;
}

/* Responsive */
@media (max-width: 1024px) {
    .courses-layout {
        grid-template-columns: 1fr;
    }

    .filter-sidebar {
        position: fixed;
        top: 0;
        left: -320px;
        height: 100vh;
        width: 320px;
        z-index: 9998;
        transition: left var(--transition-normal);
        overflow-y: auto;
    }

    .filter-sidebar.open {
        left: 0;
    }

    .courses-grid.grid-view {
        grid-template-columns: repeat(2, 1fr);
    }
}

@media (max-width: 768px) {
    .courses-grid.grid-view {
        grid-template-columns: 1fr;
    }

    .filter-pills {
        overflow-x: auto;
        justify-content: flex-start;
        padding-bottom: var(--space-2);
    }

    .results-header {
        flex-direction: column;
        align-items: stretch;
        gap: var(--space-4);
    }
}
```

---

## JavaScript Interactions

### Infinite Scroll

```javascript
// wwwroot/js/courses-page.js

window.coursesPageInterop = {
    initInfiniteScroll: function(dotNetHelper) {
        const options = {
            root: null,
            rootMargin: '200px',
            threshold: 0
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    dotNetHelper.invokeMethodAsync('LoadMoreCourses');
                }
            });
        }, options);

        const sentinel = document.querySelector('.load-more-sentinel');
        if (sentinel) {
            observer.observe(sentinel);
        }

        return {
            dispose: () => observer.disconnect()
        };
    },

    scrollToTop: function() {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
};
```

### Razor Component Integration

```csharp
@inject IJSRuntime JS
@implements IAsyncDisposable

@code {
    private IJSObjectReference? moduleRef;
    private DotNetObjectReference<Courses>? dotNetRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            moduleRef = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./js/courses-page.js");

            dotNetRef = DotNetObjectReference.Create(this);
            await moduleRef.InvokeVoidAsync("initInfiniteScroll", dotNetRef);
        }
    }

    [JSInvokable]
    public async Task LoadMoreCourses()
    {
        await LoadMoreAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleRef != null)
        {
            await moduleRef.DisposeAsync();
        }

        dotNetRef?.Dispose();
    }
}
```

---

## Performance Optimizations

### Lazy Loading Images

```html
<img src="@course.ThumbnailUrl"
     alt="@course.Title"
     loading="lazy"
     decoding="async"
     width="360"
     height="203" />
```

### Virtualization (if >50 courses)

```csharp
@using Microsoft.AspNetCore.Components.Web.Virtualization

<Virtualize Items="@courses" Context="course" ItemSize="400">
    <ItemContent>
        <CourseCard Course="@course" />
    </ItemContent>
    <Placeholder>
        <CourseSkeletonCard />
    </Placeholder>
</Virtualize>
```

### Debounced Search

```csharp
private System.Timers.Timer? searchDebounceTimer;

private void OnSearchInput(ChangeEventArgs e)
{
    searchDebounceTimer?.Stop();
    searchDebounceTimer = new System.Timers.Timer(500);
    searchDebounceTimer.Elapsed += async (sender, args) =>
    {
        await InvokeAsync(async () =>
        {
            searchQuery = e.Value?.ToString() ?? "";
            await ApplyFiltersAsync();
            StateHasChanged();
        });
        searchDebounceTimer.Dispose();
    };
    searchDebounceTimer.Start();
}
```

---

## Accessibility Checklist

✅ **Semantic HTML**
- Main content in `<main>` landmark
- Sidebar in `<aside>` landmark
- Proper heading hierarchy (H1 → H2 → H3)

✅ **Keyboard Navigation**
- All interactive elements tabbable
- Enter/Space to activate buttons
- Arrow keys for filter options
- Escape to close modal/sidebar

✅ **Screen Reader Support**
- Alt text for all images
- ARIA labels for icon buttons
- ARIA live regions for dynamic updates
- Loading state announcements

✅ **Focus Management**
- Visible focus indicators (red outline)
- Focus trap in modal/sidebar
- Focus restoration after close

✅ **Color Contrast**
- All text meets WCAG AA (4.5:1 minimum)
- Interactive elements clearly distinguishable
- Not relying solely on color for meaning

---

## Testing Checklist

### Functional Testing

- [ ] Courses load on page mount
- [ ] Filters apply correctly
- [ ] Sort dropdown works
- [ ] Infinite scroll loads more
- [ ] Search filters results
- [ ] Empty state displays when no results
- [ ] Grid/list view toggle works
- [ ] Card click navigates to course detail

### Responsive Testing

- [ ] Desktop (1920px): 3-column grid, sidebar visible
- [ ] Laptop (1024px): 2-column grid, sidebar toggles
- [ ] Tablet (768px): 1-column grid
- [ ] Mobile (375px): Stacked layout, touch-friendly

### Performance Testing

- [ ] Lighthouse score >90
- [ ] First Contentful Paint <1.5s
- [ ] Time to Interactive <2.5s
- [ ] No layout shifts (CLS <0.1)
- [ ] Images lazy load
- [ ] API calls debounced

### Accessibility Testing

- [ ] Keyboard navigation works
- [ ] Screen reader announces correctly (NVDA/VoiceOver)
- [ ] Focus indicators visible
- [ ] Color contrast passes
- [ ] ARIA labels present

---

## API Integration

### Endpoint: Get Courses

```
GET /api/courses
Query Parameters:
- page: number (default: 1)
- limit: number (default: 12, max: 50)
- category: string (optional)
- level: string[] (optional: beginner, intermediate, advanced)
- minPrice: number (optional)
- maxPrice: number (optional)
- minRating: number (optional: 0-5)
- minDuration: number (optional, in hours)
- maxDuration: number (optional, in hours)
- sortBy: string (popular, rating, newest, price-low, price-high)
- search: string (optional)

Response: 200 OK
{
  "courses": [
    {
      "id": "uuid",
      "title": "string",
      "slug": "string",
      "description": "string",
      "thumbnailUrl": "string",
      "instructorName": "string",
      "instructorAvatar": "string",
      "category": "string",
      "level": "string",
      "rating": 4.8,
      "reviewCount": 1234,
      "studentCount": 5432,
      "duration": 40.5,
      "price": 49.99,
      "originalPrice": 199.99,
      "discount": 75,
      "badge": "Bestseller",
      "updatedAt": "2025-01-01T00:00:00Z"
    }
  ],
  "totalCount": 342,
  "page": 1,
  "pageSize": 12,
  "hasMore": true
}
```

### Service Implementation

```csharp
// Services/ICourseService.cs
public interface ICourseService
{
    Task<CourseListResponse> GetCoursesAsync(
        int page = 1,
        int limit = 12,
        CourseFilterOptions? filters = null,
        string sortBy = "popular",
        string? searchQuery = null
    );
}

// Services/CourseService.cs
public class CourseService : ICourseService
{
    private readonly HttpClient _httpClient;
    private readonly IEndpointConfigurationService _endpointConfig;

    public async Task<CourseListResponse> GetCoursesAsync(
        int page = 1,
        int limit = 12,
        CourseFilterOptions? filters = null,
        string sortBy = "popular",
        string? searchQuery = null)
    {
        var endpoint = await _endpointConfig.GetEndpointAsync("Courses", "GetCourses");
        var queryString = BuildQueryString(page, limit, filters, sortBy, searchQuery);

        var response = await _httpClient.GetAsync($"{endpoint}?{queryString}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CourseListResponse>();
    }

    private string BuildQueryString(...)
    {
        // Build query string from parameters
    }
}
```

---

## Implementation Steps

### Day 1: Setup & Layout
1. Create `/Pages/Courses.razor` component
2. Implement hero section
3. Create basic layout with sidebar and main content
4. Add placeholder content

### Day 2: Filtering & Data
1. Create `CourseFilterSidebar.razor` component
2. Implement filter logic
3. Integrate with API
4. Add loading states

### Day 3: Course Grid & Cards
1. Reuse/adapt existing `CourseCard.razor`
2. Implement grid/list view toggle
3. Add sort functionality
4. Create empty state

### Day 4: Interactions & Polish
1. Add infinite scroll
2. Implement search with debounce
3. Add animations
4. Test responsive behavior
5. Accessibility audit

---

## Related Components

**Reuse:**
- `/Components/CourseCard.razor` - Course display card
- `/Components/CategoryGrid.razor` - Category filters

**New:**
- `/Components/CourseFilterSidebar.razor` - Filter controls
- `/Components/CourseSkeletonLoader.razor` - Loading placeholder

---

## Design Assets Needed

- [ ] Empty state illustration (SVG)
- [ ] Course placeholder thumbnails
- [ ] Category icons (if not using emoji/Font Awesome)

---

**Ready to Implement:** Yes
**Blockers:** None
**Dependencies:** Course API endpoint, CourseCard component
