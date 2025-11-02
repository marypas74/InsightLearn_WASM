// InsightLearn New Home Page 2025 - Interactive Features
// This script handles all interactive elements on the new home page

(function() {
    'use strict';

    // Main initialization function
    window.initNewHomePage = function() {
        console.log('Initializing new home page features...');

        // Initialize all features
        initCounterAnimations();
        initLiveStudentsCounter();
        initScrollReveal();
        initSmoothScroll();
        initParallaxEffect();

        console.log('New home page features initialized successfully!');
    };

    // Counter Animation
    function initCounterAnimations() {
        const counters = document.querySelectorAll('.stat-number, .stat-value');
        const animationDuration = 2000; // 2 seconds

        const animateCounter = (counter) => {
            const target = parseFloat(counter.getAttribute('data-target'));
            const isPercentage = counter.textContent.includes('%');
            const isPlus = counter.textContent.includes('+');
            const isDecimal = target % 1 !== 0;

            let current = 0;
            const increment = target / (animationDuration / 16); // 60fps
            const decimals = isDecimal ? 1 : 0;

            const updateCounter = () => {
                current += increment;

                if (current < target) {
                    counter.textContent = current.toFixed(decimals) +
                        (isPercentage ? '%' : '') +
                        (isPlus ? '+' : '');
                    requestAnimationFrame(updateCounter);
                } else {
                    counter.textContent = target.toFixed(decimals) +
                        (isPercentage ? '%' : '') +
                        (isPlus ? '+' : '');
                }
            };

            updateCounter();
        };

        // Use Intersection Observer to trigger animation when visible
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !entry.target.classList.contains('animated')) {
                    entry.target.classList.add('animated');
                    animateCounter(entry.target);
                }
            });
        }, {
            threshold: 0.5
        });

        counters.forEach(counter => {
            if (counter.getAttribute('data-target')) {
                observer.observe(counter);
            }
        });
    }

    // Live Students Counter
    function initLiveStudentsCounter() {
        const liveStudentsElement = document.getElementById('liveStudents');
        if (!liveStudentsElement) return;

        let currentCount = 2847;

        const updateLiveCount = () => {
            // Randomly increase or decrease by 1-5 students
            const change = Math.floor(Math.random() * 5) + 1;
            const increase = Math.random() > 0.3; // 70% chance to increase

            if (increase) {
                currentCount += change;
            } else {
                currentCount = Math.max(2800, currentCount - change); // Don't go below 2800
            }

            // Keep within reasonable bounds
            if (currentCount > 3000) currentCount = 3000;

            // Format with comma
            liveStudentsElement.textContent = currentCount.toLocaleString();
        };

        // Update every 3-8 seconds
        const scheduleNextUpdate = () => {
            const delay = Math.random() * 5000 + 3000; // 3-8 seconds
            setTimeout(() => {
                updateLiveCount();
                scheduleNextUpdate();
            }, delay);
        };

        scheduleNextUpdate();
    }

    // Scroll Reveal Animation
    function initScrollReveal() {
        const revealElements = document.querySelectorAll(
            '.step-card, .course-card, .testimonial-card, .stat-box'
        );

        const revealObserver = new IntersectionObserver((entries) => {
            entries.forEach((entry, index) => {
                if (entry.isIntersecting) {
                    // Add staggered delay
                    setTimeout(() => {
                        entry.target.classList.add('scroll-reveal', 'revealed');
                    }, index * 100);
                    revealObserver.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.1,
            rootMargin: '0px 0px -100px 0px'
        });

        revealElements.forEach(element => {
            element.classList.add('scroll-reveal');
            revealObserver.observe(element);
        });
    }

    // Smooth Scroll
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function(e) {
                const href = this.getAttribute('href');
                if (href === '#' || !href) return;

                e.preventDefault();
                const target = document.querySelector(href);

                if (target) {
                    const headerOffset = 80;
                    const elementPosition = target.getBoundingClientRect().top;
                    const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

                    window.scrollTo({
                        top: offsetPosition,
                        behavior: 'smooth'
                    });
                }
            });
        });
    }

    // Parallax Effect for Hero Section
    function initParallaxEffect() {
        const heroSection = document.querySelector('.hero-section');
        if (!heroSection) return;

        let ticking = false;

        const updateParallax = () => {
            const scrolled = window.pageYOffset;
            const heroCards = document.querySelectorAll('.hero-card');

            heroCards.forEach((card, index) => {
                const speed = 0.5 + (index * 0.1);
                const yPos = -(scrolled * speed);
                card.style.transform = `translateY(${yPos}px)`;
            });

            ticking = false;
        };

        const requestTick = () => {
            if (!ticking) {
                window.requestAnimationFrame(updateParallax);
                ticking = true;
            }
        };

        window.addEventListener('scroll', requestTick, { passive: true });
    }

    // Rotating Text Animation (for future use)
    function initRotatingText() {
        const rotatingTexts = document.querySelectorAll('[data-rotating-text]');

        rotatingTexts.forEach(element => {
            const texts = element.getAttribute('data-rotating-text').split(',');
            let currentIndex = 0;

            const rotateText = () => {
                element.style.opacity = '0';
                element.style.transform = 'translateY(-20px)';

                setTimeout(() => {
                    currentIndex = (currentIndex + 1) % texts.length;
                    element.textContent = texts[currentIndex];
                    element.style.opacity = '1';
                    element.style.transform = 'translateY(0)';
                }, 300);
            };

            element.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
            setInterval(rotateText, 3000);
        });
    }

    // Utility: Check if element is in viewport
    function isInViewport(element) {
        const rect = element.getBoundingClientRect();
        return (
            rect.top >= 0 &&
            rect.left >= 0 &&
            rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
            rect.right <= (window.innerWidth || document.documentElement.clientWidth)
        );
    }

    // Utility: Debounce function
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Utility: Throttle function
    function throttle(func, limit) {
        let inThrottle;
        return function(...args) {
            if (!inThrottle) {
                func.apply(this, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }

    // Handle page visibility changes (pause animations when tab is not active)
    document.addEventListener('visibilitychange', function() {
        if (document.hidden) {
            console.log('Page hidden - pausing animations');
            // Could pause animations here if needed
        } else {
            console.log('Page visible - resuming animations');
            // Could resume animations here if needed
        }
    });

    // Performance monitoring
    if (window.performance && window.performance.mark) {
        window.performance.mark('new-home-page-init-start');

        window.addEventListener('load', function() {
            window.performance.mark('new-home-page-init-end');
            window.performance.measure(
                'new-home-page-init',
                'new-home-page-init-start',
                'new-home-page-init-end'
            );

            const measure = window.performance.getEntriesByName('new-home-page-init')[0];
            console.log(`New home page initialization took ${measure.duration.toFixed(2)}ms`);
        });
    }

    // Error handling
    window.addEventListener('error', function(event) {
        console.error('Error in new home page script:', event.error);
    });

    // Accessibility: Respect prefers-reduced-motion
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        console.log('Reduced motion preference detected - disabling animations');
        document.documentElement.style.setProperty('--transition', 'none');
    }

    // Log initialization
    console.log('New home page 2025 script loaded successfully!');

})();
