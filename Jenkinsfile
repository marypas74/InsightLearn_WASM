// InsightLearn WASM - Jenkins Pipeline for Automated Testing
// This pipeline runs load tests and site monitoring every hour

pipeline {
    agent any

    // Run every hour
    triggers {
        cron('H * * * *')
    }

    environment {
        SITE_URL = 'https://wasm.insightlearn.cloud'
        SLACK_CHANNEL = '#insightlearn-alerts' // Optional: configure Slack notifications
        EMAIL_RECIPIENTS = 'marcello.pasqui@gmail.com'
    }

    stages {
        stage('Preparation') {
            steps {
                echo '=== InsightLearn Automated Testing Pipeline ==='
                echo "Testing site: ${env.SITE_URL}"
                echo "Build: ${env.BUILD_NUMBER}"
                echo "Timestamp: ${new Date()}"
            }
        }

        stage('Health Check') {
            steps {
                script {
                    echo '=== Running Health Checks ==='
                    sh '''
                        # Test main site
                        curl -f -s -o /dev/null -w "Main site: %{http_code}\\n" ${SITE_URL}

                        # Test API endpoints (expect 502 until fixed)
                        curl -s -o /dev/null -w "API Health: %{http_code}\\n" ${SITE_URL}/health || true
                        curl -s -o /dev/null -w "API Info: %{http_code}\\n" ${SITE_URL}/api/info || true
                    '''
                }
            }
        }

        stage('Page Availability Test') {
            steps {
                script {
                    echo '=== Testing All Pages (404 Detection) ==='
                    sh '''#!/bin/bash
                        failed=0
                        pages=("" "login" "register" "courses" "dashboard" "admin" "profile" "about" "contact")

                        for page in "${pages[@]}"; do
                            status=$(curl -s -o /dev/null -w "%{http_code}" "${SITE_URL}/${page}")
                            if [ "$status" = "200" ]; then
                                echo "✅ /${page}: $status"
                            else
                                echo "❌ /${page}: $status"
                                failed=$((failed + 1))
                            fi
                        done

                        if [ $failed -gt 0 ]; then
                            echo "WARNING: $failed pages returned non-200 status"
                        fi
                    '''
                }
            }
        }

        stage('Performance Benchmarking') {
            steps {
                script {
                    echo '=== Performance Benchmark ==='
                    sh '''#!/bin/bash
                        echo "Running 10 performance tests..."
                        total=0
                        count=10

                        for i in $(seq 1 $count); do
                            time=$(curl -s -o /dev/null -w "%{time_total}" ${SITE_URL})
                            echo "Test $i: ${time}s"
                            total=$(echo "$total + $time" | bc)
                        done

                        avg=$(echo "scale=3; $total / $count" | bc)
                        echo "========================================="
                        echo "Average response time: ${avg}s"
                        echo "========================================="

                        # Alert if response time > 500ms
                        threshold=0.500
                        if (( $(echo "$avg > $threshold" | bc -l) )); then
                            echo "⚠️ WARNING: Average response time exceeds threshold (${threshold}s)"
                            exit 1
                        fi
                    '''
                }
            }
        }

        stage('Load Testing') {
            steps {
                script {
                    echo '=== Load Testing (Light) ==='
                    sh '''#!/bin/bash
                        # Light load test: 50 concurrent requests
                        echo "Simulating 50 concurrent users..."

                        # Use GNU parallel if available, otherwise sequential
                        if command -v parallel &> /dev/null; then
                            seq 1 50 | parallel -j 10 "curl -s -o /dev/null -w 'Request {}: %{http_code} in %{time_total}s\\n' ${SITE_URL}"
                        else
                            for i in {1..50}; do
                                curl -s -o /dev/null -w "Request $i: %{http_code} in %{time_total}s\\n" ${SITE_URL} &
                            done
                            wait
                        fi

                        echo "Load test completed"
                    '''
                }
            }
        }

        stage('Asset Validation') {
            steps {
                script {
                    echo '=== Validating Static Assets ==='
                    sh '''#!/bin/bash
                        failed=0

                        # CSS files
                        for css in "css/app.css" "css/site.css" "css/bootstrap/bootstrap.min.css" "css/responsive.css"; do
                            status=$(curl -s -o /dev/null -w "%{http_code}" "${SITE_URL}/${css}")
                            if [ "$status" = "200" ]; then
                                echo "✅ $css"
                            else
                                echo "❌ $css: $status"
                                failed=$((failed + 1))
                            fi
                        done

                        # JavaScript files
                        for js in "_framework/blazor.webassembly.js" "js/httpClient.js" "js/cookie-consent-wall.js"; do
                            status=$(curl -s -o /dev/null -w "%{http_code}" "${SITE_URL}/${js}")
                            if [ "$status" = "200" ]; then
                                echo "✅ $js"
                            else
                                echo "❌ $js: $status"
                                failed=$((failed + 1))
                            fi
                        done

                        # Images
                        for img in "favicon.png" "icon-192.png"; do
                            status=$(curl -s -o /dev/null -w "%{http_code}" "${SITE_URL}/${img}")
                            if [ "$status" = "200" ]; then
                                echo "✅ $img"
                            else
                                echo "❌ $img: $status"
                                failed=$((failed + 1))
                            fi
                        done

                        if [ $failed -gt 0 ]; then
                            echo "ERROR: $failed assets failed to load"
                            exit 1
                        fi
                    '''
                }
            }
        }

        stage('Security Headers Check') {
            steps {
                script {
                    echo '=== Security Headers Validation ==='
                    sh '''
                        headers=$(curl -I -s ${SITE_URL})

                        echo "Checking security headers..."
                        echo "$headers" | grep -i "x-frame-options" && echo "✅ X-Frame-Options found" || echo "❌ X-Frame-Options missing"
                        echo "$headers" | grep -i "x-xss-protection" && echo "✅ X-XSS-Protection found" || echo "❌ X-XSS-Protection missing"
                        echo "$headers" | grep -i "x-content-type-options" && echo "✅ X-Content-Type-Options found" || echo "❌ X-Content-Type-Options missing"
                        echo "$headers" | grep -i "referrer-policy" && echo "✅ Referrer-Policy found" || echo "❌ Referrer-Policy missing"
                        echo "$headers" | grep -i "content-security-policy" && echo "✅ Content-Security-Policy found" || echo "⚠️ Content-Security-Policy missing (recommended)"
                    '''
                }
            }
        }

        stage('Backend API Monitoring') {
            steps {
                script {
                    echo '=== Backend API Status ==='
                    sh '''
                        # Check if running in Kubernetes environment
                        if command -v kubectl &> /dev/null; then
                            echo "Kubernetes pod status:"
                            kubectl get pods -n insightlearn | grep -E "NAME|api|ollama|mongodb|redis|sqlserver" || echo "Namespace not found or no pods"
                        else
                            echo "kubectl not available, skipping pod status check"
                        fi
                    '''
                }
            }
        }

        stage('Generate Report') {
            steps {
                script {
                    echo '=== Test Report Summary ==='
                    sh '''
                        echo "========================================="
                        echo "InsightLearn Test Report"
                        echo "========================================="
                        echo "Site: ${SITE_URL}"
                        echo "Build: ${BUILD_NUMBER}"
                        echo "Date: $(date)"
                        echo "========================================="
                        echo "✅ All tests completed"
                        echo "Check console output for detailed results"
                        echo "========================================="
                    '''
                }
            }
        }
    }

    post {
        success {
            echo '✅ All tests passed successfully!'
            // Optional: Send success notification
            // slackSend(channel: env.SLACK_CHANNEL, color: 'good', message: "InsightLearn tests passed - Build #${env.BUILD_NUMBER}")
        }

        failure {
            echo '❌ Some tests failed!'
            // Optional: Send failure notification
            // slackSend(channel: env.SLACK_CHANNEL, color: 'danger', message: "InsightLearn tests failed - Build #${env.BUILD_NUMBER}")

            // Send email notification
            emailext(
                subject: "Jenkins - InsightLearn Tests Failed - Build #${env.BUILD_NUMBER}",
                body: """
                    Test execution failed for InsightLearn WASM.

                    Build: ${env.BUILD_NUMBER}
                    Site: ${env.SITE_URL}

                    Check console output: ${env.BUILD_URL}console
                """,
                to: env.EMAIL_RECIPIENTS,
                attachLog: true
            )
        }

        always {
            echo 'Pipeline execution completed'
            // Archive test results
            // archiveArtifacts artifacts: 'test-results/**', allowEmptyArchive: true
        }
    }
}
