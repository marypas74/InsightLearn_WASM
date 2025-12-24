# InsightLearn API Reference

**Version:** 1.4.22-dev
**Base URL:** `https://www.insightlearn.cloud/api`

## 📚 Table of Contents

- [Authentication](#authentication)
- [Quick Start](#quick-start)
- [Endpoints by Category](#endpoints-by-category)
  - [Auth](#auth-endpoints)
  - [Chat](#chat-endpoints-ai-chatbot)
  - [Courses](#courses-endpoints)
  - [Categories](#categories-endpoints)
  - [Users](#users-endpoints)
  - [Enrollments](#enrollments-endpoints)
  - [Reviews](#reviews-endpoints)
  - [Payments](#payments-endpoints)
  - [Dashboard](#dashboard-endpoints)
- [Error Handling](#error-handling)
- [Rate Limiting](#rate-limiting)

---

## 🔐 Authentication

Most API endpoints require JWT (JSON Web Token) authentication.

### Obtaining a Token

**Login:**
```bash
curl -X POST https://www.insightlearn.cloud/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@insightlearn.cloud",
    "password": "YourPassword123!"
  }'
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "user": {
    "id": 1,
    "email": "admin@insightlearn.cloud",
    "firstName": "Admin",
    "lastName": "User",
    "role": "Admin"
  }
}
```

### Using the Token

Include the token in the `Authorization` header for protected endpoints:

```bash
curl https://www.insightlearn.cloud/api/users/profile \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## 🚀 Quick Start

### 1. Register a New User

```bash
curl -X POST https://www.insightlearn.cloud/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "student@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### 2. Get All Courses

```bash
curl https://www.insightlearn.cloud/api/courses
```

### 3. Chat with AI Chatbot

```bash
curl -X POST https://www.insightlearn.cloud/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What programming courses do you offer?",
    "contactEmail": "student@example.com"
  }'
```

---

## 📋 Endpoints by Category

### Auth Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/auth/login` | User login | No |
| `POST` | `/auth/register` | User registration | No |
| `GET` | `/auth/me` | Get current user info | Yes |
| `POST` | `/auth/refresh` | Refresh access token | No |
| `POST` | `/auth/complete-registration` | Complete registration | Yes |
| `POST` | `/auth/oauth-callback` | OAuth provider callback | No |

#### Login Example

```bash
curl -X POST https://www.insightlearn.cloud/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@insightlearn.cloud",
    "password": "Admin123!"
  }'
```

**Success Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1...",
  "refreshToken": "refresh_abc123...",
  "user": {
    "id": 1,
    "email": "admin@insightlearn.cloud",
    "role": "Admin"
  }
}
```

**Error Response (401):**
```json
{
  "error": "Invalid credentials",
  "status": 401
}
```

---

### Chat Endpoints (AI Chatbot)

Powered by **Ollama phi3:mini** LLM model.

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/chat/message` | Send message to chatbot | No |
| `GET` | `/chat/history` | Get chat history | No |

#### Send Message to Chatbot

```bash
curl -X POST https://www.insightlearn.cloud/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Tell me about your web development courses",
    "contactEmail": "student@example.com",
    "sessionId": "session_abc123"
  }'
```

**Response:**
```json
{
  "response": "We offer comprehensive web development courses including HTML, CSS, JavaScript, React, and Node.js. Our courses are designed for beginners to advanced learners...",
  "sessionId": "session_abc123",
  "timestamp": "2025-11-07T12:00:00Z"
}
```

#### Get Chat History

```bash
curl "https://www.insightlearn.cloud/api/chat/history?sessionId=session_abc123&limit=50"
```

---

### Courses Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/courses` | Get all courses | No |
| `GET` | `/courses/{id}` | Get course by ID | No |
| `GET` | `/courses/search` | Search courses | No |
| `GET` | `/courses/category/{id}` | Get courses by category | No |
| `POST` | `/courses` | Create new course | Yes (Instructor/Admin) |
| `PUT` | `/courses/{id}` | Update course | Yes (Instructor/Admin) |
| `DELETE` | `/courses/{id}` | Delete course | Yes (Admin) |

#### Get All Courses

```bash
curl "https://www.insightlearn.cloud/api/courses?page=1&pageSize=20"
```

#### Search Courses

```bash
curl "https://www.insightlearn.cloud/api/courses/search?q=javascript&level=Beginner"
```

#### Create New Course

```bash
curl -X POST https://www.insightlearn.cloud/api/courses \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Complete JavaScript Course",
    "description": "Learn JavaScript from basics to advanced",
    "categoryId": 1,
    "price": 49.99,
    "level": "Beginner"
  }'
```

---

### Categories Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/categories` | Get all categories | No |
| `GET` | `/categories/{id}` | Get category by ID | No |
| `POST` | `/categories` | Create category | Yes (Admin) |
| `PUT` | `/categories/{id}` | Update category | Yes (Admin) |
| `DELETE` | `/categories/{id}` | Delete category | Yes (Admin) |

#### Get All Categories

```bash
curl https://www.insightlearn.cloud/api/categories
```

---

### Users Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/users` | Get all users | Yes (Admin) |
| `GET` | `/users/{id}` | Get user by ID | Yes |
| `GET` | `/users/profile` | Get current user profile | Yes |
| `PUT` | `/users/{id}` | Update user | Yes |
| `DELETE` | `/users/{id}` | Delete user | Yes (Admin) |

#### Get Current User Profile

```bash
curl https://www.insightlearn.cloud/api/users/profile \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

### Enrollments Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/enrollments` | Get all enrollments | Yes |
| `GET` | `/enrollments/{id}` | Get enrollment by ID | Yes |
| `GET` | `/enrollments/user/{userId}` | Get user enrollments | Yes |
| `GET` | `/enrollments/course/{courseId}` | Get course enrollments | Yes |
| `POST` | `/enrollments` | Create enrollment | Yes |

#### Enroll in a Course

```bash
curl -X POST https://www.insightlearn.cloud/api/enrollments \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": 5
  }'
```

---

### Reviews Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/reviews` | Get all reviews | No |
| `GET` | `/reviews/{id}` | Get review by ID | No |
| `GET` | `/reviews/course/{courseId}` | Get course reviews | No |
| `POST` | `/reviews` | Create review | Yes |

#### Get Reviews for a Course

```bash
curl https://www.insightlearn.cloud/api/reviews/course/5
```

#### Submit a Review

```bash
curl -X POST https://www.insightlearn.cloud/api/reviews \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": 5,
    "rating": 5,
    "comment": "Excellent course! Highly recommended."
  }'
```

---

### Payments Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/payments/create-checkout` | Create checkout session | Yes |
| `GET` | `/payments/transactions` | Get all transactions | Yes |
| `GET` | `/payments/transactions/{id}` | Get transaction by ID | Yes |

#### Create Checkout Session

```bash
curl -X POST https://www.insightlearn.cloud/api/payments/create-checkout \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": 5,
    "amount": 49.99
  }'
```

---

### Dashboard Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/dashboard/stats` | Get dashboard statistics | Yes |
| `GET` | `/dashboard/recent-activity` | Get recent activity feed | Yes |

#### Get Dashboard Stats

```bash
curl https://www.insightlearn.cloud/api/dashboard/stats \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response:**
```json
{
  "totalCourses": 125,
  "totalStudents": 1543,
  "totalEnrollments": 3421,
  "revenue": 54789.50
}
```

---

## ⚠️ Error Handling

All error responses follow this format:

```json
{
  "error": "Error message describing what went wrong",
  "status": 400,
  "timestamp": "2025-11-07T12:00:00Z"
}
```

### Common HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| `200` | OK | Request successful |
| `201` | Created | Resource created successfully |
| `204` | No Content | Request successful, no content to return |
| `400` | Bad Request | Invalid request parameters |
| `401` | Unauthorized | Missing or invalid authentication token |
| `403` | Forbidden | Insufficient permissions |
| `404` | Not Found | Resource not found |
| `409` | Conflict | Resource already exists |
| `500` | Internal Server Error | Server error |

---

## 🚦 Rate Limiting

Rate limits apply per IP address:
- **100 requests per minute** for authenticated users
- **20 requests per minute** for anonymous users
- **10 requests per minute** for chatbot endpoints

When rate limit is exceeded:
```json
{
  "error": "Rate limit exceeded. Please try again later.",
  "status": 429,
  "retryAfter": 60
}
```

---

## 📊 Database Backend

All endpoints are backed by:
- **SQL Server** for relational data (users, courses, enrollments)
- **Connection**: `sqlserver-clusterip` service (Kubernetes)
- **Database**: `InsightLearnDb`
- **Total Tables**: 45 (via EF Core migrations)
- **System Endpoints**: 39 catalogued endpoints

---

## 🤖 AI Chatbot

The chatbot uses **Ollama phi3:mini** model running on:
- **Service**: `ollama-service.insightlearn.svc.cluster.local:11434`
- **Model**: `phi3:mini` (optimized for performance)
- **Context**: InsightLearn LMS domain knowledge
- **Storage**: Chat history stored in SQL Server `ChatbotMessages` table

---

## 🔧 Technical Details

- **Framework**: ASP.NET Core 8 Minimal APIs
- **Authentication**: JWT Bearer tokens (7-day expiration)
- **Database**: SQL Server 2022 (EF Core 8)
- **Deployment**: Kubernetes (K3s) on Rocky Linux 10
- **Reverse Proxy**: Nginx + Cloudflare Tunnel
- **API Base URL**: `https://www.insightlearn.cloud/api`
- **Frontend**: Blazor WebAssembly

---

## 📝 Notes

1. **Endpoint Configuration**: All endpoint paths are stored in the `SystemEndpoints` database table and can be modified without code changes.

2. **Cache Headers**: API responses include `Cache-Control: no-cache` headers to prevent stale data.

3. **CORS**: API supports CORS for `https://www.insightlearn.cloud` origin.

4. **Health Check**: Use `/health` endpoint to verify API availability.

---

## 📞 Support

For API issues or questions:
- **Email**: marcello.pasqui@gmail.com
- **GitHub**: https://github.com/marypas74/InsightLearn_WASM/issues

---

**Last Updated**: November 7, 2025
**API Version**: 1.4.22-dev
