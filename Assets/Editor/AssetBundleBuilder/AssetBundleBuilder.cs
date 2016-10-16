using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using UGFoundation.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetInfo
{
    public string Path;
    public HashSet<string> ChildDependences;
    public string AssetBundlePath;
}

public class AssetBundleBuilder
{
    public static BuildTarget AssetBundleBuildTarget = BuildTarget.StandaloneWindows;

    private static Dictionary<string, AssetInfo> GameAssets = new Dictionary<string, AssetInfo>();
    private static Dictionary<string, bool> AssetWithoutExtMap = new Dictionary<string, bool>();
    private static HashSet<string> HasBuildAssetBundleMap = new HashSet<string>();
    private static HashSet<string> HasBuildAssetBundleMapTemp = new HashSet<string>();

    private static string[] IgnoreFileExtensionList = new string[]
    {
        ".meta", ".cs", ".js", ".boo", ".dll"
    };

    //不打包下面路径开头的资源
    private static string[] IgnoreFileList = new string[]
    {
        "Assets/Resources/ReadonlyData"
    };

    public static string ExportDir = "/Export";

    public static void LoadAllAssets()
    {
        GameAssets.Clear();
        AssetWithoutExtMap.Clear();
        HasBuildAssetBundleMap.Clear();

        var filePaths = EditorFileUtility.FilterDirectoryIgnoreExt(Application.dataPath + "/Resources", IgnoreFileExtensionList);

        var rootPath = Path.GetDirectoryName(Application.dataPath);
        rootPath = rootPath.Replace("\\", "/") + "/";

        if (filePaths != null)
        {
            Queue<string> assetQueue = new Queue<string>();
            foreach (var filePath in filePaths)
            {
                var path = filePath.Replace("\\", "/");
                path = path.Replace(rootPath, "");

                bool canAdd = true;
                foreach (var ignoreFile in IgnoreFileList)
                {
                    if (path.StartsWith(ignoreFile))
                    {
                        canAdd = false;
                    }
                }

                if (canAdd)
                {
                    assetQueue.Enqueue(path);
                } 
            }

            while (assetQueue.Count > 0)
            {
                var path = assetQueue.Dequeue();
                if (GameAssets.ContainsKey(path))
                {
                    continue;
                }

                GetAssetDependences(assetQueue, path);
            }

            ReadBuildConfig();
            ClearExportDir();
            BuildAssetBundles();
            ExportAssetDependences();
            PackScripts();

            string assetName = "";
            try
            {
                foreach (var gameAsset in GameAssets)
                {
                    assetName = gameAsset.Key;
                    CheckAssetsDenpendences(assetName);
                }
            }
            catch (Exception e)
            {
                if (e.ToString().StartsWith("System.StackOverflowException"))
                {
                    Debug.LogError("检查资源依赖关系出现栈溢出问题，可能是资源存在环形依赖问题，资源名称是:"+assetName+",请修复问题并重新打包:" + e);
                }
                else
                {
                    Debug.LogError("检查资源依赖关系出现错误，请修复问题并重新打包:"+e);
                }
            }
        }

        var exportRootPath = Path.GetDirectoryName(Application.dataPath);
        EditorFileUtility.DeleteDirectory(exportRootPath + ExportDir+ "/Temp");
    }

    private static void GetAssetDependences(Queue<string> assetQueue, string path)
    {
        if (IsIgoreAsset(path) || GameAssets.ContainsKey(path))
        {
            return;
        }

        var assetInfo = new AssetInfo();
        assetInfo.Path = path;

        var dependencesList = new HashSet<string>();
        var dependences = AssetDatabase.GetDependencies(new string[] {path});
        foreach (var dependencesPath in dependences)
        {
            if (dependencesPath == path)
            {
                continue;
            }

            if (IsIgoreAsset(dependencesPath))
            {
                continue;
            }

            dependencesList.Add(dependencesPath);
            assetQueue.Enqueue(dependencesPath);
        }

        assetInfo.ChildDependences = dependencesList;
        GameAssets.Add(path, assetInfo);

        var pathWithoutExt = EditorFileUtility.GetPathWithoutExt(path);
        if (AssetWithoutExtMap.ContainsKey(pathWithoutExt))
        {
            Debug.LogError("同一个目录下不应该有相同文件名，不同扩展名的资源命名存在:"+ path);
        }
        else
        {
            AssetWithoutExtMap.Add(pathWithoutExt,true);
        }

    }

    //读取xml打包配置
    private static List<string> BuildAllFilePaths = new List<string>();
    private static List<string> BuildSingleFilePaths = new List<string>();
  
    private static void ReadBuildConfig()
    {
        var path = Application.dataPath + "/Editor/AssetBundleBuilder/Config/AssetBundleBuildConfig.xml";
        
        BuildAllFilePaths.Clear();
        BuildSingleFilePaths.Clear();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(path);

        var rootChildNodes = xmlDoc.SelectSingleNode("root").ChildNodes;
        foreach (var childNode in rootChildNodes)
        {
            var childElement = childNode as XmlElement;
            if (childElement != null)
            {
                if (childElement.Name == "IncludeAllFilePath")
                {
                    var includeAllFileChildNodes = childElement.ChildNodes;
                    foreach (var includeAllFileChildNode in includeAllFileChildNodes)
                    {
                        var includeAllFileChildNodeElement = includeAllFileChildNode as XmlElement;
                        BuildAllFilePaths.Add(includeAllFileChildNodeElement.InnerText);
                    }
                }
                else if (childElement.Name == "SingleFilePath")
                {
                    var singleFileChildNodes = childElement.ChildNodes;
                    foreach (var singleFileChildNode in singleFileChildNodes)
                    {
                        var singleFileChildNodeElement = singleFileChildNode as XmlElement;
                        BuildSingleFilePaths.Add(singleFileChildNodeElement.InnerText);
                    }
                }
            }
        }
    }   
    
    //清理输出环境
    private static void ClearExportDir()
    {
        var path = Path.GetDirectoryName(Application.dataPath);
        var exportPath = path + ExportDir;
        EditorFileUtility.ClearDirectory(exportPath);
        EditorFileUtility.ClearDirectory(exportPath + "/temp");
    }

    //创建AssetBundle
    private static void BuildAssetBundles()
    {
        var path = Path.GetDirectoryName(Application.dataPath);
        var exportPath = path + ExportDir;
        foreach (var asset in GameAssets)
        {
            if (BuildAllFileToAssetBundle(exportPath, asset.Value))
            {
                continue;
            }

            if (BuildSingleFileToAssetBundle(exportPath, asset.Value))
            {
                continue;
            }

            BuildCurrentDirectoryToAssetBundle(exportPath, asset.Value);
        }
    }


    private static void BuildCurrentDirectoryToAssetBundle(string exportPath, AssetInfo asset)
    {
        //判断是不是已经生成正常的AssetBundle
        var tempPath = EditorFileUtility.GetPath(asset.Path);
        var assetBundleName = Path.GetFileName(Path.GetDirectoryName(asset.Path));
        var assetBundlePath = exportPath + "/" + tempPath + "/" + assetBundleName + ".pkg";
        if (HasBuildAssetBundleMap.Contains(assetBundlePath))
        {
            return;
        }
        EditorFileUtility.CreateParentDirecotry(assetBundlePath);

        //收集和自己同一个目录下的资源(不包括子目录)
        var currentDir = Path.GetDirectoryName(asset.Path);
        List<AssetInfo> readyToBuildAssets = new List<AssetInfo>();
        foreach (var gameAsset in GameAssets)
        {
            if (Path.GetDirectoryName(gameAsset.Value.Path) == currentDir)
            {
                readyToBuildAssets.Add(gameAsset.Value);
            }
        }

        //收集asset的所有依赖，如果依赖是同一个assetBundle下的跳过
        List<string> allChildDependences = new List<string>();
        foreach (var gameAsset in readyToBuildAssets)
        {
            foreach (var dependence in gameAsset.ChildDependences)
            {
                if (Path.GetDirectoryName(dependence) != currentDir)
                {
                    allChildDependences.Add(dependence);
                }
            }
        }

        //删除临时目录的AssetBundle
        EditorFileUtility.DeleteDirectory(exportPath + "/temp");
        HasBuildAssetBundleMapTemp.Clear();

        BuildPipeline.PushAssetDependencies();

        foreach (var dependence in allChildDependences)
        {
            if (BuildAllFileToAssetBundleTemp(allChildDependences, exportPath, dependence))
            {
                continue;
            }

            if (BuildSingleFileToAssetBundleTemp(exportPath, dependence))
            {
                continue;
            }

            BuildCurrentDirectoryToAssetBundleTemp(allChildDependences,exportPath, dependence);
        }

        List<Object> objects = new List<Object>();
        List<string> assetNames = new List<string>();
        foreach (var buildAsset in readyToBuildAssets)
        {
            objects.Add(AssetDatabase.LoadMainAssetAtPath(buildAsset.Path));
            assetNames.Add(buildAsset.Path);
        }
 
        BuildPipeline.PushAssetDependencies();
        BuildAssetBundle(objects.ToArray(), assetNames.ToArray(), assetBundlePath);
        BuildPipeline.PopAssetDependencies();

        BuildPipeline.PopAssetDependencies();

        //配置asset的Assetbundle路径
        foreach (var buildAsset in readyToBuildAssets)
        {
            buildAsset.AssetBundlePath = tempPath + "/" + assetBundleName + ".pkg";
        }
        HasBuildAssetBundleMap.Add(assetBundlePath);
    }

    private static bool BuildSingleFileToAssetBundle(string exportPath, AssetInfo asset)
    {
        bool buildSingleFile = false;
        foreach (var path in BuildSingleFilePaths)
        {
            if (asset.Path.StartsWith(path))
            {
                buildSingleFile = true;
                break;
            }
        }

        if (buildSingleFile)
        {
            //判断是不是已经生成正常的AssetBundle
            var tempPath = EditorFileUtility.GetPathWithoutExt(asset.Path);
            var assetbundlePath = exportPath + "/" + tempPath + ".pkg";
            if (HasBuildAssetBundleMap.Contains(assetbundlePath))
            {
                return false;
            }
            EditorFileUtility.CreateParentDirecotry(assetbundlePath);

            //收集asset的所有依赖
            List<string> allChildDependences = new List<string>();
            foreach (var dependence in asset.ChildDependences)
            {
                if(asset.Path == dependence)
                    continue;
                allChildDependences.Add(dependence);
            }

            //删除临时目录的AssetBundle
            EditorFileUtility.DeleteDirectory(exportPath + "/temp");
            HasBuildAssetBundleMapTemp.Clear();

            BuildPipeline.PushAssetDependencies();

            foreach (var dependence in allChildDependences)
            {
                if (BuildAllFileToAssetBundleTemp(allChildDependences, exportPath, dependence))
                {
                    continue;
                }

                if (BuildSingleFileToAssetBundleTemp(exportPath, dependence))
                {
                    continue;
                }

                BuildCurrentDirectoryToAssetBundleTemp(allChildDependences,exportPath, dependence);
            }

            List<Object> objects = new List<Object>();
            List<string> assetNames = new List<string>();

            objects.Add(AssetDatabase.LoadMainAssetAtPath(asset.Path));
            assetNames.Add(asset.Path);

            BuildPipeline.PushAssetDependencies();
            BuildAssetBundle(objects.ToArray(), assetNames.ToArray(), assetbundlePath);
            BuildPipeline.PopAssetDependencies();

            BuildPipeline.PopAssetDependencies();

            //配置asset的Assetbundle路径
            asset.AssetBundlePath = tempPath + ".pkg";
            HasBuildAssetBundleMap.Add(assetbundlePath);
            return true;
        }

        return false;
    }

    private static bool BuildAllFileToAssetBundle(string exportPath,AssetInfo asset)
    {
        bool isIncludeAllFile = false;
        string includeFilePath = "";
        foreach (var path in BuildAllFilePaths)
        {
            if (asset.Path.StartsWith(path))
            {
                isIncludeAllFile = true;
                includeFilePath = path;
                break;
            }
        }

        if (isIncludeAllFile)
        {
            //判断是不是已经生成正常的AssetBundle
            var tempPath = includeFilePath.Substring(0, includeFilePath.Length - 1);
            tempPath += "/"+Path.GetFileNameWithoutExtension(tempPath);
            var assetbundlePath = exportPath + "/" + tempPath + ".pkg";
            if (HasBuildAssetBundleMap.Contains(assetbundlePath))
            {
                return false;
            }
            EditorFileUtility.CreateParentDirecotry(assetbundlePath);

            //收集所有同一个目录下(包括子目录)的asset
            List<AssetInfo> readyToBuildAssets = new List<AssetInfo>();
            foreach (var gameAsset in GameAssets)
            {
                if (gameAsset.Value.Path.StartsWith(includeFilePath))
                {
                    readyToBuildAssets.Add(gameAsset.Value);
                }
            }

            //收集asset的所有依赖，如果依赖是同一个assetBundle下的跳过
            List<string> allChildDependences = new List<string>();
            foreach (var gameAsset in readyToBuildAssets)
            {
                foreach (var dependence in gameAsset.ChildDependences)
                {
                    if (dependence.StartsWith(includeFilePath))
                    {
                        continue;
                    }
                    allChildDependences.Add(dependence);
                }               
            }

            //删除临时目录的AssetBundle
            EditorFileUtility.DeleteDirectory(exportPath + "/temp");
            HasBuildAssetBundleMapTemp.Clear();

            BuildPipeline.PushAssetDependencies();

            foreach (var dependence in allChildDependences)
            {
                if(BuildAllFileToAssetBundleTemp(allChildDependences, exportPath, dependence))
                {
                    continue;
                }

                if (BuildSingleFileToAssetBundleTemp(exportPath, dependence))
                {
                    continue;
                }

                BuildCurrentDirectoryToAssetBundleTemp(allChildDependences,exportPath, dependence);
            }

            List<Object> objects = new List<Object>();
            List<string> assetNames = new List<string>();

            foreach (var buildAsset in readyToBuildAssets)
            {
                objects.Add(AssetDatabase.LoadMainAssetAtPath(buildAsset.Path));
                assetNames.Add(buildAsset.Path);             
            }

            BuildPipeline.PushAssetDependencies();
            BuildAssetBundle(objects.ToArray(), assetNames.ToArray(), assetbundlePath);
            BuildPipeline.PopAssetDependencies();

            BuildPipeline.PopAssetDependencies();

            //配置asset的Assetbundle路径
            foreach (var buildAsset in readyToBuildAssets)
            {
                buildAsset.AssetBundlePath = tempPath + ".pkg";
            }

            HasBuildAssetBundleMap.Add(assetbundlePath);
            return true;
        }
        return false;
    }

    private static bool BuildAllFileToAssetBundleTemp(List<string> allChildDependences,string exportPath, string assetPath)
    {
        bool isIncludeAllFile = false;
        string includeFilePath = "";
        foreach (var path in BuildAllFilePaths)
        {
            if (assetPath.StartsWith(path))
            {
                isIncludeAllFile = true;
                includeFilePath = path;
                break;
            }
        }

        if (isIncludeAllFile)
        {
            var tempPath = includeFilePath.Substring(0, includeFilePath.Length - 1);
            tempPath += "/" + Path.GetFileNameWithoutExtension(tempPath);
            var assetbundlePath = exportPath + "/temp/" + tempPath + ".pkg";
            if (HasBuildAssetBundleMapTemp.Contains(assetbundlePath))
            {
                return true;
            }
            EditorFileUtility.CreateParentDirecotry(assetbundlePath);

            //收集所有同一个目录下(包括子目录)的asset
            List<string> allPath = new List<string>();
            foreach (var dependencePath in allChildDependences)
            {
                if (dependencePath.StartsWith(includeFilePath))
                {
                    allPath.Add(dependencePath);
                }
            }

            List<Object> objects = new List<Object>();
            List<string> assetNames = new List<string>();
            
            foreach (var p in allPath)
            {
                objects.Add(AssetDatabase.LoadMainAssetAtPath(p));
                assetNames.Add(p);
            }

            BuildAssetBundleWithoutCollectDependencies(objects.ToArray(), assetNames.ToArray(), assetbundlePath);
            HasBuildAssetBundleMapTemp.Add(assetbundlePath);
            return true;
        }
        return false;
    }

    private static bool BuildSingleFileToAssetBundleTemp(string exportPath, string assetPath)
    {
        bool buildSingleFileAb = false;
        foreach (var path in BuildSingleFilePaths)
        {
            if (assetPath.StartsWith(path))
            {
                buildSingleFileAb = true;
                break;
            }
        }

        if (buildSingleFileAb)
        {
            var tempPath = EditorFileUtility.GetPathWithoutExt(assetPath);
            var assetbundlePath = exportPath + "/temp/" + tempPath + ".pkg";
            if (HasBuildAssetBundleMapTemp.Contains(assetbundlePath))
            {
                return true;
            }
            EditorFileUtility.CreateParentDirecotry(assetbundlePath);

            List<Object> objects = new List<Object>();
            List<string> assetNames = new List<string>();
            objects.Add(AssetDatabase.LoadMainAssetAtPath(assetPath));
            assetNames.Add(assetPath);

            BuildAssetBundleWithoutCollectDependencies(objects.ToArray(), assetNames.ToArray(), assetbundlePath);
            HasBuildAssetBundleMapTemp.Add(assetbundlePath);
            return true;
        }

        return false;
    }

    private static bool BuildCurrentDirectoryToAssetBundleTemp(List<string> allChildDependences, string exportPath, string assetPath)
    {
        var tempPath = EditorFileUtility.GetPath(assetPath);
        var assetBundleName = Path.GetFileName(Path.GetDirectoryName(assetPath));
    
        var assetBundlePath = exportPath + "/temp/" + tempPath + "/" + assetBundleName + ".pkg";
        if (HasBuildAssetBundleMapTemp.Contains(assetBundlePath))
        {
            return true;
        }
        EditorFileUtility.CreateParentDirecotry(assetBundlePath);

        //收集和自己同一个目录下的资源(不包括子目录)
        var currentDir = Path.GetDirectoryName(assetPath);
        List<string> readyToBuildAssets = new List<string>();
        foreach (var dependence in allChildDependences)
        {
            if (Path.GetDirectoryName(dependence) == currentDir)
            {
                readyToBuildAssets.Add(dependence);
            }
        }

        List<Object> objects = new List<Object>();
        List<string> assetNames = new List<string>();
        foreach (var asset in readyToBuildAssets)
        {
            objects.Add(AssetDatabase.LoadMainAssetAtPath(asset));
            assetNames.Add(asset);
        }

        BuildAssetBundleWithoutCollectDependencies(objects.ToArray(),assetNames.ToArray(), assetBundlePath);
        HasBuildAssetBundleMapTemp.Add(assetBundlePath);
        return true;
    }

    private static void ExportAssetDependences()
    {
        List<string>  stringBuff = new List<string>();
        Dictionary<string,int> stringBuffIndex = new Dictionary<string, int>();

        foreach (var gameAsset in GameAssets)
        {
            var path = EditorFileUtility.GetPathWithoutExt(gameAsset.Value.Path);
            if (!stringBuffIndex.ContainsKey(path))
            {
                stringBuff.Add(path);
                stringBuffIndex.Add(path, stringBuff.Count - 1);
            }

            if (!stringBuffIndex.ContainsKey(gameAsset.Value.AssetBundlePath))
            {
                stringBuff.Add(gameAsset.Value.AssetBundlePath);
                stringBuffIndex.Add(gameAsset.Value.AssetBundlePath, stringBuff.Count - 1);
            }

            foreach (var dependence in gameAsset.Value.ChildDependences)
            {
                path = EditorFileUtility.GetPathWithoutExt(dependence);
                if (!stringBuffIndex.ContainsKey(path))
                {
                    stringBuff.Add(path);
                    stringBuffIndex.Add(path, stringBuff.Count - 1);
                }
            }
        }

        FileStream fs = File.Create(Application.dataPath + "/Resources/AssetsDependences.xml");

        //资源的数量
        var count = GameAssets.Count;
        var bytes = BitConverter.GetBytes(count);
        BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
        fs.Write(bytes, 0, bytes.Length);

        //字符串的数量
        count = stringBuff.Count;
        bytes = BitConverter.GetBytes(count);
        BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
        fs.Write(bytes, 0, bytes.Length);

        //写入字符串资源
        var buffsize = GetStringBufferSize(stringBuff);
        bytes = BitConverter.GetBytes(buffsize);
        BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
        fs.Write(bytes, 0, bytes.Length);

        foreach (var str in stringBuff)
        {
            bytes = Encoding.UTF8.GetBytes(str);
            fs.Write(bytes, 0, bytes.Length);
            fs.WriteByte(0);
        }

        //写入每一个资源的条目
        foreach (var gameAsset in GameAssets)
        {
            var index = stringBuffIndex[EditorFileUtility.GetPathWithoutExt(gameAsset.Value.Path)];
            bytes = BitConverter.GetBytes(index);
            BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
            fs.Write(bytes, 0, bytes.Length);

            index = stringBuffIndex[gameAsset.Value.AssetBundlePath];
            bytes = BitConverter.GetBytes(index);
            BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
            fs.Write(bytes, 0, bytes.Length);

            count = gameAsset.Value.ChildDependences.Count;
            bytes = BitConverter.GetBytes(count);
            BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
            fs.Write(bytes, 0, bytes.Length);

            foreach (var d in gameAsset.Value.ChildDependences)
            {
                index = stringBuffIndex[EditorFileUtility.GetPathWithoutExt(d)];
                bytes = BitConverter.GetBytes(index);
                BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        fs.Flush();
        fs.Close();

        AssetDatabase.Refresh();
        Object[] objs = new Object[1];
        objs[0] = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/AssetsDependences.xml");
        string[] assetsName = new string[] { "Assets/Resources/AssetsDependences" };
        BuildAssetBundle(objs, assetsName, Path.GetDirectoryName(Application.dataPath) + ExportDir +"/AssetsDependences.pkg");

        File.Delete(Application.dataPath + "/Resources/AssetsDependences.xml");
        File.Delete(Application.dataPath + "/Resources/AssetsDependences.xml.meta");
        AssetDatabase.Refresh();
    }

    private static int GetStringBufferSize(List<string> stringBuffList)
    {
        int bytes = 0;
        foreach (var priority in stringBuffList)
        {
            bytes += Encoding.UTF8.GetBytes(priority).Length;
        }
        bytes += stringBuffList.Count;
        return bytes;
    }

    private static void BuildAssetBundle(Object[] objs,string[] assetsName,string abPath)
    {
        for (int i = 0; i < assetsName.Length; i++)
        {
            assetsName[i] = EditorFileUtility.GetPathWithoutExt(assetsName[i]);
        }

        BuildPipeline.BuildAssetBundleExplicitAssetNames(objs, assetsName,
            abPath,
            BuildAssetBundleOptions.CollectDependencies |
            BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle,
            AssetBundleBuildTarget);
    }

    private static void BuildAssetBundleWithoutCollectDependencies(Object[] objs, string[] assetsName, string abPath)
    {
        for (int i = 0; i < assetsName.Length; i++)
        {
            assetsName[i] = EditorFileUtility.GetPathWithoutExt(assetsName[i]);
        }

        BuildPipeline.BuildAssetBundleExplicitAssetNames(objs, assetsName,
            abPath,
            BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle,
            AssetBundleBuildTarget);
    }

    private static bool IsIgoreAsset(string assetPath)
    {
        foreach (var ext in IgnoreFileExtensionList)
        {
            if (Path.GetExtension(assetPath).ToLower() == ext)
            {
                return true;
            }
        }
        return false;
    }

    private static void CheckAssetsDenpendences(string assetName)
    {
        var childDependencesList = GameAssets[assetName].ChildDependences;
        foreach (var cp in childDependencesList)
        {
            CheckAssetsDenpendences(cp);
        }
    }

    private static void PackScripts()
    {
        var scriptsPath = Directory.GetParent(Application.dataPath) + "/obj/Release/";
        FileSystemUtility.ClearDirectory(scriptsPath);

        var path = Application.dataPath + "/Editor/AssetBundleBuilder/Config/BuildScripts.bat";
        var process = System.Diagnostics.Process.Start(path);
        process.WaitForExit();

        var dllPath = scriptsPath + "Assembly-CSharp.dll";
        File.Move(dllPath, Application.dataPath + "/Resources/Assembly-CSharp.bytes");

        AssetDatabase.Refresh();
        Object[] objs = new Object[1];
        objs[0] = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/Assembly-CSharp.bytes");
        string[] assetsName = new string[] { "Assets/Resources/RuntimeScripts" };
        BuildAssetBundle(objs, assetsName, Path.GetDirectoryName(Application.dataPath) + ExportDir + "/RuntimeScripts.pkg");

        File.Delete(Application.dataPath + "/Resources/Assembly-CSharp.bytes");
        File.Delete(Application.dataPath + "/Resources/Assembly-CSharp.bytes.meta");
        AssetDatabase.Refresh();

        //打apk
       /* var directoryInfo = new DirectoryInfo(Application.dataPath).Parent;
        String bundleVersion = PlayerSettings.bundleVersion;
        String unionName = "union";
        String unionApkName = "zsby_" + unionName + "_" + bundleVersion + "_" + ".apk";
        var targetApkName = directoryInfo + "/apk/" + unionApkName;

        if (!Directory.Exists(Path.GetDirectoryName(targetApkName)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetApkName));
        }

        string[] levels = { "Assets/LaunchScene/LaunchScene.unity" };
        BuildPipeline.BuildPlayer(levels, targetApkName, BuildTarget.Android, BuildOptions.None);*/
    }

    [MenuItem("Tools/Asset Tool/GetAssetPath")]
    public static void GetAssetPath()
    {
        if (Selection.activeObject != null)
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            Debug.LogError(path);
        }
    }

    [MenuItem("Tools/Asset Tool/GetAssetDependences")]
    public static void GetAssetDependencesPath()
    {
        if (Selection.activeObject != null)
        {
            var path = AssetDatabase.GetDependencies(new string[] { AssetDatabase.GetAssetPath(Selection.activeObject)});
            foreach (var p in path)
            {
                Debug.LogError(p);
            }
        }
    }

    public static Dictionary<string, string> GetFilesMD5(string path, string ext)
    {
        Dictionary<string,string> fileMd5Map = new Dictionary<string, string>();
        var files = EditorFileUtility.FilterDirectory(path, new string[] {ext});

        if (files != null)
        {
            foreach (var file in files)
            {
                var md5 = EditorFileUtility.GetMD5HashFromFile(file.FullName);
                fileMd5Map.Add(file.FullName, md5);
            }
        }

        return fileMd5Map;
    }

    public static void PackPatches(string dirPath, string patchName, string savePath, string ext)
    {
        var prefix = Path.GetFullPath(dirPath);

        var zipfiles = EditorFileUtility.FilterDirectory(savePath, new string[] { ".patch" }, false);
        if (zipfiles != null)
        {
            foreach (var zip in zipfiles)
            {
                File.Delete(zip.FullName);
            }
        }

        var files = EditorFileUtility.FilterDirectory(dirPath, new string[] { ext });
        if (files != null)
        {
            int patchCount = 1;
            HashSet<string> patchList = new HashSet<string>();

            foreach (var file in files)
            {
                if (!patchList.Contains(patchName + "p" + patchCount))
                {
                    patchList.Add(patchName + "p" + patchCount);
                    using (ZipFile zip = ZipFile.Create(savePath + "/" + patchName + "p" + patchCount + ".patch"))
                    {
                        zip.BeginUpdate();
                        zip.Add(file.FullName, file.FullName.Replace(prefix, ""));
                        zip.CommitUpdate();
                    }
                }
                else
                {
                    using (ZipFile zip = new ZipFile(savePath + "/" + patchName + "p" + patchCount + ".patch"))
                    {
                        zip.BeginUpdate();
                        zip.Add(file.FullName, file.FullName.Replace(prefix, ""));
                        zip.CommitUpdate();
                    }
                }

                FileInfo fi = new FileInfo(savePath + "/" + patchName + "p" + patchCount + ".patch");
                if (fi.Length / 1024 > 1024 * 5)
                {
                    ++patchCount;
                }
            }

            if (patchCount == 1)
            {
                FileInfo fi = new FileInfo(savePath + "/" + patchName + "p" + patchCount + ".patch");
                fi.MoveTo(savePath + "/" + patchName + ".patch");
            }
        }

    }

    [MenuItem("Tools/Asset Tool/Check Resource Reference")]
    public static void CheckResourceReference()
    {
        var filePaths = EditorFileUtility.FilterDirectoryIgnoreExt(Application.dataPath + "/Resources",
            IgnoreFileExtensionList);

        var rootPath = Path.GetDirectoryName(Application.dataPath);
        rootPath = rootPath.Replace("\\", "/") + "/";

        if (filePaths != null)
        {
            Queue<string> assetQueue = new Queue<string>();
            foreach (var filePath in filePaths)
            {
                var path = filePath.Replace("\\", "/");
                path = path.Replace(rootPath, "");
                assetQueue.Enqueue(path);
            }

            while (assetQueue.Count > 0)
            {
                var path = assetQueue.Dequeue();
                if (GameAssets.ContainsKey(path))
                {
                    continue;
                }

                GetAssetDependences(assetQueue, path);
            }

            Dictionary<string, List<string>> sameAssets = new Dictionary<string, List<string>>();
            foreach (var gameAsset in GameAssets)
            {
                var md5 =
                    EditorFileUtility.GetMD5HashFromFile(Path.GetDirectoryName(Application.dataPath) + "/" +
                                                         gameAsset.Value.Path);
                if (!sameAssets.ContainsKey(md5))
                {
                    sameAssets.Add(md5, new List<string>());
                    sameAssets[md5].Add(gameAsset.Value.Path);
                }
                else
                {
                    sameAssets[md5].Add(gameAsset.Value.Path);
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach (var asset in sameAssets)
            {
                if (asset.Value.Count > 1)
                {
                    sb.AppendLine("MD5: " + asset.Key);
                    foreach (var p in asset.Value)
                    {
                        sb.AppendLine("\t资源路径:" + p);
                        foreach (var gameAsset in GameAssets)
                        {
                            foreach (var cd in gameAsset.Value.ChildDependences)
                            {
                                if (cd == p)
                                {
                                    sb.AppendLine("\t\t引用到它的资源:" + gameAsset.Key);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            sb.AppendLine();

            sb.AppendLine("Resources目录下未被直接引用的资源(可能是代码中引用)");
            foreach (var gameAsset in GameAssets)
            {
                var hasRefer = false;
                foreach (var gameAsset1 in GameAssets)
                {
                    var isRefer = false;
                    foreach (var cd in gameAsset1.Value.ChildDependences)
                    {
                        if (cd == gameAsset.Value.Path)
                        {
                            isRefer = true;
                            break;
                        }
                    }
                    if (isRefer)
                    {
                        hasRefer = true;
                        break;

                    }
                }

                if (!hasRefer)
                    sb.AppendLine("\t资源路径:" + gameAsset.Value.Path);
            }

            File.WriteAllText(Path.GetDirectoryName(Application.dataPath) + "/CheckResourceReference.txt", sb.ToString());
            WindowsOSUtility.ExploreFile(Path.GetDirectoryName(Application.dataPath));
        }
    }

    public static void CheckAssetBundleSize(string path, string ext)
    {
        var files = EditorFileUtility.FilterDirectory(path, new string[] { ext });
        if (files != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("大于1m的AssetBundle包(建议AssetBundle的包小于1m,最好不要超过2m)");
            foreach (var file in files)
            {
                FileInfo fi = new FileInfo(file.FullName);
                if (fi.Length / 1024 > 1024)
                {
                    sb.AppendLine("\tAssetBundle文件路径:" + fi.FullName);
                    sb.AppendLine("\t\t文件大小:" + (fi.Length / 1024f / 1024f) + "m");
                }
            }

            File.WriteAllText(path + "/AssetBundleSize.txt", sb.ToString());
            WindowsOSUtility.ExploreFile(path);
        }
    }
}
