pipeline {
    agent any

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO = '1'
    }

    stages {
        stage('Backend: Restore, Build & Test') {
            steps {
                script {
                    docker.image('mcr.microsoft.com/dotnet/sdk:8.0').inside {
                        dir('backend') {
                            sh 'dotnet restore'
                            sh 'dotnet build --no-restore -c Release'
                            sh 'dotnet test --no-restore -c Release'
                        }
                    }
                }
            }
        }

        stage('Frontend: Install, Test & Build') {
            steps {
                script {
                    docker.image('node:20').inside {
                        dir('frontend') {
                            sh 'npm ci'
                            sh 'npm test'
                            sh 'npm run build'
                        }
                    }
                }
            }
        }
    }

    post {
        success {
            echo 'Backend and frontend built and tested successfully.'
        }
        failure {
            echo 'Pipeline failed -- check the stage logs above for details.'
        }
    }
}