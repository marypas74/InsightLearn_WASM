import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import { MongoDBService } from './services/MongoDBService.js';
import { CaptionService } from './services/CaptionService.js';
import { RenderService } from './services/RenderService.js';
import { createRenderRoutes } from './routes/render.routes.js';

// Load environment variables
dotenv.config();

const PORT = process.env.PORT || 3000;
const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://localhost:27017';
const MONGODB_DB = process.env.MONGODB_DB || 'InsightLearnDB';

async function main() {
  console.log('========================================');
  console.log('  Remotion Renderer - InsightLearn');
  console.log('  Video export with burned-in captions');
  console.log('========================================');

  console.log(`[Main] MONGODB_URI: ${MONGODB_URI.replace(/:[^:@]*@/, ':****@')}`);
  console.log(`[Main] MONGODB_DB: ${MONGODB_DB}`);

  // Initialize services
  const mongoService = new MongoDBService({
    connectionString: MONGODB_URI,
    databaseName: MONGODB_DB,
  });

  // Retry MongoDB connection with backoff
  const maxRetries = 5;
  let connected = false;
  for (let attempt = 1; attempt <= maxRetries && !connected; attempt++) {
    try {
      console.log(`[Main] MongoDB connection attempt ${attempt}/${maxRetries}...`);
      await mongoService.connect();
      connected = true;
      console.log('[Main] MongoDB connected successfully!');
    } catch (error) {
      console.error(`[Main] MongoDB connection attempt ${attempt} failed:`, error);
      if (attempt < maxRetries) {
        const waitSec = attempt * 5;
        console.log(`[Main] Waiting ${waitSec}s before retry...`);
        await new Promise(r => setTimeout(r, waitSec * 1000));
      }
    }
  }

  if (!connected) {
    console.error('[Main] Failed to connect to MongoDB after all retries. Starting without MongoDB...');
    // Continue without MongoDB - health check will report not ready
  }

  // CaptionService requires MongoDB - create only if connected
  let captionService: CaptionService | null = null;
  if (connected && mongoService.isConnected()) {
    try {
      captionService = new CaptionService(mongoService.getDb());
      console.log('[Main] CaptionService initialized');
    } catch (err) {
      console.warn('[Main] CaptionService not available:', err);
    }
  } else {
    console.warn(`[Main] CaptionService not available (connected=${connected}, isConnected=${mongoService.isConnected()})`);
  }
  const renderService = new RenderService();

  // Pre-bundle Remotion (optional, for faster first render)
  console.log('[Main] Pre-bundling Remotion compositions...');
  try {
    await renderService.ensureBundle();
    console.log('[Main] Bundle ready');
  } catch (error) {
    console.warn('[Main] Pre-bundle failed (will bundle on first request):', error);
  }

  // Create Express app
  const app = express();

  // Middleware
  app.use(cors());
  app.use(express.json());

  // Health endpoints
  app.get('/health', (_req, res) => {
    res.json({ status: 'ok', service: 'remotion-renderer' });
  });

  app.get('/health/ready', async (_req, res) => {
    try {
      // Check MongoDB connection
      await mongoService.getDb().command({ ping: 1 });
      res.json({ status: 'ready', mongodb: 'connected' });
    } catch {
      res.status(503).json({ status: 'not ready', mongodb: 'disconnected' });
    }
  });

  // API routes (only if MongoDB is available)
  if (captionService) {
    const renderRoutes = createRenderRoutes(renderService, captionService, mongoService);
    app.use('/api', renderRoutes);
  } else {
    app.use('/api', (_req, res) => {
      res.status(503).json({ error: 'Service unavailable - MongoDB not connected' });
    });
  }

  // Error handling middleware
  app.use((err: Error, _req: express.Request, res: express.Response, _next: express.NextFunction) => {
    console.error('[Main] Unhandled error:', err);
    res.status(500).json({
      success: false,
      error: 'Internal server error',
    });
  });

  // Start server
  app.listen(PORT, () => {
    console.log(`[Main] Server running on port ${PORT}`);
    console.log(`[Main] MongoDB: ${MONGODB_URI}/${MONGODB_DB}`);
    console.log('[Main] Endpoints:');
    console.log('  POST /api/render - Start render job');
    console.log('  GET  /api/render/:jobId/status - Job status');
    console.log('  GET  /api/render/:fileId/download - Download video');
    console.log('  GET  /api/render/jobs - List all jobs');
    console.log('  GET  /api/captions/:lessonId/:lang - Get captions');
    console.log('  GET  /api/captions/:lessonId/languages - Available languages');
  });

  // Graceful shutdown
  const shutdown = async () => {
    console.log('[Main] Shutting down...');
    await mongoService.disconnect();
    process.exit(0);
  };

  process.on('SIGTERM', shutdown);
  process.on('SIGINT', shutdown);
}

main().catch((error) => {
  console.error('[Main] Fatal error:', error);
  process.exit(1);
});
