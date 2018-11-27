gitlabBuilds(builds: ["provision", "build", "quality", "test"]) {

    node () {
        checkout scm

        try {
            stage('Provision') {
                gitlabCommitStatus("provision") {
                    echo 'Install hostupdater'
                    sh 'vagrant plugin install vagrant-hostsupdater'

                    sh 'vagrant plugin install vagrant-winrm'
                    echo 'Install WinRM Plugin'

                    echo "Start vagrant"
                    sh 'vagrant up --provision'

                    //sh 'wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb'
                    //sh 'sudo dpkg -i packages-microsoft-prod.deb'
                    //sh 'sudo apt-get update'
                    //sh 'sudo apt-get install -y powershell'
                    //sh 'wget -q https://github.com/PowerShell/PowerShell/releases/download/v6.1.1/powershell_6.1.1-1.ubuntu.18.04_amd64.deb'
                    //sh 'sudo dpkg -i powershell_6.1.1-1.ubuntu.18.04_amd64.deb'
                    //sh 'sudo apt-get install -f'
                }
            }
            gitlabCommitStatus("build") {
                stage('Build') {
                    echo "Build"
                    //sh 'vagrant winrm -c "& \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\BuildTools\\MSBuild\\15.0\\Bin\\MSBuild.exe\" \".C:\\Users\\vagrant\\apprien-unity-sdk\\ApprienUnitySDK\\ApprienUnitySDK.sln /target:rebuild /verbosity:minimal\""'

                    sh 'vagrant winrm -c "Powershell -File C:\\Users\\vagrant\\apprien-unity-sdk\\build.ps1"'
                }
            }
            stage('Quality') {
                gitlabCommitStatus("quality") {
                    echo 'Running QA check'
                    //warnings canComputeNew: false, canResolveRelativePaths: false, categoriesPattern: '', defaultEncoding: '', excludePattern: '', healthy: '', includePattern: '', messagesPattern: '', parserConfigurations: [[parserName: 'StyleCop', pattern: '*.cs']], unHealthy: ''
                    //sh 'vagrant winrm -c "& \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\BuildTools\\MSBuild\\15.0\\Bin\\MSBuild.exe\" \".C:\\Users\\vagrant\\apprien-unity-sdk\\ApprienUnitySDK\\ApprienUnitySDK.csproj /target:rebuild /verbosity:minimal /p:StyleCopOverrideSettingsFile=\'stylecop.json\' /p:WarningLevel=1\""'
                    sh 'vagrant winrm -c "Powershell -File C:\\Users\\vagrant\\apprien-unity-sdk\\qa.ps1"'
                }
            }
            stage('Tests') {
                gitlabCommitStatus("test") {
                    echo 'Running tests'
                    sh 'vagrant winrm -c "Powershell -File C:\\Users\\vagrant\\apprien-unity-sdk\\test.ps1"'
                }
            }
            echo 'Halt vagrant box'
            sh 'vagrant halt'
        } catch(e) {
            currentBuild = 'error'
            throw e
            vagrant destroy
        } finally {
            echo 'Cleaning up'
            sh 'vagrant halt'
        }
    }
}
