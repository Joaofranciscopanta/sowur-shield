#!/bin/bash

# Build Verification Script for Sowur Shield WebGL Demo
# This script validates Unity WebGL builds before deployment

set -e  # Exit on any error

BUILD_DIR="${1:-docs}"
EXIT_CODE=0

echo "🔍 Starting build verification for: $BUILD_DIR"
echo "================================================"

# Check 1: Verify directory exists
echo "✓ Checking build directory..."
if [ ! -d "$BUILD_DIR" ]; then
    echo "❌ ERROR: Build directory '$BUILD_DIR' does not exist"
    exit 1
fi
echo "  ✅ Build directory exists"

# Check 2: Verify index.html exists
echo "✓ Checking index.html..."
if [ ! -f "$BUILD_DIR/index.html" ]; then
    echo "❌ ERROR: index.html not found"
    exit 1
fi
echo "  ✅ index.html found"

# Check 3: Verify Build folder exists
echo "✓ Checking Build folder..."
if [ ! -d "$BUILD_DIR/Build" ]; then
    echo "❌ ERROR: Build folder not found"
    exit 1
fi
echo "  ✅ Build folder exists"

# Check 4: Verify TemplateData folder exists
echo "✓ Checking TemplateData folder..."
if [ ! -d "$BUILD_DIR/TemplateData" ]; then
    echo "❌ ERROR: TemplateData folder not found"
    exit 1
fi
echo "  ✅ TemplateData folder exists"

# Check 5: Verify critical Unity files exist
echo "✓ Checking critical Unity build files..."
CRITICAL_FILES_FOUND=0
CRITICAL_FILES_TOTAL=0

for ext in "data" "framework.js" "loader.js" "wasm"; do
    CRITICAL_FILES_TOTAL=$((CRITICAL_FILES_TOTAL + 1))
    FILE_FOUND=$(find "$BUILD_DIR/Build" -name "*.$ext" | head -n 1)
    if [ -n "$FILE_FOUND" ]; then
        echo "  ✅ Found Unity .$ext file"
        CRITICAL_FILES_FOUND=$((CRITICAL_FILES_FOUND + 1))
    else
        echo "  ⚠️  Warning: No .$ext file found"
    fi
done

if [ $CRITICAL_FILES_FOUND -lt 3 ]; then
    echo "❌ ERROR: Missing critical Unity build files (found $CRITICAL_FILES_FOUND of $CRITICAL_FILES_TOTAL)"
    exit 1
fi

# Check 6: Verify custom CSS is preserved
echo "✓ Checking custom CSS preservation..."
if [ ! -f "$BUILD_DIR/TemplateData/style.css" ]; then
    echo "❌ ERROR: style.css not found"
    exit 1
fi

# Check for release notes sidebar CSS
if grep -q "#release-notes" "$BUILD_DIR/TemplateData/style.css"; then
    echo "  ✅ Custom sidebar CSS preserved"
else
    echo "❌ ERROR: Custom CSS missing - #release-notes styles not found"
    exit 1
fi

# Check 7: Verify file sizes are reasonable
echo "✓ Checking file sizes..."
INDEX_SIZE=$(stat -f%z "$BUILD_DIR/index.html" 2>/dev/null || stat -c%s "$BUILD_DIR/index.html" 2>/dev/null || echo "0")
if [ "$INDEX_SIZE" -lt 1000 ]; then
    echo "⚠️  Warning: index.html is suspiciously small ($INDEX_SIZE bytes)"
    EXIT_CODE=1
else
    echo "  ✅ index.html size looks good ($INDEX_SIZE bytes)"
fi

BUILD_FOLDER_SIZE=$(du -sh "$BUILD_DIR/Build" | cut -f1)
echo "  ℹ️  Build folder size: $BUILD_FOLDER_SIZE"

# Check 8: Verify style.css has reasonable size
CSS_SIZE=$(stat -f%z "$BUILD_DIR/TemplateData/style.css" 2>/dev/null || stat -c%s "$BUILD_DIR/TemplateData/style.css" 2>/dev/null || echo "0")
if [ "$CSS_SIZE" -lt 2000 ]; then
    echo "⚠️  Warning: style.css is suspiciously small ($CSS_SIZE bytes) - might be Unity's default"
    EXIT_CODE=1
else
    echo "  ✅ style.css size looks good ($CSS_SIZE bytes)"
fi

echo ""
echo "================================================"
if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Build verification PASSED"
    echo "Build is ready for deployment!"
else
    echo "⚠️  Build verification completed with WARNINGS"
    echo "Please review the warnings above."
fi
echo "================================================"

exit $EXIT_CODE
