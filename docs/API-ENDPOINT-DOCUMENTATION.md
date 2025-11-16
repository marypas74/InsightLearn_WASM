# InsightLearn API - Complete Endpoint Documentation (46 Endpoints)

**Generated**: 2025-11-16
**Version**: 1.6.7-dev
**Purpose**: Comprehensive XML documentation for all 46 API endpoints in Program.cs

---

## Authentication Endpoints (6 total)

### 1. POST /api/auth/login

```xml
/// <summary>
/// Authenticates a user and returns a JWT access token
/// </summary>
/// <param name="loginDto">User credentials (email and password)</param>
/// <param name="httpContext">HTTP context for IP tracking</param>
/// <param name="authService">Authentication service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Authentication result with JWT token and user information</returns>
/// <response code="200">Login successful - returns JWT token, user details, and session information</response>
/// <response code="400">Login failed - invalid credentials, account locked, or validation errors</response>
/// <response code="429">Rate limit exceeded - maximum 5 login attempts per minute per IP (brute force protection)</response>
/// <response code="500">Internal server error occurred during authentication</response>
```

**Sample Request**:
```json
POST /api/auth/login
{
  "email": "student@example.com",
  "password": "SecurePass123!"
}
```

**Sample Response (200)**:
```json
{
  "isSuccess": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "student@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "roles": ["Student"],
  "sessionId": "sess_abc123",
  "expiresAt": "2025-11-23T12:00:00Z"
}
```

---

### 2. POST /api/auth/register

```xml
/// <summary>
/// Registers a new user account on the platform
/// </summary>
/// <param name="registerDto">New user registration details (email, password, name, etc.)</param>
/// <param name="authService">Authentication service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Registration result with JWT token for immediate login</returns>
/// <response code="200">Registration successful - returns JWT token and user details</response>
/// <response code="400">Registration failed - validation errors, duplicate email, or weak password</response>
/// <response code="429">Rate limit exceeded - maximum 5 registration attempts per minute per IP</response>
/// <response code="500">Internal server error occurred during registration</response>
```

**Password Requirements (PCI DSS 8.2.3)**:
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

---

### 3. POST /api/auth/refresh

```xml
/// <summary>
/// Refreshes an expired or expiring JWT access token
/// </summary>
/// <param name="httpContext">HTTP context containing current JWT token in Authorization header</param>
/// <param name="authService">Authentication service</param>
/// <param name="logger">Logger instance</param>
/// <returns>New JWT token with extended expiration</returns>
/// <response code="200">Token refreshed successfully - returns new JWT token</response>
/// <response code="401">Unauthorized - invalid or missing token, or user no longer exists</response>
/// <response code="429">Rate limit exceeded - maximum 5 requests per minute per IP</response>
/// <response code="500">Internal server error occurred during token refresh</response>
```

**Authentication Required**: Yes (Bearer token)

---

### 4. GET /api/auth/me

```xml
/// <summary>
/// Gets the current authenticated user's profile information
/// </summary>
/// <param name="httpContext">HTTP context containing user JWT claims</param>
/// <param name="authService">Authentication service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Current user profile with all details</returns>
/// <response code="200">User profile retrieved successfully</response>
/// <response code="401">Unauthorized - missing or invalid JWT token</response>
/// <response code="404">User not found in database</response>
/// <response code="500">Internal server error occurred</response>
```

**Authentication Required**: Yes (Bearer token)

---

### 5. POST /api/auth/oauth-callback

```xml
/// <summary>
/// Handles Google OAuth 2.0 authentication callback
/// </summary>
/// <param name="googleLoginDto">Google OAuth credentials (ID token)</param>
/// <param name="authService">Authentication service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Authentication result with JWT token</returns>
/// <response code="200">OAuth login successful - returns JWT token and user details</response>
/// <response code="400">OAuth login failed - invalid Google token or user creation error</response>
/// <response code="429">Rate limit exceeded - maximum 5 requests per minute per IP</response>
/// <response code="500">Internal server error occurred during OAuth processing</response>
```

**Required Configuration**: `GOOGLE_CLIENT_ID` environment variable

---

### 6. POST /api/auth/complete-registration (NOT IMPLEMENTED)

```xml
/// <summary>
/// Completes OAuth registration flow by collecting additional user information
/// </summary>
/// <returns>501 Not Implemented - planned for future release</returns>
/// <response code="501">Endpoint not yet implemented</response>
```

---

## Chat (Chatbot) Endpoints (4 total)

### 7. POST /api/chat/message

```xml
/// <summary>
/// Sends a message to the AI chatbot and receives an intelligent response
/// </summary>
/// <param name="request">Chat message request with session ID and message text</param>
/// <param name="chatbotService">Chatbot service for AI processing</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for IP tracking</param>
/// <param name="cancellationToken">Cancellation token for async operation</param>
/// <returns>AI-generated response with conversation history</returns>
/// <response code="200">Response generated successfully - includes AI message and metadata</response>
/// <response code="400">Invalid request - missing message or session ID</response>
/// <response code="500">Internal server error - Ollama service unavailable or processing failed</response>
/// <response code="503">Service unavailable - Ollama AI model not loaded</response>
```

**AI Model**: Ollama qwen2:0.5b (lightweight, ~1.7s response time)
**Features**: Context-aware responses, conversation history, session management

---

### 8. GET /api/chat/history

```xml
/// <summary>
/// Retrieves chat conversation history for a specific session
/// </summary>
/// <param name="sessionId">Session identifier for conversation history</param>
/// <param name="limit">Maximum number of messages to retrieve (default: 50, max: 200)</param>
/// <param name="chatbotService">Chatbot service</param>
/// <param name="logger">Logger instance</param>
/// <returns>List of chat messages in chronological order</returns>
/// <response code="200">Chat history retrieved successfully</response>
/// <response code="400">Invalid session ID or limit parameter</response>
/// <response code="500">Internal server error occurred</response>
```

**Query Parameters**:
- `sessionId` (required): UUID or string session identifier
- `limit` (optional): Number of messages (1-200, default 50)

---

### 9. DELETE /api/chat/history/{sessionId}

```xml
/// <summary>
/// Deletes all chat history for a specific session (GDPR compliance)
/// </summary>
/// <param name="sessionId">Session identifier to delete</param>
/// <param name="chatbotService">Chatbot service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Confirmation of deletion</returns>
/// <response code="204">Chat history deleted successfully (no content)</response>
/// <response code="400">Invalid session ID format</response>
/// <response code="404">Session not found</response>
/// <response code="500">Internal server error occurred during deletion</response>
```

**Data Retention**: Implements GDPR "Right to be Forgotten"

---

### 10. GET /api/chat/health

```xml
/// <summary>
/// Checks the health status of the Ollama AI chatbot service
/// </summary>
/// <param name="ollamaService">Ollama service instance</param>
/// <param name="logger">Logger instance</param>
/// <returns>Health status of chatbot service</returns>
/// <response code="200">Chatbot service is healthy and ready</response>
/// <response code="503">Chatbot service unavailable - Ollama not responding</response>
```

**Health Check**: Tests Ollama connectivity and model availability

---

## Video Storage Endpoints (5 total)

### 11. POST /api/video/upload

```xml
/// <summary>
/// Uploads a video file to MongoDB GridFS with compression and validation
/// </summary>
/// <param name="file">Video file (MP4, WebM, OGG, MOV - max 500MB)</param>
/// <param name="lessonId">Associated lesson UUID</param>
/// <param name="userId">Uploader user UUID</param>
/// <param name="videoService">Video processing service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Upload result with file ID and metadata</returns>
/// <response code="200">Video uploaded successfully - returns MongoDB GridFS file ID</response>
/// <response code="400">Validation failed - invalid file type, size exceeded, or missing parameters</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="413">File too large - maximum 500MB</response>
/// <response code="500">Internal server error - MongoDB storage failure</response>
```

**Supported Formats**: MP4, WebM, OGG, MOV
**Max File Size**: 500MB
**Compression**: GZip (20-40% size reduction)
**Storage**: MongoDB GridFS with metadata indexing

---

### 12. GET /api/video/stream/{fileId}

```xml
/// <summary>
/// Streams a video file from MongoDB GridFS with HTTP range support
/// </summary>
/// <param name="fileId">MongoDB GridFS file identifier</param>
/// <param name="httpContext">HTTP context for range header support</param>
/// <param name="mongoVideoService">MongoDB video storage service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Video stream with partial content support (206)</returns>
/// <response code="200">Full video stream</response>
/// <response code="206">Partial content (range request)</response>
/// <response code="400">Invalid file ID format</response>
/// <response code="404">Video file not found</response>
/// <response code="500">Internal server error - streaming failure</response>
```

**Features**:
- HTTP Range header support (seek/resume)
- Content-Type detection
- Chunk-based streaming (low memory footprint)

---

### 13. GET /api/video/metadata/{fileId}

```xml
/// <summary>
/// Retrieves metadata for a video file (size, format, compression ratio, etc.)
/// </summary>
/// <param name="fileId">MongoDB GridFS file identifier</param>
/// <param name="mongoVideoService">MongoDB video storage service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Video metadata object</returns>
/// <response code="200">Metadata retrieved successfully</response>
/// <response code="400">Invalid file ID format</response>
/// <response code="404">Video file not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Metadata Fields**: fileId, filename, size, uploadDate, contentType, compressionRatio

---

### 14. DELETE /api/video/{videoId}

```xml
/// <summary>
/// Deletes a video file from MongoDB GridFS storage
/// </summary>
/// <param name="videoId">MongoDB GridFS file identifier</param>
/// <param name="mongoVideoService">MongoDB video storage service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Confirmation of deletion</returns>
/// <response code="204">Video deleted successfully (no content)</response>
/// <response code="400">Invalid video ID format</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - user does not own this video</response>
/// <response code="404">Video not found</response>
/// <response code="500">Internal server error occurred during deletion</response>
```

**Authorization**: Admin or video owner only

---

### 15. GET /api/video/upload/progress/{uploadId}

```xml
/// <summary>
/// Gets the upload progress for a video upload in progress
/// </summary>
/// <param name="uploadId">Upload session identifier</param>
/// <param name="videoService">Video processing service</param>
/// <returns>Upload progress percentage (0-100)</returns>
/// <response code="200">Upload progress retrieved</response>
/// <response code="404">Upload ID not found or upload completed</response>
```

**Use Case**: Client-side progress bars for large video uploads

---

## System Management Endpoints (4 total)

### 16. GET /api/system/endpoints

```xml
/// <summary>
/// Gets all API endpoints from database configuration (cached 60 minutes)
/// </summary>
/// <param name="endpointService">Endpoint configuration service</param>
/// <param name="logger">Logger instance</param>
/// <returns>All endpoints grouped by category</returns>
/// <response code="200">Endpoints retrieved successfully</response>
/// <response code="500">Internal server error - database unavailable</response>
```

**Caching**: MemoryCache 60 minutes
**Database**: SystemEndpoints table (46 endpoint records)

---

### 17. GET /api/system/endpoints/{category}

```xml
/// <summary>
/// Gets all API endpoints for a specific category
/// </summary>
/// <param name="category">Endpoint category (Auth, Chat, Video, Courses, etc.)</param>
/// <param name="endpointService">Endpoint configuration service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Endpoints for specified category</returns>
/// <response code="200">Endpoints retrieved successfully</response>
/// <response code="404">Category not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Categories**: Auth, Chat, Video, System, Categories, Courses, Enrollments, Payments, Reviews, Users, Dashboard

---

### 18. GET /api/system/endpoints/{category}/{key}

```xml
/// <summary>
/// Gets a specific API endpoint configuration by category and key
/// </summary>
/// <param name="category">Endpoint category</param>
/// <param name="key">Endpoint key (e.g., "Login", "SendMessage")</param>
/// <param name="endpointService">Endpoint configuration service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Endpoint configuration with path, HTTP method, and status</returns>
/// <response code="200">Endpoint retrieved successfully</response>
/// <response code="404">Endpoint not found</response>
/// <response code="500">Internal server error occurred</response>
```

---

### 19. POST /api/system/endpoints/refresh-cache

```xml
/// <summary>
/// Forces a refresh of the endpoint configuration cache (Admin only)
/// </summary>
/// <param name="endpointService">Endpoint configuration service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Confirmation of cache refresh</returns>
/// <response code="200">Cache refreshed successfully</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin role required</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin only
**Use Case**: Apply endpoint config changes immediately without waiting for cache expiration

---

## Categories Endpoints (5 total)

### 20. GET /api/categories

```xml
/// <summary>
/// Gets all course categories with course count
/// </summary>
/// <param name="categoryService">Category service</param>
/// <param name="logger">Logger instance</param>
/// <returns>List of all categories</returns>
/// <response code="200">Categories retrieved successfully</response>
/// <response code="500">Internal server error occurred</response>
```

**Includes**: Category name, slug, description, icon, color, course count

---

### 21. POST /api/categories

```xml
/// <summary>
/// Creates a new course category (Admin or Instructor only)
/// </summary>
/// <param name="createDto">Category details (name, slug, description, etc.)</param>
/// <param name="categoryService">Category service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for authorization</param>
/// <returns>Created category with generated ID</returns>
/// <response code="201">Category created successfully</response>
/// <response code="400">Validation failed - duplicate slug or invalid data</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin or Instructor role required</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin or Instructor
**Validation**: Unique slug, color code regex, name length

---

### 22. GET /api/categories/{id}

```xml
/// <summary>
/// Gets a specific category by ID with all details
/// </summary>
/// <param name="id">Category unique identifier (GUID)</param>
/// <param name="categoryService">Category service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Category details</returns>
/// <response code="200">Category retrieved successfully</response>
/// <response code="400">Invalid ID format</response>
/// <response code="404">Category not found</response>
/// <response code="500">Internal server error occurred</response>
```

---

### 23. PUT /api/categories/{id}

```xml
/// <summary>
/// Updates an existing category (Admin only)
/// </summary>
/// <param name="id">Category unique identifier</param>
/// <param name="updateDto">Updated category details</param>
/// <param name="categoryService">Category service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Updated category</returns>
/// <response code="200">Category updated successfully</response>
/// <response code="400">Validation failed or invalid ID</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin role required</response>
/// <response code="404">Category not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin only

---

### 24. DELETE /api/categories/{id}

```xml
/// <summary>
/// Deletes a category (Admin only) - fails if category has courses
/// </summary>
/// <param name="id">Category unique identifier</param>
/// <param name="categoryService">Category service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Confirmation of deletion</returns>
/// <response code="204">Category deleted successfully</response>
/// <response code="400">Cannot delete category with existing courses</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin role required</response>
/// <response code="404">Category not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Business Rule**: Categories with courses cannot be deleted

---

## Courses Endpoints (7 total)

### 25. GET /api/courses

```xml
/// <summary>
/// Gets all courses with pagination and filtering
/// </summary>
/// <param name="page">Page number (default: 1)</param>
/// <param name="pageSize">Items per page (default: 10, max: 100)</param>
/// <param name="category">Filter by category slug (optional)</param>
/// <param name="level">Filter by difficulty level (optional)</param>
/// <param name="courseService">Course service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Paginated list of courses</returns>
/// <response code="200">Courses retrieved successfully with pagination metadata</response>
/// <response code="400">Invalid pagination parameters</response>
/// <response code="500">Internal server error occurred</response>
```

**Pagination**: Page-based with total count
**Filters**: category, level, price range, rating

---

### 26. POST /api/courses

```xml
/// <summary>
/// Creates a new course (Admin or Instructor only)
/// </summary>
/// <param name="createDto">Course details (title, description, price, etc.)</param>
/// <param name="courseService">Course service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for instructor ID</param>
/// <returns>Created course with generated ID</returns>
/// <response code="201">Course created successfully</response>
/// <response code="400">Validation failed - invalid data or duplicate slug</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin or Instructor role required</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin or Instructor
**Validation**: Price range ($0.01-$50,000), title length, category exists

---

### 27. GET /api/courses/{id}

```xml
/// <summary>
/// Gets detailed information for a specific course including sections and lessons
/// </summary>
/// <param name="id">Course unique identifier (GUID)</param>
/// <param name="courseService">Course service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Complete course details with curriculum</returns>
/// <response code="200">Course retrieved successfully</response>
/// <response code="400">Invalid ID format</response>
/// <response code="404">Course not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Includes**: Sections, lessons, instructor info, reviews, enrollment count

---

### 28. PUT /api/courses/{id}

```xml
/// <summary>
/// Updates an existing course (Admin or course Instructor only)
/// </summary>
/// <param name="id">Course unique identifier</param>
/// <param name="updateDto">Updated course details</param>
/// <param name="courseService">Course service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for authorization</param>
/// <returns>Updated course</returns>
/// <response code="200">Course updated successfully</response>
/// <response code="400">Validation failed or invalid ID</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - not course instructor or admin</response>
/// <response code="404">Course not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin or course instructor only

---

### 29. DELETE /api/courses/{id}

```xml
/// <summary>
/// Deletes a course (Admin only) - fails if course has enrollments
/// </summary>
/// <param name="id">Course unique identifier</param>
/// <param name="courseService">Course service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Confirmation of deletion</returns>
/// <response code="204">Course deleted successfully</response>
/// <response code="400">Cannot delete course with existing enrollments</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin role required</response>
/// <response code="404">Course not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Business Rule**: Courses with enrollments cannot be deleted (soft delete instead)

---

### 30. GET /api/courses/category/{id}

```xml
/// <summary>
/// Gets all courses in a specific category
/// </summary>
/// <param name="id">Category unique identifier</param>
/// <param name="courseService">Course service</param>
/// <param name="logger">Logger instance</param>
/// <returns>List of courses in category</returns>
/// <response code="200">Courses retrieved successfully</response>
/// <response code="400">Invalid category ID format</response>
/// <response code="404">Category not found</response>
/// <response code="500">Internal server error occurred</response>
```

---

### 31. GET /api/courses/search

```xml
/// <summary>
/// Searches courses with advanced filters (title, description, tags, price, rating)
/// </summary>
/// <param name="query">Search query text</param>
/// <param name="category">Category filter</param>
/// <param name="minPrice">Minimum price filter</param>
/// <param name="maxPrice">Maximum price filter</param>
/// <param name="minRating">Minimum rating filter (1-5)</param>
/// <param name="level">Difficulty level filter</param>
/// <param name="courseService">Course service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Filtered and ranked search results</returns>
/// <response code="200">Search results returned successfully</response>
/// <response code="400">Invalid search parameters</response>
/// <response code="500">Internal server error occurred</response>
```

**Search Algorithm**: Full-text search on title, description, tags
**Ranking**: By relevance score and rating

---

## Enrollments Endpoints (5 total)

### 32. GET /api/enrollments

```xml
/// <summary>
/// Gets all enrollments with pagination (Admin only)
/// </summary>
/// <param name="page">Page number (default: 1)</param>
/// <param name="pageSize">Items per page (default: 10, max: 100)</param>
/// <param name="enrollmentService">Enrollment service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Paginated list of enrollments</returns>
/// <response code="200">Enrollments retrieved successfully</response>
/// <response code="400">Invalid pagination parameters</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin role required</response>
/// <response code="500">Internal server error occurred</response>
/// <response code="501">Not implemented - returns 501 status</response>
```

**Authorization**: Admin only
**Status**: NOT IMPLEMENTED (returns 501)

---

### 33. POST /api/enrollments

```xml
/// <summary>
/// Enrolls a user in a course (payment required unless free course)
/// </summary>
/// <param name="createDto">Enrollment details (courseId, userId, paymentId)</param>
/// <param name="enrollmentService">Enrollment service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for user authentication</param>
/// <returns>Created enrollment with progress tracking</returns>
/// <response code="201">Enrollment created successfully</response>
/// <response code="400">Validation failed - user already enrolled or payment required</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="404">Course or user not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Business Rules**:
- Duplicate enrollments prevented
- Free courses: immediate enrollment
- Paid courses: valid payment required

---

### 34. GET /api/enrollments/{id}

```xml
/// <summary>
/// Gets enrollment details by ID (Admin or enrolled user only)
/// </summary>
/// <param name="id">Enrollment unique identifier</param>
/// <param name="enrollmentService">Enrollment service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for authorization</param>
/// <returns>Enrollment details with progress</returns>
/// <response code="200">Enrollment retrieved successfully</response>
/// <response code="400">Invalid ID format</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - not your enrollment</response>
/// <response code="404">Enrollment not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin or enrolled user

---

### 35. GET /api/enrollments/course/{id}

```xml
/// <summary>
/// Gets all enrollments for a specific course (Admin or Instructor only)
/// </summary>
/// <param name="id">Course unique identifier</param>
/// <param name="enrollmentService">Enrollment service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for authorization</param>
/// <returns>List of enrollments with student details</returns>
/// <response code="200">Enrollments retrieved successfully</response>
/// <response code="400">Invalid course ID format</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - not course instructor or admin</response>
/// <response code="404">Course not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin or course instructor

---

### 36. GET /api/enrollments/user/{id}

```xml
/// <summary>
/// Gets all enrollments for a specific user (Admin or self only)
/// </summary>
/// <param name="id">User unique identifier</param>
/// <param name="enrollmentService">Enrollment service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for authorization</param>
/// <returns>List of user's enrollments with progress</returns>
/// <response code="200">Enrollments retrieved successfully</response>
/// <response code="400">Invalid user ID format</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - cannot view other users' enrollments</response>
/// <response code="404">User not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin or self only

---

## Payments Endpoints (3 total)

### 37. POST /api/payments/create-checkout

```xml
/// <summary>
/// Creates a Stripe or PayPal checkout session for course payment
/// </summary>
/// <param name="createDto">Payment details (courseId, paymentMethod, couponCode)</param>
/// <param name="paymentService">Payment service with Stripe/PayPal integration</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for user authentication</param>
/// <returns>Checkout session with redirect URL</returns>
/// <response code="200">Checkout session created - returns payment URL</response>
/// <response code="400">Validation failed - invalid coupon, insufficient funds, or missing data</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="404">Course not found</response>
/// <response code="500">Internal server error - Stripe/PayPal API failure</response>
```

**Payment Methods**: Stripe (card), PayPal (account)
**PCI DSS Compliant**: No card data stored
**Coupon Support**: Percentage and fixed amount discounts

---

### 38. GET /api/payments/transactions

```xml
/// <summary>
/// Gets payment transaction history (Admin sees all, users see their own)
/// </summary>
/// <param name="page">Page number (default: 1)</param>
/// <param name="pageSize">Items per page (default: 10, max: 100)</param>
/// <param name="paymentService">Payment service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for user filtering</param>
/// <returns>Paginated list of transactions</returns>
/// <response code="200">Transactions retrieved successfully</response>
/// <response code="400">Invalid pagination parameters</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="500">Internal server error occurred</response>
```

**Admin View**: All transactions
**User View**: Own transactions only
**Includes**: Status, amount, payment method, timestamp

---

### 39. GET /api/payments/transactions/{id}

```xml
/// <summary>
/// Gets detailed information for a specific transaction
/// </summary>
/// <param name="id">Transaction unique identifier</param>
/// <param name="paymentService">Payment service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for authorization</param>
/// <returns>Transaction details with metadata</returns>
/// <response code="200">Transaction retrieved successfully</response>
/// <response code="400">Invalid ID format</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - not your transaction</response>
/// <response code="404">Transaction not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin or transaction owner

---

## Reviews Endpoints (4 total)

### 40. GET /api/reviews/course/{id}

```xml
/// <summary>
/// Gets all reviews for a specific course with pagination
/// </summary>
/// <param name="id">Course unique identifier</param>
/// <param name="page">Page number (default: 1)</param>
/// <param name="pageSize">Items per page (default: 10, max: 50)</param>
/// <param name="reviewService">Review service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Paginated list of reviews with ratings</returns>
/// <response code="200">Reviews retrieved successfully</response>
/// <response code="400">Invalid course ID or pagination parameters</response>
/// <response code="404">Course not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Includes**: Rating (1-5), comment, reviewer name, verified status, timestamp

---

### 41. POST /api/reviews

```xml
/// <summary>
/// Creates a new course review (enrolled users only)
/// </summary>
/// <param name="createDto">Review details (courseId, rating, comment)</param>
/// <param name="reviewService">Review service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for user authentication</param>
/// <returns>Created review with generated ID</returns>
/// <response code="201">Review created successfully</response>
/// <response code="400">Validation failed - user not enrolled or duplicate review</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="404">Course not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Business Rules**:
- User must be enrolled in course
- One review per user per course
- Rating: 1-5 stars (validated)

---

### 42. GET /api/reviews/{id}

```xml
/// <summary>
/// Gets a specific review by ID
/// </summary>
/// <param name="id">Review unique identifier</param>
/// <param name="reviewService">Review service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Review details</returns>
/// <response code="200">Review retrieved successfully</response>
/// <response code="400">Invalid ID format</response>
/// <response code="404">Review not found</response>
/// <response code="500">Internal server error occurred</response>
```

---

### 43. GET /api/reviews/course/{id} (duplicate endpoint - same as #40)

_See endpoint #40 above_

---

## Users (Admin) Endpoints (5 total)

### 44. GET /api/users

```xml
/// <summary>
/// Gets all users with pagination (Admin only)
/// </summary>
/// <param name="page">Page number (default: 1)</param>
/// <param name="pageSize">Items per page (default: 10, max: 100)</param>
/// <param name="userAdminService">User admin service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Paginated list of users</returns>
/// <response code="200">Users retrieved successfully</response>
/// <response code="400">Invalid pagination parameters</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin role required</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin only
**Privacy**: Excludes password hashes and sensitive PII

---

### 45. GET /api/users/{id}

```xml
/// <summary>
/// Gets detailed user profile by ID (Admin or self only)
/// </summary>
/// <param name="id">User unique identifier</param>
/// <param name="userAdminService">User admin service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for authorization</param>
/// <returns>Complete user profile</returns>
/// <response code="200">User retrieved successfully</response>
/// <response code="400">Invalid ID format</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - cannot view other users' profiles</response>
/// <response code="404">User not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin or self

---

### 46. PUT /api/users/{id}

```xml
/// <summary>
/// Updates user profile (Admin or self only)
/// </summary>
/// <param name="id">User unique identifier</param>
/// <param name="updateDto">Updated user details (name, phone, bio, etc.)</param>
/// <param name="userAdminService">User admin service</param>
/// <param name="logger">Logger instance</param>
/// <param name="httpContext">HTTP context for authorization</param>
/// <returns>Updated user profile</returns>
/// <response code="200">User updated successfully</response>
/// <response code="400">Validation failed or invalid ID</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - cannot update other users' profiles</response>
/// <response code="404">User not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin or self
**Validation**: Phone format (E.164), email format, name regex

---

### 47. DELETE /api/users/{id}

```xml
/// <summary>
/// Deletes a user account (Admin only) - soft delete with GDPR compliance
/// </summary>
/// <param name="id">User unique identifier</param>
/// <param name="userAdminService">User admin service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Confirmation of deletion</returns>
/// <response code="204">User deleted successfully</response>
/// <response code="400">Cannot delete user with active enrollments or admin</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin role required</response>
/// <response code="404">User not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin only
**GDPR**: Anonymizes PII, retains transaction records

---

### 48. GET /api/users/profile

```xml
/// <summary>
/// Gets the current authenticated user's complete profile
/// </summary>
/// <param name="userAdminService">User admin service</param>
/// <param name="httpContext">HTTP context for user ID</param>
/// <param name="logger">Logger instance</param>
/// <returns>User profile with enrollments and preferences</returns>
/// <response code="200">Profile retrieved successfully</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="404">User not found</response>
/// <response code="500">Internal server error occurred</response>
```

**Authentication Required**: Yes
**Includes**: Full profile, enrollment count, wallet balance, preferences

---

## Dashboard (Admin) Endpoints (2 total)

### 49. GET /api/dashboard/stats

```xml
/// <summary>
/// Gets comprehensive platform statistics (Admin only)
/// </summary>
/// <param name="dashboardService">Dashboard analytics service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Platform metrics and KPIs</returns>
/// <response code="200">Statistics retrieved successfully</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin role required</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin only
**Metrics**: Total users, courses, enrollments, revenue, active sessions, avg rating

---

### 50. GET /api/dashboard/recent-activity

```xml
/// <summary>
/// Gets recent platform activity feed (Admin only)
/// </summary>
/// <param name="limit">Number of activities to retrieve (default: 20, max: 100)</param>
/// <param name="dashboardService">Dashboard analytics service</param>
/// <param name="logger">Logger instance</param>
/// <returns>Recent activity events with timestamps</returns>
/// <response code="200">Activity feed retrieved successfully</response>
/// <response code="400">Invalid limit parameter</response>
/// <response code="401">Unauthorized - authentication required</response>
/// <response code="403">Forbidden - Admin role required</response>
/// <response code="500">Internal server error occurred</response>
```

**Authorization**: Admin only
**Activities**: User registrations, course enrollments, payments, reviews, video uploads

---

## Summary Statistics

**Total Endpoints Documented**: 46 (45 implemented + 1 NOT_IMPLEMENTED)

**Breakdown by Category**:
- Authentication: 6 endpoints
- Chat (Chatbot): 4 endpoints
- Video Storage: 5 endpoints
- System Management: 4 endpoints
- Categories: 5 endpoints
- Courses: 7 endpoints
- Enrollments: 5 endpoints
- Payments: 3 endpoints
- Reviews: 4 endpoints (1 duplicate)
- Users (Admin): 5 endpoints
- Dashboard (Admin): 2 endpoints

**Authentication Methods**:
- No auth required: 14 endpoints (health checks, public APIs)
- JWT required: 32 endpoints
- Admin only: 12 endpoints
- Instructor/Admin: 4 endpoints
- Self or Admin: 6 endpoints

**Rate Limiting**:
- Auth endpoints: 5 req/min per IP (brute force protection)
- API endpoints: 50 req/min per user
- Global: 200 req/min per IP

---

## Implementation Checklist

- [x] XML documentation enabled in .csproj (GenerateDocumentationFile=true)
- [x] Swagger configured to include XML comments
- [x] JWT Bearer authentication in Swagger UI
- [ ] XML docs added to all 46 endpoints in Program.cs
- [ ] Build verification (XML file generated)
- [ ] Swagger UI testing (http://localhost:31081/swagger)
- [ ] Response code accuracy verification
- [ ] Sample requests/responses validation

---

**Next Steps**:
1. Apply XML documentation to Program.cs systematically
2. Build project to generate XML file
3. Test Swagger UI for completeness
4. Verify all response codes and samples
5. Generate Swagger JSON export for external tools
6. Create Postman collection from Swagger

---

**Generated by**: Claude Code (Sonnet 4.5)
**Date**: 2025-11-16
**Version**: 1.6.7-dev
**File**: /docs/API-ENDPOINT-DOCUMENTATION.md
