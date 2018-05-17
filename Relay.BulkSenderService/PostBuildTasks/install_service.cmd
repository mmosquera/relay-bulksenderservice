powershell Set-ExecutionPolicy Unrestricted
powershell -executionpolicy bypass -noprofile -file scripts/InstallService.ps1
sleep 5
SC failure "RelayBulkSenderService" reset= 0 actions= restart/60000
exit