import { Router, Request, Response } from 'express';
import { RenderService } from '../services/RenderService.js';
import { CaptionService } from '../services/CaptionService.js';
import { MongoDBService } from '../services/MongoDBService.js';
import fs from 'fs';
import path from 'path';

export interface RenderRequest {
  lessonId: string;
  targetLanguage: string;
  videoUrl: string;
  compositionId?: string;
  fps?: number;
}

export function createRenderRoutes(
  renderService: RenderService,
  captionService: CaptionService,
  mongoService: MongoDBService
): Router {
  const router = Router();

  /**
   * POST /render
   * Start a new video render job with burned-in captions
   */
  router.post('/render', async (req: Request, res: Response) => {
    try {
      const { lessonId, targetLanguage, videoUrl, compositionId, fps } =
        req.body as RenderRequest;

      // Validate required fields
      if (!lessonId || !targetLanguage || !videoUrl) {
        return res.status(400).json({
          success: false,
          error: 'Missing required fields: lessonId, targetLanguage, videoUrl',
        });
      }

      // Check if captions exist
      const captionsExist = await captionService.captionsExist(lessonId, targetLanguage);
      if (!captionsExist) {
        return res.status(404).json({
          success: false,
          error: `No captions found for lesson ${lessonId}, language ${targetLanguage}`,
        });
      }

      // Create job
      const job = renderService.createJob(lessonId, targetLanguage);

      // Start rendering asynchronously
      setImmediate(async () => {
        try {
          // Get captions from MongoDB
          const captions = await captionService.getCaptions(lessonId, targetLanguage);
          const durationMs = await captionService.getVideoDuration(lessonId, targetLanguage);

          // Render video
          const outputPath = await renderService.renderVideo(
            job.id,
            videoUrl,
            captions,
            durationMs,
            { compositionId, fps }
          );

          // Upload to GridFS
          const updatedJob = renderService.getJob(job.id);
          if (updatedJob) {
            updatedJob.status = 'uploading';
            updatedJob.progress = 95;
          }

          const fileId = await mongoService.uploadVideo(outputPath, `rendered_${lessonId}_${targetLanguage}.mp4`, {
            lessonId,
            targetLanguage,
            jobId: job.id,
          });

          if (updatedJob) {
            updatedJob.gridfsFileId = fileId.toString();
            updatedJob.status = 'completed';
            updatedJob.progress = 100;
          }

          // Clean up temp file
          if (fs.existsSync(outputPath)) {
            fs.unlinkSync(outputPath);
          }
        } catch (error) {
          console.error(`[RenderRoutes] Async render failed for job ${job.id}:`, error);
        }
      });

      // Return job info immediately (async processing)
      return res.status(202).json({
        success: true,
        jobId: job.id,
        status: job.status,
        message: 'Render job queued',
      });
    } catch (error) {
      console.error('[RenderRoutes] POST /render error:', error);
      return res.status(500).json({
        success: false,
        error: error instanceof Error ? error.message : 'Internal server error',
      });
    }
  });

  /**
   * GET /render/:jobId/status
   * Get render job status
   */
  router.get('/render/:jobId/status', (req: Request, res: Response) => {
    const { jobId } = req.params;
    const job = renderService.getJob(jobId);

    if (!job) {
      return res.status(404).json({
        success: false,
        error: `Job ${jobId} not found`,
      });
    }

    return res.json({
      success: true,
      job: {
        id: job.id,
        lessonId: job.lessonId,
        targetLanguage: job.targetLanguage,
        status: job.status,
        progress: job.progress,
        gridfsFileId: job.gridfsFileId,
        error: job.error,
        createdAt: job.createdAt,
        updatedAt: job.updatedAt,
      },
    });
  });

  /**
   * GET /render/:fileId/download
   * Download rendered video from GridFS
   */
  router.get('/render/:fileId/download', async (req: Request, res: Response) => {
    try {
      const { fileId } = req.params;
      const { ObjectId } = await import('mongodb');
      const objectId = new ObjectId(fileId);

      const fileInfo = await mongoService.getVideoInfo(objectId);
      if (!fileInfo) {
        return res.status(404).json({
          success: false,
          error: 'File not found',
        });
      }

      const bucket = mongoService.getBucket();
      const downloadStream = bucket.openDownloadStream(objectId);

      res.setHeader('Content-Type', 'video/mp4');
      res.setHeader(
        'Content-Disposition',
        `attachment; filename="${fileInfo.filename}"`
      );
      res.setHeader('Content-Length', fileInfo.length.toString());

      downloadStream.pipe(res);
    } catch (error) {
      console.error('[RenderRoutes] GET /render/:fileId/download error:', error);
      return res.status(500).json({
        success: false,
        error: error instanceof Error ? error.message : 'Internal server error',
      });
    }
  });

  /**
   * GET /render/jobs
   * List all render jobs (for monitoring)
   */
  router.get('/render/jobs', (_req: Request, res: Response) => {
    const jobs = renderService.getAllJobs();
    return res.json({
      success: true,
      jobs: jobs.map((j) => ({
        id: j.id,
        lessonId: j.lessonId,
        targetLanguage: j.targetLanguage,
        status: j.status,
        progress: j.progress,
        createdAt: j.createdAt,
        updatedAt: j.updatedAt,
      })),
    });
  });

  /**
   * DELETE /render/:jobId
   * Cancel/cleanup a render job
   */
  router.delete('/render/:jobId', async (req: Request, res: Response) => {
    try {
      const { jobId } = req.params;
      const job = renderService.getJob(jobId);

      if (!job) {
        return res.status(404).json({
          success: false,
          error: `Job ${jobId} not found`,
        });
      }

      await renderService.cleanupJob(jobId);

      return res.json({
        success: true,
        message: `Job ${jobId} cleaned up`,
      });
    } catch (error) {
      console.error('[RenderRoutes] DELETE /render/:jobId error:', error);
      return res.status(500).json({
        success: false,
        error: error instanceof Error ? error.message : 'Internal server error',
      });
    }
  });

  /**
   * GET /captions/:lessonId/:targetLanguage
   * Get captions for preview
   */
  router.get('/captions/:lessonId/:targetLanguage', async (req: Request, res: Response) => {
    try {
      const { lessonId, targetLanguage } = req.params;
      const captions = await captionService.getCaptions(lessonId, targetLanguage);

      return res.json({
        success: true,
        lessonId,
        targetLanguage,
        captionCount: captions.length,
        captions,
      });
    } catch (error) {
      console.error('[RenderRoutes] GET /captions error:', error);
      return res.status(500).json({
        success: false,
        error: error instanceof Error ? error.message : 'Internal server error',
      });
    }
  });

  /**
   * GET /captions/:lessonId/languages
   * Get available languages for a lesson
   */
  router.get('/captions/:lessonId/languages', async (req: Request, res: Response) => {
    try {
      const { lessonId } = req.params;
      const languages = await captionService.getAvailableLanguages(lessonId);

      return res.json({
        success: true,
        lessonId,
        languages,
      });
    } catch (error) {
      console.error('[RenderRoutes] GET /captions/:lessonId/languages error:', error);
      return res.status(500).json({
        success: false,
        error: error instanceof Error ? error.message : 'Internal server error',
      });
    }
  });

  return router;
}

export default createRenderRoutes;
