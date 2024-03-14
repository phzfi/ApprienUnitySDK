#!/bin/bash
set -ex

./root/project-local/src/bash/unity-set-manifest.sh $1


xvfb-run \
/opt/unity/Editor/Unity \
-batchmode \
-nographics \
-logFile /dev/stdout \
-manualLicenseFile /root/project-local/licenses/$1/Unity_lic.ulf


xvfb-run \
/opt/unity/Editor/Unity \
-batchmode \
-nographics \
-quit \
-logFile /dev/stdout \
-projectPath /root/project-local/src/ApprienUnitySDK \
-exportPackage \
Assets/Apprien/Editor \
Assets/Apprien/Scripts \
Assets/Apprien/ExampleStoreContent/Fonts \
Assets/Apprien/ExampleStoreContent/Resources \
Assets/Apprien/ExampleStoreContent/Sprites \
Assets/Apprien/ExampleStoreContent/Scripts \
Assets/Apprien/ExampleStoreContent/Scenes/$1 \
/root/project/UnityPackages/apprien-unity-$1.unityPackage
