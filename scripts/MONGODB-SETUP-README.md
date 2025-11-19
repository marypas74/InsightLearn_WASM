# MongoDB Collections Setup - Student Learning Space v2.1.0

## Overview

This directory contains MongoDB setup scripts for the Student Learning Space feature (v2.1.0).

## Collections

| Collection | Purpose | Size Estimate | Indexes |
|------------|---------|---------------|---------|
| **VideoTranscripts** | Video transcript segments with ASR confidence | ~50-500 KB per lesson | 4 (lessonId unique, fulltext) |
| **VideoKeyTakeaways** | AI-extracted key concepts and learning points | ~10-50 KB per lesson | 4 (lessonId unique, category, relevance) |
| **AIConversationHistory** | Student-AI chat message history | ~5-50 KB per session | 5 (sessionId unique, userId+date, fulltext) |

## Setup Methods

### Method 1: Manual Execution (Development)

**Prerequisites**:
- MongoDB 7.0+ running
- `mongosh` client installed
- Database: `insightlearn_videos`
- User: `insightlearn` with `readWrite` role

**Execute**:
```bash
# Local MongoDB instance
mongosh -u insightlearn -p <password> insightlearn_videos < mongodb-collections-setup.js

# Kubernetes MongoDB pod
kubectl exec -it mongodb-0 -n insightlearn -- \
  mongosh -u insightlearn -p <password> insightlearn_videos < /tmp/mongodb-collections-setup.js
```

### Method 2: Kubernetes Job (Production)

**Prerequisites**:
- MongoDB Secret configured: `insightlearn-secrets` with `mongodb-password` key
- MongoDB Service accessible: `mongodb-service.insightlearn.svc.cluster.local`

**Deploy**:
```bash
# Create ConfigMap with setup script
kubectl create configmap mongodb-setup-script \
  --from-file=mongodb-collections-setup.js \
  -n insightlearn

# Apply Kubernetes Job
kubectl apply -f k8s/18-mongodb-setup-job.yaml

# Monitor job execution
kubectl logs -f job/mongodb-collections-setup -n insightlearn

# Verify completion
kubectl get job mongodb-collections-setup -n insightlearn
# Expected: COMPLETIONS: 1/1
```

**Cleanup after success**:
```bash
kubectl delete job mongodb-collections-setup -n insightlearn
kubectl delete configmap mongodb-setup-script -n insightlearn
```

## Validation Schemas

### VideoTranscripts Schema

```javascript
{
  lessonId: string (GUID),           // REQUIRED, UNIQUE
  language: string (regex: ^[a-z]{2}-[A-Z]{2}$),  // REQUIRED
  transcript: [                       // REQUIRED
    {
      startTime: double (>= 0),       // REQUIRED
      endTime: double (>= 0),         // REQUIRED
      speaker: string | null,
      text: string (min 1 char),      // REQUIRED
      confidence: double (0-1) | null
    }
  ],
  metadata: {                         // OPTIONAL
    wordCount: int,
    averageConfidence: double (0-1),
    processingModel: string,
    processedAt: date
  },
  createdAt: date,                    // REQUIRED
  updatedAt: date | null
}
```

### VideoKeyTakeaways Schema

```javascript
{
  lessonId: string (GUID),           // REQUIRED, UNIQUE
  takeaways: [                        // REQUIRED
    {
      takeawayId: string (GUID),      // REQUIRED
      text: string (1-1000 chars),    // REQUIRED
      category: enum [                // REQUIRED
        "CoreConcept", "BestPractice", "Example",
        "Warning", "Summary", "KeyPoint"
      ],
      relevanceScore: double (0-1),   // REQUIRED
      timestampStart: double | null,
      timestampEnd: double | null,
      userFeedback: int (1 | -1 | null)  // Thumbs up/down
    }
  ],
  metadata: {                         // OPTIONAL
    totalTakeaways: int,
    processingModel: string,
    processedAt: date
  },
  createdAt: date,                    // REQUIRED
  updatedAt: date | null
}
```

### AIConversationHistory Schema

```javascript
{
  sessionId: string (GUID),          // REQUIRED, UNIQUE
  userId: string (GUID),             // REQUIRED
  lessonId: string (GUID) | null,
  messages: [                         // REQUIRED
    {
      messageId: string (GUID),       // REQUIRED
      role: enum ["user", "assistant", "system"],  // REQUIRED
      content: string (1-10000 chars), // REQUIRED
      timestamp: date,                 // REQUIRED
      videoTimestamp: int (>= 0) | null
    }
  ],
  createdAt: date,                    // REQUIRED
  lastActivityAt: date | null
}
```

## Indexes

### VideoTranscripts Indexes

1. **idx_lessonId_unique**: `{ lessonId: 1 }` (UNIQUE)
2. **idx_language**: `{ language: 1 }`
3. **idx_transcript_fulltext**: `{ "transcript.text": "text" }` (FULL-TEXT SEARCH)
4. **idx_createdAt_desc**: `{ createdAt: -1 }`

### VideoKeyTakeaways Indexes

1. **idx_lessonId_unique**: `{ lessonId: 1 }` (UNIQUE)
2. **idx_takeaway_category**: `{ "takeaways.category": 1 }`
3. **idx_relevance_score_desc**: `{ "takeaways.relevanceScore": -1 }`
4. **idx_createdAt_desc**: `{ createdAt: -1 }`

### AIConversationHistory Indexes

1. **idx_sessionId_unique**: `{ sessionId: 1 }` (UNIQUE)
2. **idx_userId_createdAt**: `{ userId: 1, createdAt: -1 }` (COMPOSITE)
3. **idx_lessonId**: `{ lessonId: 1 }` (SPARSE)
4. **idx_lastActivity_desc**: `{ lastActivityAt: -1 }`
5. **idx_messages_fulltext**: `{ "messages.content": "text" }` (FULL-TEXT SEARCH)

## Testing

### Test Full-Text Search

```javascript
// Search transcripts
db.VideoTranscripts.find({
  $text: { $search: "authentication security" }
}, {
  score: { $meta: "textScore" }
}).sort({ score: { $meta: "textScore" } });

// Search conversation messages
db.AIConversationHistory.find({
  $text: { $search: "how do I implement" }
}, {
  score: { $meta: "textScore" }
}).sort({ score: { $meta: "textScore" } });
```

### Test Validation

```javascript
// Should FAIL (missing required field)
db.VideoTranscripts.insertOne({
  language: "en-US",
  transcript: []
  // Missing lessonId - should fail
});

// Should FAIL (invalid language format)
db.VideoTranscripts.insertOne({
  lessonId: "550e8400-e29b-41d4-a716-446655440000",
  language: "english",  // Should be en-US
  transcript: []
});

// Should SUCCEED
db.VideoTranscripts.insertOne({
  lessonId: "550e8400-e29b-41d4-a716-446655440000",
  language: "en-US",
  transcript: [
    {
      startTime: 0.0,
      endTime: 5.2,
      text: "Welcome to this tutorial",
      confidence: 0.95
    }
  ],
  createdAt: new Date()
});
```

### Verify Setup

```javascript
// List all collections
db.getCollectionNames();

// Check collection stats
db.VideoTranscripts.stats();
db.VideoKeyTakeaways.stats();
db.AIConversationHistory.stats();

// List indexes
db.VideoTranscripts.getIndexes();
db.VideoKeyTakeaways.getIndexes();
db.AIConversationHistory.getIndexes();
```

## Troubleshooting

### Issue: "Collection already exists"

```bash
# Drop existing collection (⚠️ WARNING: Deletes all data)
mongosh -u insightlearn -p <password> insightlearn_videos --eval "db.VideoTranscripts.drop()"

# Re-run setup script
mongosh -u insightlearn -p <password> insightlearn_videos < mongodb-collections-setup.js
```

### Issue: "Validation error on insert"

Check document structure matches schema:
```bash
# Get validation rules
db.getCollectionInfos({ name: "VideoTranscripts" })[0].options.validator
```

### Issue: Kubernetes Job fails

```bash
# Check job logs
kubectl logs -f job/mongodb-collections-setup -n insightlearn

# Common causes:
# 1. MongoDB Secret missing or incorrect
kubectl get secret insightlearn-secrets -n insightlearn -o jsonpath='{.data.mongodb-password}' | base64 -d

# 2. MongoDB Service not accessible
kubectl get svc mongodb-service -n insightlearn

# 3. ConfigMap not created
kubectl get configmap mongodb-setup-script -n insightlearn
```

## Performance Considerations

### Index Performance

- **Full-text search**: O(log n) lookup, but large index size (~20-30% of data)
- **Unique indexes**: O(log n) insert/update validation
- **Composite indexes**: Efficient for queries filtering by both fields

### Storage Estimates

| Collection | Documents | Avg Size | Total Size (1000 lessons) |
|------------|-----------|----------|---------------------------|
| VideoTranscripts | 1 per lesson | 200 KB | ~200 MB |
| VideoKeyTakeaways | 1 per lesson | 20 KB | ~20 MB |
| AIConversationHistory | Variable (sessions) | 10 KB | ~10 MB (1000 sessions) |

### Cleanup Recommendations

- **AIConversationHistory**: Delete inactive conversations older than 90 days (automated via `IAIConversationRepository.DeleteOldConversationsAsync()`)
- **VideoTranscripts**: Keep indefinitely (valuable learning resource)
- **VideoKeyTakeaways**: Keep indefinitely (improves with user feedback)

## Security

- **Validation Level**: `strict` - All documents must pass validation
- **Validation Action**: `error` - Invalid documents are rejected (not logged and allowed)
- **Authentication**: Required (user: `insightlearn`, password from Secret)
- **Least Privilege**: User has `readWrite` role on `insightlearn_videos` database only

## Next Steps

After successful setup:

1. ✅ Verify all 3 collections created
2. ✅ Verify 13 indexes created (4 + 4 + 5)
3. ✅ Test full-text search on sample data
4. ✅ Test validation with invalid documents
5. ✅ Integrate with .NET repositories (already implemented)
6. ✅ Configure MongoDB connection string in Kubernetes deployment
7. ✅ Test end-to-end workflow (API → Repository → MongoDB)

## References

- MongoDB JSON Schema Validation: https://www.mongodb.com/docs/manual/core/schema-validation/
- MongoDB Text Indexes: https://www.mongodb.com/docs/manual/core/indexes/index-types/index-text/
- MongoDB .NET Driver: https://www.mongodb.com/docs/drivers/csharp/current/
