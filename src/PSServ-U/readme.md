# PSServu Build ad deploy

- Build a release version
- Copy the bin/release/netstandard2.0 folder /release/PSServU
- Using PowerShell 7 (it's not working from Powershell 5.1 from my system) publish to the PSGallery:
  Publish-Module -Path .\PSServU -Repository PSGallery -NuGetApiKey "api key from vault"


## Error while publishing to PSGallery

Error: The underlying connection was closed
Error: Publish-PSArtifactUtility : Failed to publish module ”: ‘The underlying connection was closed: An unexpected error occurred on a send.

Create AddTls.reg file with content:

Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework\v4.0.30319]
"SchUseStrongCrypto"=dword:00000001

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v4.0.30319]
"SchUseStrongCrypto"=dword:00000001