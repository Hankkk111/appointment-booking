# Custom Jenkins image with the Docker CLI installed, so Jenkins pipelines
# can run build/test stages inside ephemeral containers (dotnet SDK, node)
# via the Docker Pipeline plugin, using the host's Docker daemon.
FROM jenkins/jenkins:lts-jdk17

USER root

# Install Docker CLI (not the full Docker daemon -- we talk to the host's
# daemon via the socket that docker-compose.yml mounts into this container)
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates curl gnupg && \
    install -m 0755 -d /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg && \
    chmod a+r /etc/apt/keyrings/docker.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian bookworm stable" \
      > /etc/apt/sources.list.d/docker.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends docker-ce-cli && \
    rm -rf /var/lib/apt/lists/*

# Pre-install the plugins this project's Jenkinsfile needs
COPY plugins.txt /usr/share/jenkins/ref/plugins.txt
RUN jenkins-plugin-cli --plugin-file /usr/share/jenkins/ref/plugins.txt

USER jenkins
