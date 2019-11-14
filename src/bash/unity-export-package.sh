set -x
/opt/Unity/Editor/Unity \
-username "$LICENSE_EMAIL" \
-password "$LICENSE_PASSWORD" \
-batchmode \
-nographics \
-quit \
-logFile /dev/stdout \
-projectPath /root/project/src/ApprienUnitySDK \
-exportPackage \
Assets/Apprien/Editor \
Assets/Apprien/Scripts \
Assets/Apprien/Tests \
Assets/Apprien/ExampleStoreContent/Fonts \
Assets/Apprien/ExampleStoreContent/Resources \
Assets/Apprien/ExampleStoreContent/Sprites \
Assets/Apprien/ExampleStoreContent/Scenes/$1 \
/root/project/UnityPackages/apprien-unity-$1.unityPackage
