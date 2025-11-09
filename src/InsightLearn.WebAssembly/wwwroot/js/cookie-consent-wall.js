// Cookie Consent Wall JavaScript
// Handles cookie consent storage, validation, and third-party script loading

window.cookieConsentWall = {
    // Check if user has valid consent
    hasValidConsent: function() {
        try {
            const consentData = localStorage.getItem('cookie-consent');
            if (!consentData) {
                console.log('[COOKIE-WALL-JS] No consent found');
                return false;
            }

            const consent = JSON.parse(consentData);
            const now = new Date().getTime();
            const expiryDays = 365; // 1 year
            const expiryTime = consent.timestamp + (expiryDays * 24 * 60 * 60 * 1000);

            if (now > expiryTime) {
                console.log('[COOKIE-WALL-JS] Consent expired');
                localStorage.removeItem('cookie-consent');
                return false;
            }

            console.log('[COOKIE-WALL-JS] Valid consent found');
            return true;
        } catch (e) {
            console.error('[COOKIE-WALL-JS] Error checking consent:', e);
            return false;
        }
    },

    // Get user's saved preferences
    getPreferences: function() {
        try {
            const consentData = localStorage.getItem('cookie-consent');
            if (!consentData) {
                return {
                    Necessary: true,
                    Analytics: false,
                    Marketing: false,
                    Functional: false
                };
            }

            const consent = JSON.parse(consentData);
            return {
                Necessary: true, // Always true
                Analytics: consent.analytics || false,
                Marketing: consent.marketing || false,
                Functional: consent.functional || false
            };
        } catch (e) {
            console.error('[COOKIE-WALL-JS] Error getting preferences:', e);
            return {
                Necessary: true,
                Analytics: false,
                Marketing: false,
                Functional: false
            };
        }
    },

    // Save user consent
    saveConsent: function(preferences) {
        try {
            const consentData = {
                necessary: true,
                analytics: preferences.Analytics || false,
                marketing: preferences.Marketing || false,
                functional: preferences.Functional || false,
                timestamp: new Date().getTime()
            };

            localStorage.setItem('cookie-consent', JSON.stringify(consentData));
            console.log('[COOKIE-WALL-JS] Consent saved:', consentData);

            // Unblock interaction
            this.unblockInteraction();
        } catch (e) {
            console.error('[COOKIE-WALL-JS] Error saving consent:', e);
        }
    },

    // Block user interaction with page
    blockInteraction: function() {
        // Only block if not on an auth page
        const currentPath = window.location.pathname.toLowerCase();
        if (currentPath.includes('/login') ||
            currentPath.includes('/register') ||
            currentPath.includes('/forgot-password') ||
            currentPath.includes('/reset-password')) {
            console.log('[COOKIE-WALL-JS] On auth page - not blocking interaction');
            return;
        }

        console.log('[COOKIE-WALL-JS] Blocking page interaction');
        document.body.style.overflow = 'hidden';

        // Add pointer-events blocking for better isolation
        const overlay = document.querySelector('.cookie-wall-overlay');
        if (overlay) {
            overlay.style.pointerEvents = 'auto';
        }
    },

    // Unblock user interaction
    unblockInteraction: function() {
        console.log('[COOKIE-WALL-JS] Unblocking page interaction');
        document.body.style.overflow = '';

        // Remove pointer-events blocking
        const overlay = document.querySelector('.cookie-wall-overlay');
        if (overlay) {
            overlay.style.pointerEvents = 'none';
        }
    },

    // Load Google Analytics (if consent given)
    loadAnalytics: function() {
        if (window.gtag) {
            console.log('[COOKIE-WALL-JS] Google Analytics already loaded');
            return;
        }

        console.log('[COOKIE-WALL-JS] Loading Google Analytics...');
        // Add your Google Analytics tracking code here
        // Example: (uncomment and add your GA tracking ID)
        // const script = document.createElement('script');
        // script.async = true;
        // script.src = 'https://www.googletagmanager.com/gtag/js?id=G-XXXXXXXXXX';
        // document.head.appendChild(script);
        //
        // window.dataLayer = window.dataLayer || [];
        // function gtag(){dataLayer.push(arguments);}
        // gtag('js', new Date());
        // gtag('config', 'G-XXXXXXXXXX');
    },

    // Load marketing scripts (if consent given)
    loadMarketing: function() {
        console.log('[COOKIE-WALL-JS] Loading marketing scripts...');
        // Add Facebook Pixel, Google Ads, etc. here
    },

    // Enable functional cookies
    enableFunctional: function() {
        console.log('[COOKIE-WALL-JS] Enabling functional cookies...');
        // Add language preference, theme, etc. here
    }
};

// Log when script loads
console.log('[COOKIE-WALL-JS] Cookie Consent Wall JavaScript loaded');
