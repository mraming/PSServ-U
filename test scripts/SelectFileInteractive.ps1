Import-Module ../src/PSServ-U/bin/Release/netstandard2.0/PSServU.psd1

$UserName = "muf319"
$pass = ConvertTo-SecureString "rC#z3Que" -AsPlainText -Force
$credentials =  New-Object -TypeName PSCredential -ArgumentList $UserName, $pass

New-ServUSession -Url "https://ftp.pdo.co.om" -Credential $credentials

$Files =  Get-ServUChildItems -RemotePath /ewcat | 
    select Name, LastWriteTime | 
    sort LastWriteTime

$GridArguments = @{
    OutputMode = 'Single'
    Title      = 'Please select a file to install and click OK'
}

$File = $Files | Out-GridView @GridArguments | foreach {
    $_.Name
}

$remoteFile = "/ewcat/$File"

Write-Host "Downloading file $remoteFile"

Get-ServUFile -RemoteFile $remoteFile -localPath "C:\temp\ewcat_install\" -overwrite
Expand-archive -path "C:\temp\ewcat_install\$File" -DestinationPath "C:\temp\ewcat_install\"



# PSServu Build ad deploy
