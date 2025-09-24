#!/bin/bash

# Menlo Home Server Setup Script
# This script prepares a home server for Menlo deployment

set -e

echo "🏠 Menlo Home Server Setup Script"
echo "=================================="

# Configuration
MENLO_USER="menlo"
MENLO_DIR="/opt/menlo"
DOCKER_COMPOSE_VERSION="2.24.0"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running as root
if [[ $EUID -eq 0 ]]; then
   log_error "This script should not be run as root. Please run as a regular user with sudo privileges."
   exit 1
fi

# Check sudo privileges
if ! sudo -n true 2>/dev/null; then
    log_error "This script requires sudo privileges. Please ensure your user has sudo access."
    exit 1
fi

log_info "Starting Menlo home server setup..."

# Update system packages
log_info "Updating system packages..."
sudo apt update && sudo apt upgrade -y

# Install required packages
log_info "Installing required packages..."
sudo apt install -y \
    curl \
    wget \
    git \
    htop \
    unzip \
    ca-certificates \
    gnupg \
    lsb-release \
    ufw \
    fail2ban \
    logrotate

# Install Docker
log_info "Installing Docker..."
if ! command -v docker &> /dev/null; then
    # Add Docker's official GPG key
    sudo mkdir -p /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

    # Add Docker repository
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

    # Install Docker
    sudo apt update
    sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # Add current user to docker group
    sudo usermod -aG docker $USER

    log_info "Docker installed successfully"
else
    log_info "Docker is already installed"
fi

# Install Docker Compose (if not available via plugin)
if ! docker compose version &> /dev/null; then
    log_info "Installing Docker Compose..."
    sudo curl -L "https://github.com/docker/compose/releases/download/v${DOCKER_COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi

# Create menlo user
log_info "Creating menlo user..."
if ! id "$MENLO_USER" &>/dev/null; then
    sudo useradd -m -s /bin/bash $MENLO_USER
    sudo usermod -aG docker $MENLO_USER
    log_info "Created user: $MENLO_USER"
else
    log_info "User $MENLO_USER already exists"
fi

# Create application directory
log_info "Creating application directory..."
sudo mkdir -p $MENLO_DIR
sudo chown $MENLO_USER:$MENLO_USER $MENLO_DIR

# Create required subdirectories
sudo -u $MENLO_USER mkdir -p $MENLO_DIR/{backups,logs,data,ssl}

# Set up firewall
log_info "Configuring firewall..."
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow ssh
sudo ufw allow 8080  # Menlo API
# Note: We don't open external ports since we use Cloudflare Tunnel
sudo ufw --force enable

# Configure fail2ban
log_info "Configuring fail2ban..."
sudo systemctl enable fail2ban
sudo systemctl start fail2ban

# Install Cloudflare Tunnel (cloudflared)
log_info "Installing Cloudflare Tunnel..."
if ! command -v cloudflared &> /dev/null; then
    # Download and install cloudflared
    wget -q https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
    sudo dpkg -i cloudflared-linux-amd64.deb || sudo apt-get install -f -y
    rm cloudflared-linux-amd64.deb
    log_info "Cloudflared installed successfully"
else
    log_info "Cloudflared is already installed"
fi

# Create systemd service for Docker containers
log_info "Creating systemd service..."
sudo tee /etc/systemd/system/menlo.service > /dev/null <<EOF
[Unit]
Description=Menlo Home Management Application
After=docker.service
Requires=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=$MENLO_DIR
User=$MENLO_USER
Group=$MENLO_USER
ExecStart=/usr/bin/docker compose -f docker-compose.prod.yml up -d
ExecStop=/usr/bin/docker compose -f docker-compose.prod.yml down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable menlo.service

# Set up log rotation
log_info "Configuring log rotation..."
sudo tee /etc/logrotate.d/menlo > /dev/null <<EOF
$MENLO_DIR/logs/*.log {
    daily
    missingok
    rotate 14
    compress
    delaycompress
    notifempty
    create 644 $MENLO_USER $MENLO_USER
    postrotate
        /usr/bin/docker compose -f $MENLO_DIR/docker-compose.prod.yml restart menlo-api || true
    endscript
}
EOF

# Create backup script
log_info "Creating backup script..."
sudo tee $MENLO_DIR/backup.sh > /dev/null <<'EOF'
#!/bin/bash

# Menlo Backup Script
set -e

BACKUP_DIR="/opt/menlo/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=30

log_info() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] INFO: $1"
}

log_info "Starting Menlo backup..."

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup database
log_info "Backing up database..."
docker compose -f /opt/menlo/docker-compose.prod.yml exec -T postgres pg_dump -U $POSTGRES_USER $POSTGRES_DB | gzip > $BACKUP_DIR/menlo_db_$TIMESTAMP.sql.gz

# Backup configuration files
log_info "Backing up configuration..."
tar -czf $BACKUP_DIR/menlo_config_$TIMESTAMP.tar.gz \
    /opt/menlo/docker-compose.prod.yml \
    /opt/menlo/*.env 2>/dev/null || true

# Backup Ollama models (if needed)
if [ -d "/var/lib/docker/volumes/menlo_ollama_data" ]; then
    log_info "Backing up Ollama models..."
    tar -czf $BACKUP_DIR/menlo_ollama_$TIMESTAMP.tar.gz -C /var/lib/docker/volumes/menlo_ollama_data _data/
fi

# Clean old backups
log_info "Cleaning old backups..."
find $BACKUP_DIR -name "*.gz" -mtime +$RETENTION_DAYS -delete

log_info "Backup completed: $BACKUP_DIR/menlo_*_$TIMESTAMP.*"
EOF

sudo chmod +x $MENLO_DIR/backup.sh
sudo chown $MENLO_USER:$MENLO_USER $MENLO_DIR/backup.sh

# Set up daily backup cron job
log_info "Setting up daily backup..."
(sudo crontab -u $MENLO_USER -l 2>/dev/null; echo "0 2 * * * $MENLO_DIR/backup.sh >> $MENLO_DIR/logs/backup.log 2>&1") | sudo crontab -u $MENLO_USER -

# Create environment template
log_info "Creating environment template..."
sudo -u $MENLO_USER tee $MENLO_DIR/.env.template > /dev/null <<EOF
# Menlo Environment Configuration
# Copy this file to .env and fill in the values

# Database Configuration
POSTGRES_DB=menlo
POSTGRES_USER=menlo_user
POSTGRES_PASSWORD=your_secure_password_here

# Application Configuration
ASPNETCORE_ENVIRONMENT=Production
DATABASE_CONNECTION_STRING=Host=postgres;Port=5432;Database=menlo;Username=menlo_user;Password=your_secure_password_here

# Ollama Configuration
OLLAMA_BASE_URL=http://ollama:11434

# Cloudflare Tunnel
CLOUDFLARE_TUNNEL_TOKEN=your_tunnel_token_here
EOF

# Create healthcheck script
log_info "Creating healthcheck script..."
sudo -u $MENLO_USER tee $MENLO_DIR/healthcheck.sh > /dev/null <<'EOF'
#!/bin/bash

# Menlo Health Check Script
set -e

API_URL="http://localhost:8080"
TIMEOUT=10

check_service() {
    local service=$1
    local url=$2

    if curl -f -s --max-time $TIMEOUT "$url" > /dev/null; then
        echo "✅ $service is healthy"
        return 0
    else
        echo "❌ $service is unhealthy"
        return 1
    fi
}

echo "🏥 Menlo Health Check"
echo "===================="

# Check API
check_service "API" "$API_URL/health"

# Check database
if docker compose -f /opt/menlo/docker-compose.prod.yml exec -T postgres pg_isready -U $POSTGRES_USER -d $POSTGRES_DB > /dev/null 2>&1; then
    echo "✅ Database is healthy"
else
    echo "❌ Database is unhealthy"
fi

# Check Ollama
if curl -f -s --max-time $TIMEOUT "http://localhost:11434/api/tags" > /dev/null; then
    echo "✅ Ollama is healthy"
else
    echo "❌ Ollama is unhealthy"
fi

echo "Health check completed"
EOF

sudo chmod +x $MENLO_DIR/healthcheck.sh

# Display completion message
log_info "Home server setup completed successfully!"
echo
echo "📋 Next Steps:"
echo "=============="
echo "1. Copy $MENLO_DIR/.env.template to $MENLO_DIR/.env and configure it"
echo "2. Set up Cloudflare Tunnel:"
echo "   - cloudflared tunnel login"
echo "   - cloudflared tunnel create menlo-api"
echo "   - Configure tunnel in Cloudflare dashboard"
echo "3. Add SSH public key for GitHub Actions deployment"
echo "4. Test the deployment with: $MENLO_DIR/healthcheck.sh"
echo
echo "📁 Important directories:"
echo "- Application: $MENLO_DIR"
echo "- Backups: $MENLO_DIR/backups"
echo "- Logs: $MENLO_DIR/logs"
echo
echo "🔧 Useful commands:"
echo "- View logs: docker compose -f $MENLO_DIR/docker-compose.prod.yml logs -f"
echo "- Restart services: sudo systemctl restart menlo"
echo "- Run backup: $MENLO_DIR/backup.sh"
echo "- Health check: $MENLO_DIR/healthcheck.sh"

if [[ $USER != "$MENLO_USER" ]]; then
    log_warn "You may need to log out and back in for Docker group membership to take effect"
fi
