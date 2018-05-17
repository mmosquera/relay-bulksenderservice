$serviceName = "RelayBulkSenderService"
$exePath = $(Get-Location).ToString().Replace("PostBuildTasks",".\bin\Debug\Relay.BulkSenderService.exe")

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

"Installing the service."
New-Service -BinaryPathName $exePath -Name $serviceName -DisplayName $serviceName -StartupType Automatic 
"Installed the service."
"Starting the service."
net start $serviceName
"Completed."