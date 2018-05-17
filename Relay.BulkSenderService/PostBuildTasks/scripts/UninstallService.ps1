$serviceName = "RelayBulkSenderService"

$existingService = Get-WmiObject -Class Win32_Service -Filter "Name='$serviceName'"

if ($existingService) 
{
  "'$serviceName' exists already. Stopping."
  Stop-Service $serviceName
  "Waiting 2 seconds to allow existing service to stop."
  Start-Sleep -s 2

  $existingService.Delete()
  "Waiting 3 seconds to allow service to be uninstalled."
  Start-Sleep -s 3  
}