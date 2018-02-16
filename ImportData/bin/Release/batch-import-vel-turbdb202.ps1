echo off
<# Batch file for ingesting and informing over email #>

$efrom = "ssh@jhu.edu"
$eto = "stephenshamilton@gmail.com"
$subject = "DSP012 turbdb202 update"
$body = "Import Initiated"
$SMTPServer = "smtp.johnshopkins.edu"
$SMTPClient = New-Object Net.Mail.SmtpClient($SMTPServer, 25)
$SMTPClient.Send($efrom, $eto, $subject, $body)

echo "Verify email was sent, then press enter to start ingest"
pause
.\isoimportdatanew z:\ vel 15050 15100 10 -4800 134217728 268434944 turbdb202 localhost
$body = "Finished velocity timestep 15100"
$SMTPClient.Send($efrom, $eto, $subject, $body)
.\isoimportdatanew z:\snapshots\ vel 15110 20410 10 -4800 134217728 268434944 turbdb202 localhost
$body = "Finished velocity timestep 20410"
$SMTPClient.Send($efrom, $eto, $subject, $body)
.\isoimportdatanew z:\snapshots_2\ vel 20420 30100 10 -4800 134217728 268434944 turbdb202 localhost
$body = "Finished velocity timestep 30100"
$SMTPClient.Send($efrom, $eto, $subject, $body) 
.\isoimportdatanew z:\snapshots_3\ vel 30110 55100 10 -4800 134217728 268434944 turbdb202 localhost
$body = "Finished velocity turbdb205 final."
$SMTPClient.Send($efrom, $eto, $subject, $body)

