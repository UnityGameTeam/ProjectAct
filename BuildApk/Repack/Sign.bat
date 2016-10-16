cd %~dp0\apktool2.1.1

jarsigner -digestalg SHA1 -sigalg MD5withRSA -verbose -keystore uazdl.keystore -storepass a123456b -signedjar final.apk release.apk uazdl