# PSServu Build ad deploy

- Build a release version
- Copy the bin/release/netstandard2.0 folder /release/PSServU
- Using PowerShell 7 (it's not working from Powershell 5.1 from my system) publish to the PSGallery:
  Publish-Module -Path .\PSServU -Repository PSGallery -NuGetApiKey "api key from vault"