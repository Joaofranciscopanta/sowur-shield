@echo off
echo ================================================
echo Unity Build System Error Fix
echo ================================================
echo.

echo Step 1: Closing Unity processes...
taskkill /F /IM Unity.exe 2>nul
taskkill /F /IM UnityShaderCompiler.exe 2>nul
taskkill /F /IM Unity.ILPP.Runner.exe 2>nul
timeout /t 2 >nul

echo Step 2: Cleaning Unity temporary files...
if exist "Library\BuildPlayerData\" rmdir /S /Q "Library\BuildPlayerData\"
if exist "Library\PlayerDataCache\" rmdir /S /Q "Library\PlayerDataCache\"
if exist "Library\ShaderCache\" rmdir /S /Q "Library\ShaderCache\"
if exist "Temp\" rmdir /S /Q "Temp\"

echo Step 3: Cleaning build cache...
if exist "Library\Bee\" rmdir /S /Q "Library\Bee\"
if exist "Library\ArtifactDB\" rmdir /S /Q "Library\ArtifactDB\"

echo.
echo ================================================
echo Fix completed!
echo ================================================
echo.
echo Next steps:
echo 1. Reopen Unity
echo 2. Wait for it to reimport assets
echo 3. Try your operation again
echo.
pause
