/**
 * Video Player Interop Module for SmartVideoPlayer Component
 * Handles JS-to-Blazor communication for HTML5 video events
 * Part of SmartVideoPlayer component stack (v2.2.0-dev)
 */

window.VideoInterop = {
    instances: new Map(),

    /**
     * Initialize video player with Blazor component reference
     * @param {string} elementId - The video element ID
     * @param {object} dotNetRef - Blazor .NET reference for callbacks
     */
    initialize: function (elementId, dotNetRef) {
        const video = document.getElementById(elementId);
        if (!video) {
            console.error('[VideoInterop] Video element not found:', elementId);
            return false;
        }

        // Store reference
        this.instances.set(elementId, { video, dotNetRef });

        // Bind event handlers
        this._bindEvents(elementId, video, dotNetRef);

        console.log('[VideoInterop] Initialized for:', elementId);
        return true;
    },

    /**
     * Dispose video player and clean up event listeners
     * @param {string} elementId - The video element ID
     */
    dispose: function (elementId) {
        const instance = this.instances.get(elementId);
        if (instance) {
            this._unbindEvents(elementId, instance.video);
            this.instances.delete(elementId);
            console.log('[VideoInterop] Disposed:', elementId);
        }
    },

    /**
     * Play video
     * @param {string} elementId - The video element ID
     */
    play: function (elementId) {
        const instance = this.instances.get(elementId);
        if (instance?.video) {
            return instance.video.play().catch(err => {
                console.warn('[VideoInterop] Play failed:', err.message);
                return false;
            });
        }
        return Promise.resolve(false);
    },

    /**
     * Pause video
     * @param {string} elementId - The video element ID
     */
    pause: function (elementId) {
        const instance = this.instances.get(elementId);
        if (instance?.video) {
            instance.video.pause();
            return true;
        }
        return false;
    },

    /**
     * Seek to specific time
     * @param {string} elementId - The video element ID
     * @param {number} timeSeconds - Time in seconds
     */
    seek: function (elementId, timeSeconds) {
        const instance = this.instances.get(elementId);
        if (instance?.video) {
            // Guard against NaN/Infinity values
            if (!isFinite(timeSeconds)) {
                console.warn('[VideoInterop] Seek failed: non-finite time value', timeSeconds);
                return false;
            }
            const duration = instance.video.duration;
            // Clamp to valid range if duration is available
            const clampedTime = isFinite(duration)
                ? Math.max(0, Math.min(duration, timeSeconds))
                : Math.max(0, timeSeconds);
            instance.video.currentTime = clampedTime;
            return true;
        }
        return false;
    },

    /**
     * Set volume (0.0 to 1.0)
     * @param {string} elementId - The video element ID
     * @param {number} volume - Volume level
     */
    setVolume: function (elementId, volume) {
        const instance = this.instances.get(elementId);
        if (instance?.video) {
            instance.video.volume = Math.max(0, Math.min(1, volume));
            return true;
        }
        return false;
    },

    /**
     * Toggle mute
     * @param {string} elementId - The video element ID
     */
    toggleMute: function (elementId) {
        const instance = this.instances.get(elementId);
        if (instance?.video) {
            instance.video.muted = !instance.video.muted;
            return instance.video.muted;
        }
        return false;
    },

    /**
     * Set playback rate
     * @param {string} elementId - The video element ID
     * @param {number} rate - Playback rate (0.5, 1.0, 1.5, 2.0)
     */
    setPlaybackRate: function (elementId, rate) {
        const instance = this.instances.get(elementId);
        if (instance?.video) {
            instance.video.playbackRate = rate;
            return true;
        }
        return false;
    },

    /**
     * Toggle fullscreen
     * @param {string} elementId - The video element ID
     */
    toggleFullscreen: function (elementId) {
        const instance = this.instances.get(elementId);
        if (!instance?.video) return false;

        const container = instance.video.closest('.smart-video-container');
        if (!container) return false;

        if (document.fullscreenElement) {
            document.exitFullscreen();
            return false;
        } else {
            container.requestFullscreen().catch(err => {
                console.warn('[VideoInterop] Fullscreen failed:', err.message);
            });
            return true;
        }
    },

    /**
     * Get current video state
     * @param {string} elementId - The video element ID
     * @returns {object} Video state object
     */
    getState: function (elementId) {
        const instance = this.instances.get(elementId);
        if (!instance?.video) return null;

        const video = instance.video;
        return {
            currentTime: video.currentTime,
            duration: video.duration || 0,
            paused: video.paused,
            muted: video.muted,
            volume: video.volume,
            playbackRate: video.playbackRate,
            buffered: this._getBufferedPercentage(video),
            isFullscreen: !!document.fullscreenElement
        };
    },

    /**
     * Skip forward/backward
     * @param {string} elementId - The video element ID
     * @param {number} seconds - Seconds to skip (negative for backward)
     */
    skip: function (elementId, seconds) {
        const instance = this.instances.get(elementId);
        if (instance?.video) {
            const duration = instance.video.duration;
            const currentTime = instance.video.currentTime;

            // Guard against NaN/Infinity values when video isn't loaded
            if (!isFinite(duration) || !isFinite(currentTime) || !isFinite(seconds)) {
                console.warn('[VideoInterop] Skip failed: non-finite values detected',
                    { duration, currentTime, seconds });
                return instance.video.currentTime || 0;
            }

            const newTime = Math.max(0, Math.min(duration, currentTime + seconds));
            instance.video.currentTime = newTime;
            return newTime;
        }
        return 0;
    },

    /**
     * Set subtitle track by language code
     * @param {string} elementId - The video element ID
     * @param {string} languageCode - Language code (e.g., 'en', 'it') or 'off'
     */
    setSubtitleTrack: function (elementId, languageCode) {
        const instance = this.instances.get(elementId);
        if (!instance?.video) return false;

        const tracks = instance.video.textTracks;
        console.log('[VideoInterop] Setting subtitle track:', languageCode, 'Available tracks:', tracks.length);

        for (let i = 0; i < tracks.length; i++) {
            const track = tracks[i];
            // Compare by language property (corresponds to srclang attribute)
            const shouldShow = languageCode !== 'off' && track.language === languageCode;
            track.mode = shouldShow ? 'showing' : 'hidden';
            console.log('[VideoInterop] Track', i, ':', track.language, '->', track.mode);
        }
        return true;
    },

    // Private methods
    _bindEvents: function (elementId, video, dotNetRef) {
        const handlers = {
            timeupdate: () => this._onTimeUpdate(elementId, video, dotNetRef),
            play: () => this._invokeCallback(dotNetRef, 'OnPlay'),
            pause: () => this._invokeCallback(dotNetRef, 'OnPause'),
            ended: () => this._invokeCallback(dotNetRef, 'OnEnded'),
            loadedmetadata: () => this._onLoadedMetadata(elementId, video, dotNetRef),
            volumechange: () => this._onVolumeChange(video, dotNetRef),
            ratechange: () => this._onRateChange(video, dotNetRef),
            waiting: () => this._invokeCallback(dotNetRef, 'OnBuffering', true),
            canplay: () => this._invokeCallback(dotNetRef, 'OnBuffering', false),
            error: (e) => this._onError(video, dotNetRef, e),
            progress: () => this._onProgress(video, dotNetRef)
        };

        // Store handlers for cleanup
        video._handlers = handlers;

        // Bind all events
        Object.entries(handlers).forEach(([event, handler]) => {
            video.addEventListener(event, handler);
        });

        // Fullscreen change
        document.addEventListener('fullscreenchange', () => {
            this._invokeCallback(dotNetRef, 'OnFullscreenChange', !!document.fullscreenElement);
        });
    },

    _unbindEvents: function (elementId, video) {
        if (video._handlers) {
            Object.entries(video._handlers).forEach(([event, handler]) => {
                video.removeEventListener(event, handler);
            });
            delete video._handlers;
        }
    },

    _onTimeUpdate: function (elementId, video, dotNetRef) {
        // Throttle time updates to ~4 per second
        const now = Date.now();
        const lastUpdate = video._lastTimeUpdate || 0;
        if (now - lastUpdate < 250) return;
        video._lastTimeUpdate = now;

        this._invokeCallback(dotNetRef, 'OnTimeUpdate', video.currentTime);
    },

    _onLoadedMetadata: function (elementId, video, dotNetRef) {
        this._invokeCallback(dotNetRef, 'OnMetadataLoaded', {
            duration: video.duration,
            videoWidth: video.videoWidth,
            videoHeight: video.videoHeight
        });
    },

    _onVolumeChange: function (video, dotNetRef) {
        this._invokeCallback(dotNetRef, 'OnVolumeChange', {
            volume: video.volume,
            muted: video.muted
        });
    },

    _onRateChange: function (video, dotNetRef) {
        this._invokeCallback(dotNetRef, 'OnRateChange', video.playbackRate);
    },

    _onError: function (video, dotNetRef, e) {
        const error = video.error;
        const errorCode = error ? error.code : 0;

        // If mpegts.js is handling this video, ignore "format not supported" errors
        // because mpegts.js uses MediaSource Extensions, not native video format support
        if ((video._mpegtsPlayer || video._usingMpegTs) && errorCode === 4) {
            console.log('[VideoInterop] Ignoring format error - mpegts.js is handling playback');
            return; // Don't report error - mpegts.js will handle it
        }

        const errorMessage = error ? this._getErrorMessage(errorCode) : 'Unknown error';
        console.error('[VideoInterop] Video error:', errorCode, errorMessage);
        this._invokeCallback(dotNetRef, 'OnError', errorMessage);
    },

    _onProgress: function (video, dotNetRef) {
        const buffered = this._getBufferedPercentage(video);
        this._invokeCallback(dotNetRef, 'OnBufferProgress', buffered);
    },

    _getBufferedPercentage: function (video) {
        if (video.buffered.length > 0 && video.duration > 0) {
            return (video.buffered.end(video.buffered.length - 1) / video.duration) * 100;
        }
        return 0;
    },

    _getErrorMessage: function (code) {
        switch (code) {
            case 1: return 'Video loading aborted';
            case 2: return 'Network error occurred';
            case 3: return 'Video decoding failed';
            case 4: return 'Video format not supported';
            default: return 'Unknown error';
        }
    },

    _invokeCallback: function (dotNetRef, methodName, arg) {
        try {
            if (arg !== undefined) {
                dotNetRef.invokeMethodAsync(methodName, arg);
            } else {
                dotNetRef.invokeMethodAsync(methodName);
            }
        } catch (err) {
            console.warn('[VideoInterop] Callback failed:', methodName, err.message);
        }
    },

    /**
     * Load video with authentication token (for protected endpoints)
     * Creates a Blob URL from authenticated fetch response
     * @param {string} elementId - The video element ID
     * @param {string} videoUrl - The video URL to fetch
     * @param {string} token - JWT Bearer token
     * @returns {Promise<object>} Result with success status and message
     */
    loadAuthenticatedVideo: async function (elementId, videoUrl, token) {
        console.log('[VideoInterop] Loading authenticated video:', videoUrl);

        const instance = this.instances.get(elementId);
        const video = instance?.video || document.getElementById(elementId);

        if (!video) {
            console.error('[VideoInterop] Video element not found:', elementId);
            return { success: false, message: 'Video element not found', statusCode: 0 };
        }

        try {
            // Fetch video with Authorization header
            const response = await fetch(videoUrl, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Accept': 'video/*'
                }
            });

            console.log('[VideoInterop] Fetch response:', response.status, response.statusText);

            if (!response.ok) {
                const errorMsg = `HTTP ${response.status}: ${response.statusText}`;
                console.error('[VideoInterop] Authenticated fetch failed:', errorMsg);
                return {
                    success: false,
                    message: errorMsg,
                    statusCode: response.status,
                    isAuthError: response.status === 401 || response.status === 403
                };
            }

            // Get Content-Type from response headers
            const contentType = response.headers.get('Content-Type') || '';
            console.log('[VideoInterop] Content-Type from headers:', contentType);

            // Create Blob from response
            const blob = await response.blob();
            console.log('[VideoInterop] Blob created:', blob.size, 'bytes, type:', blob.type);

            // Detect video format and use appropriate player library
            const formatInfo = this._detectVideoFormat(contentType, blob.type, videoUrl);
            console.log('[VideoInterop] Format detected:', formatInfo);

            // Use specialized player based on format
            switch (formatInfo.format) {
                case 'mpegts':
                    console.log('[VideoInterop] MPEG-TS format detected, using mpegts.js player');
                    return await this._loadWithMpegTs(elementId, video, blob, response.status);

                case 'hls':
                    console.log('[VideoInterop] HLS format detected, using hls.js player');
                    return await this._loadWithHls(elementId, video, videoUrl, token, response.status);

                case 'dash':
                    console.log('[VideoInterop] DASH format detected, using dash.js player');
                    return await this._loadWithDash(elementId, video, videoUrl, token, response.status);

                case 'flv':
                    console.log('[VideoInterop] FLV format detected, using flv.js player');
                    return await this._loadWithFlv(elementId, video, blob, response.status);

                default:
                    console.log('[VideoInterop] Standard HTML5 format, using native player');
            }

            // Standard HTML5 video (MP4, WebM, OGG, etc.)
            const blobUrl = URL.createObjectURL(blob);
            console.log('[VideoInterop] Blob URL created:', blobUrl.substring(0, 50) + '...');

            // Assign to video element
            video.src = blobUrl;

            // Store blob URL for cleanup
            video._blobUrl = blobUrl;

            return {
                success: true,
                message: 'Video loaded successfully',
                statusCode: response.status,
                blobUrl: blobUrl,
                size: blob.size,
                contentType: blob.type
            };

        } catch (error) {
            console.error('[VideoInterop] Authenticated video load error:', error);
            return {
                success: false,
                message: error.message || 'Network error',
                statusCode: 0,
                isNetworkError: true
            };
        }
    },

    /**
     * Detect video format from content type, blob type, and URL
     * @param {string} contentType - Content-Type header
     * @param {string} blobType - Blob MIME type
     * @param {string} url - Video URL
     * @returns {object} Format info with format name and library
     */
    _detectVideoFormat: function (contentType, blobType, url) {
        const ct = (contentType || '').toLowerCase();
        const bt = (blobType || '').toLowerCase();
        const urlLower = (url || '').toLowerCase();

        // MPEG-TS detection
        if (ct.includes('mp2t') || bt.includes('mp2t') || ct.includes('mpeg-ts') ||
            urlLower.endsWith('.ts') || urlLower.includes('.ts?')) {
            return { format: 'mpegts', library: 'mpegts.js' };
        }

        // HLS detection (.m3u8)
        if (ct.includes('mpegurl') || ct.includes('x-mpegurl') ||
            urlLower.endsWith('.m3u8') || urlLower.includes('.m3u8?') ||
            ct.includes('application/vnd.apple.mpegurl')) {
            return { format: 'hls', library: 'hls.js' };
        }

        // DASH detection (.mpd)
        if (ct.includes('dash+xml') || urlLower.endsWith('.mpd') || urlLower.includes('.mpd?')) {
            return { format: 'dash', library: 'dash.js' };
        }

        // FLV detection
        if (ct.includes('video/x-flv') || bt.includes('video/x-flv') ||
            urlLower.endsWith('.flv') || urlLower.includes('.flv?')) {
            return { format: 'flv', library: 'flv.js' };
        }

        // Standard HTML5 formats (MP4, WebM, OGG)
        if (ct.includes('video/mp4') || bt.includes('video/mp4')) {
            return { format: 'mp4', library: 'native' };
        }
        if (ct.includes('video/webm') || bt.includes('video/webm')) {
            return { format: 'webm', library: 'native' };
        }
        if (ct.includes('video/ogg') || bt.includes('video/ogg')) {
            return { format: 'ogg', library: 'native' };
        }

        // Default to native player
        return { format: 'unknown', library: 'native' };
    },

    /**
     * Load MPEG-TS video using mpegts.js library
     * @param {string} elementId - The video element ID
     * @param {HTMLVideoElement} video - The video element
     * @param {Blob} blob - The video blob
     * @param {number} statusCode - HTTP status code
     * @returns {Promise<object>} Result with success status
     */
    _loadWithMpegTs: async function (elementId, video, blob, statusCode) {
        // Check if mpegts.js is available
        if (typeof mpegts === 'undefined') {
            console.error('[VideoInterop] mpegts.js library not loaded!');
            return {
                success: false,
                message: 'MPEG-TS player library not available. Please refresh the page.',
                statusCode: statusCode,
                contentType: blob.type
            };
        }

        // Check browser support
        if (!mpegts.isSupported()) {
            console.error('[VideoInterop] mpegts.js: Browser not supported');
            return {
                success: false,
                message: 'Your browser does not support MPEG-TS video playback',
                statusCode: statusCode,
                contentType: blob.type
            };
        }

        try {
            // Mark video as using mpegts.js (before player creation for error handling)
            video._usingMpegTs = true;

            // Cleanup any existing mpegts player
            if (video._mpegtsPlayer) {
                console.log('[VideoInterop] Destroying existing mpegts player');
                video._mpegtsPlayer.destroy();
                video._mpegtsPlayer = null;
            }

            // Clear any existing src (prevents HTML5 video errors)
            video.src = '';
            video.removeAttribute('src');

            // Create blob URL for mpegts.js
            const blobUrl = URL.createObjectURL(blob);
            video._blobUrl = blobUrl;

            // Create mpegts.js player
            const player = mpegts.createPlayer({
                type: 'mpegts',
                isLive: false,
                url: blobUrl
            }, {
                enableWorker: true,
                enableStashBuffer: true,
                stashInitialSize: 128 * 1024, // 128KB initial buffer
                autoCleanupSourceBuffer: true,
                autoCleanupMaxBackwardDuration: 60,
                autoCleanupMinBackwardDuration: 30
            });

            // Attach to video element
            player.attachMediaElement(video);

            // Store player reference for cleanup
            video._mpegtsPlayer = player;

            // Load the video
            player.load();

            // Handle player events
            player.on(mpegts.Events.ERROR, (errorType, errorDetail, errorInfo) => {
                console.error('[VideoInterop] mpegts.js error:', errorType, errorDetail, errorInfo);
            });

            player.on(mpegts.Events.LOADING_COMPLETE, () => {
                console.log('[VideoInterop] mpegts.js loading complete');
            });

            player.on(mpegts.Events.MEDIA_INFO, (mediaInfo) => {
                console.log('[VideoInterop] mpegts.js media info:', mediaInfo);
            });

            console.log('[VideoInterop] mpegts.js player attached successfully');

            return {
                success: true,
                message: 'MPEG-TS video loaded with mpegts.js player',
                statusCode: statusCode,
                blobUrl: blobUrl,
                size: blob.size,
                contentType: blob.type,
                playerType: 'mpegts'
            };

        } catch (error) {
            console.error('[VideoInterop] mpegts.js initialization error:', error);
            return {
                success: false,
                message: `MPEG-TS player error: ${error.message}`,
                statusCode: statusCode,
                contentType: blob.type
            };
        }
    },

    /**
     * Load HLS video using hls.js library
     * @param {string} elementId - The video element ID
     * @param {HTMLVideoElement} video - The video element
     * @param {string} videoUrl - The HLS manifest URL
     * @param {string} token - JWT Bearer token
     * @param {number} statusCode - HTTP status code
     * @returns {Promise<object>} Result with success status
     */
    _loadWithHls: async function (elementId, video, videoUrl, token, statusCode) {
        // Check if hls.js is available
        if (typeof Hls === 'undefined') {
            console.error('[VideoInterop] hls.js library not loaded!');
            return {
                success: false,
                message: 'HLS player library not available. Please refresh the page.',
                statusCode: statusCode
            };
        }

        // Check if browser supports HLS natively (Safari)
        if (video.canPlayType('application/vnd.apple.mpegurl')) {
            console.log('[VideoInterop] Native HLS support detected, using native player');
            video.src = videoUrl;
            return {
                success: true,
                message: 'HLS video loaded with native player',
                statusCode: statusCode,
                playerType: 'native-hls'
            };
        }

        // Check hls.js browser support
        if (!Hls.isSupported()) {
            console.error('[VideoInterop] hls.js: Browser not supported');
            return {
                success: false,
                message: 'Your browser does not support HLS video playback',
                statusCode: statusCode
            };
        }

        try {
            // Cleanup any existing HLS player
            if (video._hlsPlayer) {
                console.log('[VideoInterop] Destroying existing HLS player');
                video._hlsPlayer.destroy();
                video._hlsPlayer = null;
            }

            // Create hls.js player with auth headers
            const hls = new Hls({
                xhrSetup: function (xhr, url) {
                    if (token) {
                        xhr.setRequestHeader('Authorization', `Bearer ${token}`);
                    }
                },
                enableWorker: true,
                lowLatencyMode: false
            });

            // Store player reference
            video._hlsPlayer = hls;
            video._usingHls = true;

            // Load source and attach to video
            hls.loadSource(videoUrl);
            hls.attachMedia(video);

            // Handle events
            hls.on(Hls.Events.MANIFEST_PARSED, () => {
                console.log('[VideoInterop] HLS manifest parsed successfully');
            });

            hls.on(Hls.Events.ERROR, (event, data) => {
                if (data.fatal) {
                    console.error('[VideoInterop] HLS fatal error:', data.type, data.details);
                } else {
                    console.warn('[VideoInterop] HLS non-fatal error:', data.type, data.details);
                }
            });

            console.log('[VideoInterop] hls.js player attached successfully');

            return {
                success: true,
                message: 'HLS video loaded with hls.js player',
                statusCode: statusCode,
                playerType: 'hls'
            };

        } catch (error) {
            console.error('[VideoInterop] hls.js initialization error:', error);
            return {
                success: false,
                message: `HLS player error: ${error.message}`,
                statusCode: statusCode
            };
        }
    },

    /**
     * Load DASH video using dash.js library
     * @param {string} elementId - The video element ID
     * @param {HTMLVideoElement} video - The video element
     * @param {string} videoUrl - The DASH manifest URL
     * @param {string} token - JWT Bearer token
     * @param {number} statusCode - HTTP status code
     * @returns {Promise<object>} Result with success status
     */
    _loadWithDash: async function (elementId, video, videoUrl, token, statusCode) {
        // Check if dash.js is available
        if (typeof dashjs === 'undefined') {
            console.error('[VideoInterop] dash.js library not loaded!');
            return {
                success: false,
                message: 'DASH player library not available. Please refresh the page.',
                statusCode: statusCode
            };
        }

        try {
            // Cleanup any existing DASH player
            if (video._dashPlayer) {
                console.log('[VideoInterop] Destroying existing DASH player');
                video._dashPlayer.reset();
                video._dashPlayer = null;
            }

            // Create dash.js player
            const player = dashjs.MediaPlayer().create();

            // Configure auth headers if token provided
            if (token) {
                player.extend('RequestModifier', function () {
                    return {
                        modifyRequestHeader: function (xhr) {
                            xhr.setRequestHeader('Authorization', `Bearer ${token}`);
                            return xhr;
                        }
                    };
                }, true);
            }

            // Store player reference
            video._dashPlayer = player;
            video._usingDash = true;

            // Initialize and load
            player.initialize(video, videoUrl, false);

            // Handle events
            player.on(dashjs.MediaPlayer.events.MANIFEST_LOADED, () => {
                console.log('[VideoInterop] DASH manifest loaded successfully');
            });

            player.on(dashjs.MediaPlayer.events.ERROR, (e) => {
                console.error('[VideoInterop] DASH error:', e.error);
            });

            console.log('[VideoInterop] dash.js player attached successfully');

            return {
                success: true,
                message: 'DASH video loaded with dash.js player',
                statusCode: statusCode,
                playerType: 'dash'
            };

        } catch (error) {
            console.error('[VideoInterop] dash.js initialization error:', error);
            return {
                success: false,
                message: `DASH player error: ${error.message}`,
                statusCode: statusCode
            };
        }
    },

    /**
     * Load FLV video using flv.js library
     * @param {string} elementId - The video element ID
     * @param {HTMLVideoElement} video - The video element
     * @param {Blob} blob - The video blob
     * @param {number} statusCode - HTTP status code
     * @returns {Promise<object>} Result with success status
     */
    _loadWithFlv: async function (elementId, video, blob, statusCode) {
        // Check if flv.js is available
        if (typeof flvjs === 'undefined') {
            console.error('[VideoInterop] flv.js library not loaded!');
            return {
                success: false,
                message: 'FLV player library not available. Please refresh the page.',
                statusCode: statusCode,
                contentType: blob.type
            };
        }

        // Check browser support
        if (!flvjs.isSupported()) {
            console.error('[VideoInterop] flv.js: Browser not supported');
            return {
                success: false,
                message: 'Your browser does not support FLV video playback',
                statusCode: statusCode,
                contentType: blob.type
            };
        }

        try {
            // Cleanup any existing FLV player
            if (video._flvPlayer) {
                console.log('[VideoInterop] Destroying existing FLV player');
                video._flvPlayer.destroy();
                video._flvPlayer = null;
            }

            // Clear any existing src
            video.src = '';
            video.removeAttribute('src');

            // Create blob URL
            const blobUrl = URL.createObjectURL(blob);
            video._blobUrl = blobUrl;

            // Create flv.js player
            const player = flvjs.createPlayer({
                type: 'flv',
                url: blobUrl,
                isLive: false
            }, {
                enableWorker: true,
                enableStashBuffer: true
            });

            // Attach to video element
            player.attachMediaElement(video);

            // Store player reference
            video._flvPlayer = player;
            video._usingFlv = true;

            // Load the video
            player.load();

            // Handle events
            player.on(flvjs.Events.ERROR, (errorType, errorDetail, errorInfo) => {
                console.error('[VideoInterop] flv.js error:', errorType, errorDetail, errorInfo);
            });

            player.on(flvjs.Events.LOADING_COMPLETE, () => {
                console.log('[VideoInterop] flv.js loading complete');
            });

            console.log('[VideoInterop] flv.js player attached successfully');

            return {
                success: true,
                message: 'FLV video loaded with flv.js player',
                statusCode: statusCode,
                blobUrl: blobUrl,
                size: blob.size,
                contentType: blob.type,
                playerType: 'flv'
            };

        } catch (error) {
            console.error('[VideoInterop] flv.js initialization error:', error);
            return {
                success: false,
                message: `FLV player error: ${error.message}`,
                statusCode: statusCode,
                contentType: blob.type
            };
        }
    },

    /**
     * Load video directly (for public endpoints) with diagnostic logging
     * @param {string} elementId - The video element ID
     * @param {string} videoUrl - The video URL
     * @returns {Promise<object>} Result with diagnostic info
     */
    loadDirectVideo: async function (elementId, videoUrl) {
        console.log('[VideoInterop] Loading direct video:', videoUrl);

        const video = document.getElementById(elementId);
        if (!video) {
            return { success: false, message: 'Video element not found' };
        }

        // First, try a HEAD request to check if the video is accessible
        try {
            const headResponse = await fetch(videoUrl, { method: 'HEAD' });
            console.log('[VideoInterop] HEAD check:', headResponse.status, headResponse.statusText);

            if (headResponse.status === 401 || headResponse.status === 403) {
                return {
                    success: false,
                    message: `Authentication required (HTTP ${headResponse.status})`,
                    statusCode: headResponse.status,
                    requiresAuth: true
                };
            }

            if (!headResponse.ok) {
                return {
                    success: false,
                    message: `HTTP ${headResponse.status}: ${headResponse.statusText}`,
                    statusCode: headResponse.status
                };
            }

            // Video is accessible, set the source
            video.src = videoUrl;
            return {
                success: true,
                message: 'Video source set',
                statusCode: headResponse.status,
                contentType: headResponse.headers.get('Content-Type'),
                contentLength: headResponse.headers.get('Content-Length')
            };

        } catch (error) {
            console.error('[VideoInterop] Direct video load error:', error);
            return {
                success: false,
                message: error.message || 'Network error',
                isNetworkError: true
            };
        }
    },

    /**
     * Revoke Blob URL to free memory (call when disposing)
     * @param {string} elementId - The video element ID
     */
    revokeBlobUrl: function (elementId) {
        const video = document.getElementById(elementId);
        if (video?._blobUrl) {
            URL.revokeObjectURL(video._blobUrl);
            console.log('[VideoInterop] Blob URL revoked for:', elementId);
            delete video._blobUrl;
        }
    }
};

// Keyboard shortcuts (when video container is focused)
document.addEventListener('keydown', function (e) {
    const activeElement = document.activeElement;
    const container = activeElement?.closest('.smart-video-container');
    if (!container) return;

    const videoId = container.querySelector('video')?.id;
    if (!videoId) return;

    switch (e.key) {
        case ' ':
        case 'k':
            e.preventDefault();
            const state = window.VideoInterop.getState(videoId);
            if (state?.paused) {
                window.VideoInterop.play(videoId);
            } else {
                window.VideoInterop.pause(videoId);
            }
            break;
        case 'ArrowLeft':
            e.preventDefault();
            window.VideoInterop.skip(videoId, -10);
            break;
        case 'ArrowRight':
            e.preventDefault();
            window.VideoInterop.skip(videoId, 10);
            break;
        case 'ArrowUp':
            e.preventDefault();
            const vol = window.VideoInterop.getState(videoId)?.volume || 0.5;
            window.VideoInterop.setVolume(videoId, Math.min(1, vol + 0.1));
            break;
        case 'ArrowDown':
            e.preventDefault();
            const vol2 = window.VideoInterop.getState(videoId)?.volume || 0.5;
            window.VideoInterop.setVolume(videoId, Math.max(0, vol2 - 0.1));
            break;
        case 'm':
            e.preventDefault();
            window.VideoInterop.toggleMute(videoId);
            break;
        case 'f':
            e.preventDefault();
            window.VideoInterop.toggleFullscreen(videoId);
            break;
    }
});
