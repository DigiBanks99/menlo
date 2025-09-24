# Podman Home Server Setup for Menlo

This guide provides setup instructions for deploying Menlo using Podman instead of Docker, offering enhanced security through rootless containers.

## 🏠 Why Podman?

### Security Benefits
- **Rootless containers**: No privileged daemon running as root
- **Better isolation**: Process and user namespace separation
- **Reduced attack surface**: No central daemon to compromise
- **Compatibility**: Drop-in replacement for Docker commands

### Architecture Benefits
- **Daemonless**: No background service consuming resources
- **Systemd integration**: Native systemd service management
- **Pods support**: Kubernetes-like pod deployment locally
- **Multi-architecture**: Better support for ARM and other architectures

## 📋 Prerequisites

- Ubuntu 20.04+ or Debian 11+ server
- Minimum 8GB RAM, 4-core CPU, 100GB storage
- Stable internet connection
- Sudo privileges

## 🛠️ Installation Steps

### Step 1: Install Podman

#### Ubuntu/Debian:
```bash
# Update packages
sudo apt update && sudo apt upgrade -y

# Install Podman
sudo apt install -y podman podman-compose

# For latest version, use Podman repository
. /etc/os-release
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://download.opensuse.org/repositories/devel:kubic:libcontainers:unstable/xUbuntu_${VERSION_ID}/Release.key \
  | gpg --dearmor \
  | sudo tee /etc/apt/keyrings/devel_kubic_libcontainers_unstable.gpg > /dev/null
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/devel_kubic_libcontainers_unstable.gpg]\
    https://download.opensuse.org/repositories/devel:kubic:libcontainers:unstable/xUbuntu_${VERSION_ID}/ /" \
  | sudo tee /etc/apt/sources.list.d/devel:kubic:libcontainers:unstable.list > /dev/null
sudo apt update
sudo apt install -y podman podman-compose
```

#### RHEL/CentOS/Fedora:
```bash
# Install Podman
sudo dnf install -y podman podman-compose

# Or for older versions
# sudo yum install -y podman podman-compose
```

### Step 2: Configure Rootless Podman

```bash
# Enable lingering for the user (allows services to run without login)
sudo loginctl enable-linger $USER

# Configure subuid and subgid ranges (if not already configured)
echo "$USER:100000:65536" | sudo tee -a /etc/subuid
echo "$USER:100000:65536" | sudo tee -a /etc/subgid

# Initialize Podman for rootless operation
podman system migrate

# Create systemd user directory
mkdir -p ~/.config/systemd/user

# Enable user services
systemctl --user daemon-reload
```

### Step 3: Configure Networking

```bash
# Create a dedicated network for Menlo
podman network create menlo-network

# List networks to verify
podman network ls
```

### Step 4: Set up Application Directory

```bash
# Create application directory in user home
mkdir -p $HOME/menlo/{backups,logs,data}
cd $HOME/menlo

# Set proper permissions
chmod 755 $HOME/menlo
chmod 750 $HOME/menlo/{backups,logs,data}
```

### Step 5: Create Systemd Services

Create a systemd service for automatic startup:

```bash
# Create menlo service file
cat > ~/.config/systemd/user/menlo.service << 'EOF'
[Unit]
Description=Menlo Home Management Application
Requires=network-online.target
After=network-online.target
RequiresMountsFor=%h/menlo

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=%h/menlo
ExecStart=/usr/bin/podman-compose -f docker-compose.prod.yml up -d
ExecStop=/usr/bin/podman-compose -f docker-compose.prod.yml down
TimeoutStartSec=0

[Install]
WantedBy=default.target
EOF

# Enable the service
systemctl --user enable menlo.service
```

## 🔐 Security Configuration

### Container Security

Create a security configuration file:

```bash
# Create security.conf for enhanced container security
cat > $HOME/menlo/security.conf << 'EOF'
# Podman security configuration
[containers]

# Default capabilities to drop
default_capabilities = [
  "AUDIT_WRITE",
  "MKNOD",
  "NET_RAW",
  "CHOWN",
  "DAC_OVERRIDE",
  "FOWNER",
  "FSETID",
  "KILL",
  "SETGID",
  "SETUID",
  "SETPCAP",
  "NET_BIND_SERVICE",
  "SYS_CHROOT"
]

# Security labels
label = true

# No new privileges
no_new_privs = true

# Read-only root filesystem where possible
read_only_tmpfs = true
EOF
```

### Firewall Configuration

```bash
# Configure UFW for Podman
sudo ufw allow ssh
sudo ufw allow 8080/tcp  # API port (internal only)
sudo ufw enable

# For external access via Cloudflare Tunnel, no additional ports needed
```

## 📦 Deployment Script

Create an enhanced deployment script optimized for Podman:

```bash
cat > $HOME/menlo/deploy.sh << 'EOF'
#!/bin/bash
set -e

echo "🚀 Starting Menlo deployment with Podman..."

# Configuration
COMPOSE_FILE="docker-compose.prod.yml"
APP_DIR="$HOME/menlo"
BACKUP_DIR="$HOME/menlo/backups"

# Create directories
mkdir -p $APP_DIR $BACKUP_DIR
cd $APP_DIR

# Backup current configuration
if [ -f "$COMPOSE_FILE" ]; then
    echo "📦 Backing up current configuration..."
    cp $COMPOSE_FILE "$BACKUP_DIR/docker-compose.$(date +%Y%m%d_%H%M%S).yml"
fi

# Pull new images
echo "📥 Pulling new container images..."
podman-compose -f $COMPOSE_FILE pull

# Perform database backup if Postgres is running
if podman-compose -f $COMPOSE_FILE ps postgres | grep -q "Up"; then
    echo "💾 Creating database backup..."
    podman-compose -f $COMPOSE_FILE exec -T postgres pg_dump -U $POSTGRES_USER $POSTGRES_DB > "$BACKUP_DIR/menlo_db_$(date +%Y%m%d_%H%M%S).sql" || echo "⚠️ Database backup failed"
fi

# Deploy with zero-downtime strategy
echo "🔄 Deploying new version..."

# Start new containers
podman-compose -f $COMPOSE_FILE up -d --remove-orphans

# Wait for health checks
echo "🏥 Waiting for health checks..."
for i in {1..30}; do
    if podman-compose -f $COMPOSE_FILE ps | grep -E "(healthy|Up \(healthy\))"; then
        echo "✅ Services are healthy"
        break
    fi
    echo "⏳ Waiting for services to be healthy... ($i/30)"
    sleep 10
done

# Clean up old images
echo "🧹 Cleaning up old container images..."
podman image prune -f

echo "✅ Deployment completed successfully!"

# Display running services
echo "📊 Running services:"
podman-compose -f $COMPOSE_FILE ps

# Show systemd status
echo "🔄 Systemd service status:"
systemctl --user status menlo.service --no-pager
EOF

chmod +x $HOME/menlo/deploy.sh
```

## 🏥 Health Monitoring

Create a comprehensive health check script:

```bash
cat > $HOME/menlo/healthcheck.sh << 'EOF'
#!/bin/bash

echo "🏥 Menlo Health Check with Podman"
echo "================================="

# Check if services are running
echo "📊 Container Status:"
podman-compose -f $HOME/menlo/docker-compose.prod.yml ps

echo ""
echo "🔍 Individual Service Health:"

# Check API health
if curl -sf http://localhost:8080/health >/dev/null 2>&1; then
    echo "✅ API is healthy"
else
    echo "❌ API is unhealthy"
fi

# Check database
if podman exec menlo-postgres pg_isready -U $POSTGRES_USER -d $POSTGRES_DB >/dev/null 2>&1; then
    echo "✅ Database is healthy"
else
    echo "❌ Database is unhealthy"
fi

# Check Ollama
if curl -sf http://localhost:11434/api/tags >/dev/null 2>&1; then
    echo "✅ Ollama is healthy"
else
    echo "❌ Ollama is unhealthy"
fi

echo ""
echo "📈 Resource Usage:"
podman stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"

echo ""
echo "🌐 Network Status:"
podman network ls
EOF

chmod +x $HOME/menlo/healthcheck.sh
```

## 🔄 Backup and Maintenance

### Automated Backup Script

```bash
cat > $HOME/menlo/backup.sh << 'EOF'
#!/bin/bash

BACKUP_DIR="$HOME/menlo/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

echo "💾 Starting backup process..."

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup configuration
cp $HOME/menlo/docker-compose.prod.yml "$BACKUP_DIR/docker-compose.$TIMESTAMP.yml"

# Backup database
if podman exec menlo-postgres pg_dump -U $POSTGRES_USER $POSTGRES_DB > "$BACKUP_DIR/menlo_db_$TIMESTAMP.sql"; then
    gzip "$BACKUP_DIR/menlo_db_$TIMESTAMP.sql"
    echo "✅ Database backup completed"
else
    echo "❌ Database backup failed"
fi

# Backup Ollama models (if needed)
podman exec menlo-ollama tar -czf - -C /root/.ollama . > "$BACKUP_DIR/ollama_models_$TIMESTAMP.tar.gz" || echo "⚠️ Ollama backup failed"

# Clean up old backups (keep last 7 days)
find $BACKUP_DIR -name "*.sql.gz" -mtime +7 -delete
find $BACKUP_DIR -name "*.yml" -mtime +7 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete

echo "✅ Backup process completed"
EOF

chmod +x $HOME/menlo/backup.sh
```

### Cron Job for Automated Backups

```bash
# Add daily backup to crontab
(crontab -l 2>/dev/null; echo "0 2 * * * $HOME/menlo/backup.sh >> $HOME/menlo/logs/backup.log 2>&1") | crontab -
```

## 🚨 Troubleshooting

### Common Issues

**Permission Issues**:
```bash
# Fix subuid/subgid
sudo usermod --add-subuids 100000-165535 --add-subgids 100000-165535 $USER
podman system migrate
```

**Network Issues**:
```bash
# Reset networking
podman system reset --force
podman network create menlo-network
```

**Service Issues**:
```bash
# Check systemd user service
systemctl --user status menlo.service
journalctl --user -u menlo.service -f
```

### Podman-Specific Commands

```bash
# List all containers
podman ps -a

# Show pods
podman pod ls

# Container logs
podman logs menlo-api

# Enter container
podman exec -it menlo-api /bin/bash

# System information
podman system info

# Clean up everything
podman system prune -a
```

## 📊 Performance Optimization

### System Limits

```bash
# Increase container limits
echo 'net.core.somaxconn = 65535' | sudo tee -a /etc/sysctl.conf
echo 'vm.max_map_count = 262144' | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

### Container Resource Limits

Add to your docker-compose.prod.yml:

```yaml
services:
  menlo-api:
    # ... other config
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '1.0'
        reservations:
          memory: 1G
          cpus: '0.5'
```

## 🎯 Benefits of Podman vs Docker

### Security
- ✅ Rootless by default
- ✅ No privileged daemon
- ✅ Better namespace isolation
- ✅ SELinux integration

### Operations
- ✅ Systemd native integration
- ✅ No central daemon to manage
- ✅ User services (no sudo needed)
- ✅ Resource efficiency

### Development
- ✅ Docker CLI compatibility
- ✅ Docker Compose support
- ✅ Kubernetes YAML support
- ✅ Multi-architecture builds

---

This Podman setup provides the same functionality as Docker but with enhanced security and better integration with Linux system services!
