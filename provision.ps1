Write-Host "Start provision.ps1 script"

$nugetPath = "C:\nuget.exe"
$vsBuildToolsPath = "C:\vs_buildtools.exe"
$net47Path = "C:\net47_redist.exe"
$unity2018_2InstallerPath = "C:\unity_2018_2.exe"
$msBuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"

$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$vsBuildToolsUrl = "https://download.visualstudio.microsoft.com/download/pr/ab370a60-3d9d-40ae-87ce-f24679cc6375/c6a3af7ab1b827bb70ac69590b59f430/vs_buildtools.exe"
$net47Url = "https://download.microsoft.com/download/9/0/1/901B684B-659E-4CBD-BEC8-B3F06967C2E7/NDP471-DevPack-ENU.exe"
$unity2018_2InstallerUrl = "https://netstorage.unity3d.com/unity/88933597c842/UnityDownloadAssistant-2018.2.17f1.exe";

$styleCopInstallParams = 'install', 'StyleCop.MSBuild'
$vsBuildToolsInstallParams = '--add Microsoft.VisualStudio.Workload.MSBuildTools --quiet'
$msBuildParams = '.\ApprienUnitySDK.sln', '/target:rebuild', '/verbosity:minimal', '/p:StyleCopOverrideSettingsFile="stylecop.json"', '/p:WarningLevel=1'
$net47Params = '/q' #, '/norestart'

#Download Nuget if not available
if (-not (Test-Path $nugetPath))
{
    Write-Host "Installing NuGet"
	# Use newest TLS1.2 protocol version for HTTPS connections
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
 
	Invoke-WebRequest -OutFile $nugetPath -Uri $nugetUrl
    Write-Host "NuGet installed!"
}
else
{
	Write-Host "NuGet is already installed"
}

# Install chocolatey
# .\nuget.exe install chocolatey -version 0.10.11
# cd .\chocolatey.0.10.11\tools\
# & .\chocolateyInstall.ps1

if (-not (Test-Path $msBuildPath))
{
    Write-Host "Installing MSBuild"
	# Use newest TLS1.2 protocol version for HTTPS connections
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
 
	Invoke-WebRequest -OutFile $vsBuildToolsPath -Uri $vsBuildToolsUrl

    # Does not work for some odd reason
    # & $vsBuildToolsPath $vsBuildToolsInstallParams
    Write-Host "Running vs_buildtools.exe"
    C:\vs_buildtools.exe --add Microsoft.VisualStudio.Workload.MSBuildTools --quiet

    Start-Sleep -s 5

    if (-not (Test-Path $msBuildPath))
    {
        Write-Host "Cannot find MSBuild.exe"
    } else {
        Write-Host "Found MSBuild.exe"
    }

    if (-not (Test-Path "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vs_installer.exe")) 
    {
        Write-Host "Cannot find vs_installer.exe"
    }
    else 
    {
        Write-Host "Running vs_installer.exe"
        Start-Process "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vs_installer.exe" -ArgumentList 'modify --installPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools" --quiet --add Microsoft.VisualStudio.Component.NuGet.BuildTools --add Microsoft.Net.Component.4.5.TargetingPack --norestart --force' -Wait -PassThru
    }

    Write-Host "MSBuild installed!"
}
else
{
	Write-Host "MSBuild is already installed"
}

if (-not (Test-Path $net47Path))
{
    Write-Host "Installing .NET Framework 4.7"
	# Use newest TLS1.2 protocol version for HTTPS connections
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
 
	Invoke-WebRequest -OutFile $net47Path -Uri $net47Url

    Start-Process $net47Path -ArgumentList $net47Params -Wait -PassThru

    Write-Host ".NET Framework 4.7 installed!"
}
else
{
	Write-Host ".NET Framework 4.7 is already installed"
}

if (-not (Test-Path $unity2018_2InstallerPath)) 
{
    Write-Host "Installing Unity 2018.2"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -OutFile $unity2018_2InstallerPath -Uri $unity2018_2InstallerUrl

    C:\unity_2018_2.exe /S /D=C:\Unity

    Write-Host "Unity 2018.2 installed!"
}
else
{
	Write-Host "Unity 2018.2 is already installed"
}

# Install required NuGet packages
Write-Host "Installing NuGet packages..."

cd "C:/Users/vagrant/apprien-unity-sdk/ApprienUnitySDK/NuGet"
& $nugetPath $styleCopInstallParams

 