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
CRITICAL_FILES_TOTAL=4

# Check for data file (can be .data, .data.gz, .data.br, .data.unityweb)
DATA_FILE=$(find "$BUILD_DIR/Build" \( -name "*.data*" -o -name "*.unityweb" \) -type f | head -n 1)
if [ -n "$DATA_FILE" ]; then
    echo "  ✅ Found Unity data file: $(basename "$DATA_FILE")"
    CRITICAL_FILES_FOUND=$((CRITICAL_FILES_FOUND + 1))
else
    echo "  ⚠️  Warning: No data file found"
fi

# Check for framework file (can be .framework.js, .framework.js.gz, .framework.js.br)
FRAMEWORK_FILE=$(find "$BUILD_DIR/Build" -name "*.framework.js*" -type f | head -n 1)
if [ -n "$FRAMEWORK_FILE" ]; then
    echo "  ✅ Found Unity framework file: $(basename "$FRAMEWORK_FILE")"
    CRITICAL_FILES_FOUND=$((CRITICAL_FILES_FOUND + 1))
else
    echo "  ⚠️  Warning: No framework file found"
fi

# Check for loader file (can be .loader.js, .loader.js.gz, .loader.js.br)
LOADER_FILE=$(find "$BUILD_DIR/Build" -name "*.loader.js*" -type f | head -n 1)
if [ -n "$LOADER_FILE" ]; then
    echo "  ✅ Found Unity loader file: $(basename "$LOADER_FILE")"
    CRITICAL_FILES_FOUND=$((CRITICAL_FILES_FOUND + 1))
else
    echo "  ⚠️  Warning: No loader file found"
fi

# Check for wasm file (can be .wasm, .wasm.gz, .wasm.br)
WASM_FILE=$(find "$BUILD_DIR/Build" -name "*.wasm*" -type f | head -n 1)
if [ -n "$WASM_FILE" ]; then
    echo "  ✅ Found Unity wasm file: $(basename "$WASM_FILE")"
    CRITICAL_FILES_FOUND=$((CRITICAL_FILES_FOUND + 1))
else
    echo "  ⚠️  Warning: No wasm file found"
fi

echo "  ℹ️  Found $CRITICAL_FILES_FOUND of $CRITICAL_FILES_TOTAL critical files"

# List all files in Build folder for debugging
echo "  ℹ️  Build folder contents:"
ls -lh "$BUILD_DIR/Build" | tail -n +2 | awk '{print "    - " $9 " (" $5 ")"}'

if [ $CRITICAL_FILES_FOUND -lt 2 ]; then
    echo "❌ ERROR: Missing critical Unity build files (found $CRITICAL_FILES_FOUND of $CRITICAL_FILES_TOTAL)"
    exit 1
fi

# Check 6: Verify custom CSS is preserved
echo "✓ Checking custom CSS preservation..."
if [ ! -f "$BUILD_DIR/TemplateData/style.css" ]; then
    echo "❌ ERROR: style.css not found"
    exit 1
fi

# Check for custom landing page CSS
if grep -q ".site-header" "$BUILD_DIR/TemplateData/style.css"; then
    echo "  ✅ Custom landing page CSS preserved"
else
    echo "❌ ERROR: Custom CSS missing - .site-header styles not found"
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
