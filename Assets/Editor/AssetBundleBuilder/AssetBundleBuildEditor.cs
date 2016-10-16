using System.Collections.Generic;
using System.IO;
using UGCore;
using UnityEditor;
using UnityEngine;

public class AssetBundleBuildEditor : EditorWindow
{
    public enum BuildPlatform
    {
        StandaloneWindows,
        Android,
    }

    public enum BuildOperation
    {
        Version,
        Patch,
    }

    private BuildPlatform m_BuildPlatform = BuildPlatform.StandaloneWindows;
    private BuildPlatform m_OldBuildPlatform = BuildPlatform.StandaloneWindows;
    private string m_ExportAssetBundleDir = "./Export/";

    private bool versionPart;
    private bool patchPart;

    private BuildOperation m_BuildOperation = BuildOperation.Version;
    private BuildOperation m_OldBuildOperation = BuildOperation.Version;
    private string m_CreateVersion = "1.0.0.0";

    private int m_lowVersion = 0;
    private int m_highVersion = 0;

    private Dictionary<string,int> m_VersionMap = new Dictionary<string, int>();
    private Dictionary<string, int> m_PatchMap = new Dictionary<string, int>();

    [MenuItem("Tools/AssetBundle Build Window")]
    static void OpenAssetBundleBuildEditor()
    {
        AssetBundleBuildEditor window = (AssetBundleBuildEditor)GetWindow(typeof(AssetBundleBuildEditor));
        window.minSize = new Vector2(300, 300);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        m_ExportAssetBundleDir = EditorGUILayout.TextField("AssetBundle导出目录:", m_ExportAssetBundleDir);
        CreateOrExploreButton(m_ExportAssetBundleDir);
        EditorGUILayout.EndHorizontal();

        GUIContent[] guiObjs =
        {
            new GUIContent("PC"),
            new GUIContent("Android"),
        };

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        m_BuildPlatform = (BuildPlatform)GUILayout.Toolbar((int)m_BuildPlatform, guiObjs);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (m_BuildPlatform == BuildPlatform.StandaloneWindows)
        {
            ShowBuildUI("PC");
        }
        else if (m_BuildPlatform == BuildPlatform.Android)
        {
            ShowBuildUI("Android");
        }

        if (m_BuildPlatform != m_OldBuildPlatform)
        {
            m_VersionMap.Clear();
            m_PatchMap.Clear();
        }
        m_OldBuildPlatform = m_BuildPlatform;
    }

    private void ShowBuildUI(string platform)
    {
        versionPart = EditorGUILayout.Foldout(versionPart, "已打版本");
        if (versionPart)
        {
            var versionList = GetVersions(m_ExportAssetBundleDir + platform+"/Version");
            if (versionList != null && versionList.Count > 0)
            {
                GUILayoutOption[] options =
                {
                        GUILayout.Width(150),
                        GUILayout.Height(20),
                    };

                EditorGUILayout.BeginVertical();
                EditorGUILayout.Space();

                foreach (var v in versionList)
                {
                    if (!m_VersionMap.ContainsKey(v.ToString()))
                    {
                        m_VersionMap.Add(v.ToString(), -1);
                    }

                    var selectIndex = m_VersionMap[v.ToString()];
                    selectIndex = GUILayout.SelectionGrid(selectIndex, new string[] { v.ToString() }, 1);
                    m_VersionMap[v.ToString()] = selectIndex;

                    if (selectIndex == 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space();

                        if (GUILayout.Button("浏览", options))
                        {
                            WindowsOSUtility.ExploreFile(m_ExportAssetBundleDir + platform+"/Version/" + v.ToString());
                        }

                        if (GUILayout.Button("检查ab包大小", options))
                        {
                            AssetBundleBuilder.CheckAssetBundleSize(m_ExportAssetBundleDir + platform + "/Version/" + v, ".pkg");
                        }

                        if (GUILayout.Button("取消查看", options))
                        {
                            m_VersionMap[v.ToString()] = -1;
                            Repaint();
                        }

                        if (GUILayout.Button("拷贝到StreamingAssets", options))
                        {
                            var targetPath = Application.dataPath + "/StreamingAssets/"+UGCoreConfig.MiddleFilePathName;
                            EditorFileUtility.ClearDirectory(targetPath);
                            EditorFileUtility.CopyDirectory(m_ExportAssetBundleDir + platform+"/Version/" + v.ToString(), targetPath);
                            AssetDatabase.Refresh();
                            WindowsOSUtility.ExploreFile(targetPath);
                        }

                        if (GUILayout.Button("拷贝到persistentDataPath", options))
                        {
                            var targetPath = Application.persistentDataPath + "/" + UGCoreConfig.MiddleFilePathName + "/" + UGCoreConfig.ResourcesFolderName;
                            EditorFileUtility.ClearDirectory(targetPath);
                            EditorFileUtility.CopyDirectory(m_ExportAssetBundleDir + platform+"/Version/" + v.ToString(), targetPath);
                            AssetDatabase.Refresh();
                            WindowsOSUtility.ExploreFile(targetPath);
                        }

                        if (GUILayout.Button("删除版本", options))
                        {
                            Directory.Delete(m_ExportAssetBundleDir + platform + "/Version/" + v.ToString(), true);
                            Repaint();
                        }
                        EditorGUILayout.Space();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space();
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
        }

        patchPart = EditorGUILayout.Foldout(patchPart, "已打补丁");
        if (patchPart)
        {
            var patchList = GetPatches(m_ExportAssetBundleDir + platform+"/Patches");
            if (patchList != null && patchList.Count > 0)
            {
                GUILayoutOption[] options =
                {
                        GUILayout.Width(150),
                        GUILayout.Height(20),
                };

                EditorGUILayout.BeginVertical();
                EditorGUILayout.Space();

                foreach (var p in patchList)
                {
                    if (!m_VersionMap.ContainsKey(p.ToString()))
                    {
                        m_VersionMap.Add(p.ToString(), -1);
                    }

                    var selectIndex = m_VersionMap[p.ToString()];
                    selectIndex = GUILayout.SelectionGrid(selectIndex, new string[] { p.ToString() }, 1);
                    m_VersionMap[p.ToString()] = selectIndex;

                    if (selectIndex == 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space();

                        if (GUILayout.Button("浏览", options))
                        {
                            WindowsOSUtility.ExploreFile(m_ExportAssetBundleDir + platform + "/Patches/" + p);
                        }

                        if (GUILayout.Button("取消查看", options))
                        {
                            m_VersionMap[p.ToString()] = -1;
                            Repaint();
                        }

                        if (GUILayout.Button("删除补丁", options))
                        {
                            Directory.Delete(m_ExportAssetBundleDir + platform + "/Patches/" + p, true);
                            Repaint();
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space();
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
        }

        GUIContent[] guiBuildObjs =
        {
                new GUIContent("打版本"),
                new GUIContent("打补丁"),
            };

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        m_BuildOperation = (BuildOperation)GUILayout.Toolbar((int)m_BuildOperation, guiBuildObjs);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (m_BuildOperation == BuildOperation.Version)
        {
            m_CreateVersion = EditorGUILayout.TextField("打包版本号:", m_CreateVersion);

            GUILayoutOption[] options =
            {
                    GUILayout.Width(150),
                    GUILayout.Height(26),
                };
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("重新打包版本(慎用)", options))
            {
                var versionNumber = ValidVersion(m_CreateVersion);
                if (versionNumber == null)
                {
                    GetWindow<AssetBundleBuildEditor>().ShowNotification(new GUIContent("版本名称不符合规范,请重新输入"));
                    return;
                }

                if (!Directory.Exists(m_ExportAssetBundleDir + platform+"/Version/" + versionNumber))
                {
                    GetWindow<AssetBundleBuildEditor>().ShowNotification(new GUIContent("版本号未经过打包,请使用打包版本选项来打包"));
                    return;
                }

                CreateVersion(m_ExportAssetBundleDir + platform+"/Version/" + versionNumber, true);
                WindowsOSUtility.ExploreFile(m_ExportAssetBundleDir + platform + "/Version/" + versionNumber);
            }
            if (GUILayout.Button("打包版本", options))
            {
                var versionNumber = ValidVersion(m_CreateVersion);
                if (versionNumber == null)
                {
                    GetWindow<AssetBundleBuildEditor>().ShowNotification(new GUIContent("版本名称不符合规范,请重新输入"));
                    return;
                }

                if (Directory.Exists(m_ExportAssetBundleDir + platform+"/Version/" + versionNumber))
                {
                    GetWindow<AssetBundleBuildEditor>().ShowNotification(new GUIContent("版本目录已经存在,请选择新的版本号或者清除现有的版本号目录"));
                    return;
                }

                var versionList = GetVersions(m_ExportAssetBundleDir + platform+"/Version");
                if (versionList != null && versionList.Count > 0)
                {
                    if (VersionNumber.CompareTo(versionList[versionList.Count - 1], versionNumber) > 0)
                    {
                        GetWindow<AssetBundleBuildEditor>().ShowNotification(new GUIContent("当前版本号低于最新的版本号，无法打包,请重新选择版本号"));
                        return;
                    }
                }

                CreateVersion(m_ExportAssetBundleDir + platform+"/Version/" + versionNumber);
                WindowsOSUtility.ExploreFile(m_ExportAssetBundleDir + platform + "/Version/" + versionNumber);
            }
            GUILayout.EndHorizontal();
        }
        else if (m_BuildOperation == BuildOperation.Patch)
        {
            var versionList = GetVersions(m_ExportAssetBundleDir + platform + "/Version");
            if (versionList != null && versionList.Count > 0)
            {
                List<string> versionStringList = new List<string>();
                foreach (var v in versionList)
                {
                    versionStringList.Add(v.ToString());
                }
                GUILayout.BeginHorizontal();
                EditorGUILayout.Space();

                GUILayout.BeginVertical();
                EditorGUILayout.LabelField("低版本");
                m_lowVersion = EditorGUILayout.Popup(m_lowVersion, versionStringList.ToArray());
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical();
                EditorGUILayout.LabelField("高版本");
                m_highVersion = EditorGUILayout.Popup(m_highVersion, versionStringList.ToArray());
                GUILayout.EndVertical();

                EditorGUILayout.Space();
                GUILayout.EndHorizontal();

                GUILayoutOption[] options =
                {
                    GUILayout.Width(150),
                    GUILayout.Height(26),
            };
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("打补丁", options))
                {
                    if (VersionNumber.CompareTo(versionList[m_lowVersion], versionList[m_highVersion]) >= 0)
                    {
                        GetWindow<AssetBundleBuildEditor>().ShowNotification(new GUIContent("低版本号不能大于等于高版本号"));
                        return;
                    }

                    var patchName = versionList[m_lowVersion] + "-" + versionList[m_highVersion];
                   
                    var fileMd5LowVersion =
                        AssetBundleBuilder.GetFilesMD5(
                            m_ExportAssetBundleDir + platform + "/Version/" + versionList[m_lowVersion], ".pkg");

                    var fileMd5HighVersion =
                        AssetBundleBuilder.GetFilesMD5(
                            m_ExportAssetBundleDir + platform + "/Version/" + versionList[m_highVersion], ".pkg");

                    Dictionary<string,string> lowVersionAssets = new Dictionary<string, string>();
                    foreach (var v in fileMd5LowVersion)
                    {
                        var str = v.Key.Replace(Path.GetFullPath(m_ExportAssetBundleDir + platform + "/Version/"+ versionList[m_lowVersion]+"/"), "");
                        lowVersionAssets.Add(str,v.Value);
                    }  

                    Dictionary<string, string> highVersionAssets = new Dictionary<string, string>();
                    foreach (var v in fileMd5HighVersion)
                    {
                        var str = v.Key.Replace(Path.GetFullPath(m_ExportAssetBundleDir + platform + "/Version/" + versionList[m_highVersion] + "/"), "");
                        highVersionAssets.Add(str, v.Value);
                    }

                    Dictionary<string, string> resultAssets = new Dictionary<string, string>();
                    foreach (var v in highVersionAssets)
                    {
                        if (!lowVersionAssets.ContainsKey(v.Key))
                        {
                            resultAssets.Add(v.Key,v.Value);
                        }
                        else
                        {
                            if (lowVersionAssets[v.Key] != v.Value)
                            {
                                resultAssets.Add(v.Key, v.Value);
                            }
                        }
                    }

                    if (resultAssets.Count == 0)
                    {
                        GetWindow<AssetBundleBuildEditor>().ShowNotification(new GUIContent("两个版本之间没有差异,无需生成补丁"));
                        return;
                    }

                    EditorFileUtility.ClearDirectory(m_ExportAssetBundleDir + platform + "/Patches/" + patchName);

                    foreach (var v in resultAssets)
                    {
                        var sourceFileName = m_ExportAssetBundleDir + platform + "/Version/" +
                                             versionList[m_highVersion] + "/" + v.Key;
                        var targetFileName = m_ExportAssetBundleDir + platform + "/Patches/" + patchName + "/" + v.Key;
                        if (!Directory.Exists(Path.GetDirectoryName(targetFileName)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetFileName));
                        }

                        File.Copy(sourceFileName,targetFileName);
                    }

                    var targetPath = m_ExportAssetBundleDir + platform + "/Patches/" + patchName + "/";
                    AssetBundleBuilder.PackPatches(targetPath, patchName, targetPath, ".pkg");
                    WindowsOSUtility.ExploreFile(targetPath);
                }
                GUILayout.EndHorizontal();
            }
        }

        if (m_OldBuildOperation != m_BuildOperation)
        {
            m_lowVersion = 0;
            m_highVersion = 0;
        }

        m_OldBuildOperation = m_BuildOperation;
    }

    private bool CreateOrExploreButton(string path)
    {
        GUILayoutOption[] options =
        {
            GUILayout.Width(44),
        };
        bool isExistExcelExportDir = Directory.Exists(path);
        if (isExistExcelExportDir)
        {
            if (GUILayout.Button("浏览", options))
            {
                if (Directory.Exists(path))
                {
                    WindowsOSUtility.ExploreFile(path);
                }
            }
        }
        else
        {
            if (GUILayout.Button("创建", options))
            {
                Directory.CreateDirectory(path);
            }
        }
        return isExistExcelExportDir;
    }

    private void CreateVersion(string path,bool clearPath = false)
    {
        if (clearPath && !Directory.Exists(path))
        {
            Directory.Delete(path,true);
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        if (EditorUtility.DisplayDialog("打包提示", "请确保打包的时候资源变化都已经保存,否则打包出来的资源可能不是最新的", "确定","取消"))
        {
            if (m_BuildPlatform == BuildPlatform.StandaloneWindows)
            {
                AssetBundleBuilder.AssetBundleBuildTarget = BuildTarget.StandaloneWindows;
            }
            else if (m_BuildPlatform == BuildPlatform.Android)
            {
                AssetBundleBuilder.AssetBundleBuildTarget = BuildTarget.Android;
            }

            AssetBundleBuilder.ExportDir = path;
            AssetBundleBuilder.LoadAllAssets();
        }
    }

    public class VersionNumber
    {
        public int[] VersionNumberArray = new int[4];

     /* 
        public int MajorVersionNumber;
        public int MinorVersionNumber;
        public int BuildVersionNumber;
        public int RevisionVersionNumber;
    */
        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}.{3}", VersionNumberArray[0],VersionNumberArray[1], VersionNumberArray[2], VersionNumberArray[3]);
        }

        public void SetVersionNumber(int index, int value)
        {
            if (index >= 4)
            {
                return;
            }

            VersionNumberArray[index] = value;
        }

        public static int CompareTo(VersionNumber obj1,VersionNumber obj2)
        {
            for (int i = 0; i < obj1.VersionNumberArray.Length; ++i)
            {
                var result = obj1.VersionNumberArray[i].CompareTo(obj2.VersionNumberArray[i]);
                if (result != 0)
                {
                    return result;
                }
            }
            return 0;
        }
    }

    private List<VersionNumber> GetVersions(string path)
    {
        if (!Directory.Exists(path))
        {
            return null;
        }

        List<VersionNumber> versionNumberList = new List<VersionNumber>();
        DirectoryInfo di = new DirectoryInfo(path);
        foreach (var d in di.GetDirectories())
        {
            var versionNumber = ValidVersion(d.Name);
            if (versionNumber != null)
            {
                versionNumberList.Add(versionNumber);
            }    
        }
         
        versionNumberList.Sort(VersionNumber.CompareTo);
        return versionNumberList;
    }

    private VersionNumber ValidVersion(string name)
    {
        VersionNumber versionNumber = new VersionNumber();

        int count = 0;
        int startIndex = 0;
        for (int i = 0; i < name.Length; ++i)
        {
            if (name[i] < '0' || name[i] > '9')
            {
                int result = 0;
                if(!int.TryParse(name.Substring(startIndex, i - startIndex),out result))
                {

                    return null;
                }

                versionNumber.SetVersionNumber(count, result);
                ++count;

                if (name[i] != '.' || count >= 4)
                {
                    return null;
                }

                startIndex = i + 1;
                continue;
            }

            if (i == name.Length - 1)
            {
                int result = 0;
                if (!int.TryParse(name.Substring(startIndex, i - startIndex + 1), out result))
                {
                    return null;
                }
                versionNumber.SetVersionNumber(count,result);

                if (count >= 4)
                {
                    return null;
                }
            }
        }

        return versionNumber;
    }

    private List<string> GetPatches(string path)
    {
        if (!Directory.Exists(path))
        {
            return null;
        }
 
        List<string>  patchesList = new List<string>();
        DirectoryInfo di = new DirectoryInfo(path);
        foreach (var d in di.GetDirectories())
        {
            if (ValidPatch(d.Name))
            {
                patchesList.Add(d.Name);
            }
        }

        return patchesList;
    }

    private bool ValidPatch(string path)
    {
        if (!path.Contains("-"))
        {
            return false;
        }

        var index = path.IndexOf('-');

        if (ValidVersion(path.Substring(0,index)) != null && ValidVersion(path.Substring(index + 1)) != null)
        {
            return true;
        }
        return false;
    }

}
