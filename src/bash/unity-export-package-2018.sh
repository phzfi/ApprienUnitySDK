set -x
/opt/Unity/Editor/Unity \
-username "$LICENSE_EMAIL" \
-password "$LICENSE_PASSWORD" \
-batchmode \
-nographics \
-quit \
-logFile /dev/stdout \
# TODO: support 2018 and later unity project versions that use UnityEngine.Purchasing iap from package manager
-projectPath /root/project-local/src/ApprienUnitySDK-5-6-3f1 \ 
-exportPackage \
Assets/Apprien/Editor \
Assets/Apprien/Scripts \
Assets/Apprien/ExampleStoreContent/Fonts \
Assets/Apprien/ExampleStoreContent/Resources \
Assets/Apprien/ExampleStoreContent/Sprites \
Assets/Apprien/ExampleStoreContent/Scripts \
Assets/Apprien/ExampleStoreContent/Scenes/$1 \
/root/project/UnityPackages/apprien-unity-$1.unityPackage
