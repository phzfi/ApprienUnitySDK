Write-Host "Start qa.ps1 script"
Write-Host "Running StyleCop with MSBuild"
cd "C:\Users\vagrant\apprien-unity-sdk\ApprienUnitySDK"
dir
& "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe" "C:\Users\vagrant\apprien-unity-sdk\ApprienUnitySDK\ApprienUnitySDK.csproj", "/target:rebuild", "/verbosity:minimal", "/p:StyleCopOverrideSettingsFile='stylecop.json'", "/p:WarningLevel=1"