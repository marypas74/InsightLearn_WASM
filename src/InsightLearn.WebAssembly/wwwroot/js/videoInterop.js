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
            instance.video.currentTime = timeSeconds;
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
            const newTime = Math.max(0, Math.min(
                instance.video.duration,
                instance.video.currentTime + seconds
            ));
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
        const errorMessage = error ? this._getErrorMessage(error.code) : 'Unknown error';
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
