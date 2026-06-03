FROM python:3.11-slim

# Install system dependencies for Nmap and Nuclei
ARG NUCLEI_VERSION=3.3.8
ARG TARGETARCH

RUN apt-get update && apt-get install -y \
    ca-certificates \
    curl \
    git \
    nmap \
    unzip \
    wget \
    && rm -rf /var/lib/apt/lists/*

# Install Nuclei from a pinned release binary instead of running a remote shell script.
RUN set -eux; \
    case "${TARGETARCH:-amd64}" in \
        amd64) nuclei_arch='amd64' ;; \
        arm64) nuclei_arch='arm64' ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac; \
    curl -fsSL -o /tmp/nuclei.zip "https://github.com/projectdiscovery/nuclei/releases/download/v${NUCLEI_VERSION}/nuclei_${NUCLEI_VERSION}_linux_${nuclei_arch}.zip"; \
    unzip /tmp/nuclei.zip -d /tmp/nuclei; \
    install -m 0755 /tmp/nuclei/nuclei /usr/local/bin/nuclei; \
    rm -rf /tmp/nuclei /tmp/nuclei.zip

WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

CMD ["uvicorn", "src.api.main:app", "--host", "0.0.0.0", "--port", "8000"]
