@echo off
echo Verificando processos na porta 5000...

FOR /F "tokens=5" %%P IN ('netstat -aon ^| findstr :5000 ^| findstr LISTENING') DO (
    echo Matando processo com PID %%P...
    taskkill /F /PID %%P
)

echo Iniciando Flask...
:: Altere para o nome do seu script Python
python app.py
pause