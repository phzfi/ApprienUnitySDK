Write-Host "Start build.ps1 script"
Write-Host "Opening project with Unity"
C:\Unity\Editor\Unity.exe -quit -force-free -batchmode -projectPath C:\Users\vagrant\apprien-unity-sdk\ApprienUnitySDK

Write-Host "Building with MSBuild"
cd "C:\Users\vagrant\apprien-unity-sdk\ApprienUnitySDK"
dir
& "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe" "C:\Users\vagrant\apprien-unity-sdk\ApprienUnitySDK\ApprienUnitySDK.csproj", "/target:rebuild", "/verbosity:minimal", "/p:WarningLevel=1"