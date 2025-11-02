/**
 * Sticky Header Enhancement Script
 * Adds dynamic shadow and glassmorphism effects during scroll
 *
 * Features:
 * - Adds .scrolled class to header when user scrolls down
 * - Smooth shadow enhancement for depth perception
 * - Debounced for performance
 * - Works with Blazor WebAssembly routing
 *
 * Version: 1.0.0
 * Author: InsightLearn Team
 */

(function() {
    'use strict';

    // Configuration
    const SCROLL_THRESHOLD = 10; // pixels scrolled before adding .scrolled class
    const DEBOUNCE_DELAY = 10; // milliseconds to debounce scroll events

    // State
    let isScrolled = false;
    let scrollTimeout = null;
    let headerElement = null;

    /**
     * Initialize sticky header enhancements
     */
    function init() {
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', setupHeaderEnhancement);
        } else {
            setupHeaderEnhancement();
        }
    }

    /**
     * Setup header enhancement after DOM is ready
     */
    function setupHeaderEnhancement() {
        // Find header element
        headerElement = document.querySelector('.main-header');

        if (!headerElement) {
            console.warn('[Sticky Header] .main-header element not found. Retrying...');
            // Retry after Blazor renders
            setTimeout(setupHeaderEnhancement, 500);
            return;
        }

        console.log('[Sticky Header] Enhancement initialized');

        // Add scroll listener
        window.addEventListener('scroll', handleScroll, { passive: true });

        // Initial check (in case page loads scrolled)
        checkScrollPosition();

        // Re-check after Blazor navigation (if using Blazor routing)
        observeBlazorNavigation();
    }

    /**
     * Handle scroll events with debouncing
     */
    function handleScroll() {
        // Clear existing timeout
        if (scrollTimeout) {
            clearTimeout(scrollTimeout);
        }

        // Debounce scroll event
        scrollTimeout = setTimeout(checkScrollPosition, DEBOUNCE_DELAY);
    }

    /**
     * Check current scroll position and update header state
     */
    function checkScrollPosition() {
        if (!headerElement) return;

        const scrollY = window.scrollY || window.pageYOffset;
        const shouldBeScrolled = scrollY > SCROLL_THRESHOLD;

        // Only update if state changed (prevent unnecessary DOM updates)
        if (shouldBeScrolled !== isScrolled) {
            isScrolled = shouldBeScrolled;

            if (isScrolled) {
                headerElement.classList.add('scrolled');
            } else {
                headerElement.classList.remove('scrolled');
            }
        }
    }

    /**
     * Observe Blazor navigation to re-check scroll position
     * Blazor SPA routing doesn't trigger page reload
     */
    function observeBlazorNavigation() {
        // Listen for Blazor navigation events
        const observer = new MutationObserver(function(mutations) {
            // Check if navigation occurred by observing body changes
            const hasNavigation = mutations.some(mutation =>
                mutation.type === 'childList' &&
                mutation.target.classList &&
                mutation.target.classList.contains('main-content')
            );

            if (hasNavigation) {
                // Re-check scroll position after navigation
                setTimeout(checkScrollPosition, 100);
            }
        });

        // Observe changes to main content area
        const mainContent = document.querySelector('.main-content') || document.body;
        observer.observe(mainContent, {
            childList: true,
            subtree: true
        });
    }

    /**
     * Public API (optional - for manual control)
     */
    window.StickyHeaderEnhancement = {
        refresh: checkScrollPosition,
        enable: function() {
            window.addEventListener('scroll', handleScroll, { passive: true });
            checkScrollPosition();
        },
        disable: function() {
            window.removeEventListener('scroll', handleScroll);
            if (headerElement) {
                headerElement.classList.remove('scrolled');
            }
        }
    };

    // Auto-initialize
    init();

})();
