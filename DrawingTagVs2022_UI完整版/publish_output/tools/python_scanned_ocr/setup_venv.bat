@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "VENV_DIR=%SCRIPT_DIR%.venv"

if exist "%VENV_DIR%\pyvenv.cfg" (
    findstr /c:"include-system-site-packages = true" "%VENV_DIR%\pyvenv.cfg" >nul 2>nul
    if errorlevel 1 (
        echo Existing virtual environment does not include system site-packages. Recreating...
        rmdir /s /q "%VENV_DIR%"
    )
)

if exist "%VENV_DIR%\Scripts\python.exe" goto install

if exist "C:\Program Files\Python312\python.exe" (
    set "PYTHON_EXE=C:\Program Files\Python312\python.exe"
    goto create_with_python
) else (
    goto create_with_launcher
)

:create_with_python
echo [1/3] Creating Python virtual environment...
"%PYTHON_EXE%" -m venv "%VENV_DIR%" --system-site-packages
if errorlevel 1 goto fail
goto install

:create_with_launcher
echo [1/3] Creating Python virtual environment...
py -3.12 -m venv "%VENV_DIR%" --system-site-packages
if errorlevel 1 goto fail

:install
echo [2/3] Checking pip...
"%VENV_DIR%\Scripts\python.exe" -m pip --version >nul
if errorlevel 1 goto fail

echo [3/3] Checking existing OCR packages from system site-packages...
"%VENV_DIR%\Scripts\python.exe" -c "import importlib.util,sys;mods=['numpy','cv2','rapidocr_onnxruntime'];missing=[m for m in mods if importlib.util.find_spec(m) is None];print('OK' if not missing else 'MISSING:' + ','.join(missing));sys.exit(0 if not missing else 1)"
if not errorlevel 1 goto ready

echo [3/3] Installing scanned OCR requirements...
"%VENV_DIR%\Scripts\python.exe" -m pip install -r "%SCRIPT_DIR%requirements.txt"
if errorlevel 1 goto fail

:ready
echo Python scanned OCR environment is ready:
echo   "%VENV_DIR%\Scripts\python.exe"
exit /b 0

:fail
echo Failed to prepare Python scanned OCR environment.
exit /b 1
