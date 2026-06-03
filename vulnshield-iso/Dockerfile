FROM python:3.11-slim

# Install system dependencies for Nmap and Nuclei
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    nmap \
    git \
    && rm -rf /var/lib/apt/lists/*

# Install Nuclei
RUN curl -sL https://raw.githubusercontent.com/projectdiscovery/nuclei/main/go-install.sh | bash /dev/stdin \
    && ln -s /root/go/bin/nuclei /usr/local/bin/nuclei

WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

CMD ["uvicorn", "src.api.main:app", "--host", "0.0.0.0", "--port", "8000"]
