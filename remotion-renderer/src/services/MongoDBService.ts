import { MongoClient, GridFSBucket, ObjectId, Db } from 'mongodb';
import { Readable, Writable } from 'stream';
import { pipeline } from 'stream/promises';
import fs from 'fs';
import path from 'path';

export interface MongoDBConfig {
  connectionString: string;
  databaseName: string;
}

export interface GridFSFileInfo {
  _id: ObjectId;
  filename: string;
  length: number;
  contentType?: string;
  uploadDate: Date;
}

export class MongoDBService {
  private client: MongoClient | null = null;
  private db: Db | null = null;
  private bucket: GridFSBucket | null = null;
  private config: MongoDBConfig;

  constructor(config: MongoDBConfig) {
    this.config = config;
  }

  async connect(): Promise<void> {
    if (this.client) return;

    this.client = new MongoClient(this.config.connectionString);
    await this.client.connect();
    this.db = this.client.db(this.config.databaseName);
    this.bucket = new GridFSBucket(this.db, { bucketName: 'videos' });
    console.log('[MongoDB] Connected to database:', this.config.databaseName);
  }

  async disconnect(): Promise<void> {
    if (this.client) {
      await this.client.close();
      this.client = null;
      this.db = null;
      this.bucket = null;
      console.log('[MongoDB] Disconnected');
    }
  }

  getDb(): Db {
    if (!this.db) throw new Error('MongoDB not connected - call connect() first');
    return this.db;
  }

  isConnected(): boolean {
    return this.db !== null;
  }

  getBucket(): GridFSBucket {
    if (!this.bucket) throw new Error('MongoDB not connected');
    return this.bucket;
  }

  /**
   * Download a video from GridFS to a local file
   */
  async downloadVideoToFile(fileId: ObjectId, outputPath: string): Promise<void> {
    const bucket = this.getBucket();
    const downloadStream = bucket.openDownloadStream(fileId);
    const writeStream = fs.createWriteStream(outputPath);

    await pipeline(downloadStream, writeStream);
    console.log(`[MongoDB] Downloaded video ${fileId} to ${outputPath}`);
  }

  /**
   * Download video by filename
   */
  async downloadVideoByFilename(filename: string, outputPath: string): Promise<void> {
    const bucket = this.getBucket();
    const downloadStream = bucket.openDownloadStreamByName(filename);
    const writeStream = fs.createWriteStream(outputPath);

    await pipeline(downloadStream, writeStream);
    console.log(`[MongoDB] Downloaded video "${filename}" to ${outputPath}`);
  }

  /**
   * Upload a rendered video to GridFS
   */
  async uploadVideo(
    inputPath: string,
    filename: string,
    metadata?: Record<string, unknown>
  ): Promise<ObjectId> {
    const bucket = this.getBucket();
    const readStream = fs.createReadStream(inputPath);

    const uploadStream = bucket.openUploadStream(filename, {
      contentType: 'video/mp4',
      metadata: {
        ...metadata,
        renderedAt: new Date(),
        source: 'remotion-renderer',
      },
    });

    await pipeline(readStream, uploadStream);
    console.log(`[MongoDB] Uploaded video "${filename}" with id ${uploadStream.id}`);
    return uploadStream.id;
  }

  /**
   * Get video file info from GridFS
   */
  async getVideoInfo(fileId: ObjectId): Promise<GridFSFileInfo | null> {
    const db = this.getDb();
    const filesCollection = db.collection('videos.files');
    const file = await filesCollection.findOne({ _id: fileId });
    return file as GridFSFileInfo | null;
  }

  /**
   * Check if a rendered video already exists
   */
  async findRenderedVideo(
    lessonId: string,
    targetLanguage: string
  ): Promise<GridFSFileInfo | null> {
    const db = this.getDb();
    const filesCollection = db.collection('videos.files');
    const file = await filesCollection.findOne({
      'metadata.lessonId': lessonId,
      'metadata.targetLanguage': targetLanguage,
      'metadata.source': 'remotion-renderer',
    });
    return file as GridFSFileInfo | null;
  }

  /**
   * Delete a video from GridFS
   */
  async deleteVideo(fileId: ObjectId): Promise<void> {
    const bucket = this.getBucket();
    await bucket.delete(fileId);
    console.log(`[MongoDB] Deleted video ${fileId}`);
  }
}

export default MongoDBService;
