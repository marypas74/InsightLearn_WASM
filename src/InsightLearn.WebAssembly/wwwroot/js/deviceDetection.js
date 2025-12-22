/**
 * Device Detection Module for InsightLearn
 * Provides comprehensive device, browser, and platform detection
 * for adaptive rendering and responsive optimizations.
 *
 * Version: 1.0.0
 * Date: 2025-12-22
 */

window.deviceDetection = (function() {
    'use strict';

    let dotNetRef = null;
    let resizeTimeout = null;
    let previousWidth = 0;
    let previousHeight = 0;
    let previousOrientation = '';

    /**
     * Initialize the device detection module with .NET reference
     */
    function initialize(dotNetReference) {
        dotNetRef = dotNetReference;

        // Store initial values
        previousWidth = window.innerWidth;
        previousHeight = window.innerHeight;
        previousOrientation = getOrientation();

        // Set up event listeners
        window.addEventListener('resize', handleResize, { passive: true });
        window.addEventListener('orientationchange', handleOrientationChange, { passive: true });

        // Also listen for screen orientation API if available
        if (screen.orientation) {
            screen.orientation.addEventListener('change', handleOrientationChange, { passive: true });
        }

        // Add CSS class based on device type
        updateDeviceClasses();

        console.log('[DeviceDetection] Initialized successfully');
    }

    /**
     * Get comprehensive device information
     */
    function getDeviceInfo() {
        const ua = navigator.userAgent;
        const browser = detectBrowser(ua);
        const os = detectOS(ua);
        const deviceType = detectDeviceType();

        return {
            deviceType: deviceType,
            browser: browser.name,
            browserVersion: browser.version,
            operatingSystem: os.name,
            osVersion: os.version,
            isTouch: isTouchDevice(),
            viewportWidth: window.innerWidth,
            viewportHeight: window.innerHeight,
            pixelRatio: window.devicePixelRatio || 1,
            orientation: getOrientationEnum(),
            isStandalone: isStandaloneMode(),
            userAgent: ua,
            preferredColorScheme: getPreferredColorScheme(),
            prefersReducedMotion: prefersReducedMotion()
        };
    }

    /**
     * Detect browser name and version
     */
    function detectBrowser(ua) {
        const browsers = [
            { name: 'Edge', regex: /Edg\/(\d+)/ },
            { name: 'Chrome', regex: /Chrome\/(\d+)/ },
            { name: 'Firefox', regex: /Firefox\/(\d+)/ },
            { name: 'Safari', regex: /Version\/(\d+).*Safari/ },
            { name: 'Opera', regex: /OPR\/(\d+)/ },
            { name: 'Samsung', regex: /SamsungBrowser\/(\d+)/ },
            { name: 'UC Browser', regex: /UCBrowser\/(\d+)/ },
            { name: 'IE', regex: /MSIE (\d+)|Trident.*rv:(\d+)/ }
        ];

        for (const browser of browsers) {
            const match = ua.match(browser.regex);
            if (match) {
                return {
                    name: browser.name,
                    version: match[1] || match[2] || '0'
                };
            }
        }

        return { name: 'Unknown', version: '0' };
    }

    /**
     * Detect operating system
     */
    function detectOS(ua) {
        const systems = [
            { name: 'iOS', regex: /iPhone|iPad|iPod/, versionRegex: /OS (\d+[._]\d+)/ },
            { name: 'Android', regex: /Android/, versionRegex: /Android (\d+[._]?\d*)/ },
            { name: 'Windows', regex: /Windows NT/, versionRegex: /Windows NT (\d+\.\d+)/ },
            { name: 'macOS', regex: /Mac OS X/, versionRegex: /Mac OS X (\d+[._]\d+)/ },
            { name: 'Linux', regex: /Linux/, versionRegex: null },
            { name: 'Chrome OS', regex: /CrOS/, versionRegex: null }
        ];

        for (const system of systems) {
            if (system.regex.test(ua)) {
                let version = '0';
                if (system.versionRegex) {
                    const match = ua.match(system.versionRegex);
                    if (match) {
                        version = match[1].replace(/_/g, '.');
                    }
                }
                return { name: system.name, version: version };
            }
        }

        return { name: 'Unknown', version: '0' };
    }

    /**
     * Detect device type based on viewport and user agent
     */
    function detectDeviceType() {
        const ua = navigator.userAgent.toLowerCase();
        const width = window.innerWidth;

        // Check for mobile devices first
        const mobileKeywords = ['iphone', 'ipod', 'android', 'blackberry', 'windows phone', 'opera mini', 'iemobile'];
        const isMobileUA = mobileKeywords.some(keyword => ua.includes(keyword));

        // Check for tablets
        const tabletKeywords = ['ipad', 'tablet', 'playbook', 'silk'];
        const isTabletUA = tabletKeywords.some(keyword => ua.includes(keyword));
        const isAndroidTablet = ua.includes('android') && !ua.includes('mobile');

        // Combine UA detection with viewport width
        if (isMobileUA && width < 768) {
            return 1; // Mobile
        }
        if (isTabletUA || isAndroidTablet || (width >= 768 && width < 1024)) {
            return 2; // Tablet
        }
        if (width < 768) {
            return 1; // Mobile (based on viewport)
        }

        return 0; // Desktop
    }

    /**
     * Check if device supports touch
     */
    function isTouchDevice() {
        return (
            'ontouchstart' in window ||
            navigator.maxTouchPoints > 0 ||
            navigator.msMaxTouchPoints > 0 ||
            (window.matchMedia && window.matchMedia('(pointer: coarse)').matches)
        );
    }

    /**
     * Get current orientation as string
     */
    function getOrientation() {
        if (screen.orientation) {
            return screen.orientation.type.includes('landscape') ? 'landscape' : 'portrait';
        }
        return window.innerWidth > window.innerHeight ? 'landscape' : 'portrait';
    }

    /**
     * Get orientation as enum value (0 = Portrait, 1 = Landscape)
     */
    function getOrientationEnum() {
        return getOrientation() === 'landscape' ? 1 : 0;
    }

    /**
     * Check if running in standalone mode (PWA)
     */
    function isStandaloneMode() {
        return (
            window.matchMedia('(display-mode: standalone)').matches ||
            window.navigator.standalone === true ||
            document.referrer.includes('android-app://')
        );
    }

    /**
     * Get preferred color scheme
     */
    function getPreferredColorScheme() {
        if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }

    /**
     * Check if user prefers reduced motion
     */
    function prefersReducedMotion() {
        return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    /**
     * Get current viewport width
     */
    function getViewportWidth() {
        return window.innerWidth;
    }

    /**
     * Get current viewport height
     */
    function getViewportHeight() {
        return window.innerHeight;
    }

    /**
     * Handle window resize with debouncing
     */
    function handleResize() {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(() => {
            const newWidth = window.innerWidth;
            const newHeight = window.innerHeight;

            if (newWidth !== previousWidth || newHeight !== previousHeight) {
                // Update CSS classes
                updateDeviceClasses();

                // Notify .NET if reference exists
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnViewportChanged',
                        newWidth, newHeight, previousWidth, previousHeight);
                }

                previousWidth = newWidth;
                previousHeight = newHeight;
            }
        }, 150);
    }

    /**
     * Handle orientation change
     */
    function handleOrientationChange() {
        const newOrientation = getOrientation();

        if (newOrientation !== previousOrientation) {
            // Update CSS classes
            updateDeviceClasses();

            // Notify .NET if reference exists
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnOrientationChanged',
                    newOrientation, previousOrientation);
            }

            previousOrientation = newOrientation;
        }
    }

    /**
     * Update CSS classes on body based on device characteristics
     */
    function updateDeviceClasses() {
        const body = document.body;
        const html = document.documentElement;
        const width = window.innerWidth;

        // Remove existing device classes
        body.classList.remove('device-mobile', 'device-tablet', 'device-desktop');
        body.classList.remove('orientation-portrait', 'orientation-landscape');
        body.classList.remove('touch-device', 'no-touch');

        // Add device type class
        if (width < 768) {
            body.classList.add('device-mobile');
            html.setAttribute('data-device', 'mobile');
        } else if (width < 1024) {
            body.classList.add('device-tablet');
            html.setAttribute('data-device', 'tablet');
        } else {
            body.classList.add('device-desktop');
            html.setAttribute('data-device', 'desktop');
        }

        // Add orientation class
        const orientation = getOrientation();
        body.classList.add(`orientation-${orientation}`);
        html.setAttribute('data-orientation', orientation);

        // Add touch capability class
        if (isTouchDevice()) {
            body.classList.add('touch-device');
            html.setAttribute('data-touch', 'true');
        } else {
            body.classList.add('no-touch');
            html.setAttribute('data-touch', 'false');
        }

        // Add color scheme class
        const colorScheme = getPreferredColorScheme();
        html.setAttribute('data-color-scheme', colorScheme);

        // Add reduced motion class if preferred
        if (prefersReducedMotion()) {
            body.classList.add('reduced-motion');
        } else {
            body.classList.remove('reduced-motion');
        }
    }

    /**
     * Clean up event listeners
     */
    function dispose() {
        window.removeEventListener('resize', handleResize);
        window.removeEventListener('orientationchange', handleOrientationChange);

        if (screen.orientation) {
            screen.orientation.removeEventListener('change', handleOrientationChange);
        }

        clearTimeout(resizeTimeout);
        dotNetRef = null;

        console.log('[DeviceDetection] Disposed');
    }

    // Auto-initialize on load (before Blazor connects)
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => updateDeviceClasses());
    } else {
        updateDeviceClasses();
    }

    // Public API
    return {
        initialize: initialize,
        getDeviceInfo: getDeviceInfo,
        getViewportWidth: getViewportWidth,
        getViewportHeight: getViewportHeight,
        isTouchDevice: isTouchDevice,
        getOrientation: getOrientation,
        isStandaloneMode: isStandaloneMode,
        getPreferredColorScheme: getPreferredColorScheme,
        prefersReducedMotion: prefersReducedMotion,
        dispose: dispose
    };
})();
