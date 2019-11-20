set -x
/opt/Unity/Editor/Unity \
-username "$LICENSE_EMAIL" \
-password "$LICENSE_PASSWORD" \
-batchmode \
-nographics \
-logFile /dev/stdout \
-projectPath /root/project-local/src/ApprienUnitySDK \
-runEditorTests \
-editorTestsResultFile /root/project-local/testResults.xml

cat /root/project-local/testResults.xml