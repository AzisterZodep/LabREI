@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

set PORT=5000
set ENDPOINT=http://127.0.0.1:%PORT%/status
set PYTHON_SCRIPT=app.py

echo ===============================
echo 🔎 Diagnóstico e Reinício Flask
echo Porta alvo: %PORT%
echo Endpoint: %ENDPOINT%
echo ===============================
echo.

REM 🔍 Matar processos antigos
echo ➤ Verificando processos na porta %PORT%...
FOR /F "tokens=5" %%P IN ('netstat -aon ^| findstr :%PORT% ^| findstr LISTENING') DO (
    echo ⚠️  Matando processo com PID %%P...
    taskkill /F /PID %%P >nul 2>&1
)

REM 🚀 Iniciar Flask
echo.
echo ➤ Iniciando o servidor Flask em nova janela...
start "Flask Server" cmd /k "python %PYTHON_SCRIPT%"

REM ⏳ Esperar resposta
echo.
echo ➤ Aguardando resposta do servidor...

set /A MAX_TRIES=15
set /A TRIES=0

:esperar
timeout /t 2 >nul
set /A TRIES+=1

curl --silent --connect-timeout 2 %ENDPOINT% | findstr /C:"ok" >nul
if %ERRORLEVEL%==0 (
    echo ✅ Servidor respondeu corretamente na tentativa %TRIES%.
    goto fim
)

if %TRIES% GEQ %MAX_TRIES% (
    echo ❌ Servidor não respondeu após %MAX_TRIES% tentativas.
    echo Verifique se o app.py está com erro, ou se a porta 5000 está bloqueada.
    goto fim
)

echo ...aguardando (%TRIES%/%MAX_TRIES%)...
goto esperar

:fim
echo.
pause
