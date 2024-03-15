#!/bin/sh
set -x # No -ex - we wish to display test results even if tests fail

./root/project-local/src/bash/unity-set-manifest.sh $1
if test $? -gt 0
then
    echo "Tests: Version $1 Unable to set correct manifest.json file for the Unity project - version mismatch?"
    exit 1
fi

ls -la /root/project-local/licenses
ls -la /root/project-local/licenses/*

xvfb-run \
/opt/unity/Editor/Unity \
-batchmode \
-nographics \
-logFile /dev/stdout \
-manualLicenseFile /root/licenses/$1/Unity_lic.ulf

licensingCode=$?

if test $licensingCode -gt 0
then
    echo "Tests: Licensing of Unity $1 failed with code $licensingCode"
    exit $licensingCode
fi

xvfb-run \
/opt/unity/Editor/Unity \
-batchmode \
-nographics \
-logFile /dev/stdout \
-projectPath /root/project-local/src/ApprienUnitySDK \
-runEditorTests \
-editorTestsResultFile /root/project-local/testResults.xml

unityTestsExitCode=$?

cat /root/project-local/testResults.xml

if test $unityTestsExitCode -gt 0
then
    echo "Tests: Unity tests of Unity $1 failed with code $licensingCode"
    exit $unityTestsExitCode
fi

exit $(grep failure /root/project-local/testResults.xml | wc -l | tr -d '[:space:]')
