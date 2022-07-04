#!/bin/sh
set -x # No -ex - we wish to display test results even if tests fail

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
