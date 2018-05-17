powershell Set-ExecutionPolicy Unrestricted
powershell -executionpolicy bypass -noprofile -file scripts/UninstallService.ps1
sleep 5
exit

