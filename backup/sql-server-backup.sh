#!/bin/bash

# ==============================================================================
# AUTOMATED SQL SERVER BACKUP TO GOOGLE DRIVE (RCLONE)
# Description: Performs a Full DB Backup, uploads it to Cloud, 
#              and rotates (deletes) old files (local + cloud).
# Requirements: sqlcmd, rclone
# ==============================================================================

# 1. CONFIGURATION AND SECURITY
# ------------------------------------------------------------------------------

# Automatic .env loading (useful for Cron jobs)
# Looks for a .env file in the same directory as the script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/.env" ]; then
    # Export all variables defined in .env
    set -o allexport
    # shellcheck disable=SC1090
    . "$SCRIPT_DIR/.env"
    set +o allexport
fi

# Configuration variables (fallbacks if not set in environment/.env)
DB_USER="${DB_USER:-SA}"
# The syntax below ensures the script stops if DB_PASS is missing
DB_PASS="${DB_PASS:?❌ Error: DB_PASS is not set. Export it or create a .env file.}"
DB_NAME="${DB_NAME:-ERaspored}"

BACKUP_DIR="/var/opt/mssql/backup"
TIMESTAMP="$(date +%F_%H-%M)"
FILE_NAME="${DB_NAME}_FULL_${TIMESTAMP}.bak"
LOCAL_PATH="$BACKUP_DIR/$FILE_NAME"

# sqlcmd binary path
SQLCMD="/opt/mssql-tools/bin/sqlcmd"

# Rclone configuration
RCLONE_REMOTE="gdrive_encrypted"
CLOUD_DIR="ERasporedBackups"
RETENTION_DAYS="14"  

# Logging function
log() {
    echo "[$(date +'%Y-%m-%d %H:%M:%S')] $1"
}

# Trap & strict mode – fail fast on errors
trap 'log "❌ Script failed on line $LINENO"; exit 1' ERR
set -Eeuo pipefail

# Sanity checks
[ -x "$SQLCMD" ] || { log "❌ sqlcmd not found at $SQLCMD"; exit 1; }
command -v rclone >/dev/null 2>&1 || { log "❌ rclone not found in PATH"; exit 1; }

# Ensure backup directory exists
if [ ! -d "$BACKUP_DIR" ]; then
    log "📁 Creating backup directory: $BACKUP_DIR"
    mkdir -p "$BACKUP_DIR"
    # IMPORTANT: Ensure 'mssql' user has write permissions to this folder!
    # chown mssql:mssql "$BACKUP_DIR"
fi

# 2. BACKUP PROCESS
# ------------------------------------------------------------------------------
log "🚀 Starting backup process for database: $DB_NAME"

log "⏳ Creating local backup..."
# NOTE: Remove WITH COMPRESSION if using SQL Server Express without backup compression support
"$SQLCMD" \
    -S localhost \
    -U "$DB_USER" -P "$DB_PASS" \
    -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$LOCAL_PATH' WITH COMPRESSION, STATS = 10"

if [ -f "$LOCAL_PATH" ]; then
    log "✅ Local backup successfully created: $FILE_NAME"
else
    log "❌ ERROR: Backup file was not created!"
    exit 1
fi

# 3. UPLOAD TO CLOUD (RCLONE)
# ------------------------------------------------------------------------------
log "☁️  Uploading to Google Drive ($RCLONE_REMOTE)..."
rclone copy "$LOCAL_PATH" "$RCLONE_REMOTE:$CLOUD_DIR" --checksum
log "✅ Upload completed."

# 4. ROTATION (CLEANUP)
# ------------------------------------------------------------------------------
log "🧹 Cleaning up old files (older than $RETENTION_DAYS days)..."

# Cloud rotation
rclone delete "$RCLONE_REMOTE:$CLOUD_DIR" --min-age "${RETENTION_DAYS}d"
log "   -> Old files deleted from Cloud."

# Local rotation
find "$BACKUP_DIR" -name "${DB_NAME}_FULL_*.bak" -mtime +"$RETENTION_DAYS" -delete
log "   -> Old files deleted from local disk."

log "🏁 Process successfully finished!"
exit 0


