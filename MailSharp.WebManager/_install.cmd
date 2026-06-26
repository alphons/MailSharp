@echo off
setlocal

net session >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Run this script as Administrator.
    pause
    exit /b 1
)

set SERVICE_NAME=MailSharp
set EXE_PATH=%~dp0MailSharp.WebManager.exe

if not exist "%EXE_PATH%" (
    echo ERROR: Executable not found: %EXE_PATH%
    pause
    exit /b 1
)

echo Stopping existing service (if running)...
sc stop %SERVICE_NAME% >nul 2>&1
timeout /t 3 /nobreak >nul

echo Removing existing service (if present)...
sc delete %SERVICE_NAME% >nul 2>&1
timeout /t 2 /nobreak >nul

echo Creating service...
sc create %SERVICE_NAME% binPath= "\"%EXE_PATH%\"" DisplayName= "MailSharp" start= delayed-auto
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to create service.
    pause
    exit /b 1
)

echo Configuring service dependencies...
sc config %SERVICE_NAME% depend= Eventlog/RPCSS/Dhcp

echo Configuring failure actions (restart after 60s, three times)...
sc failure %SERVICE_NAME% reset= 86400 actions= restart/60000/restart/60000/restart/60000

echo Setting service start timeout...
REG ADD "HKLM\SYSTEM\CurrentControlSet\Control" /v ServicesPipeTimeout /t REG_DWORD /d 120000 /f >nul

echo Starting service...
sc start %SERVICE_NAME%
if %ERRORLEVEL% neq 0 (
    echo WARNING: Service created but failed to start. Check Event Viewer for details.
    pause
    exit /b 1
)

echo.
echo Service "%SERVICE_NAME%" installed and started successfully.
pause
