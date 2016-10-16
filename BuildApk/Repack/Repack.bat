cd %~dp0\apktool2.1.1

rd /s /q temp
apktool d --no-src -f temp.apk -o temp


del temp\assets\bin\Data\Managed\System.Runtime.Serialization.dll
del temp\assets\bin\Data\Managed\System.Xml.dll
del temp\assets\bin\Data\Managed\Assembly-CSharp.dll

apktool b temp -o release.apk

