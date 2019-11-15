set -x
/opt/Unity/Editor/Unity \
-username "$LICENSE_EMAIL" \
-password "$LICENSE_PASSWORD" \
-batchmode \
-nographics \
-quit \
-logFile /dev/stdout \
-projectPath /root/project-local/src/ApprienUnitySDK \
-runTests -testPlatform playmode \
-testResults /root/project-local/testResults.xml

cat /root/project-local/testResults.xml