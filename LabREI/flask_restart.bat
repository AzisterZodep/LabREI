@echo off
SET /P PORTA="Por favor, digite o número da porta que deseja verificar (ex: 5000): "
echo Verificando processos na porta %PORTA%...

FOR /F "tokens=5" %%P IN ('netstat -aon ^| findstr :%PORTA% ^| findstr LISTENING') DO (
    echo Matando processo com PID %%P...
    taskkill /F /PID %%P
)

echo Iniciando Flask...
:: Altere para o nome do seu script Python
python app.py
pause
