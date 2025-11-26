/**
 * Session Timeout - User Activity Tracking
 * Monitors user activity for automatic session expiration
 * Part of InsightLearn v2.1.0-dev security features
 */
window.sessionTimeout = {
    dotNetHelper: null,
    debounceTimer: null,

    /**
     * Initialize activity tracking with DotNet interop reference
     * @param {object} dotNetRef - DotNetObjectReference for callback
     */
    initialize: function(dotNetRef) {
        this.dotNetHelper = dotNetRef;

        // Track mouse movement (debounced to avoid excessive calls)
        document.addEventListener('mousemove', this.handleActivity.bind(this));

        // Track keyboard activity
        document.addEventListener('keydown', this.handleActivity.bind(this));

        // Track mouse clicks
        document.addEventListener('click', this.handleActivity.bind(this));

        // Track scroll events
        document.addEventListener('scroll', this.handleActivity.bind(this));

        // Track touch events (mobile)
        document.addEventListener('touchstart', this.handleActivity.bind(this));

        // Track focus/visibility changes
        document.addEventListener('visibilitychange', this.handleVisibilityChange.bind(this));

        console.log('[SessionTimeout] Activity tracking initialized');
    },

    /**
     * Handle user activity events with debouncing
     */
    handleActivity: function() {
        // Debounce: Only report activity once every 5 seconds
        if (this.debounceTimer) {
            return;
        }

        this.debounceTimer = setTimeout(() => {
            this.debounceTimer = null;
        }, 5000);

        this.reportActivity();
    },

    /**
     * Report activity to Blazor
     */
    reportActivity: function() {
        if (this.dotNetHelper) {
            try {
                this.dotNetHelper.invokeMethodAsync('OnUserActivity');
            } catch (error) {
                // DotNet reference may be disposed during logout
                console.debug('[SessionTimeout] Could not report activity:', error.message);
            }
        }
    },

    /**
     * Handle document visibility changes (tab switch, minimize)
     */
    handleVisibilityChange: function() {
        if (document.visibilityState === 'visible') {
            // User returned to tab - report activity
            this.reportActivity();
            console.log('[SessionTimeout] User returned to tab');
        }
    },

    /**
     * Clean up event listeners
     */
    dispose: function() {
        document.removeEventListener('mousemove', this.handleActivity);
        document.removeEventListener('keydown', this.handleActivity);
        document.removeEventListener('click', this.handleActivity);
        document.removeEventListener('scroll', this.handleActivity);
        document.removeEventListener('touchstart', this.handleActivity);
        document.removeEventListener('visibilitychange', this.handleVisibilityChange);
        this.dotNetHelper = null;
        console.log('[SessionTimeout] Activity tracking disposed');
    }
};
