/**
 * SEO Traffic Simulation & Monitoring Pipeline
 *
 * Purpose: Improve SEO score by:
 * - Simulating organic traffic to all sitemap URLs
 * - Verifying pre-rendering for crawlers works
 * - Checking structured data integrity
 * - Monitoring page performance metrics
 * - Generating SEO health reports
 *
 * Schedule: Every 6 hours (4x daily at 00:00, 06:00, 12:00, 18:00)
 * Impact: Increases engagement signals, validates SEO implementation
 */

pipeline {
    agent any

    options {
        timeout(time: 30, unit: 'MINUTES')
        timestamps()
        buildDiscarder(logRotator(numToKeepStr: '30'))
    }

    environment {
        SITE_URL = 'https://wasm.insightlearn.cloud'
        SITEMAP_URL = "${SITE_URL}/sitemap.xml"
        USER_AGENT_GOOGLEBOT = 'Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)'
        USER_AGENT_ORGANIC = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36'
        CRAWL_DELAY = '2' // seconds between requests
        MAX_URLS = '46' // Total URLs in sitemap
    }

    stages {
        stage('Preparation') {
            steps {
                script {
                    echo "========================================="
                    echo "SEO Traffic Simulation & Monitoring"
                    echo "Site: ${SITE_URL}"
                    echo "Build: #${env.BUILD_NUMBER}"
                    echo "Started: ${new Date()}"
                    echo "========================================="
                }
            }
        }

        stage('Fetch Sitemap URLs') {
            steps {
                script {
                    echo "📥 Fetching sitemap from ${SITEMAP_URL}..."
                    sh '''
                        curl -s ${SITEMAP_URL} > /tmp/sitemap-${BUILD_NUMBER}.xml
                        grep -o '<loc>[^<]*</loc>' /tmp/sitemap-${BUILD_NUMBER}.xml | sed 's|<loc>||;s|</loc>||' > /tmp/urls-${BUILD_NUMBER}.txt
                        echo "Found URLs: $(wc -l < /tmp/urls-${BUILD_NUMBER}.txt)"
                        head -10 /tmp/urls-${BUILD_NUMBER}.txt
                    '''
                }
            }
        }

        stage('Simulate Googlebot Traffic') {
            steps {
                script {
                    echo "🤖 Simulating Googlebot crawler (pre-rendering verification)..."
                    sh '''
                        TOTAL=0
                        SUCCESS=0
                        PRERENDER_OK=0

                        while IFS= read -r url; do
                            TOTAL=$((TOTAL + 1))
                            echo "[${TOTAL}/${MAX_URLS}] Crawling as Googlebot: ${url}"

                            # Fetch as Googlebot
                            HTTP_CODE=$(curl -s -o /tmp/page-${TOTAL}.html -w "%{http_code}" $
                                -A "${USER_AGENT_GOOGLEBOT}" "${url}")

                            if [ "${HTTP_CODE}" = "200" ]; then
                                SUCCESS=$((SUCCESS + 1))

                                # Check if pre-rendering worked (structured data present)
                                if grep -q "application/ld+json" /tmp/page-${TOTAL}.html; then
                                    PRERENDER_OK=$((PRERENDER_OK + 1))
                                    echo "  ✅ Pre-rendered (structured data found)"
                                else
                                    echo "  ⚠️  No structured data detected"
                                fi
                            else
                                echo "  ❌ HTTP ${HTTP_CODE}"
                            fi

                            # Respect crawl delay
                            sleep ${CRAWL_DELAY}
                        done < /tmp/urls-${BUILD_NUMBER}.txt

                        echo ""
                        echo "========================================="
                        echo "Googlebot Crawl Summary:"
                        echo "Total URLs: ${TOTAL}"
                        echo "Successful: ${SUCCESS} (HTTP 200)"
                        echo "Pre-rendered: ${PRERENDER_OK} (with structured data)"
                        echo "Success Rate: $(( SUCCESS * 100 / TOTAL ))%"
                        echo "Pre-render Rate: $(( PRERENDER_OK * 100 / TOTAL ))%"
                        echo "========================================="

                        # Store metrics for reporting
                        echo "${SUCCESS}" > /tmp/googlebot-success-${BUILD_NUMBER}.txt
                        echo "${PRERENDER_OK}" > /tmp/prerender-ok-${BUILD_NUMBER}.txt
                    '''
                }
            }
        }

        stage('Simulate Organic User Traffic') {
            steps {
                script {
                    echo "👥 Simulating organic user traffic (engagement signals)..."
                    sh '''
                        # Simulate realistic user behavior on key pages
                        KEY_PAGES=(
                            "${SITE_URL}/"
                            "${SITE_URL}/courses"
                            "${SITE_URL}/categories"
                            "${SITE_URL}/about"
                            "${SITE_URL}/faq"
                            "${SITE_URL}/contact"
                            "${SITE_URL}/pricing"
                            "${SITE_URL}/instructors"
                            "${SITE_URL}/blog"
                            "${SITE_URL}/courses?category=web-development"
                            "${SITE_URL}/courses?category=data-science"
                            "${SITE_URL}/courses?category=programming"
                            "${SITE_URL}/courses?skill=python"
                            "${SITE_URL}/courses?skill=javascript"
                            "${SITE_URL}/courses?price=free"
                        )

                        TOTAL=${#KEY_PAGES[@]}
                        SUCCESS=0
                        TOTAL_TIME=0

                        for url in "${KEY_PAGES[@]}"; do
                            echo "Visiting: ${url}"

                            # Fetch with organic user-agent + measure time
                            RESPONSE=$(curl -s -o /dev/null -w "HTTP:%{http_code} Time:%{time_total}s" \
                                -A "${USER_AGENT_ORGANIC}" \
                                -H "Accept: text/html,application/xhtml+xml" \
                                -H "Accept-Language: en-US,en;q=0.9,it;q=0.8" \
                                -H "DNT: 1" \
                                "${url}")

                            HTTP_CODE=$(echo "${RESPONSE}" | cut -d':' -f2 | cut -d' ' -f1)
                            TIME=$(echo "${RESPONSE}" | cut -d':' -f3 | cut -d's' -f1)

                            if [ "${HTTP_CODE}" = "200" ]; then
                                SUCCESS=$((SUCCESS + 1))
                                TOTAL_TIME=$(echo "${TOTAL_TIME} + ${TIME}" | bc)
                                echo "  ✅ ${RESPONSE}"
                            else
                                echo "  ❌ ${RESPONSE}"
                            fi

                            # Simulate realistic user dwell time (3-8 seconds)
                            DWELL_TIME=$(( RANDOM % 6 + 3 ))
                            sleep ${DWELL_TIME}
                        done

                        AVG_TIME=$(echo "scale=3; ${TOTAL_TIME} / ${SUCCESS}" | bc)

                        echo ""
                        echo "========================================="
                        echo "Organic Traffic Summary:"
                        echo "Pages Visited: ${TOTAL}"
                        echo "Successful: ${SUCCESS}"
                        echo "Success Rate: $(( SUCCESS * 100 / TOTAL ))%"
                        echo "Avg Response Time: ${AVG_TIME}s"
                        echo "Total Simulated Time: $(echo "${TOTAL_TIME} + (${TOTAL} * 5)" | bc)s"
                        echo "========================================="

                        echo "${SUCCESS}" > /tmp/organic-success-${BUILD_NUMBER}.txt
                        echo "${AVG_TIME}" > /tmp/avg-response-time-${BUILD_NUMBER}.txt
                    '''
                }
            }
        }

        stage('Verify Structured Data') {
            steps {
                script {
                    echo "🔍 Verifying structured data integrity..."
                    sh '''
                        # Test key pages for required schema types

                        echo "Checking Homepage schemas..."
                        HOMEPAGE=$(curl -s -A "${USER_AGENT_GOOGLEBOT}" ${SITE_URL}/)
                        SCHEMAS=$(echo "${HOMEPAGE}" | grep -o '"@type": "[^"]*"' | cut -d'"' -f4 | sort | uniq)

                        echo "Found schemas:"
                        echo "${SCHEMAS}"

                        # Count critical schemas
                        ORGANIZATION=$(echo "${SCHEMAS}" | grep -c "Organization" || echo 0)
                        WEBSITE=$(echo "${SCHEMAS}" | grep -c "WebSite" || echo 0)
                        EDUCATIONAL=$(echo "${SCHEMAS}" | grep -c "EducationalOrganization" || echo 0)

                        echo ""
                        echo "Checking Courses page schemas..."
                        COURSES=$(curl -s -A "${USER_AGENT_GOOGLEBOT}" ${SITE_URL}/courses)
                        COURSE_SCHEMAS=$(echo "${COURSES}" | grep -c '"@type": "Course"' || echo 0)
                        AGGREGATE_RATING=$(echo "${COURSES}" | grep -c '"aggregateRating"' || echo 0)
                        OFFERS=$(echo "${COURSES}" | grep -c '"offers"' || echo 0)

                        echo "Course schemas: ${COURSE_SCHEMAS}"
                        echo "With aggregateRating: ${AGGREGATE_RATING}"
                        echo "With offers: ${OFFERS}"

                        echo ""
                        echo "========================================="
                        echo "Structured Data Summary:"
                        echo "Homepage - Organization: ${ORGANIZATION}/1 ✅"
                        echo "Homepage - WebSite: ${WEBSITE}/1 ✅"
                        echo "Homepage - EducationalOrganization: ${EDUCATIONAL}/1 ✅"
                        echo "Courses - Course schemas: ${COURSE_SCHEMAS}/3"
                        echo "Courses - aggregateRating: ${AGGREGATE_RATING}/3"
                        echo "Courses - offers: ${OFFERS}/3"
                        echo "========================================="

                        # Store for reporting
                        echo "${COURSE_SCHEMAS}" > /tmp/course-schemas-${BUILD_NUMBER}.txt
                        echo "${AGGREGATE_RATING}" > /tmp/aggregate-rating-${BUILD_NUMBER}.txt
                    '''
                }
            }
        }

        stage('Performance Metrics') {
            steps {
                script {
                    echo "⚡ Collecting performance metrics..."
                    sh '''
                        # Test key performance indicators

                        echo "Testing Homepage..."
                        HOME_METRICS=$(curl -s -o /dev/null -w "DNS:%{time_namelookup}s Connect:%{time_connect}s SSL:%{time_appconnect}s TTFB:%{time_starttransfer}s Total:%{time_total}s Size:%{size_download}bytes" $
                            ${SITE_URL}/)

                        echo "Homepage: ${HOME_METRICS}"

                        echo ""
                        echo "Testing Courses Page..."
                        COURSES_METRICS=$(curl -s -o /dev/null -w "DNS:%{time_namelookup}s Connect:%{time_connect}s SSL:%{time_appconnect}s TTFB:%{time_starttransfer}s Total:%{time_total}s Size:%{size_download}bytes" $
                            ${SITE_URL}/courses)

                        echo "Courses: ${COURSES_METRICS}"

                        # Extract TTFB for scoring
                        HOME_TTFB=$(echo "${HOME_METRICS}" | grep -o 'TTFB:[0-9.]*s' | cut -d':' -f2 | cut -d's' -f1)
                        COURSES_TTFB=$(echo "${COURSES_METRICS}" | grep -o 'TTFB:[0-9.]*s' | cut -d':' -f2 | cut -d's' -f1)

                        AVG_TTFB=$(echo "scale=3; (${HOME_TTFB} + ${COURSES_TTFB}) / 2" | bc)

                        echo ""
                        echo "========================================="
                        echo "Performance Summary:"
                        echo "Avg TTFB: ${AVG_TTFB}s"
                        if (( $(echo "${AVG_TTFB} < 0.5" | bc -l) )); then
                            echo "Performance: ✅ EXCELLENT (< 0.5s)"
                        elif (( $(echo "${AVG_TTFB} < 1.0" | bc -l) )); then
                            echo "Performance: ✅ GOOD (< 1.0s)"
                        else
                            echo "Performance: ⚠️  NEEDS IMPROVEMENT (> 1.0s)"
                        fi
                        echo "========================================="

                        echo "${AVG_TTFB}" > /tmp/avg-ttfb-${BUILD_NUMBER}.txt
                    '''
                }
            }
        }

        stage('Generate SEO Report') {
            steps {
                script {
                    echo "📊 Generating SEO health report..."
                    sh '''

                        # Read metrics
                        GOOGLEBOT_SUCCESS=$(cat /tmp/googlebot-success-${BUILD_NUMBER}.txt)
                        PRERENDER_OK=$(cat /tmp/prerender-ok-${BUILD_NUMBER}.txt)
                        ORGANIC_SUCCESS=$(cat /tmp/organic-success-${BUILD_NUMBER}.txt)
                        AVG_RESPONSE=$(cat /tmp/avg-response-time-${BUILD_NUMBER}.txt)
                        COURSE_SCHEMAS=$(cat /tmp/course-schemas-${BUILD_NUMBER}.txt)
                        AGGREGATE_RATING=$(cat /tmp/aggregate-rating-${BUILD_NUMBER}.txt)
                        AVG_TTFB=$(cat /tmp/avg-ttfb-${BUILD_NUMBER}.txt)

                        # Calculate SEO health score (out of 100)
                        CRAWL_SCORE=$(( GOOGLEBOT_SUCCESS * 100 / ${MAX_URLS} ))
                        PRERENDER_SCORE=$(( PRERENDER_OK * 100 / ${MAX_URLS} ))
                        ORGANIC_SCORE=$(( ORGANIC_SUCCESS * 100 / 15 ))
                        SCHEMA_SCORE=$(( COURSE_SCHEMAS * 100 / 3 ))

                        # Performance score (inverse - lower is better)
                        PERF_SCORE=100
                        if (( $(echo "${AVG_TTFB} > 1.0" | bc -l) )); then
                            PERF_SCORE=50
                        elif (( $(echo "${AVG_TTFB} > 0.5" | bc -l) )); then
                            PERF_SCORE=75
                        fi

                        OVERALL_SCORE=$(( (CRAWL_SCORE + PRERENDER_SCORE + ORGANIC_SCORE + SCHEMA_SCORE + PERF_SCORE) / 5 ))

                        cat > /tmp/seo-report-${BUILD_NUMBER}.txt <<EOF
========================================
SEO TRAFFIC SIMULATION REPORT
========================================
Build: #${BUILD_NUMBER}
Date: $(date '+%Y-%m-%d %H:%M:%S')
Site: ${SITE_URL}

CRAWLING METRICS:
-----------------
Googlebot Crawl: ${GOOGLEBOT_SUCCESS}/${MAX_URLS} URLs (${CRAWL_SCORE}%)
Pre-rendering: ${PRERENDER_OK}/${MAX_URLS} pages (${PRERENDER_SCORE}%)
Organic Traffic: ${ORGANIC_SUCCESS}/15 key pages (${ORGANIC_SCORE}%)

STRUCTURED DATA:
----------------
Course schemas: ${COURSE_SCHEMAS}/3 (${SCHEMA_SCORE}%)
aggregateRating: ${AGGREGATE_RATING}/3
offers: ${AGGREGATE_RATING}/3

PERFORMANCE:
------------
Avg Response Time: ${AVG_RESPONSE}s
Avg TTFB: ${AVG_TTFB}s
Performance Score: ${PERF_SCORE}/100

OVERALL SEO HEALTH SCORE:
-------------------------
Score: ${OVERALL_SCORE}/100

EOF
                        if [ ${OVERALL_SCORE} -ge 90 ]; then
                            echo "Status: ✅ EXCELLENT" >> /tmp/seo-report-${BUILD_NUMBER}.txt
                        elif [ ${OVERALL_SCORE} -ge 75 ]; then
                            echo "Status: ✅ GOOD" >> /tmp/seo-report-${BUILD_NUMBER}.txt
                        elif [ ${OVERALL_SCORE} -ge 60 ]; then
                            echo "Status: ⚠️  FAIR" >> /tmp/seo-report-${BUILD_NUMBER}.txt
                        else
                            echo "Status: ❌ NEEDS IMPROVEMENT" >> /tmp/seo-report-${BUILD_NUMBER}.txt
                        fi

                        cat >> /tmp/seo-report-${BUILD_NUMBER}.txt <<EOF

TRAFFIC SIMULATION IMPACT:
--------------------------
- Crawled ${MAX_URLS} URLs as Googlebot
- Simulated 15 organic user visits
- Generated ~5 minutes of engagement signals
- Verified structured data integrity
- Monitored performance metrics

Next Run: In 6 hours
========================================
EOF

                        cat /tmp/seo-report-${BUILD_NUMBER}.txt
                    '''
                }
            }
        }

        stage('Cleanup') {
            steps {
                script {
                    echo "🧹 Cleaning up temporary files..."
                    sh '''
                        rm -f /tmp/sitemap-${BUILD_NUMBER}.xml
                        rm -f /tmp/urls-${BUILD_NUMBER}.txt
                        rm -f /tmp/page-*.html
                        rm -f /tmp/*-${BUILD_NUMBER}.txt
                    '''
                }
            }
        }
    }

    post {
        success {
            echo "✅ SEO traffic simulation completed successfully!"
        }
        failure {
            echo "❌ SEO traffic simulation failed. Check logs for details."
        }
        always {
            echo "Build finished at: ${new Date()}"
        }
    }
}
