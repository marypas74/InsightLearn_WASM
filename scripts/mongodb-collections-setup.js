// MongoDB Collection Setup for Student Learning Space v2.1.0
// Database: insightlearn_videos (existing)
// Collections: VideoTranscripts, VideoKeyTakeaways, AIConversationHistory
// Execute with: mongosh -u insightlearn -p <password> insightlearn_videos < mongodb-collections-setup.js

// Switch to database
use insightlearn_videos;

print("Setting up Student Learning Space MongoDB collections...");

// ==============================================================================
// 1. VideoTranscripts Collection
// ==============================================================================

print("\n1. Creating VideoTranscripts collection with validation schema...");

db.createCollection("VideoTranscripts", {
    validator: {
        $jsonSchema: {
            bsonType: "object",
            required: ["lessonId", "language", "transcript", "createdAt"],
            properties: {
                lessonId: {
                    bsonType: "string",
                    description: "Lesson GUID as string (matches SQL Server LessonId)"
                },
                language: {
                    bsonType: "string",
                    pattern: "^[a-z]{2}-[A-Z]{2}$",
                    description: "Language code (e.g., en-US, it-IT)"
                },
                transcript: {
                    bsonType: "array",
                    description: "Array of transcript segments",
                    items: {
                        bsonType: "object",
                        required: ["startTime", "endTime", "text"],
                        properties: {
                            startTime: {
                                bsonType: "double",
                                minimum: 0,
                                description: "Segment start time in seconds"
                            },
                            endTime: {
                                bsonType: "double",
                                minimum: 0,
                                description: "Segment end time in seconds"
                            },
                            speaker: {
                                bsonType: ["string", "null"],
                                description: "Speaker identifier (optional)"
                            },
                            text: {
                                bsonType: "string",
                                minLength: 1,
                                description: "Transcript text for this segment"
                            },
                            confidence: {
                                bsonType: ["double", "null"],
                                minimum: 0,
                                maximum: 1,
                                description: "ASR confidence score (0-1)"
                            }
                        }
                    }
                },
                metadata: {
                    bsonType: ["object", "null"],
                    description: "Optional processing metadata",
                    properties: {
                        wordCount: {
                            bsonType: "int",
                            minimum: 0
                        },
                        averageConfidence: {
                            bsonType: "double",
                            minimum: 0,
                            maximum: 1
                        },
                        processingModel: {
                            bsonType: "string",
                            description: "ASR model used (e.g., whisper-1, azure-speech)"
                        },
                        processedAt: {
                            bsonType: "date"
                        }
                    }
                },
                createdAt: {
                    bsonType: "date",
                    description: "Document creation timestamp"
                },
                updatedAt: {
                    bsonType: ["date", "null"],
                    description: "Last update timestamp"
                }
            }
        }
    },
    validationLevel: "strict",
    validationAction: "error"
});

// Create indexes for VideoTranscripts
print("Creating indexes for VideoTranscripts...");

db.VideoTranscripts.createIndex(
    { "lessonId": 1 },
    { unique: true, name: "idx_lessonId_unique" }
);

db.VideoTranscripts.createIndex(
    { "language": 1 },
    { name: "idx_language" }
);

// Full-text search index on transcript text
db.VideoTranscripts.createIndex(
    { "transcript.text": "text" },
    { name: "idx_transcript_fulltext", default_language: "english" }
);

db.VideoTranscripts.createIndex(
    { "createdAt": -1 },
    { name: "idx_createdAt_desc" }
);

print("✅ VideoTranscripts collection created successfully");

// ==============================================================================
// 2. VideoKeyTakeaways Collection
// ==============================================================================

print("\n2. Creating VideoKeyTakeaways collection with validation schema...");

db.createCollection("VideoKeyTakeaways", {
    validator: {
        $jsonSchema: {
            bsonType: "object",
            required: ["lessonId", "takeaways", "createdAt"],
            properties: {
                lessonId: {
                    bsonType: "string",
                    description: "Lesson GUID as string"
                },
                takeaways: {
                    bsonType: "array",
                    description: "Array of AI-extracted key takeaways",
                    items: {
                        bsonType: "object",
                        required: ["takeawayId", "text", "category", "relevanceScore"],
                        properties: {
                            takeawayId: {
                                bsonType: "string",
                                description: "Unique takeaway identifier (GUID)"
                            },
                            text: {
                                bsonType: "string",
                                minLength: 1,
                                maxLength: 1000,
                                description: "Takeaway text content"
                            },
                            category: {
                                bsonType: "string",
                                enum: ["CoreConcept", "BestPractice", "Example", "Warning", "Summary", "KeyPoint"],
                                description: "Takeaway category"
                            },
                            relevanceScore: {
                                bsonType: "double",
                                minimum: 0,
                                maximum: 1,
                                description: "AI-assigned relevance score (0-1)"
                            },
                            timestampStart: {
                                bsonType: ["double", "null"],
                                minimum: 0,
                                description: "Video timestamp where concept starts (seconds)"
                            },
                            timestampEnd: {
                                bsonType: ["double", "null"],
                                minimum: 0,
                                description: "Video timestamp where concept ends (seconds)"
                            },
                            userFeedback: {
                                bsonType: ["int", "null"],
                                enum: [null, 1, -1],
                                description: "User feedback: 1 (thumbs up), -1 (thumbs down), null (no feedback)"
                            }
                        }
                    }
                },
                metadata: {
                    bsonType: ["object", "null"],
                    properties: {
                        totalTakeaways: {
                            bsonType: "int",
                            minimum: 0
                        },
                        processingModel: {
                            bsonType: "string",
                            description: "AI model used (e.g., qwen2:0.5b, gpt-4)"
                        },
                        processedAt: {
                            bsonType: "date"
                        }
                    }
                },
                createdAt: {
                    bsonType: "date"
                },
                updatedAt: {
                    bsonType: ["date", "null"]
                }
            }
        }
    },
    validationLevel: "strict",
    validationAction: "error"
});

// Create indexes for VideoKeyTakeaways
print("Creating indexes for VideoKeyTakeaways...");

db.VideoKeyTakeaways.createIndex(
    { "lessonId": 1 },
    { unique: true, name: "idx_lessonId_unique" }
);

db.VideoKeyTakeaways.createIndex(
    { "takeaways.category": 1 },
    { name: "idx_takeaway_category" }
);

db.VideoKeyTakeaways.createIndex(
    { "takeaways.relevanceScore": -1 },
    { name: "idx_relevance_score_desc" }
);

db.VideoKeyTakeaways.createIndex(
    { "createdAt": -1 },
    { name: "idx_createdAt_desc" }
);

print("✅ VideoKeyTakeaways collection created successfully");

// ==============================================================================
// 3. AIConversationHistory Collection
// ==============================================================================

print("\n3. Creating AIConversationHistory collection with validation schema...");

db.createCollection("AIConversationHistory", {
    validator: {
        $jsonSchema: {
            bsonType: "object",
            required: ["sessionId", "userId", "messages", "createdAt"],
            properties: {
                sessionId: {
                    bsonType: "string",
                    description: "Conversation session GUID as string"
                },
                userId: {
                    bsonType: "string",
                    description: "User GUID as string"
                },
                lessonId: {
                    bsonType: ["string", "null"],
                    description: "Optional lesson context GUID"
                },
                messages: {
                    bsonType: "array",
                    description: "Array of conversation messages",
                    items: {
                        bsonType: "object",
                        required: ["messageId", "role", "content", "timestamp"],
                        properties: {
                            messageId: {
                                bsonType: "string",
                                description: "Unique message identifier (GUID)"
                            },
                            role: {
                                bsonType: "string",
                                enum: ["user", "assistant", "system"],
                                description: "Message sender role"
                            },
                            content: {
                                bsonType: "string",
                                minLength: 1,
                                maxLength: 10000,
                                description: "Message content"
                            },
                            timestamp: {
                                bsonType: "date",
                                description: "Message timestamp"
                            },
                            videoTimestamp: {
                                bsonType: ["int", "null"],
                                minimum: 0,
                                description: "Optional video timestamp for contextual messages (seconds)"
                            }
                        }
                    }
                },
                createdAt: {
                    bsonType: "date",
                    description: "Conversation creation timestamp"
                },
                lastActivityAt: {
                    bsonType: ["date", "null"],
                    description: "Last message timestamp (for cleanup)"
                }
            }
        }
    },
    validationLevel: "strict",
    validationAction: "error"
});

// Create indexes for AIConversationHistory
print("Creating indexes for AIConversationHistory...");

db.AIConversationHistory.createIndex(
    { "sessionId": 1 },
    { unique: true, name: "idx_sessionId_unique" }
);

db.AIConversationHistory.createIndex(
    { "userId": 1, "createdAt": -1 },
    { name: "idx_userId_createdAt" }
);

db.AIConversationHistory.createIndex(
    { "lessonId": 1 },
    { name: "idx_lessonId", sparse: true }
);

db.AIConversationHistory.createIndex(
    { "lastActivityAt": -1 },
    { name: "idx_lastActivity_desc" }
);

// Full-text search on message content
db.AIConversationHistory.createIndex(
    { "messages.content": "text" },
    { name: "idx_messages_fulltext", default_language: "english" }
);

print("✅ AIConversationHistory collection created successfully");

// ==============================================================================
// Verification
// ==============================================================================

print("\n📊 Verification Summary:");
print("========================");

const collections = db.getCollectionNames();

print("\nCollections created:");
["VideoTranscripts", "VideoKeyTakeaways", "AIConversationHistory"].forEach(name => {
    if (collections.includes(name)) {
        const stats = db.getCollection(name).stats();
        const indexes = db.getCollection(name).getIndexes();
        print(`✅ ${name}`);
        print(`   - Document count: ${stats.count}`);
        print(`   - Indexes: ${indexes.length}`);
        print(`   - Size: ${stats.size} bytes`);
    } else {
        print(`❌ ${name} - NOT FOUND`);
    }
});

print("\n📋 Index Summary:");
print("VideoTranscripts: 4 indexes (lessonId unique, language, fulltext, createdAt)");
print("VideoKeyTakeaways: 4 indexes (lessonId unique, category, relevanceScore, createdAt)");
print("AIConversationHistory: 5 indexes (sessionId unique, userId+createdAt, lessonId, lastActivity, fulltext)");

print("\n✅ MongoDB Collections Setup Complete!");
print("\nNext steps:");
print("1. Verify collections exist: db.getCollectionNames()");
print("2. Test validation: Insert sample documents");
print("3. Test full-text search indexes");
print("4. Configure MongoDB service in Kubernetes");
