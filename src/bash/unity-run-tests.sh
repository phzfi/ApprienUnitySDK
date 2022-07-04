#!/bin/sh
set -x # No -ex - we wish to display test results even if tests fail

./root/project-local/src/bash/unity-set-manifest.sh $1
if test $? -gt 0
then
    echo "Unable to set correct manifest.json file for the Unity project - version mismatch?"
    exit 1
fi

xvfb-run \
/opt/unity/Editor/Unity \
-username "$LICENSE_EMAIL" \
-password "$LICENSE_PASSWORD" \
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
    exit $unityTestsExitCode
fi

exit $(grep failure /root/project-local/testResults.xml | wc -l | tr -d '[:space:]')
