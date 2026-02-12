import { bundle } from '@remotion/bundler';
import { renderMedia, selectComposition, getCompositions } from '@remotion/renderer';
import path from 'path';
import fs from 'fs';
import os from 'os';
import { v4 as uuidv4 } from 'uuid';
import { CaptionSegment } from '../compositions/VideoWithCaptions.js';

export interface RenderJob {
  id: string;
  lessonId: string;
  targetLanguage: string;
  status: 'queued' | 'bundling' | 'rendering' | 'uploading' | 'completed' | 'failed';
  progress: number;
  outputPath?: string;
  gridfsFileId?: string;
  error?: string;
  createdAt: Date;
  updatedAt: Date;
  durationMs?: number;
}

export interface RenderOptions {
  compositionId?: string; // 'VideoWithCaptions' or 'VideoWithCaptions720p'
  fps?: number;
  codec?: 'h264' | 'h265';
  crf?: number; // Quality (0-51, lower = better, 23 is default)
}

const defaultRenderOptions: RenderOptions = {
  compositionId: 'VideoWithCaptions',
  fps: 30,
  codec: 'h264',
  crf: 23,
};

export class RenderService {
  private jobs: Map<string, RenderJob> = new Map();
  private bundlePath: string | null = null;
  private compositionsEntryPath: string;

  constructor() {
    // Path to compositions entry point - use src/ for Remotion bundling
    // In production container, src/ is copied alongside dist/
    this.compositionsEntryPath = path.resolve(
      process.cwd(),
      'src/compositions/index.ts'
    );
    console.log('[RenderService] Compositions entry path:', this.compositionsEntryPath);
  }

  /**
   * Create bundle (only needed once, can be cached)
   */
  async ensureBundle(): Promise<string> {
    if (this.bundlePath && fs.existsSync(this.bundlePath)) {
      return this.bundlePath;
    }

    console.log('[RenderService] Bundling Remotion project...');
    const bundleDir = path.join(os.tmpdir(), 'remotion-bundle');

    this.bundlePath = await bundle({
      entryPoint: this.compositionsEntryPath,
      outDir: bundleDir,
    });

    console.log('[RenderService] Bundle created at:', this.bundlePath);
    return this.bundlePath;
  }

  /**
   * Start a new render job
   */
  createJob(lessonId: string, targetLanguage: string): RenderJob {
    const job: RenderJob = {
      id: uuidv4(),
      lessonId,
      targetLanguage,
      status: 'queued',
      progress: 0,
      createdAt: new Date(),
      updatedAt: new Date(),
    };

    this.jobs.set(job.id, job);
    console.log(`[RenderService] Created job ${job.id} for lesson ${lessonId}, lang ${targetLanguage}`);
    return job;
  }

  /**
   * Get job by ID
   */
  getJob(jobId: string): RenderJob | undefined {
    return this.jobs.get(jobId);
  }

  /**
   * Update job status
   */
  private updateJob(jobId: string, updates: Partial<RenderJob>): void {
    const job = this.jobs.get(jobId);
    if (job) {
      Object.assign(job, updates, { updatedAt: new Date() });
    }
  }

  /**
   * Render video with burned-in captions
   */
  async renderVideo(
    jobId: string,
    videoUrl: string,
    captions: CaptionSegment[],
    durationMs: number,
    options: RenderOptions = {}
  ): Promise<string> {
    const mergedOptions = { ...defaultRenderOptions, ...options };
    const job = this.jobs.get(jobId);

    if (!job) {
      throw new Error(`Job ${jobId} not found`);
    }

    try {
      // Step 1: Bundle
      this.updateJob(jobId, { status: 'bundling', progress: 5 });
      const bundlePath = await this.ensureBundle();

      // Step 2: Select composition
      this.updateJob(jobId, { status: 'rendering', progress: 10 });

      const composition = await selectComposition({
        serveUrl: bundlePath,
        id: mergedOptions.compositionId!,
        inputProps: {
          videoUrl,
          captions,
        },
      });

      // Calculate duration in frames
      const durationInFrames = Math.ceil((durationMs / 1000) * mergedOptions.fps!);

      // Step 3: Create output path
      const outputDir = path.join(os.tmpdir(), 'remotion-output');
      if (!fs.existsSync(outputDir)) {
        fs.mkdirSync(outputDir, { recursive: true });
      }
      const outputPath = path.join(outputDir, `${job.lessonId}_${job.targetLanguage}_${jobId}.mp4`);

      // Step 4: Render
      console.log(`[RenderService] Starting render for job ${jobId}...`);
      console.log(`  - Duration: ${durationMs}ms (${durationInFrames} frames)`);
      console.log(`  - Captions: ${captions.length} segments`);
      console.log(`  - Output: ${outputPath}`);

      await renderMedia({
        composition: {
          ...composition,
          durationInFrames,
          fps: mergedOptions.fps!,
        },
        serveUrl: bundlePath,
        codec: mergedOptions.codec!,
        outputLocation: outputPath,
        inputProps: {
          videoUrl,
          captions,
        },
        crf: mergedOptions.crf,
        onProgress: ({ progress }) => {
          const percent = Math.round(10 + progress * 80); // 10% to 90%
          this.updateJob(jobId, { progress: percent });
        },
      });

      this.updateJob(jobId, {
        status: 'completed',
        progress: 100,
        outputPath,
        durationMs,
      });

      console.log(`[RenderService] Render completed for job ${jobId}`);
      return outputPath;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      console.error(`[RenderService] Render failed for job ${jobId}:`, errorMessage);

      this.updateJob(jobId, {
        status: 'failed',
        error: errorMessage,
      });

      throw error;
    }
  }

  /**
   * Clean up old job files
   */
  async cleanupJob(jobId: string): Promise<void> {
    const job = this.jobs.get(jobId);
    if (job?.outputPath && fs.existsSync(job.outputPath)) {
      fs.unlinkSync(job.outputPath);
      console.log(`[RenderService] Cleaned up output file for job ${jobId}`);
    }
    this.jobs.delete(jobId);
  }

  /**
   * Get all jobs (for monitoring)
   */
  getAllJobs(): RenderJob[] {
    return Array.from(this.jobs.values());
  }
}

export default RenderService;
