@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

set "PYTHON_EXE=C:\Users\TECHUP\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
set "OUTPUT_DIR=%SCRIPT_DIR%output\pdf"
set "TEMPLATE_DIR=%SCRIPT_DIR%templates"
set "INPUT_PDF=%~1"

if not exist "%PYTHON_EXE%" (
    echo [ERROR] Python executable not found:
    echo %PYTHON_EXE%
    exit /b 1
)

if "%INPUT_PDF%"=="" (
    echo [INFO] No input PDF provided. Generating sample PDF...
    "%PYTHON_EXE%" "%SCRIPT_DIR%tools\generate_sample_pdf.py"
    if errorlevel 1 (
        echo [ERROR] Failed to generate sample PDF.
        exit /b 1
    )
    set "INPUT_PDF=%SCRIPT_DIR%tmp\pdfs\sample-numbered-circles.pdf"
) else (
    echo [INFO] Using input PDF:
    echo %INPUT_PDF%
)

if not exist "%INPUT_PDF%" (
    echo [ERROR] Input PDF not found:
    echo %INPUT_PDF%
    exit /b 1
)

echo [INFO] Starting numbered-circle detection...
"%PYTHON_EXE%" -m circle_locator.cli "%INPUT_PDF%" --output-dir "%OUTPUT_DIR%" --template-dir "%TEMPLATE_DIR%"
if errorlevel 1 (
    echo [ERROR] Numbered-circle detection failed.
    exit /b 1
)

echo.
echo [DONE] Detection finished.
echo JSON: %OUTPUT_DIR%\detections.json
echo CSV: %OUTPUT_DIR%\detections.csv
echo Preview directory: %OUTPUT_DIR%\preview
echo.
echo [INFO] Open the preview directory to inspect the colored circle markers.
pause
