# Missing Pages - UI/UX Specifications
# InsightLearn WASM - Complete Site Analysis & WOW Design Specifications

**Generated:** 2025-11-07
**Project:** InsightLearn WASM Learning Management System
**Version:** 1.4.22-dev
**Design System:** Redis.io-inspired clean, minimal UI

---

## Table of Contents

1. [Site Analysis Summary](#site-analysis-summary)
2. [Current Design System](#current-design-system)
3. [Missing Pages Specifications](#missing-pages-specifications)
4. [Design Principles](#design-principles)
5. [Implementation Priorities](#implementation-priorities)

---

## Site Analysis Summary

### Total Links Analysis

**Navigation Structure:**
- Main Navigation (Public): 4 links
- User Navigation (Authenticated): 4 links
- Instructor Navigation (Role-based): 3 links
- Admin Navigation (Role-based): 5 links
- Footer Links: 11 links
- **Total Unique Routes Identified:** 41

### Existing Pages (23 total)

**Public Pages (6):**
- `/` - Home page (Index.razor)
- `/login` - Login page
- `/register` - Registration page
- `/help` - Help Center
- `/privacy` - Privacy Policy
- `/terms` - Terms of Service
- `/cookie-policy` - Cookie Policy

**User Pages (3):**
- `/dashboard` - User Dashboard
- `/dashboard/student` - Student Dashboard
- `/dashboard/instructor` - Instructor Dashboard

**Admin Pages (14):**
- `/admin` or `/admin/dashboard` - Admin Dashboard
- `/admin/users` - User Management
- `/admin/users/create` - Create User
- `/admin/users/edit/{id}` - Edit User
- `/admin/users/lockout` - User Lockout Management
- `/admin/courses` - Course Management
- `/admin/courses/create` - Create Course
- `/admin/courses/edit/{id}` - Edit Course
- `/admin/categories` - Category Management
- `/admin/categories/create` - Create Category
- `/admin/categories/edit/{id}` - Edit Category
- `/admin/analytics` - Analytics Dashboard
- `/admin/settings` - System Settings
- `/admin/health` - System Health

### Missing Pages (18 total)

**Critical Public Pages (10):**
1. `/courses` - Browse Courses (main catalog)
2. `/courses/{id}` - Course Detail Page
3. `/categories` - All Categories Page
4. `/categories/{slug}` - Category Detail/Browse
5. `/about` - About Us Page
6. `/cart` - Shopping Cart
7. `/search` - Search Results Page
8. `/contact` - Contact Us Page
9. `/faq` - Frequently Asked Questions
10. `/instructors` - Become an Instructor

**User Pages (3):**
11. `/my-courses` - My Enrolled Courses
12. `/profile` - User Profile
13. `/settings` - User Settings

**Instructor Pages (2):**
14. `/instructor/courses` - Instructor's Courses List
15. `/instructor/courses/create` - Create New Course (Instructor)

**Enterprise/Support (3):**
16. `/enterprise` - Enterprise Solutions
17. `/accessibility` - Accessibility Statement
18. `/categories/development` - Individual category pages (Development, Business, Design, Marketing)

---

## Current Design System

### Design Philosophy

**Inspiration:** Redis.io-inspired clean, minimal, professional UI/UX
**Approach:** Content-first, performance-optimized, WCAG 2.1 AA compliant

### Color System

```css
/* PRIMARY COLOR SYSTEM */
--primary-red: #dc2626         /* Main brand color */
--primary-red-hover: #b91c1c   /* Hover states */
--primary-red-light: #fecaca   /* Backgrounds, badges */

/* NEUTRAL GRAYSCALE */
--gray-900: #111827            /* Primary text */
--gray-700: #374151            /* Secondary text */
--gray-500: #6b7280            /* Placeholder text */
--gray-400: #9ca3af            /* Disabled text */
--gray-300: #d1d5db            /* Light borders */
--gray-200: #e5e7eb            /* Subtle borders */
--gray-100: #f3f4f6            /* Light backgrounds */
--gray-50: #f9fafb             /* Lightest background */

/* SEMANTIC COLORS */
--success: #10b981             /* Success states */
--warning: #f59e0b             /* Warning states */
--error: #ef4444               /* Error states */
--info: #3b82f6                /* Info states */
```

### Typography Scale

```css
/* FONT FAMILY */
--font-primary: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Inter', sans-serif;

/* TYPE SCALE */
--text-6xl: 3.75rem (60px)     /* Page titles */
--text-5xl: 3rem (48px)        /* Section headers */
--text-4xl: 2.25rem (36px)     /* Component titles */
--text-3xl: 1.875rem (30px)    /* Card headers */
--text-2xl: 1.5rem (24px)      /* Subsection headers */
--text-xl: 1.25rem (20px)      /* Large body text */
--text-base: 1rem (16px)       /* Standard body text */
--text-sm: 0.875rem (14px)     /* Small text, labels */
--text-xs: 0.75rem (12px)      /* Captions, metadata */

/* FONT WEIGHTS */
--font-light: 300
--font-normal: 400
--font-medium: 500
--font-semibold: 600
--font-bold: 700
```

### Spacing System

```css
--space-1: 0.25rem (4px)
--space-2: 0.5rem (8px)
--space-3: 0.75rem (12px)
--space-4: 1rem (16px)
--space-5: 1.25rem (20px)
--space-6: 1.5rem (24px)
--space-8: 2rem (32px)
--space-10: 2.5rem (40px)
--space-12: 3rem (48px)
--space-16: 4rem (64px)
--space-20: 5rem (80px)
--space-24: 6rem (96px)
```

### Component Patterns

**Buttons:**
- `.btn` - Base button (44px min-height for accessibility)
- `.btn-primary` - Primary red CTA
- `.btn-secondary` - Outlined secondary action
- `.btn-ghost` - Transparent ghost button
- Sizes: `.btn-sm`, `.btn-lg`

**Cards:**
- `.card` - White background, subtle border, shadow on hover
- `.card-elevated` - Pre-elevated with shadow
- `.metric-card` - Dashboard metric cards with hover lift
- `.course-card` - Featured course cards with badge overlays

**Forms:**
- `.form-input` - Standard text input (44px min-height)
- `.form-label` - Semibold labels with required indicator
- `.form-error` - Error message display
- Focus states: Red outline with shadow

**Tables:**
- `.clean-table` - Minimal design with hover states
- `.table-container` - Responsive wrapper with overflow

**Badges & Status:**
- `.badge` - Small inline badges
- `.status-indicator` - Status with dot indicators
- Color variants: gray, red, success, warning, error

### Layout Patterns

**Container:**
- Max-width: 1200px
- Responsive padding: 24px desktop, 16px mobile

**Grid Systems:**
- `.grid` with `.grid-cols-{1-4}`
- Responsive breakpoints: 1024px, 768px
- Auto-collapse to single column on mobile

**Header:**
- Fixed position, translucent background with blur
- 3-column grid: Logo | Search | Actions
- Height: 72px
- Z-index: 99999

**Footer:**
- Dark gradient background (#1a1d23 to #0f1115)
- 4-column grid on desktop
- Social links, legal links, badges

### Animation & Interaction

**Transitions:**
- Fast: 150ms ease-in-out (hover states)
- Normal: 250ms ease-in-out (layout changes)

**Micro-interactions:**
- Button hover: translateY(-1px) + shadow
- Card hover: shadow elevation
- Focus states: 2px red outline with offset

**Loading States:**
- Spinner component with size variants
- Skeleton loading for content
- Progress bars for multi-step processes

### Accessibility Features

- Minimum touch target: 44px × 44px
- WCAG 2.1 AA contrast ratios
- Skip-to-content link
- Screen reader labels (sr-only)
- Focus-visible indicators
- Reduced motion support
- High contrast mode support
- Keyboard navigation support

### Responsive Breakpoints

```css
/* Desktop: 1024px+ (default) */
/* Tablet: 768px - 1023px */
/* Mobile: < 768px */
```

**Mobile-First Approach:**
- Single column layouts on mobile
- Hamburger menu collapse
- Touch-optimized interactions
- Reduced font sizes
- Condensed spacing

---

## Design Principles

### 1. WOW Factor Guidelines

**What makes a page "WOW":**
- Delightful micro-interactions
- Smooth, purposeful animations
- Clear visual hierarchy
- Generous white space
- High-quality imagery/icons
- Progressive disclosure
- Instant feedback
- Personality without distraction

### 2. Performance First

- Lazy load images and components
- Optimize critical rendering path
- Minimize layout shifts
- Progressive enhancement
- Preload critical resources

### 3. Blazor WASM Best Practices

- Minimize component re-renders
- Use virtualization for long lists
- Efficient state management
- Debounce search/filter inputs
- Prerender static content where possible

### 4. Content Strategy

**Hierarchy:**
1. Primary action/message
2. Supporting information
3. Alternative paths
4. Help/assistance

**Tone:**
- Professional yet approachable
- Action-oriented
- Clear and concise
- Educational

---

## Missing Pages Specifications

---

### 1. Browse Courses Page (`/courses`)

**Route:** `/courses`
**Page Type:** Public, High Traffic, Critical
**Priority:** CRITICAL (P0)

#### Purpose
Main course catalog where users discover and browse all available courses. This is a primary conversion page.

#### WOW Factor
- **Masonry grid layout** with varying card heights based on content
- **Live course counters** showing "X students enrolled in last 24h"
- **Smooth skeleton loading** during data fetch
- **Filter sidebar** that slides in/out with animation
- **Infinite scroll** with intersection observer
- **Quick preview** on hover showing course intro video thumbnail

#### User Journey
1. User arrives from homepage "Explore Courses" CTA or navigation
2. Sees featured/popular courses immediately (above the fold)
3. Applies filters (category, price, level, rating, duration)
4. Scrolls to load more courses
5. Hovers card to see quick preview
6. Clicks course card → navigates to course detail page

#### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HEADER (Fixed)                                  │
├─────────────────────────────────────────────────┤
│ Hero Banner                                     │
│ "Explore 1,200+ Expert-Led Courses"             │
│ [Search Bar] [Filter Pills: Category, Price...] │
├──────────┬──────────────────────────────────────┤
│ SIDEBAR  │  COURSE GRID (3 cols)               │
│ Filters  │  ┌──────┐ ┌──────┐ ┌──────┐        │
│ ├─ Cat   │  │Course│ │Course│ │Course│        │
│ ├─ Price │  │Card 1│ │Card 2│ │Card 3│        │
│ ├─ Level │  └──────┘ └──────┘ └──────┘        │
│ ├─ Rating│  ┌──────┐ ┌──────┐ ┌──────┐        │
│ └─ Durat │  │Course│ │Course│ │Course│        │
│          │  │Card 4│ │Card 5│ │Card 6│        │
│          │  └──────┘ └──────┘ └──────┘        │
│          │  [Load More...]                     │
└──────────┴──────────────────────────────────────┘
│ FOOTER                                          │
└─────────────────────────────────────────────────┘
```

#### Visual Design

**Hero Section:**
- Height: 280px
- Background: Subtle gradient (gray-50 to white)
- Title: text-5xl, font-bold, gray-900
- Subtitle: text-xl, gray-600, "Learn from industry experts"
- Search bar: Large (500px wide), with icon, placeholder "Search courses, topics, instructors..."

**Filter Sidebar:**
- Width: 280px (desktop), full-screen overlay (mobile)
- Background: white
- Border: 1px solid gray-200
- Sticky position on scroll
- Collapsible sections with chevron icons
- Checkbox groups with counts (e.g., "Development (342)")
- Price range slider
- Star rating filters (interactive stars)
- "Clear All Filters" button at bottom

**Course Cards:**
- Width: ~360px (responsive)
- Border-radius: 12px
- Shadow: hover elevation from sm to lg
- Thumbnail: 16:9 aspect ratio, 240px height
- Badge overlay: "Bestseller", "New", "Popular" (top-left)
- Category tag: top-right, small badge
- Content padding: 20px
- Title: 2 lines max, text-lg, font-semibold
- Description: 3 lines max, text-sm, gray-600
- Instructor: Avatar (32px) + name
- Metadata row: Rating (stars + number), students, duration
- Price: text-2xl, font-bold, primary-red
- Original price: text-base, strikethrough, gray-400
- CTA button: "Enroll Now" - full-width, btn-primary

**Grid System:**
- Desktop: 3 columns with 24px gap
- Tablet: 2 columns with 20px gap
- Mobile: 1 column with 16px gap

#### Interactive Elements

**Animations:**
- Card entrance: stagger animation (each card fades in with 50ms delay)
- Filter apply: smooth 300ms transition
- Skeleton loading: pulse animation
- Hover: card lift (translateY -4px) + shadow increase
- Button hover: slight scale (1.02) + color darken

**Interactions:**
- Search: 500ms debounce, live results
- Filters: Instant apply with loading skeleton
- Sort dropdown: Smooth dropdown with fade
- Infinite scroll: Load more 12 courses at 80% scroll
- Quick preview: On hover 800ms delay, show video thumbnail modal

**States:**
- Loading: Skeleton cards (shimmer effect)
- Empty: Illustration + "No courses found" + "Clear filters" button
- Error: Alert banner with retry button

#### Content Strategy

**Headline:** "Explore 1,200+ Expert-Led Courses"
**Subheadline:** "Learn at your own pace from industry professionals. Start your journey today."

**Filter Labels:**
- Categories (12): Development, Business, Design, Marketing, etc.
- Price: Free, Paid, Under $50, $50-$100, $100+
- Level: Beginner, Intermediate, Advanced, All Levels
- Rating: 4.5+, 4.0+, 3.5+, 3.0+
- Duration: 0-2 hours, 2-5 hours, 5-10 hours, 10+ hours

**Sort Options:**
- Most Popular
- Highest Rated
- Newest First
- Price: Low to High
- Price: High to Low

#### Responsive Behavior

**Desktop (1024px+):**
- Sidebar always visible
- 3-column grid
- Hover effects enabled

**Tablet (768px-1023px):**
- Sidebar toggles via button
- 2-column grid
- Reduced spacing

**Mobile (<768px):**
- Sidebar full-screen overlay
- Single column grid
- Filter button fixed at bottom
- Simplified cards (smaller thumbnails)
- Touch-optimized tap targets

#### Accessibility

- Landmark: `<main role="main">`
- Heading structure: H1 (page title) → H2 (section) → H3 (card titles)
- Filter checkboxes: Proper labels and ARIA
- Course cards: Card semantic structure with links
- Skip-to-results link
- Screen reader announcements for filter changes
- Keyboard navigation: Tab order, Enter to activate
- Focus indicators: Red outline on all interactive elements
- ARIA live region for dynamic content loading

#### Performance

**Optimization Strategy:**
- Initial load: 12 courses (SSR if possible)
- Lazy load images with blur-up placeholder
- Virtualization for >50 courses
- Debounced search (500ms)
- Filter results cached for 5 minutes
- Thumbnail: WebP format, 480px width
- Preload next page of courses at 80% scroll

**Loading Strategy:**
1. Show skeleton cards immediately
2. Fetch courses from API
3. Fade in real cards with stagger
4. Total time to interactive: <2 seconds

---

### 2. Course Detail Page (`/courses/{id}`)

**Route:** `/courses/{id}` or `/courses/{slug}`
**Page Type:** Public, High Conversion, Critical
**Priority:** CRITICAL (P0)

#### Purpose
Detailed course page where users learn about course content, instructor, reviews, and make enrollment decision. Primary conversion page.

#### WOW Factor
- **Video hero** with course intro video (autoplay muted on desktop)
- **Sticky enrollment card** that follows scroll
- **Interactive curriculum accordion** with video duration tooltips
- **Live student counter** updating in real-time
- **Animated skill badges** that pop in on scroll
- **Review sentiment analysis** visualization
- **Smooth section transitions** with intersection observer

#### User Journey
1. User arrives from course catalog or search
2. Watches intro video or sees hero image
3. Scrolls to read course description
4. Expands curriculum to see all lessons
5. Reads instructor bio and credentials
6. Checks reviews and ratings
7. Clicks "Enroll Now" in sticky card or floating CTA
8. Redirects to checkout or cart

#### Layout Structure

```
┌───────────────────────────────────────────────────────────────┐
│ HEADER (Fixed)                                                │
├───────────────────────────────────────────────────────────────┤
│ HERO VIDEO SECTION (Full-width, 600px height)                │
│ [Course Intro Video with Play Overlay]                        │
│ Title: "Complete Web Development Bootcamp 2025"               │
│ Instructor | Rating | Students | Last Updated                │
├────────────────────────────────────┬──────────────────────────┤
│ MAIN CONTENT (70%)                 │ SIDEBAR (30%)            │
│                                    │ ┌────────────────────┐   │
│ ┌─ What You'll Learn              │ │ ENROLLMENT CARD    │   │
│ │  ✓ Build web apps                │ │ $49.99 $199.99    │   │
│ │  ✓ Master React                  │ │ 🎓 52,341 students│   │
│ │  ✓ Deploy to cloud               │ │ ⏱️ 40 hours       │   │
│ └──────────────────────            │ │ 📱 Mobile access  │   │
│                                    │ │ [Enroll Now] CTA  │   │
│ ┌─ Course Content                  │ │ [Add to Cart]     │   │
│ │  ▶ Section 1: HTML (2h 30m)     │ │ [Add to Wishlist] │   │
│ │    - Introduction (10m)  [FREE]  │ └────────────────────┘   │
│ │    - Setup (15m)                 │                          │
│ │  ▶ Section 2: CSS (3h 45m)      │ ┌─ This course incl.   │
│ │    - Basics (20m)                │ │ ✓ Lifetime access    │
│ │    ...                           │ │ ✓ Certificate        │
│ └──────────────────────            │ └──────────────────────┘
│                                    │                          │
│ ┌─ Requirements                    │ ┌─ Related Courses     │
│ │  • Basic computer skills         │ │ [Course Card]        │
│ │  • Internet connection           │ │ [Course Card]        │
│ └──────────────────────            │ └──────────────────────┘
│                                    │                          │
│ ┌─ Description                     │                          │
│ │  Full course description...      │                          │
│ └──────────────────────            │                          │
│                                    │                          │
│ ┌─ Instructor                      │                          │
│ │  [Avatar] Sarah Johnson          │                          │
│ │  Full Stack Developer at Google  │                          │
│ │  Bio, credentials, social links  │                          │
│ └──────────────────────            │                          │
│                                    │                          │
│ ┌─ Reviews (4.9 ⭐ - 12,450)       │                          │
│ │  [Rating Distribution Chart]     │                          │
│ │  [Individual Reviews...]         │                          │
│ └──────────────────────            │                          │
└────────────────────────────────────┴──────────────────────────┘
│ FOOTER                                                        │
└───────────────────────────────────────────────────────────────┘
```

#### Visual Design

**Hero Section:**
- Height: 500px (desktop), 300px (mobile)
- Video: Autoplay muted, controls, 16:9 aspect
- Play overlay: Large circular play button (120px), primary-red
- Gradient overlay on bottom: rgba(0,0,0,0.6) to transparent
- Title: text-5xl, font-bold, white, over video bottom
- Breadcrumb: Category > Subcategory > Course (top-left)

**Sticky Enrollment Card:**
- Width: 340px
- Background: white
- Border: 2px solid gray-200
- Border-radius: 12px
- Shadow: lg
- Sticky at 100px from top
- Padding: 24px

**Price Display:**
- Current price: text-4xl, font-bold, primary-red
- Original price: text-xl, strikethrough, gray-400
- Discount badge: "75% off" - badge-red
- Urgency text: "⏰ 2 days left at this price" - text-sm, warning color

**CTA Buttons:**
- Primary: "Enroll Now" - btn-primary, btn-lg, full-width
- Secondary: "Add to Cart" - btn-secondary, full-width
- Tertiary: "Add to Wishlist" - btn-ghost, icon + text

**Course Stats:**
- Icon + text rows
- Icons: Emoji or Font Awesome (red color)
- Text: text-sm, gray-700
- Examples:
  - 🎓 52,341 students enrolled
  - ⭐ 4.9 rating (12,450 reviews)
  - ⏱️ 40 hours total content
  - 📄 12 downloadable resources
  - 📱 Access on mobile and TV
  - ✅ Certificate of completion

**What You'll Learn Section:**
- 2-column grid (desktop), 1-column (mobile)
- Checkmark bullets (green check icon)
- Text: text-base, gray-700
- 8-12 learning outcomes
- Background: gray-50, padding 32px, border-radius 12px

**Course Content (Curriculum):**
- Accordion component
- Section headers: text-lg, font-semibold, with expand/collapse icon
- Section metadata: "8 lectures • 2h 30m"
- Lesson rows: Indent, play icon, title, duration
- Free preview: Green "FREE" badge
- Expandable: Smooth 300ms transition
- Hover: Background gray-50

**Requirements Section:**
- Bullet list with dot markers
- Text: text-base, gray-700
- Short list (3-5 items)

**Description Section:**
- Long-form content
- Rich text: paragraphs, bold, lists
- "Read More" button if >500 words (expand on click)
- Typography: text-base, line-height 1.75

**Instructor Section:**
- Avatar: 80px circle
- Name: text-2xl, font-semibold
- Title/credentials: text-base, gray-600
- Social proof: "🎓 50,000 students • 12 courses • 4.8 rating"
- Bio: text-base, 3-4 paragraphs
- Social links: LinkedIn, Twitter, website

**Reviews Section:**
- Header: "Student Reviews" + avg rating + total count
- Rating distribution: Horizontal bars (5 stars to 1 star)
- Sort dropdown: "Most Recent", "Highest Rating", "Lowest Rating"
- Individual reviews:
  - Avatar + name + date
  - Star rating
  - Review text
  - Helpful buttons: "Helpful (23)" | "Report"
- Pagination: Load more button

#### Interactive Elements

**Animations:**
- Hero video: Fade in play button on pause
- Sticky card: Slide down when scrolling past hero
- Sections: Fade in with scroll (intersection observer)
- Curriculum: Smooth accordion expand/collapse
- Skill badges: Pop in with stagger (scale + opacity)
- Review load: Fade in with upward slide

**Interactions:**
- Video: Play/pause, seek, volume, fullscreen
- Curriculum: Click to expand sections, click lesson for preview
- Enroll button: Loading spinner during redirect
- Add to cart: Success toast notification
- Wishlist: Heart icon animation (fill + scale)
- Reviews: Load more (infinite scroll or button)
- Share: Copy link with success feedback

**States:**
- Loading: Skeleton for content sections
- Already enrolled: "Go to Course" button instead of enroll
- Course full: "Waitlist" CTA
- Course archived: "Not currently available" banner

#### Content Strategy

**Headlines:**
- Hero: Course title (e.g., "Complete Web Development Bootcamp 2025")
- Subtitle: Brief description (1 sentence)
- Sections: Clear, action-oriented ("What You'll Learn", "Course Content", "Meet Your Instructor")

**Copy Tone:**
- Benefit-focused (not feature-focused)
- Social proof heavy (student counts, reviews)
- Urgency where appropriate (limited-time pricing)
- Instructor authority (credentials, experience)
- Clear learning outcomes

**Enrollment Card Copy:**
- Price: Prominent, with savings calculation
- Urgency: "Limited time offer"
- Guarantee: "30-day money-back guarantee"
- Access: "Full lifetime access"
- Certificate: "Earn a certificate"

#### Responsive Behavior

**Desktop (1024px+):**
- 70/30 split layout
- Sticky sidebar
- 2-column learning outcomes
- Video autoplay

**Tablet (768px-1023px):**
- Full-width hero
- Sidebar below hero
- 2-column learning outcomes
- Sticky "Enroll" bar at bottom

**Mobile (<768px):**
- Full-width hero (300px height)
- No autoplay
- Single column layout
- Enrollment card at top (before content)
- Sticky "Enroll" button at bottom (floating)
- Simplified curriculum (always collapsed)
- 1-column learning outcomes

#### Accessibility

- Video: Closed captions, audio description option
- Landmark: `<main>`, `<aside>` for sidebar
- Heading structure: H1 (course title) → H2 (sections) → H3 (subsections)
- Accordion: ARIA expanded/collapsed states
- Buttons: Clear labels ("Enroll in [Course Name]")
- Color contrast: All text meets WCAG AA
- Focus indicators: Red outline on all interactive elements
- Screen reader: Price announced as "Current price $49.99, original price $199.99, 75% discount"
- Keyboard: Tab navigation, Enter/Space to activate

#### Performance

**Optimization:**
- Hero video: Lazy load, poster image first
- Images: WebP, responsive sizes, lazy load
- Curriculum: Virtualization if >50 lessons
- Reviews: Pagination, load 10 at a time
- Related courses: Lazy load below fold
- Fonts: Preload, font-display: swap

**Loading Strategy:**
1. Server-side render course metadata (SEO)
2. Stream hero image/video
3. Render enrollment card immediately
4. Lazy load reviews and related courses
5. Intersection observer for section animations
6. Total time to interactive: <2.5 seconds

**API Calls:**
- Course data: GET /api/courses/{id}
- Reviews: GET /api/courses/{id}/reviews?page=1&limit=10
- Related courses: GET /api/courses/{id}/related
- Enrollment check: GET /api/users/me/enrollments/{courseId}

---

### 3. All Categories Page (`/categories`)

**Route:** `/categories`
**Page Type:** Public, Discovery
**Priority:** HIGH (P1)

#### Purpose
Visual directory of all course categories, helping users browse and discover content by topic area.

#### WOW Factor
- **Category cards with icon animations** - Icons animate on hover (bounce, rotate)
- **Live course counters** - Show real-time course counts per category
- **Color-coded system** - Each category has unique accent color
- **Masonry grid** - Dynamic heights based on popularity
- **Smooth hover effects** - Card lift with shadow elevation
- **Featured category carousel** - Horizontal scroll showcase at top

#### User Journey
1. User clicks "Categories" in main navigation or footer
2. Sees featured categories carousel (top 5-8)
3. Scrolls to see all categories grid
4. Hovers card to see category description
5. Clicks category card → navigates to category detail page

#### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HEADER (Fixed)                                  │
├─────────────────────────────────────────────────┤
│ Hero Section                                    │
│ "Explore Learning Categories"                  │
│ "Choose from 12 specialized areas"             │
├─────────────────────────────────────────────────┤
│ Featured Categories Carousel                   │
│ ◀ [Card] [Card] [Card] [Card] [Card] ▶       │
├─────────────────────────────────────────────────┤
│ All Categories (4-column grid)                 │
│ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────┐│
│ │Development│ │  Design  │ │ Business │ │Mktg││
│ │  💻       │ │  🎨     │ │  💼     │ │ 📢││
│ │342 courses│ │215 cours.│ │187 cours.│ │... ││
│ └──────────┘ └──────────┘ └──────────┘ └────┘│
│ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────┐│
│ │Data Sci. │ │  Health  │ │ Personal │ │... ││
│ └──────────┘ └──────────┘ └──────────┘ └────┘│
├─────────────────────────────────────────────────┤
│ CTA Section: "Don't see your topic?"           │
│ [Request a Category] button                    │
├─────────────────────────────────────────────────┤
│ FOOTER                                          │
└─────────────────────────────────────────────────┘
```

#### Visual Design

**Hero Section:**
- Height: 320px
- Background: Gradient (primary-red to primary-red-hover)
- Title: text-6xl, font-bold, white
- Subtitle: text-xl, white with opacity-90
- Search bar: Optional secondary CTA

**Featured Carousel:**
- Height: 280px cards
- Horizontal scroll with arrows
- 4 visible cards (desktop), scroll by 1
- Cards: Larger than grid cards, featured badge
- Background: gray-50 section background

**Category Cards:**
- Size: ~280px × 320px (dynamic based on content)
- Background: white
- Border: 2px solid category-color
- Border-radius: 16px
- Shadow: hover elevation (sm to xl)
- Padding: 32px

**Card Content:**
- Icon: 80px, centered, category-specific emoji or Font Awesome
- Category name: text-3xl, font-bold, gray-900, centered
- Course count: text-base, gray-600, centered
- Description: text-sm, gray-500, 2 lines, hidden until hover
- Popular tag: Badge (if top 3)
- CTA: "Explore →" link, appears on hover

**Color System (per category):**
- Development: Blue (#3b82f6)
- Design: Purple (#a855f7)
- Business: Green (#10b981)
- Marketing: Orange (#f59e0b)
- Data Science: Cyan (#06b6d4)
- IT & Software: Indigo (#6366f1)
- Personal Development: Pink (#ec4899)
- Health & Fitness: Red (#ef4444)
- Music: Yellow (#eab308)
- Photography: Slate (#64748b)
- Finance: Emerald (#059669)
- Teaching: Rose (#f43f5e)

**Grid System:**
- Desktop: 4 columns, 24px gap
- Tablet: 3 columns, 20px gap
- Mobile: 2 columns, 16px gap

#### Interactive Elements

**Animations:**
- Cards: Entrance fade-in with stagger (50ms delay each)
- Icon: Bounce on hover
- Card: Lift (translateY -8px) on hover
- Shadow: Smooth elevation change
- Description: Fade in on hover
- CTA: Slide in from bottom on hover

**Interactions:**
- Carousel: Drag to scroll (mobile), arrow click (desktop)
- Card hover: Show full description + CTA
- Card click: Navigate to category page
- Icon: Scale slightly on hover (1.1×)
- Search: Filter categories as you type

**States:**
- Loading: Skeleton cards with pulse
- Empty: "No categories found" (shouldn't happen)
- Hover: Full card interaction

#### Content Strategy

**Headlines:**
- Hero: "Explore Learning Categories"
- Subtitle: "Choose from 12 specialized areas to advance your skills"
- Featured section: "Most Popular Categories"
- Grid section: "All Categories"
- CTA: "Don't see what you're looking for?"

**Category Descriptions (hover):**
- Development: "Build websites, apps, and software with modern technologies"
- Design: "Create beautiful user experiences and visual designs"
- Business: "Master entrepreneurship, management, and strategy"
- Marketing: "Learn digital marketing, SEO, and brand building"
- Data Science: "Analyze data, build models, and gain insights"
- (etc. for all 12 categories)

**Course Counts:**
- Real-time from database
- Format: "342 courses" or "1.2K courses" if >999

#### Responsive Behavior

**Desktop (1024px+):**
- 4-column grid
- Carousel shows 4 cards
- Full descriptions on hover

**Tablet (768px-1023px):**
- 3-column grid
- Carousel shows 3 cards
- Hover effects enabled

**Mobile (<768px):**
- 2-column grid
- Carousel shows 1.5 cards (peek next)
- Tap to see description (no hover)
- Larger tap targets

#### Accessibility

- Landmark: `<main>`
- Heading: H1 (page title) → H2 (category names)
- Cards: Links with aria-label "Explore Development category, 342 courses"
- Carousel: ARIA role carousel, keyboard arrow navigation
- Focus indicators: Red outline
- Screen reader: Announce course counts
- Keyboard: Tab through cards, Enter to navigate

#### Performance

- Lazy load category images/icons
- Preload featured categories
- Static data (categories change rarely) - consider caching
- Intersection observer for card animations
- Total time to interactive: <1.5 seconds

---

### 4. Category Detail Page (`/categories/{slug}`)

**Route:** `/categories/development`, `/categories/business`, etc.
**Page Type:** Public, Discovery
**Priority:** HIGH (P1)

#### Purpose
Browse all courses within a specific category with filtering and sorting options. Sub-category navigation.

#### WOW Factor
- **Category-specific hero** with unique gradient per category
- **Animated skill path visualization** showing learning progression
- **Live learning stats** for the category
- **Sub-category quick filters** with smooth transitions
- **Instructor spotlight** carousel for category experts
- **Related career paths** section with salary data

#### User Journey
1. User arrives from categories page or navigation dropdown
2. Sees category hero and overview stats
3. Browses sub-categories (optional)
4. Applies filters (level, price, duration)
5. Scrolls course list
6. Clicks course card → course detail page

#### Layout Structure

```
┌───────────────────────────────────────────────────┐
│ HEADER                                            │
├───────────────────────────────────────────────────┤
│ HERO - Category-Specific                          │
│ Background: [Development Icon Pattern]            │
│ "Development Courses"                             │
│ "Master web, mobile, and software development"    │
│ Stats: 342 courses | 150K students | 4.7 avg      │
├───────────────────────────────────────────────────┤
│ Sub-Categories Pills (horizontal scroll)          │
│ [All] [Web Dev] [Mobile] [Game Dev] [Software...] │
├────────────────────────────────────────┬──────────┤
│ Course List (with filters)             │ SIDEBAR  │
│                                        │ ─────────│
│ Sort: [Most Popular ▼]  Results: 342  │ Filters  │
│                                        │ ├─ Level │
│ ┌─────────────────────────────────┐   │ ├─ Price │
│ │ Course Card 1                   │   │ ├─ Rating│
│ └─────────────────────────────────┘   │ └─ Durat │
│ ┌─────────────────────────────────┐   │          │
│ │ Course Card 2                   │   │ Popular  │
│ └─────────────────────────────────┘   │ Instructr│
│ ┌─────────────────────────────────┐   │ [Avatar] │
│ │ Course Card 3                   │   │ [Avatar] │
│ └─────────────────────────────────┘   │ [Avatar] │
│                                        │          │
│ [Load More...]                         │ Career   │
│                                        │ Paths    │
│                                        │ • Web Dev│
│                                        │ • Data   │
├────────────────────────────────────────┴──────────┤
│ Learning Path Section                             │
│ "Your Development Learning Journey"               │
│ [Beginner → Intermediate → Advanced → Expert]     │
├───────────────────────────────────────────────────┤
│ FOOTER                                            │
└───────────────────────────────────────────────────┘
```

#### Visual Design

**Hero Section:**
- Height: 400px
- Background: Category-specific gradient + icon pattern
- Development: Blue gradient (#3b82f6 to #1e40af)
- Large category icon: 120px, white, top-right
- Breadcrumb: Home > Categories > Development
- Title: text-6xl, font-bold, white
- Description: text-xl, white with opacity-90, 2 lines max
- Stats bar: 3 columns, text-base, white
  - "342 courses" | "150,248 students" | "4.7 avg rating"

**Sub-Category Pills:**
- Horizontal scrollable row
- Pill style: rounded-full, padding 12px 24px
- Active: primary-red background, white text
- Inactive: white background, gray-700 text, border gray-300
- Hover: gray-100 background
- Smooth scroll with fade edges

**Course List:**
- Same card style as /courses page
- List view option toggle: Grid | List
- List view: Horizontal card with thumbnail left, info right

**Sidebar:**
- Same filter sidebar as /courses
- Plus: "Popular Instructors in Development" section
- Plus: "Career Paths" section with job roles

**Learning Path Visualization:**
- Full-width section, background gray-50
- Timeline style: 4 stages connected by arrows
- Each stage: Icon, title, courses list, time estimate
- Example:
  - Beginner (0-3 months): HTML, CSS, JavaScript basics
  - Intermediate (3-6 months): React, Node.js, Databases
  - Advanced (6-12 months): System design, Architecture
  - Expert (12+ months): Leadership, Advanced patterns

#### Interactive Elements

**Animations:**
- Hero: Parallax effect on scroll
- Sub-categories: Smooth horizontal scroll
- Learning path: Stages fade in as you scroll (intersection observer)
- Course cards: Stagger entrance

**Interactions:**
- Sub-category pill click: Filter courses instantly
- Filter apply: Smooth transition
- View toggle: Grid ⇄ List (animate layout change)
- Instructor avatar: Tooltip on hover with course count

#### Content Strategy

**Hero Headlines (per category):**
- Development: "Master Modern Development"
- Business: "Build Your Business Acumen"
- Design: "Create Beautiful Experiences"
- Marketing: "Grow Your Brand & Reach"

**Descriptions:**
- 1-2 sentences describing category value
- Action-oriented language
- Highlight career outcomes

#### Responsive Behavior

**Desktop:** Full layout with sidebar
**Tablet:** Sidebar toggles
**Mobile:** Single column, sidebar as overlay

#### Accessibility

- Same standards as /courses page
- Learning path: ARIA labels for timeline
- Sub-categories: ARIA role "tablist"

#### Performance

- Category data: Static (consider CDN caching)
- Courses: Paginated (12 at a time)
- Lazy load learning path section
- Total time to interactive: <2 seconds

---

### 5. Shopping Cart Page (`/cart`)

**Route:** `/cart`
**Page Type:** Authenticated (or Guest), Conversion Critical
**Priority:** CRITICAL (P0)

#### Purpose
Review courses added to cart, apply promo codes, proceed to checkout. Critical conversion funnel step.

#### WOW Factor
- **Smooth item removal** with undo toast notification
- **Real-time price updates** when applying promo codes
- **Savings calculator** showing total savings
- **Related courses** carousel - "Students also bought"
- **Trust badges** (money-back guarantee, secure checkout)
- **Sticky checkout button** on mobile
- **Empty cart illustration** with course recommendations

#### User Journey
1. User adds course to cart from course detail page
2. Arrives at /cart
3. Reviews cart items
4. Applies promo code (optional)
5. Sees total price with breakdown
6. Clicks "Checkout" → proceeds to /checkout or payment

#### Layout Structure

```
┌───────────────────────────────────────────────────────┐
│ HEADER (Fixed)                                        │
├───────────────────────────────────────────────────────┤
│ Breadcrumb: Home > Shopping Cart                      │
│ Title: "Shopping Cart" (3 items)                      │
├─────────────────────────────────────┬─────────────────┤
│ CART ITEMS (70%)                    │ SUMMARY (30%)   │
│                                     │                 │
│ ┌──────────────────────────────┐   │ ┌─────────────┐ │
│ │ [Thumbnail] Course Title 1   │   │ │ Order Sum.  │ │
│ │ Instructor Name              │   │ │ ─────────── │ │
│ │ 4.8 ⭐ (1.2K) • 12h total   │   │ │ Subtotal:   │ │
│ │ $49.99 [$199.99 struck]      │   │ │   $134.97   │ │
│ │ [Remove] [Save for Later]    │   │ │ Discount:   │ │
│ └──────────────────────────────┘   │ │   -$85.00   │ │
│                                     │ │ ─────────── │ │
│ ┌──────────────────────────────┐   │ │ Total:      │ │
│ │ Course 2...                  │   │ │ $49.97 USD  │ │
│ └──────────────────────────────┘   │ │             │ │
│                                     │ │ [Checkout]  │ │
│ ┌──────────────────────────────┐   │ │             │ │
│ │ Course 3...                  │   │ │ Promo Code: │ │
│ └──────────────────────────────┘   │ │ [________]  │ │
│                                     │ │ [Apply]     │ │
│ ┌─ Promo Code ─────────────────┐   │ │             │ │
│ │ [INPUT] [Apply Button]       │   │ │ 30-Day      │ │
│ │ ✓ "SAVE20" applied (-$10)    │   │ │ Money-Back  │ │
│ └──────────────────────────────┘   │ │ Guarantee   │ │
│                                     │ └─────────────┘ │
├─────────────────────────────────────┴─────────────────┤
│ "Students also bought" Carousel                       │
│ ◀ [Course Card] [Course Card] [Course Card] ▶        │
├───────────────────────────────────────────────────────┤
│ FOOTER                                                │
└───────────────────────────────────────────────────────┘
```

#### Visual Design

**Page Header:**
- Title: "Shopping Cart" + item count badge
- Breadcrumb navigation
- Background: white
- Border bottom: gray-200

**Cart Items:**
- Each item: White card with padding 24px
- Border: gray-200, border-radius 12px
- Spacing: 16px between items
- Thumbnail: 120px × 67px (16:9), left side
- Info: Right side, flexible width
- Actions: Bottom-right (Remove, Save for Later)

**Item Content:**
- Title: text-lg, font-semibold, gray-900
- Instructor: text-sm, gray-600
- Metadata: Rating + reviews + duration
- Price: Large text-2xl, primary-red
- Original price: Strikethrough, gray-400, text-base

**Action Buttons:**
- Remove: text-sm, red color, no bg, hover underline
- Save for Later: text-sm, primary-red, no bg, hover underline
- Icon: Trash, Bookmark

**Order Summary Card:**
- Background: gray-50
- Border: 2px solid gray-200
- Border-radius: 12px
- Padding: 24px
- Sticky at 100px from top (desktop)

**Summary Content:**
- Title: text-xl, font-semibold
- Line items: Flex between, text-base
- Divider: gray-300, 1px
- Total: text-3xl, font-bold, gray-900
- Subtotal, Discount (green), Tax (if applicable)

**Promo Code Input:**
- Input + button combo
- Input: form-input, placeholder "Enter promo code"
- Button: btn-secondary, "Apply"
- Success: Green badge "✓ SAVE20 applied • You saved $10"
- Error: Red text "Invalid code. Try again."

**Checkout Button:**
- btn-primary, btn-lg, full-width
- Text: "Proceed to Checkout"
- Icon: Shopping bag or arrow-right

**Trust Badges:**
- Icons + text, text-sm, gray-600
- Examples:
  - 🔒 Secure checkout
  - 💯 30-day money-back guarantee
  - 🎓 Lifetime access

**Empty Cart State:**
- Illustration: Shopping cart with sad face (or custom SVG)
- Text: "Your cart is empty"
- Subtitle: "Looks like you haven't added any courses yet"
- CTA: "Browse Courses" button (btn-primary)
- Recommended courses: 3-card carousel

#### Interactive Elements

**Animations:**
- Remove item: Slide out left + fade, then collapse
- Add item: Slide in from right + fade
- Promo apply: Loading spinner → success/error animation
- Total update: Number count-up animation
- Undo toast: Slide in from top, auto-dismiss 5s

**Interactions:**
- Remove: Confirm with toast "Item removed • Undo" (5s timeout)
- Undo: Re-add item to cart with animation
- Save for Later: Move to wishlist, show toast
- Promo code: Validate on Apply, update prices in real-time
- Quantity: If courses had quantity (N/A for courses)

**States:**
- Loading: Skeleton for cart items
- Empty: Empty state illustration + CTAs
- Error: Alert banner if cart fetch fails
- Processing: Disable buttons during checkout redirect

#### Content Strategy

**Headlines:**
- Page: "Shopping Cart (3 items)"
- Summary: "Order Summary"
- Empty: "Your cart is empty"

**Copy:**
- Remove confirmation: "Course removed from cart"
- Undo: "Undo" button in toast
- Promo success: "✓ Promo code applied! You saved $10"
- Promo error: "Invalid code. Please try again."
- Trust: "30-Day Money-Back Guarantee • Secure Checkout • Lifetime Access"

**CTA:**
- Primary: "Proceed to Checkout"
- Secondary: "Continue Shopping"
- Empty: "Browse Courses"

#### Responsive Behavior

**Desktop (1024px+):**
- 70/30 split layout
- Sticky sidebar
- Remove buttons always visible

**Tablet (768px-1023px):**
- Full-width cart items
- Summary below items
- Sticky checkout button at bottom

**Mobile (<768px):**
- Single column
- Smaller thumbnails (80px)
- Summary at top (above items)
- Sticky floating checkout button (fixed bottom)
- Swipe to remove gesture

#### Accessibility

- Landmark: `<main>`
- Heading: H1 "Shopping Cart"
- Buttons: Clear labels "Remove [Course Name] from cart"
- Form: Promo code input with label
- Toast: ARIA live region for announcements
- Focus: Trap focus in remove confirmation
- Keyboard: Tab order, Enter to activate
- Screen reader: Announce price updates

#### Performance

**Optimization:**
- Cart data: Fetch on mount
- Promo validation: Debounce 500ms
- Price updates: Optimistic UI + rollback on error
- Thumbnails: Lazy load, WebP format
- Related courses: Lazy load below fold

**Loading Strategy:**
1. Show skeleton immediately
2. Fetch cart data from API
3. Render items
4. Lazy load related courses
5. Total time to interactive: <1.5 seconds

**API Calls:**
- GET /api/cart (with auth token or session)
- POST /api/cart/promo (validate promo code)
- DELETE /api/cart/items/{id} (remove item)
- POST /api/cart/items (add item)

---

### 6. About Us Page (`/about`)

**Route:** `/about`
**Page Type:** Public, Informational
**Priority:** MEDIUM (P2)

#### Purpose
Tell InsightLearn's story, mission, team, and values. Build trust and credibility with potential students.

#### WOW Factor
- **Animated mission statement** that types out on scroll
- **Team photo grid** with hover overlay bios
- **Milestone timeline** with scroll-triggered animations
- **Video testimonial** from founder
- **Interactive values cards** that flip on hover
- **Live student map** showing global reach
- **Behind-the-scenes photo gallery**

#### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HEADER                                          │
├─────────────────────────────────────────────────┤
│ HERO                                            │
│ "Empowering Learners. Transforming Futures."    │
│ [Founder Video or Hero Image]                   │
├─────────────────────────────────────────────────┤
│ MISSION SECTION                                 │
│ "Our Mission" + animated text                   │
├─────────────────────────────────────────────────┤
│ STORY TIMELINE                                  │
│ 2019: Founded • 2020: 10K students • etc.       │
├─────────────────────────────────────────────────┤
│ VALUES SECTION (3-column grid)                  │
│ [Quality] [Accessibility] [Community]           │
├─────────────────────────────────────────────────┤
│ TEAM SECTION                                    │
│ "Meet Our Team"                                 │
│ [Photo Grid with hover bios]                    │
├─────────────────────────────────────────────────┤
│ GLOBAL REACH MAP                                │
│ Animated map with student locations             │
├─────────────────────────────────────────────────┤
│ CTA: "Join Our Community"                       │
│ [Get Started] button                            │
├─────────────────────────────────────────────────┤
│ FOOTER                                          │
└─────────────────────────────────────────────────┘
```

#### Visual Design

**Hero:**
- Full-width, 600px height
- Video or image background
- Overlay: rgba(0,0,0,0.4)
- Title: text-6xl, white, centered
- Subtitle: text-xl, white, centered
- Play button for founder video

**Mission Section:**
- Centered text, max-width 800px
- Title: text-5xl, font-bold
- Body: text-xl, gray-700, line-height 1.75
- Animation: Type-writer effect or fade-in

**Timeline:**
- Vertical or horizontal
- Milestones with icons
- Animated on scroll (fade in + scale)

**Values Cards:**
- 3-column grid
- Cards flip on hover to show detail
- Icons: Large, centered
- Front: Icon + title
- Back: Description

**Team Grid:**
- 4-column grid (desktop)
- Photo: Grayscale → color on hover
- Overlay: Name + title + social links
- Modal: Click for full bio

**Map:**
- Interactive map showing student distribution
- Animated dots for locations
- Stats overlay: "150 countries, 500K+ students"

#### Content Strategy

**Mission Statement:**
"We believe education should be accessible to everyone, everywhere. Our mission is to empower learners worldwide with high-quality, affordable courses taught by industry experts."

**Story:**
- Founded in 2019 by [Name]
- Started with 5 courses, now 1,200+
- 500,000+ students in 150 countries
- Partnerships with top universities

**Values:**
1. **Quality First:** Rigorous instructor vetting, high production standards
2. **Accessibility:** Affordable pricing, mobile access, offline downloads
3. **Community:** Supportive learner community, discussion forums, peer reviews

**Team:**
- Founders: Photos + bios
- Key team members: Product, Engineering, Content
- Advisors: Industry leaders

#### Interactive Elements

- Scroll animations: Fade, slide, scale
- Video: Autoplay (muted) or play button
- Timeline: Animated progress line
- Map: Interactive hover/click
- Values cards: 3D flip effect

#### Responsive Behavior

**Desktop:** Full layout
**Tablet:** 2-column grids
**Mobile:** Single column, stacked sections

#### Accessibility

- Video: Captions, transcript
- Alt text: All images
- Heading structure: H1 → H2 → H3
- ARIA labels for interactive elements

#### Performance

- Lazy load images and video
- Intersection observer for animations
- Total time to interactive: <2 seconds

---

### 7. My Courses Page (`/my-courses`)

**Route:** `/my-courses`
**Page Type:** Authenticated, User Dashboard
**Priority:** HIGH (P1)

#### Purpose
Dashboard for enrolled courses showing progress, continue learning, and certificates.

#### WOW Factor
- **Progress visualization** - Circular progress rings for each course
- **Continue learning** smart suggestions based on last accessed
- **Achievement badges** earned from course completion
- **Learning streak** gamification
- **Downloadable certificates** with animation
- **Personalized recommendations** "You might also like"

#### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HEADER                                          │
├─────────────────────────────────────────────────┤
│ Page Header                                     │
│ "My Courses" (5 enrolled)                       │
│ [Tabs: All | In Progress | Completed | Saved]  │
├─────────────────────────────────────────────────┤
│ Learning Stats Cards (3-col grid)               │
│ [Hours Learned] [Courses Completed] [Streak]    │
├─────────────────────────────────────────────────┤
│ Continue Learning                               │
│ [Last Accessed Course Card - large]             │
├─────────────────────────────────────────────────┤
│ My Courses Grid (3-col)                         │
│ ┌──────────┐ ┌──────────┐ ┌──────────┐        │
│ │ Course 1 │ │ Course 2 │ │ Course 3 │        │
│ │ Progress │ │ Progress │ │ Progress │        │
│ │ 45% ●    │ │ 78% ●    │ │ 100% ✓   │        │
│ │[Continue]│ │[Continue]│ │[Certif.] │        │
│ └──────────┘ └──────────┘ └──────────┘        │
├─────────────────────────────────────────────────┤
│ Recommended For You                             │
│ [Course Cards Carousel]                         │
├─────────────────────────────────────────────────┤
│ FOOTER                                          │
└─────────────────────────────────────────────────┘
```

#### Visual Design

**Stats Cards:**
- 3-column metric cards
- Large numbers (text-4xl)
- Icons: Clock, graduation cap, flame
- Animated count-up on load

**Continue Learning Card:**
- Large horizontal card
- Thumbnail left, info right
- Progress bar prominent
- "Continue" CTA button
- Time remaining: "2h 30m left"

**Course Cards:**
- Same as catalog cards + progress overlay
- Circular progress ring around thumbnail
- Percentage text in center
- Status badge: "In Progress" | "Completed" | "Not Started"
- CTA: "Continue Learning" | "Start Course" | "View Certificate"

**Tabs:**
- Horizontal tabs below header
- Active: underline + primary-red
- Filters: All, In Progress, Completed, Saved

**Certificates:**
- Preview thumbnail
- Download button: "Download Certificate (PDF)"
- Share button: "Share on LinkedIn"

#### Interactive Elements

- Progress rings: Animated on load
- Hover: Card lift + shadow
- Click: Navigate to course player
- Certificate: Download with success toast
- Streak: Confetti animation on milestone

#### Content Strategy

**Headlines:**
- "My Courses" + count
- "Continue Learning"
- "Your Learning Stats"
- "Recommended For You"

**Copy:**
- Motivational: "You're doing great! Keep learning."
- Empty state: "You haven't enrolled in any courses yet"

#### Responsive Behavior

**Desktop:** 3-column grid
**Tablet:** 2-column
**Mobile:** Single column

#### Accessibility

- Progress: ARIA valuenow, valuemin, valuemax
- Buttons: Clear labels
- Keyboard navigation

#### Performance

- Paginate courses (12 at a time)
- Lazy load thumbnails
- Cache certificate data

---

### 8. User Profile Page (`/profile`)

**Route:** `/profile`
**Page Type:** Authenticated, User Account
**Priority:** HIGH (P1)

#### Purpose
View and edit user profile information, avatar, bio, social links, and public profile settings.

#### WOW Factor
- **Live avatar preview** when uploading
- **Badge showcase** with earned achievements
- **Activity timeline** showing learning history
- **Public profile toggle** with preview
- **Social proof stats** (courses completed, hours learned)
- **Editable inline fields** (no separate edit mode)

#### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HEADER                                          │
├─────────────────────────────────────────────────┤
│ Cover Photo (editable)                          │
├─────────────────────────────────────────────────┤
│ Profile Header                                  │
│ [Avatar] Name                                   │
│          Title/Bio                              │
│          [Edit Profile Button]                  │
├──────────┬──────────────────────────────────────┤
│ SIDEBAR  │ MAIN CONTENT (Tabs)                  │
│ Stats    │ ┌─[About]─[Activity]─[Certificates]─┐│
│ ├─ Badge │ │                                   ││
│ ├─ Cours │ │ About Section:                    ││
│ └─ Hours │ │ • Full Name [edit inline]         ││
│          │ │ • Email [readonly]                ││
│ Public   │ │ • Bio [textarea]                  ││
│ Profile  │ │ • Location                        ││
│ Toggle   │ │ • Website                         ││
│          │ │ • Social Links                    ││
│          │ │                                   ││
│          │ └───────────────────────────────────┘│
└──────────┴──────────────────────────────────────┘
│ FOOTER                                          │
└─────────────────────────────────────────────────┘
```

#### Visual Design

**Cover Photo:**
- Height: 280px
- Upload: Hover overlay with "Change Cover" button
- Default: Gradient matching brand

**Avatar:**
- Size: 150px circle
- Positioned over cover photo (overlapping)
- Upload: Click to upload, live preview
- Default: Initials with color

**Profile Header:**
- Name: text-4xl, font-bold
- Title: text-lg, gray-600 (e.g., "Full Stack Developer")
- Bio: text-base, gray-700, 2-3 lines
- Edit button: btn-secondary, top-right

**Sidebar Stats:**
- Metric cards: Small, stacked
- Icons + numbers
- Examples:
  - 🎓 12 courses completed
  - ⏱️ 84 hours learned
  - 🏆 8 certificates
  - 🔥 15-day streak

**Tabs:**
- About | Activity | Certificates | Settings
- Active: underline + primary-red

**About Tab:**
- Form fields: Inline editing
- Click field → becomes editable
- Save button appears on change
- Cancel button to revert

**Activity Tab:**
- Timeline of learning activity
- Completed courses, certificates earned
- Chronological order

**Certificates Tab:**
- Grid of certificate thumbnails
- Download and share options

**Settings Tab:**
- Public profile toggle
- Privacy settings
- Email preferences
- Delete account (red danger zone)

#### Interactive Elements

- Avatar upload: Drag-drop or click
- Cover upload: Same
- Inline edit: Click field → edit → save
- Preview: "View Public Profile" link
- Badge hover: Tooltip with description

#### Content Strategy

**Headlines:**
- "My Profile"
- "About Me"
- "Learning Activity"
- "My Certificates"

**Empty States:**
- No bio: "Tell us about yourself"
- No activity: "Start learning to see your activity"

#### Responsive Behavior

**Desktop:** Sidebar + main content
**Tablet:** Stacked layout
**Mobile:** Single column

#### Accessibility

- Form labels: Clear and associated
- File upload: Keyboard accessible
- Error messages: Announced
- Focus management

#### Performance

- Avatar upload: Client-side resize before upload
- Image optimization
- Debounce auto-save

---

### 9. User Settings Page (`/settings`)

**Route:** `/settings`
**Page Type:** Authenticated, User Account
**Priority:** MEDIUM (P2)

#### Purpose
Manage account settings, notifications, privacy, billing, and preferences.

#### WOW Factor
- **Instant save feedback** with success animations
- **Dark mode toggle** with smooth transition
- **Notification preferences matrix** (channel × type)
- **Password strength meter** with real-time feedback
- **Two-factor authentication** setup wizard
- **Account data export** with progress indicator

#### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HEADER                                          │
├─────────────────────────────────────────────────┤
│ Page Header: "Settings"                         │
├──────────┬──────────────────────────────────────┤
│ SIDEBAR  │ MAIN CONTENT                         │
│ Nav Menu │                                      │
│ ├─Account│ [Selected Section Content]           │
│ ├─Securit│                                      │
│ ├─Notific│ Example: Account Section             │
│ ├─Privacy│ ┌─────────────────────────────────┐ │
│ ├─Billing│ │ Profile Information             │ │
│ └─Prefere│ │ • Email: user@example.com       │ │
│          │ │ • Phone: +1 (555) 123-4567      │ │
│          │ │ [Save Changes]                  │ │
│          │ └─────────────────────────────────┘ │
│          │                                      │
│          │ ┌─────────────────────────────────┐ │
│          │ │ Language & Region               │ │
│          │ │ • Language: English             │ │
│          │ │ • Timezone: PST                 │ │
│          │ └─────────────────────────────────┘ │
└──────────┴──────────────────────────────────────┘
│ FOOTER                                          │
└─────────────────────────────────────────────────┘
```

#### Sections

**1. Account:**
- Email (change with verification)
- Phone number
- Language
- Timezone
- Delete account

**2. Security:**
- Change password (with strength meter)
- Two-factor authentication toggle
- Active sessions list
- Trusted devices

**3. Notifications:**
- Email notifications (matrix)
- Push notifications
- SMS notifications
- Notification frequency

**4. Privacy:**
- Public profile toggle
- Learning activity visibility
- Certificates visibility
- Data sharing preferences

**5. Billing:**
- Payment methods
- Billing history
- Invoices (download)
- Subscription management

**6. Preferences:**
- Theme: Light | Dark | Auto
- Playback speed default
- Autoplay next lesson
- Subtitle language
- Video quality

#### Visual Design

**Sidebar:**
- Width: 240px
- Sticky navigation
- Active: Background gray-100, primary-red indicator

**Content Sections:**
- White cards with sections
- Form fields: Standard design system
- Save buttons: Bottom-right of each section
- Success: Green check toast

**Password Strength Meter:**
- Progress bar under input
- Colors: Red (weak) → Yellow (medium) → Green (strong)
- Text: "Weak" | "Medium" | "Strong"

**2FA Setup:**
- Step-by-step wizard
- QR code display
- Verification code input
- Backup codes download

#### Interactive Elements

- Save: Loading spinner → success animation
- Delete account: Confirmation modal
- Theme toggle: Smooth color transition
- 2FA: Modal flow

#### Content Strategy

**Section Headers:** Clear action-oriented
**Help Text:** Inline explanations
**CTAs:** "Save Changes", "Enable 2FA", etc.

#### Responsive Behavior

**Desktop:** Sidebar + content
**Tablet:** Tabs instead of sidebar
**Mobile:** Accordion sections

#### Accessibility

- Form labels
- Error messages
- Focus management
- Keyboard navigation

#### Performance

- Auto-save: Debounce 1 second
- Optimistic UI updates
- Rollback on error

---

### 10. Search Results Page (`/search`)

**Route:** `/search?q={query}`
**Page Type:** Public, Discovery
**Priority:** HIGH (P1)

#### Purpose
Display search results for courses, instructors, and categories. Multi-faceted search with filters.

#### WOW Factor
- **Instant search** with results-as-you-type
- **Multi-tab results** (Courses | Instructors | Categories)
- **Search suggestions** dropdown with autocomplete
- **Highlighted keywords** in results
- **Search history** (logged-in users)
- **Did you mean?** suggestion for misspellings
- **Empty state** with popular searches and categories

#### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HEADER (with large search bar)                  │
├─────────────────────────────────────────────────┤
│ Search: "web development" [X] [Search Button]   │
│ Results for "web development" (342 found)       │
│ [Tabs: Courses (300) | Instructors (35) | Cat(7)]│
├────────────────────────────────────┬────────────┤
│ SEARCH RESULTS                     │ SIDEBAR    │
│                                    │ Filters    │
│ ┌──────────────────────────────┐  │ ├─ Level   │
│ │ Course Card 1                │  │ ├─ Price   │
│ │ "web development" highlighted │  │ ├─ Rating  │
│ └──────────────────────────────┘  │ └─ Duration│
│ ┌──────────────────────────────┐  │            │
│ │ Course Card 2                │  │ Related    │
│ └──────────────────────────────┘  │ Searches:  │
│ ...                                │ • react    │
│ [Load More]                        │ • node.js  │
└────────────────────────────────────┴────────────┘
│ FOOTER                                          │
└─────────────────────────────────────────────────┘
```

#### Visual Design

**Search Bar:**
- Large: 600px wide
- Placeholder: "Search courses, topics, instructors..."
- Clear button (X) when text exists
- Search button: Primary CTA

**Results Header:**
- Query display: "Results for 'web development'"
- Count: "(342 courses found)"
- Sort: Dropdown (Relevance, Most Popular, Highest Rated, Newest)

**Tabs:**
- Courses | Instructors | Categories
- Count badges: "Courses (300)"
- Active: underline + primary-red

**Result Cards:**
- Same as course catalog cards
- Keyword highlight: Bold + background yellow
- Relevance score: Optional (for debugging)

**Suggestions:**
- Dropdown below search bar
- Recent searches (icon: clock)
- Popular searches (icon: fire)
- Categories (icon: folder)

**Empty State:**
- Illustration: Magnifying glass + sad face
- Text: "No results found for 'xyz'"
- Suggestions:
  - "Did you mean: 'web development'?"
  - "Try these popular searches:"
  - "Browse all categories"

#### Interactive Elements

- Search: Debounce 300ms, instant results
- Clear button: Clear input + results
- Filter: Apply instantly
- Tab switch: Smooth transition
- Highlight: Animated fade-in

#### Content Strategy

**Headlines:**
- "Search Results"
- "Results for '[query]' (X found)"

**Empty State Copy:**
- "No results found"
- "Did you mean...?"
- "Try searching for..."

**Popular Searches:**
- React
- Python
- Data Science
- UI/UX Design
- (dynamically generated)

#### Responsive Behavior

**Desktop:** Full layout
**Tablet:** Sidebar below results
**Mobile:** Single column, filters overlay

#### Accessibility

- Search input: aria-label, autocomplete="off"
- Results: aria-live="polite" for count updates
- Suggestions: ARIA combobox pattern
- Keyboard: Arrow keys for suggestions

#### Performance

- Debounced search: 300ms
- Pagination: 12 results at a time
- Cache: Recent searches cached
- Indexing: Elasticsearch for fast search

---

### 11. Contact Us Page (`/contact`)

**Route:** `/contact`
**Page Type:** Public, Support
**Priority:** MEDIUM (P2)

#### Purpose
Provide contact options for support, sales, partnerships, and general inquiries.

#### WOW Factor
- **Smart form routing** - Inquiry type determines recipient
- **AI-powered response time** - Shows estimated response time
- **Live chat widget** integration
- **FAQ suggestions** - As user types, suggest relevant FAQs
- **Office locations** interactive map
- **Social media wall** - Latest tweets/posts
- **Submit confirmation** with animation

#### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HEADER                                          │
├─────────────────────────────────────────────────┤
│ HERO                                            │
│ "Get in Touch"                                  │
│ "We're here to help. Reach out anytime."       │
├──────────────────────┬──────────────────────────┤
│ CONTACT FORM (60%)   │ SIDEBAR (40%)            │
│                      │                          │
│ Select Inquiry Type: │ Quick Contact Info:      │
│ ○ General Support    │ 📧 support@...           │
│ ○ Technical Issue    │ 📞 +1 (800) 123-4567     │
│ ○ Sales/Partnerships │ 💬 Live Chat (online)    │
│ ○ Billing Question   │                          │
│                      │ Office Hours:            │
│ Your Name*           │ Mon-Fri: 9am-6pm PST     │
│ [__________________] │                          │
│                      │ Helpful Links:           │
│ Email Address*       │ • Help Center            │
│ [__________________] │ • FAQs                   │
│                      │ • System Status          │
│ Subject*             │                          │
│ [__________________] │ Follow Us:               │
│                      │ [Twitter] [LinkedIn]     │
│ Message*             │                          │
│ [                  ] │                          │
│ [                  ] │                          │
│ [                  ] │                          │
│                      │                          │
│ [ ] I agree to T&C   │                          │
│                      │                          │
│ [Send Message]       │                          │
└──────────────────────┴──────────────────────────┘
│ Office Locations Map                            │
├─────────────────────────────────────────────────┤
│ FAQ Section: "Common Questions"                 │
│ [Accordion with 5-6 top questions]              │
├─────────────────────────────────────────────────┤
│ FOOTER                                          │
└─────────────────────────────────────────────────┘
```

#### Visual Design

**Hero:**
- Height: 300px
- Background: Gradient (gray-50 to white)
- Title: text-5xl, font-bold
- Subtitle: text-xl, gray-600

**Contact Form:**
- White card, padding 40px
- Border-radius: 12px
- Shadow: md
- Fields: Standard form-input design
- Labels: Semibold with required asterisk
- Textarea: 150px height minimum

**Inquiry Type:**
- Radio buttons or dropdown
- Icons for each type
- Changes form route/recipient

**Submit Button:**
- btn-primary, btn-lg, full-width
- Loading: Spinner during submission
- Success: Checkmark animation

**Sidebar:**
- Sticky at 100px from top
- White card background
- Icons + text for contact methods
- Live chat: Green "online" indicator
- Social links: Icon buttons

**Success State:**
- Replace form with success message
- Checkmark animation (green circle + check)
- "Thank you! We'll respond within 24 hours."
- "Return to Home" button

**Map:**
- Interactive map (Google Maps or Mapbox)
- Pins for office locations
- Click pin: Show address + hours

**FAQ Accordion:**
- Standard accordion component
- 5-6 most common questions
- Link to full FAQ page

#### Interactive Elements

- Form validation: Real-time on blur
- Inquiry type: Shows estimated response time
- Live chat: Click to open widget
- Map: Interactive zoom/pan
- FAQ: Expand/collapse
- Success: Confetti animation

#### Content Strategy

**Inquiry Types:**
1. General Support → support@insightlearn.cloud
2. Technical Issue → tech@insightlearn.cloud
3. Sales/Partnerships → sales@insightlearn.cloud
4. Billing Question → billing@insightlearn.cloud
5. Press/Media → press@insightlearn.cloud

**Contact Info:**
- Email: support@insightlearn.cloud
- Phone: +1 (800) 123-LEARN
- Live Chat: Available Mon-Fri 9am-6pm PST
- Office: 123 Learning St, San Francisco, CA 94105

**FAQs:**
1. How do I reset my password?
2. How do I enroll in a course?
3. What payment methods do you accept?
4. Can I get a refund?
5. How do I contact an instructor?
6. Are certificates accredited?

#### Responsive Behavior

**Desktop:** Form + sidebar
**Tablet:** Stacked layout
**Mobile:** Single column, sticky chat button

#### Accessibility

- Form labels: Associated with inputs
- Error messages: aria-describedby
- Required: aria-required="true"
- Submit: Disabled until valid
- Focus management: First field on load

#### Performance

- Form submission: Async with loading state
- Map: Lazy load below fold
- FAQ: Load expanded content on demand
- Total time to interactive: <1.5 seconds

**API:**
- POST /api/contact (form submission)
- Rate limiting: 5 submissions per hour per IP

---

### 12. FAQ Page (`/faq`)

**Route:** `/faq`
**Page Type:** Public, Support
**Priority:** MEDIUM (P2)

#### Purpose
Comprehensive frequently asked questions organized by category with search functionality.

#### WOW Factor
- **Smart search** with instant filtering
- **Category tabs** with emoji icons
- **Expandable answers** with smooth animations
- **Helpful voting** (Was this helpful? Yes/No)
- **Related questions** suggestions
- **Can't find answer?** CTA with direct contact form
- **Print-friendly** version option

#### Layout Structure

```
┌─────────────────────────────────────────────────┐
│ HEADER                                          │
├─────────────────────────────────────────────────┤
│ HERO                                            │
│ "Frequently Asked Questions"                    │
│ [Search FAQs...] 🔍                             │
├─────────────────────────────────────────────────┤
│ Category Tabs (horizontal scroll)               │
│ [All] [🎓 Courses] [💳 Billing] [🔐 Account]   │
│ [📱 Technical] [🎓 Certificates]                │
├─────────────────────────────────────────────────┤
│ FAQ List (Accordion)                            │
│                                                 │
│ 🎓 Courses (12)                                 │
│ ▼ How do I enroll in a course?                 │
│   Answer text with links...                     │
│   [👍 Helpful (42)] [👎 Not Helpful (3)]       │
│                                                 │
│ ▶ How do I access my courses?                  │
│                                                 │
│ ▶ Can I download course videos?                │
│                                                 │
│ 💳 Billing (8)                                  │
│ ▶ What payment methods do you accept?          │
│ ▶ How do refunds work?                         │
│ ...                                             │
├─────────────────────────────────────────────────┤
│ CTA Section: "Didn't find your answer?"        │
│ [Contact Support] button                        │
├─────────────────────────────────────────────────┤
│ FOOTER                                          │
└─────────────────────────────────────────────────┘
```

#### Visual Design

**Hero:**
- Height: 280px
- Background: Gradient
- Title: text-5xl, centered
- Search bar: 600px, centered, large
- Placeholder: "Search for answers..."

**Search Bar:**
- Large input with icon
- Instant filter as you type
- Clear button (X)
- Results count: "Showing 12 results"

**Category Tabs:**
- Horizontal scroll
- Emoji + text labels
- Active: primary-red background, white text
- Inactive: gray background
- Badge: Question count "(12)"

**FAQ Accordion:**
- Grouped by category
- Category header: text-2xl, font-bold, with count
- Question: text-lg, font-semibold, click to expand
- Answer: text-base, gray-700, padding 20px, background gray-50
- Icon: Chevron rotates 90° on expand

**Helpful Voting:**
- Below each answer
- Text: "Was this helpful?"
- Buttons: Thumbs up | Thumbs down
- Count displayed: "(42)"
- Voted: Button turns green/red

**CTA Section:**
- Background: gray-100
- Title: text-3xl
- Button: btn-primary, btn-lg

#### Categories

1. **Courses** (🎓)
   - How do I enroll?
   - How do I access courses?
   - Can I download videos?
   - How long do I have access?
   - Can I share my account?

2. **Billing** (💳)
   - Payment methods accepted?
   - How do refunds work?
   - Can I change payment method?
   - Do you offer discounts?
   - What currency is pricing in?

3. **Account** (🔐)
   - How do I reset password?
   - How do I change email?
   - Can I delete my account?
   - How do I update profile?
   - What is 2FA?

4. **Technical** (📱)
   - Supported browsers?
   - Mobile app available?
   - Video playback issues?
   - How do I report a bug?
   - System requirements?

5. **Certificates** (🎓)
   - Are certificates accredited?
   - How do I get certificate?
   - Can I verify certificates?
   - How do I share on LinkedIn?
   - Can I retake for better score?

6. **Instructors** (🎓)
   - How do I become instructor?
   - How is revenue shared?
   - What support is provided?
   - Course approval process?

#### Interactive Elements

- Search: Debounce 300ms, filter questions
- Accordion: Smooth expand/collapse 300ms
- Helpful voting: AJAX submit, disable after vote
- Category tabs: Smooth scroll, auto-scroll to active
- Scroll to top: Button appears after 500px scroll

#### Content Strategy

**Search Placeholder:**
- "Search for answers..."
- "What can we help you with?"

**Empty Search:**
- "No results found for 'xyz'"
- "Try different keywords"
- Popular questions suggestions

**Related Questions:**
- Below each answer: "Related: [Q1] [Q2] [Q3]"

#### Responsive Behavior

**Desktop:** Full layout
**Tablet:** Narrower search, 2-column tabs
**Mobile:** Single column, tabs scroll horizontally

#### Accessibility

- Accordion: ARIA expanded states
- Search: aria-live for results count
- Heading structure: H1 → H2 (category) → H3 (question)
- Keyboard: Tab to question, Enter to expand
- Focus: Visible indicators

#### Performance

- Static content (pre-render)
- Search: Client-side filtering
- Lazy render: Only render visible accordions
- Total time to interactive: <1 second

---

## Implementation Priorities

### Phase 1: Critical Path (P0) - Week 1-2

**Revenue-impacting pages:**
1. `/courses` - Browse Courses
2. `/courses/{id}` - Course Detail
3. `/cart` - Shopping Cart
4. `/my-courses` - My Enrolled Courses

**Why:** These pages directly impact enrollment and revenue. Without them, users cannot discover, purchase, or access courses.

### Phase 2: Core Experience (P1) - Week 3-4

**User engagement pages:**
5. `/categories` - All Categories
6. `/categories/{slug}` - Category Detail
7. `/profile` - User Profile
8. `/search` - Search Results

**Why:** These pages enhance discoverability and user engagement. Essential for retention and course completion.

### Phase 3: Support & Trust (P2) - Week 5-6

**Supporting pages:**
9. `/settings` - User Settings
10. `/about` - About Us
11. `/contact` - Contact Us
12. `/faq` - FAQ Page

**Why:** These pages build trust and provide support. Important for user satisfaction but not critical for initial launch.

### Phase 4: Growth & Optimization (P3) - Week 7+

**Growth pages:**
13. `/instructors` - Become an Instructor
14. `/enterprise` - Enterprise Solutions
15. `/accessibility` - Accessibility Statement
16. `/instructor/courses` - Instructor Course List
17. `/instructor/courses/create` - Create Course (Instructor)

**Why:** These pages support growth initiatives and compliance. Can be added after core functionality is stable.

---

## Design System Checklist

**Before starting each page, ensure:**

✅ Color variables from design-system-base.css
✅ Typography scale and weights
✅ Spacing system (multiples of 4px)
✅ Component patterns (buttons, cards, forms)
✅ Responsive breakpoints (1024px, 768px)
✅ Accessibility (WCAG 2.1 AA)
✅ Performance (lazy loading, optimization)
✅ Animations (consistent timing, reduced motion)
✅ Loading states (skeletons, spinners)
✅ Empty states (helpful CTAs)
✅ Error states (clear recovery paths)

---

## Next Steps for Frontend Developer

### For Each Missing Page:

1. **Read the specification** in this document
2. **Review the individual page spec** in `/docs/design/pages/[page-name].md`
3. **Check existing similar pages** for patterns to reuse
4. **Create Razor component** in `/src/InsightLearn.WebAssembly/Pages/`
5. **Implement using design system** components and utilities
6. **Test responsive behavior** on all breakpoints
7. **Test accessibility** with keyboard and screen reader
8. **Test performance** (Lighthouse score >90)
9. **Create pull request** with screenshots and test results

### Resources

- **Design System:** `/src/InsightLearn.WebAssembly/wwwroot/css/design-system-*.css`
- **Component Library:** `/src/InsightLearn.WebAssembly/Components/`
- **Existing Pages:** `/src/InsightLearn.WebAssembly/Pages/` for reference
- **API Endpoints:** Documented in `CLAUDE.md` (database-driven)

---

## Questions?

**Slack:** #insightlearn-dev
**Email:** dev-team@insightlearn.cloud
**Documentation:** Refer to `CLAUDE.md` for architecture details

---

**Document Version:** 1.0
**Last Updated:** 2025-11-07
**Author:** Claude Code (UI/UX Design Analysis)
**Reviewed By:** Pending Frontend Team Review
