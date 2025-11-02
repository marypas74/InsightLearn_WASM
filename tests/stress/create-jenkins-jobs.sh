#!/bin/bash
#
# Create Jenkins Jobs via Jenkins CLI or REST API
#
# This script creates the stress testing jobs in Jenkins
#

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

JENKINS_URL="${JENKINS_URL:-http://192.168.49.2:32000}"
JENKINS_USER="${JENKINS_USER:-admin}"
JENKINS_PASSWORD="${JENKINS_PASSWORD}"

echo -e "${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║           Create Jenkins Jobs for Stress Testing             ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}\n"

# Check if Jenkins is accessible
echo -e "${BLUE}🔍 Checking Jenkins availability...${NC}"
if curl -s -o /dev/null -w "%{http_code}" "$JENKINS_URL" | grep -q "200\|403"; then
    echo -e "${GREEN}✅ Jenkins is accessible at $JENKINS_URL${NC}"
else
    echo -e "${RED}❌ Jenkins is not accessible at $JENKINS_URL${NC}"
    echo -e "${YELLOW}Make sure Jenkins is running and accessible.${NC}"
    exit 1
fi

# Get Jenkins password if not provided
if [ -z "$JENKINS_PASSWORD" ]; then
    echo -e "\n${YELLOW}Jenkins admin password required.${NC}"
    echo -e "${BLUE}You can get it with:${NC}"
    echo -e "  ${GREEN}kubectl exec -n jenkins deployment/jenkins -- cat /var/jenkins_home/secrets/initialAdminPassword${NC}\n"

    read -sp "Enter Jenkins admin password: " JENKINS_PASSWORD
    echo ""
fi

# Create temporary directory for job configs
TMP_DIR=$(mktemp -d)
echo -e "\n${BLUE}📁 Creating job configurations in $TMP_DIR${NC}"

# Job 1: InsightLearn-CI-CD (main pipeline)
cat > "$TMP_DIR/InsightLearn-CI-CD.xml" << 'EOF'
<?xml version='1.1' encoding='UTF-8'?>
<flow-definition plugin="workflow-job">
  <actions/>
  <description>InsightLearn CI/CD Pipeline with Unit Tests, Integration Tests, and Stress Testing</description>
  <keepDependencies>false</keepDependencies>
  <properties>
    <org.jenkinsci.plugins.workflow.job.properties.PipelineTriggersJobProperty>
      <triggers>
        <hudson.triggers.SCMTrigger>
          <spec>H/5 * * * *</spec>
          <ignorePostCommitHooks>false</ignorePostCommitHooks>
        </hudson.triggers.SCMTrigger>
      </triggers>
    </org.jenkinsci.plugins.workflow.job.properties.PipelineTriggersJobProperty>
  </properties>
  <definition class="org.jenkinsci.plugins.workflow.cps.CpsScmFlowDefinition" plugin="workflow-cps">
    <scm class="hudson.plugins.git.GitSCM" plugin="git">
      <configVersion>2</configVersion>
      <userRemoteConfigs>
        <hudson.plugins.git.UserRemoteConfig>
          <url>file:///home/mpasqui/kubernetes/Insightlearn</url>
        </hudson.plugins.git.UserRemoteConfig>
      </userRemoteConfigs>
      <branches>
        <hudson.plugins.git.BranchSpec>
          <name>*/main</name>
        </hudson.plugins.git.BranchSpec>
      </branches>
      <doGenerateSubmoduleConfigurations>false</doGenerateSubmoduleConfigurations>
      <submoduleCfg class="list"/>
      <extensions/>
    </scm>
    <scriptPath>Jenkinsfile</scriptPath>
    <lightweight>true</lightweight>
  </definition>
  <triggers/>
  <disabled>false</disabled>
</flow-definition>
EOF

# Job 2: InsightLearn-Stress-Test (stress testing only)
cat > "$TMP_DIR/InsightLearn-Stress-Test.xml" << 'EOF'
<?xml version='1.1' encoding='UTF-8'?>
<flow-definition plugin="workflow-job">
  <actions/>
  <description>Run stress tests only for InsightLearn (configurable)</description>
  <keepDependencies>false</keepDependencies>
  <properties>
    <hudson.model.ParametersDefinitionProperty>
      <parameterDefinitions>
        <hudson.model.ChoiceParameterDefinition>
          <name>TEST_TYPE</name>
          <description>Select which test to run</description>
          <choices class="java.util.Arrays$ArrayList">
            <a class="string-array">
              <string>smoke</string>
              <string>load</string>
              <string>stress</string>
              <string>spike</string>
              <string>soak</string>
              <string>all</string>
            </a>
          </choices>
        </hudson.model.ChoiceParameterDefinition>
        <hudson.model.StringParameterDefinition>
          <name>API_URL</name>
          <description>API URL to test</description>
          <defaultValue>http://192.168.49.2:31081</defaultValue>
          <trim>true</trim>
        </hudson.model.StringParameterDefinition>
        <hudson.model.StringParameterDefinition>
          <name>WEB_URL</name>
          <description>Web URL to test</description>
          <defaultValue>http://192.168.49.2:31080</defaultValue>
          <trim>true</trim>
        </hudson.model.StringParameterDefinition>
      </parameterDefinitions>
    </hudson.model.ParametersDefinitionProperty>
  </properties>
  <definition class="org.jenkinsci.plugins.workflow.cps.CpsFlowDefinition" plugin="workflow-cps">
    <script>
pipeline {
    agent any

    parameters {
        choice(name: 'TEST_TYPE', choices: ['smoke', 'load', 'stress', 'spike', 'soak', 'all'], description: 'Select test type')
        string(name: 'API_URL', defaultValue: 'http://192.168.49.2:31081', description: 'API URL')
        string(name: 'WEB_URL', defaultValue: 'http://192.168.49.2:31080', description: 'Web URL')
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: 'main', url: 'file:///home/mpasqui/kubernetes/Insightlearn'
            }
        }

        stage('Run Stress Test') {
            steps {
                script {
                    def testScript = ""
                    switch(params.TEST_TYPE) {
                        case 'smoke': testScript = 'smoke-test.js'; break
                        case 'load': testScript = 'load-test.js'; break
                        case 'stress': testScript = 'stress-test.js'; break
                        case 'spike': testScript = 'spike-test.js'; break
                        case 'soak': testScript = 'soak-test.js'; break
                        case 'all': testScript = 'all'; break
                    }

                    dir('tests/stress') {
                        if (testScript == 'all') {
                            sh './run-all-tests.sh ${API_URL} ${WEB_URL}'
                        } else {
                            sh """
                                export API_URL=${params.API_URL}
                                export WEB_URL=${params.WEB_URL}
                                k6 run ${testScript}
                            """
                        }
                    }
                }
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'tests/stress/results/**/*', allowEmptyArchive: true
        }
    }
}
    </script>
    <sandbox>true</sandbox>
  </definition>
  <triggers/>
  <disabled>false</disabled>
</flow-definition>
EOF

# Job 3: InsightLearn-Nightly-Stress (scheduled nightly)
cat > "$TMP_DIR/InsightLearn-Nightly-Stress.xml" << 'EOF'
<?xml version='1.1' encoding='UTF-8'?>
<flow-definition plugin="workflow-job">
  <actions/>
  <description>Nightly comprehensive stress testing (runs at 2 AM)</description>
  <keepDependencies>false</keepDependencies>
  <properties>
    <org.jenkinsci.plugins.workflow.job.properties.PipelineTriggersJobProperty>
      <triggers>
        <hudson.triggers.TimerTrigger>
          <spec>H 2 * * *</spec>
        </hudson.triggers.TimerTrigger>
      </triggers>
    </org.jenkinsci.plugins.workflow.job.properties.PipelineTriggersJobProperty>
  </properties>
  <definition class="org.jenkinsci.plugins.workflow.cps.CpsFlowDefinition" plugin="workflow-cps">
    <script>
pipeline {
    agent any

    environment {
        API_URL = 'http://192.168.49.2:31081'
        WEB_URL = 'http://192.168.49.2:31080'
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: 'main', url: 'file:///home/mpasqui/kubernetes/Insightlearn'
            }
        }

        stage('Nightly Stress Tests') {
            steps {
                dir('tests/stress') {
                    sh './run-all-tests.sh ${API_URL} ${WEB_URL}'
                }
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'tests/stress/results/**/*', allowEmptyArchive: true
        }
        failure {
            echo '❌ Nightly stress tests failed! Alert the team!'
        }
    }
}
    </script>
    <sandbox>true</sandbox>
  </definition>
  <triggers/>
  <disabled>false</disabled>
</flow-definition>
EOF

# Function to create a job
create_job() {
    local job_name=$1
    local job_file=$2

    echo -e "\n${BLUE}📝 Creating job: ${job_name}${NC}"

    # Try to create the job
    response=$(curl -s -w "\n%{http_code}" -X POST \
        -u "$JENKINS_USER:$JENKINS_PASSWORD" \
        -H "Content-Type: application/xml" \
        --data-binary "@$job_file" \
        "$JENKINS_URL/createItem?name=$job_name")

    status_code=$(echo "$response" | tail -n1)

    if [ "$status_code" = "200" ]; then
        echo -e "${GREEN}✅ Job '$job_name' created successfully${NC}"
        return 0
    elif [ "$status_code" = "400" ]; then
        echo -e "${YELLOW}⚠️  Job '$job_name' already exists, updating...${NC}"

        # Update existing job
        update_response=$(curl -s -w "\n%{http_code}" -X POST \
            -u "$JENKINS_USER:$JENKINS_PASSWORD" \
            -H "Content-Type: application/xml" \
            --data-binary "@$job_file" \
            "$JENKINS_URL/job/$job_name/config.xml")

        update_status=$(echo "$update_response" | tail -n1)

        if [ "$update_status" = "200" ]; then
            echo -e "${GREEN}✅ Job '$job_name' updated successfully${NC}"
            return 0
        else
            echo -e "${RED}❌ Failed to update job '$job_name' (HTTP $update_status)${NC}"
            return 1
        fi
    else
        echo -e "${RED}❌ Failed to create job '$job_name' (HTTP $status_code)${NC}"
        return 1
    fi
}

# Create all jobs
echo -e "\n${BLUE}Creating Jenkins jobs...${NC}"
create_job "InsightLearn-CI-CD" "$TMP_DIR/InsightLearn-CI-CD.xml"
create_job "InsightLearn-Stress-Test" "$TMP_DIR/InsightLearn-Stress-Test.xml"
create_job "InsightLearn-Nightly-Stress" "$TMP_DIR/InsightLearn-Nightly-Stress.xml"

# Cleanup
rm -rf "$TMP_DIR"

echo -e "\n${BLUE}╔═══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                   Jobs Created Successfully!                  ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${GREEN}Jenkins Jobs Created:${NC}"
echo -e "  1. ${BLUE}InsightLearn-CI-CD${NC} - Full CI/CD pipeline"
echo -e "  2. ${BLUE}InsightLearn-Stress-Test${NC} - On-demand stress testing"
echo -e "  3. ${BLUE}InsightLearn-Nightly-Stress${NC} - Scheduled nightly tests"

echo -e "\n${YELLOW}Access Jenkins:${NC} ${BLUE}$JENKINS_URL${NC}"
echo -e "\n${GREEN}Next Steps:${NC}"
echo -e "  1. Open Jenkins in your browser"
echo -e "  2. Verify the jobs are listed on the dashboard"
echo -e "  3. Click 'Build Now' on any job to run it"
echo -e "  4. View results in the job's build history"

echo -e "\n${GREEN}Done! 🚀${NC}\n"
