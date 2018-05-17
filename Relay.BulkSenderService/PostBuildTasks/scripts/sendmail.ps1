$user = "salesrelay@dopplerrelay.com"
$password = ConvertTo-SecureString -String "j]YkIZJd8X@&?[f" -AsPlainText -Force
$recipients = @('mmosquera@makingsense.com', 'dnoya@makingsense.com')
$credential = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $user, $password
Send-MailMessage -From "Support@dopplerrelay.com" -To $recipients -Subject "BULKSENDERSERVICE ERROR" -Body "Revisar servicio bulksenderworker. No se puede iniciar correctamente." -SmtpServer "smtp.dopplerrelay.com" -Port "587" -Credential $credential