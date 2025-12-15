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

        // Error handling with Firefox codec detection
        video.addEventListener('error', function (e) {
            console.error(`[VideoPlayer] Error loading video:`, e);

            // Detect specific error codes
            let errorMessage = 'Video playback error';
            let errorCode = video.error ? video.error.code : 0;

            switch (errorCode) {
                case MediaError.MEDIA_ERR_ABORTED:
                    errorMessage = 'Video playback was aborted';
                    break;
                case MediaError.MEDIA_ERR_NETWORK:
                    errorMessage = 'Network error while loading video';
                    break;
                case MediaError.MEDIA_ERR_DECODE:
                    // Codec error - likely Firefox/Linux H.264 issue
                    errorMessage = 'Video codec not supported. Firefox on Linux may require additional codecs for MP4/H.264 videos. Try using Chrome/Edge or install OpenH264 codec.';
                    break;
                case MediaError.MEDIA_ERR_SRC_NOT_SUPPORTED:
                    errorMessage = 'Video format not supported by your browser';
                    break;
            }

            // Notify .NET component of the error
            if (video.dotNetRef) {
                video.dotNetRef.invokeMethodAsync('OnVideoErrorFromJS', errorMessage, errorCode);
            }
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
     * Set subtitle track by language code
     * @param {string} videoId - Video element ID
     * @param {string|null} language - Language code (e.g., "en", "it") or null to turn off subtitles
     */
    setSubtitle: function (videoId, language) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return;
        }

        const tracks = video.textTracks;
        if (!tracks || tracks.length === 0) {
            console.warn(`[VideoPlayer] No subtitle tracks available for ${videoId}`);
            return;
        }

        // Disable all tracks first
        for (let i = 0; i < tracks.length; i++) {
            tracks[i].mode = 'disabled';
        }

        // If language is provided, enable the matching track
        if (language) {
            for (let i = 0; i < tracks.length; i++) {
                if (tracks[i].language === language) {
                    tracks[i].mode = 'showing';
                    console.log(`[VideoPlayer] Subtitle track enabled: ${tracks[i].label} (${language})`);
                    return;
                }
            }
            console.warn(`[VideoPlayer] Subtitle track for language '${language}' not found`);
        } else {
            console.log(`[VideoPlayer] Subtitles turned off`);
        }
    },

    /**
     * Get available subtitle tracks
     * @param {string} videoId - Video element ID
     * @returns {Array} Array of available subtitle tracks with language and label
     */
    getSubtitleTracks: function (videoId) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return [];
        }

        const tracks = video.textTracks;
        const result = [];

        if (tracks) {
            for (let i = 0; i < tracks.length; i++) {
                result.push({
                    language: tracks[i].language,
                    label: tracks[i].label,
                    kind: tracks[i].kind,
                    mode: tracks[i].mode
                });
            }
        }

        return result;
    },

    /**
     * Create a Blob URL from WebVTT content for translated subtitles
     * @param {string} vttContent - WebVTT file content as string
     * @returns {string} Blob URL that can be used as track src
     */
    createSubtitleBlobUrl: function (vttContent) {
        const blob = new Blob([vttContent], { type: 'text/vtt' });
        const url = URL.createObjectURL(blob);
        console.log(`[VideoPlayer] Created subtitle blob URL: ${url.substring(0, 50)}...`);
        return url;
    },

    /**
     * Add translated subtitle track to video element and enable it
     * @param {string} videoId - Video element ID
     * @param {string} vttUrl - URL to the translated VTT file (can be blob URL)
     * @param {string} language - Language code (e.g., "es", "fr")
     * @param {string} label - Human-readable label (e.g., "Español (AI)")
     */
    addTranslatedSubtitle: function (videoId, vttUrl, language, label) {
        const video = document.getElementById(videoId);
        if (!video) {
            console.error(`[VideoPlayer] Video element '${videoId}' not found`);
            return;
        }

        // Remove any existing translated track with same language
        const existingTracks = video.querySelectorAll(`track[data-translated="${language}"]`);
        existingTracks.forEach(t => t.remove());

        // Disable all current tracks
        for (let i = 0; i < video.textTracks.length; i++) {
            video.textTracks[i].mode = 'disabled';
        }

        // Create and add new translated track
        const track = document.createElement('track');
        track.kind = 'subtitles';
        track.label = `${label} (AI)`;
        track.srclang = language;
        track.src = vttUrl;
        track.default = true;
        track.setAttribute('data-translated', language);

        // Add track to video
        video.appendChild(track);

        // Enable the track after it's loaded
        track.addEventListener('load', function() {
            for (let i = 0; i < video.textTracks.length; i++) {
                if (video.textTracks[i].language === language &&
                    video.textTracks[i].label.includes('(AI)')) {
                    video.textTracks[i].mode = 'showing';
                    console.log(`[VideoPlayer] Translated subtitle enabled: ${label} (${language})`);
                    break;
                }
            }
        });

        // Force track to load
        track.track.mode = 'showing';

        console.log(`[VideoPlayer] Added translated subtitle track: ${label} (${language})`);
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
