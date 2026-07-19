#!/bin/bash

# CSS Restoration Script for Sowur Shield WebGL Demo
# This script restores custom sidebar CSS after Unity build extraction

set -e  # Exit on any error

TEMPLATE_CSS="${1:-.github/templates/style.css}"
TARGET_CSS="${2:-docs/TemplateData/style.css}"

echo "🎨 Restoring custom CSS..."
echo "================================================"
echo "Source: $TEMPLATE_CSS"
echo "Target: $TARGET_CSS"

# Check 1: Verify template CSS exists
if [ ! -f "$TEMPLATE_CSS" ]; then
    echo "❌ ERROR: Template CSS not found at $TEMPLATE_CSS"
    exit 1
fi
echo "✅ Template CSS found"

# Check 2: Verify target directory exists
TARGET_DIR=$(dirname "$TARGET_CSS")
if [ ! -d "$TARGET_DIR" ]; then
    echo "❌ ERROR: Target directory not found: $TARGET_DIR"
    exit 1
fi
echo "✅ Target directory exists"

# Check 3: Backup existing CSS if it exists
if [ -f "$TARGET_CSS" ]; then
    BACKUP_CSS="${TARGET_CSS}.backup"
    cp "$TARGET_CSS" "$BACKUP_CSS"
    echo "✅ Backed up existing CSS to $BACKUP_CSS"
fi

# Check 4: Copy template CSS to target
cp "$TEMPLATE_CSS" "$TARGET_CSS"
echo "✅ Custom CSS copied to target"

# Check 5: Verify CSS was copied correctly
if [ ! -f "$TARGET_CSS" ]; then
    echo "❌ ERROR: CSS copy failed - target file does not exist"
    exit 1
fi

# Check 6: Verify custom CSS content is present
if grep -q ".site-header" "$TARGET_CSS"; then
    echo "✅ Custom landing page CSS verified (.site-header found)"
else
    echo "❌ ERROR: Custom CSS verification failed - .site-header not found"
    exit 1
fi

# Check 7: Report file size
CSS_SIZE=$(stat -f%z "$TARGET_CSS" 2>/dev/null || stat -c%s "$TARGET_CSS" 2>/dev/null || echo "0")
echo "ℹ️  CSS file size: $CSS_SIZE bytes"

echo "================================================"
echo "✅ CSS restoration completed successfully!"
echo "================================================"

exit 0
