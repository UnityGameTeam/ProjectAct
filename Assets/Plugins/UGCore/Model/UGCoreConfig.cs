using UnityEngine;
using System.IO;

namespace UGCore
{
    public static class UGCoreConfig
    {
        public static readonly string AssetBundleFileHead = "file://";
        public static readonly string MiddleFilePathName  = "RuntimeResources";
        public static readonly string ResourcesFolderName = "Resources";
        public static readonly string ConfigFolderName    = "Config";
        public static readonly string DownloadFolderName  = "Download";
        public static readonly string LogFolderName       = "Log";
        public static readonly string ProgramVersion      = "ProgramVersion";
        public static readonly string ResourceVersion     = "ResourceVersion";

        static UGCoreConfig()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                AssetBundleFileHead = "file:///";
            }
         }

        public static string GetAssetBundlePath(string assetBundlePath)
        {
            var externalAbPath = string.Format("{0}/{1}/{2}/{3}",Application.persistentDataPath, MiddleFilePathName, ResourcesFolderName, assetBundlePath);
            if (File.Exists(externalAbPath))
            {
                return string.Format("{0}{1}", AssetBundleFileHead, externalAbPath); ;
            }
            var filePath = string.Format("{0}/{1}/{2}", Application.streamingAssetsPath, MiddleFilePathName, assetBundlePath);
            if (filePath.Contains("://"))
            {
                return filePath;
            }
            return string.Format("{0}{1}/{2}/{3}", AssetBundleFileHead,Application.streamingAssetsPath, MiddleFilePathName, assetBundlePath);
        }

        public static string GetExternalResourceFolder()
        {
            return string.Format("{0}/{1}/{2}", Application.persistentDataPath, MiddleFilePathName, ResourcesFolderName);
        }

        public static string GetExternalConfigFolder()
        {
            return string.Format("{0}/{1}/{2}", Application.persistentDataPath, MiddleFilePathName, ConfigFolderName);
        }

        public static string GetExternalDownloadFolder()
        {
            return string.Format("{0}/{1}/{2}", Application.persistentDataPath, MiddleFilePathName, DownloadFolderName);
        }

        public static string GetExternalLogFolder()
        {
            return string.Format("{0}/{1}/{2}", Application.persistentDataPath, MiddleFilePathName, LogFolderName);
        }
    }
}