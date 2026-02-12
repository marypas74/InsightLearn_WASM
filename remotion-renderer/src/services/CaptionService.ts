import { Db, ObjectId } from 'mongodb';
import { CaptionSegment } from '../compositions/VideoWithCaptions.js';

// MongoDB TranslatedSubtitles schema (from InsightLearn)
export interface TranslatedSubtitleDocument {
  _id: ObjectId;
  LessonId: string; // GUID as string
  TargetLanguage: string;
  Segments: MongoSegment[];
  CreatedAt: Date;
  UpdatedAt?: Date;
  TranslationService?: string;
  SourceTranscriptId?: ObjectId;
}

export interface MongoSegment {
  Index: number;
  StartSeconds: number;
  EndSeconds: number;
  OriginalText: string;
  TranslatedText: string;
  Confidence?: number;
}

export class CaptionService {
  private db: Db;

  constructor(db: Db) {
    this.db = db;
  }

  /**
   * Fetch translated subtitles from MongoDB and convert to Remotion format
   */
  async getCaptions(
    lessonId: string,
    targetLanguage: string
  ): Promise<CaptionSegment[]> {
    const collection = this.db.collection<TranslatedSubtitleDocument>('TranslatedSubtitles');

    const doc = await collection.findOne({
      LessonId: lessonId,
      TargetLanguage: targetLanguage,
    });

    if (!doc || !doc.Segments || doc.Segments.length === 0) {
      console.warn(
        `[CaptionService] No captions found for lesson ${lessonId}, language ${targetLanguage}`
      );
      return [];
    }

    // Convert MongoDB format to Remotion CaptionSegment format
    return doc.Segments.map((seg) => this.convertSegment(seg));
  }

  /**
   * Fetch original transcript (source language) from VideoTranscripts collection
   */
  async getOriginalTranscript(lessonId: string): Promise<CaptionSegment[]> {
    const collection = this.db.collection('VideoTranscripts');

    const doc = await collection.findOne({
      LessonId: lessonId,
    });

    if (!doc || !doc.Segments || doc.Segments.length === 0) {
      console.warn(`[CaptionService] No original transcript for lesson ${lessonId}`);
      return [];
    }

    return doc.Segments.map((seg: MongoSegment) => ({
      startMs: Math.round(seg.StartSeconds * 1000),
      endMs: Math.round(seg.EndSeconds * 1000),
      text: seg.OriginalText || seg.TranslatedText || '',
    }));
  }

  /**
   * Convert MongoDB segment to Remotion CaptionSegment
   */
  private convertSegment(seg: MongoSegment): CaptionSegment {
    return {
      startMs: Math.round(seg.StartSeconds * 1000),
      endMs: Math.round(seg.EndSeconds * 1000),
      text: seg.TranslatedText || '',
    };
  }

  /**
   * Get available languages for a lesson
   */
  async getAvailableLanguages(lessonId: string): Promise<string[]> {
    const collection = this.db.collection<TranslatedSubtitleDocument>('TranslatedSubtitles');

    const docs = await collection
      .find({ LessonId: lessonId }, { projection: { TargetLanguage: 1 } })
      .toArray();

    return docs.map((d) => d.TargetLanguage);
  }

  /**
   * Check if captions exist for a lesson/language combo
   */
  async captionsExist(lessonId: string, targetLanguage: string): Promise<boolean> {
    const collection = this.db.collection<TranslatedSubtitleDocument>('TranslatedSubtitles');

    const count = await collection.countDocuments({
      LessonId: lessonId,
      TargetLanguage: targetLanguage,
    });

    return count > 0;
  }

  /**
   * Get video duration from captions (last segment end time)
   */
  async getVideoDuration(lessonId: string, targetLanguage: string): Promise<number> {
    const captions = await this.getCaptions(lessonId, targetLanguage);

    if (captions.length === 0) {
      throw new Error(`No captions found for lesson ${lessonId}, language ${targetLanguage}`);
    }

    // Return last caption end time in milliseconds
    return captions[captions.length - 1].endMs;
  }
}

export default CaptionService;
