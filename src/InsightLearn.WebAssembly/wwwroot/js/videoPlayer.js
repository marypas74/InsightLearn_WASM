/**
 * VideoPlayer JavaScript Interop
 * Provides video control and event handling for Blazor VideoPlayer component
 * Part of Student Learning Space v2.1.0
 */

window.videoPlayer = {
    /**
     * Initialize video player with event listeners
     * @param {string} videoId - Unique video element ID
     * @param {DotNetObjectReference} dotNetRef - Reference to .NET VideoPlayer component
     */
    initialize: function (videoId, dotNetRef) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return;
        }

        console.log(`[VideoPlayer] Initializing ${videoId}`);

        // Store DotNetObjectReference for event callbacks
        video.dotNetRef = dotNetRef;

        // Time update event (fires every 250ms during playback)
        video.addEventListener('timeupdate', function () {
            if (video.dotNetRef) {
                video.dotNetRef.invokeMethodAsync('OnTimeUpdateFromJS',
                    video.currentTime,
                    video.duration || 0
                );
            }
        });

        // Play state changed (play)
        video.addEventListener('play', function () {
            if (video.dotNetRef) {
                video.dotNetRef.invokeMethodAsync('OnPlayStateChangedFromJS', true);
            }
        });

        // Play state changed (pause)
        video.addEventListener('pause', function () {
            if (video.dotNetRef) {
                video.dotNetRef.invokeMethodAsync('OnPlayStateChangedFromJS', false);
            }
        });

        // Video ended event
        video.addEventListener('ended', function () {
            if (video.dotNetRef) {
                video.dotNetRef.invokeMethodAsync('OnVideoEndedFromJS');
            }
        });

        // Metadata loaded (duration available)
        video.addEventListener('loadedmetadata', function () {
            console.log(`[VideoPlayer] Metadata loaded - Duration: ${video.duration}s`);
        });

        // Error handling
        video.addEventListener('error', function (e) {
            console.error(`[VideoPlayer] Error loading video:`, e);
        });

        console.log(`[VideoPlayer] ${videoId} initialized successfully`);
    },

    /**
     * Seek to specific timestamp
     * @param {string} videoId - Video element ID
     * @param {number} timestampSeconds - Timestamp in seconds
     */
    seekTo: function (videoId, timestampSeconds) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return;
        }

        video.currentTime = timestampSeconds;
        console.log(`[VideoPlayer] Seeked to ${timestampSeconds}s`);
    },

    /**
     * Get current playback time
     * @param {string} videoId - Video element ID
     * @returns {number} Current time in seconds
     */
    getCurrentTime: function (videoId) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return 0;
        }

        return video.currentTime;
    },

    /**
     * Play video
     * @param {string} videoId - Video element ID
     */
    play: function (videoId) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return;
        }

        video.play().catch(err => {
            console.error(`[VideoPlayer] Error playing video:`, err);
        });
    },

    /**
     * Pause video
     * @param {string} videoId - Video element ID
     */
    pause: function (videoId) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return;
        }

        video.pause();
    },

    /**
     * Get video duration
     * @param {string} videoId - Video element ID
     * @returns {number} Duration in seconds
     */
    getDuration: function (videoId) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return 0;
        }

        return video.duration || 0;
    },

    /**
     * Check if video is playing
     * @param {string} videoId - Video element ID
     * @returns {boolean} True if playing
     */
    isPlaying: function (videoId) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return false;
        }

        return !video.paused && !video.ended && video.readyState > 2;
    },

    /**
     * Set playback rate (speed)
     * @param {string} videoId - Video element ID
     * @param {number} rate - Playback rate (0.5 = half speed, 2.0 = double speed)
     */
    setPlaybackRate: function (videoId, rate) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return;
        }

        video.playbackRate = rate;
        console.log(`[VideoPlayer] Playback rate set to ${rate}x`);
    },

    /**
     * Set volume
     * @param {string} videoId - Video element ID
     * @param {number} volume - Volume level (0.0 to 1.0)
     */
    setVolume: function (videoId, volume) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return;
        }

        video.volume = Math.max(0, Math.min(1, volume));
        console.log(`[VideoPlayer] Volume set to ${video.volume}`);
    },

    /**
     * Toggle fullscreen mode
     * @param {string} videoId - Video element ID
     */
    toggleFullscreen: function (videoId) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return;
        }

        if (document.fullscreenElement) {
            document.exitFullscreen();
        } else {
            video.requestFullscreen().catch(err => {
                console.error(`[VideoPlayer] Error entering fullscreen:`, err);
            });
        }
    },

    /**
     * Dispose video player (cleanup event listeners)
     * @param {string} videoId - Video element ID
     */
    dispose: function (videoId) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.warn(`[VideoPlayer] Video element '${videoId}' not found during disposal`);
            return;
        }

        // Remove DotNetObjectReference
        if (video.dotNetRef) {
            video.dotNetRef = null;
        }

        // Pause video
        video.pause();

        // Remove all event listeners by cloning and replacing the element
        const newVideo = video.cloneNode(true);
        video.parentNode.replaceChild(newVideo, video);

        console.log(`[VideoPlayer] ${videoId} disposed successfully`);
    }
};

// Keyboard shortcuts (optional - can be enabled/disabled via component parameter)
window.videoPlayer.enableKeyboardShortcuts = function (videoId) {
    document.addEventListener('keydown', function (e) {
        const video = document.getElementById(videoId);
        if (!video || document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'TEXTAREA') {
            return;
        }

        switch (e.key) {
            case ' ': // Space - play/pause
                e.preventDefault();
                if (video.paused) {
                    window.videoPlayer.play(videoId);
                } else {
                    window.videoPlayer.pause(videoId);
                }
                break;
            case 'ArrowLeft': // Left arrow - seek back 5s
                e.preventDefault();
                window.videoPlayer.seekTo(videoId, Math.max(0, video.currentTime - 5));
                break;
            case 'ArrowRight': // Right arrow - seek forward 5s
                e.preventDefault();
                window.videoPlayer.seekTo(videoId, Math.min(video.duration, video.currentTime + 5));
                break;
            case 'f': // F - fullscreen
                e.preventDefault();
                window.videoPlayer.toggleFullscreen(videoId);
                break;
            case 'm': // M - mute/unmute
                e.preventDefault();
                video.muted = !video.muted;
                break;
        }
    });
};

console.log('[VideoPlayer] JavaScript module loaded');
