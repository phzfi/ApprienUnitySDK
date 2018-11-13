gitlabBuilds(builds: ["provision", "build", "quality", "test"]) {

    node () {
        checkout scm

        try {
            stage('Provision') {
                gitlabCommitStatus("provision") {
                    echo 'Install hostupdater'
                    sh 'vagrant plugin install vagrant-hostsupdater'

                    echo "Start vagrant"
                    sh 'vagrant up'
                }
            }
            gitlabCommitStatus("build") {
                stage('Build') {
                    echo "Build"
                }
            }
            stage('Quality') {
                gitlabCommitStatus("quality") {
                    echo 'Running QA check'
                }
            }
            stage('Tests') {
                gitlabCommitStatus("test") {
                    echo 'Running tests'
                }
            }
            echo 'Halt vagrant box'
            sh 'vagrant halt'
        } catch(e) {
            currentBuild = 'error'
            throw e
            vagrant destroy
        }
    }
}
