using UnityEngine;
using System.IO;
using System.Xml;
using UnityEditor;

public class SettingUI
{
    enum ExportLanguage
    {
        CSharp,
        Lua,
    }

    private ExportLanguage m_ExportLanguage = ExportLanguage.CSharp;

    private string m_ExcelExportDirectory = "../../Excel/Data";
    private string m_XmlExportDirectory = "./ExternalResources/Data/Xml";
    private string m_XmlBinExportDirectory = "./Resources/Data/GameData";
    private string m_CsExportDirectory = "./Scripts/Data/GameData";

    private long m_ConfigFileChangeTime;
    private string m_DataPath;
    private ExcelExportEditor m_Window;

    public string ExcelExportDirectory
    {
        get { return Path.GetFullPath(m_DataPath +"/"+m_ExcelExportDirectory); }
    }

    public string XmlExportDirectory
    {
        get { return Path.GetFullPath(m_DataPath + "/" + m_XmlExportDirectory); }
    }

    public string XmlBinExportDirectory
    {
        get { return Path.GetFullPath(m_DataPath + "/" + m_XmlBinExportDirectory); }
    }

    public string CsExportDirectory
    {
        get { return Path.GetFullPath(m_DataPath + "/" + m_CsExportDirectory); }
    }

    public SettingUI(ExcelExportEditor window)
    {
        m_Window = window;
        m_DataPath = Application.dataPath;
    }

    public void SettingModeUI()
    {
        EditorGUILayout.BeginVertical();

        var isExistExcelExportDir = false;

        EditorGUILayout.BeginHorizontal();
        m_ExcelExportDirectory = EditorGUILayout.TextField("Excel文件目录:", m_ExcelExportDirectory);
        isExistExcelExportDir = CreateOrExploreButton(ExcelExportDirectory);
        EditorGUILayout.EndHorizontal();

        if (!isExistExcelExportDir)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("Excel文件导出目录不存在", MessageType.Error);
            EditorGUILayout.EndHorizontal();
        }

        GUIContent[] guiObjs =
        {
            new GUIContent("C#"),
            new GUIContent("Lua"),
        };

        GUILayoutOption[] options = new GUILayoutOption[]
        {
            GUILayout.Width(100),
            GUILayout.Height(36),
        };

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        m_ExportLanguage = (ExportLanguage)GUILayout.Toolbar((int)m_ExportLanguage, guiObjs, options);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        switch (m_ExportLanguage)
        {
            case ExportLanguage.CSharp:
                LoadCSSettingConfig();
                ExportCSharp();
                break;
        }
        EditorGUILayout.EndVertical();
    }

    private void ExportCSharp()
    {
        EditorGUILayout.BeginHorizontal();
        m_XmlExportDirectory = EditorGUILayout.TextField("Xml导出目录:", m_XmlExportDirectory);
        CreateOrExploreButton(XmlExportDirectory);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_XmlBinExportDirectory = EditorGUILayout.TextField("Xml编译导出目录:", m_XmlBinExportDirectory);
        CreateOrExploreButton(XmlBinExportDirectory);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_CsExportDirectory = EditorGUILayout.TextField("C#导出目录:", m_CsExportDirectory);
        CreateOrExploreButton(CsExportDirectory);
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        GUILayoutOption[] options =
        {
            GUILayout.Width(100),
            GUILayout.Height(36),
        };

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("应用", options))
        {
            switch (m_ExportLanguage)
            {
                case ExportLanguage.CSharp:
                    ApplyCsConfig();
                    break;
            }
        }
        if (GUILayout.Button("重置", options))
        {
            switch (m_ExportLanguage)
            {
                case ExportLanguage.CSharp:
                    ResetCsConfig();
                    break;
            }
        }

        GUILayout.EndHorizontal();
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

    private void ApplyCsConfig()
    {
        ApplyCsSettingConfig();
        m_Window.ShowNotification(new GUIContent("应用设置成功"));
    }

    private void ResetCsConfig()
    {
        m_ExcelExportDirectory = "../../Excel/Data";
        m_XmlExportDirectory = "./ExternalResources/Data/Xml";
        m_XmlBinExportDirectory = "./Resources/Data/GameData";
        m_CsExportDirectory = "./Scripts/Data/GameData";

        ApplyCsSettingConfig();
        m_Window.ShowNotification(new GUIContent("重置设置成功"));
    }

    public void LoadCSSettingConfig()
    {
        var configFilePath = Application.dataPath + "/Editor/ExcelExportTool/Config/SettingConfig.xml";
        FileInfo fileInfo = new FileInfo(configFilePath);
        var lastFileWriteTime = EditorTimeUtility.DateTimeToUnixTimesStamp(fileInfo.LastWriteTimeUtc);
        if (lastFileWriteTime != m_ConfigFileChangeTime)
        {
            m_ConfigFileChangeTime = lastFileWriteTime;
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(configFilePath);
            var nodeList = xmlDoc.SelectSingleNode("root").ChildNodes;
            foreach (var childNode in nodeList)
            {
                var childElement = (XmlElement)childNode;
                if (childElement.Name == "ExcelExportDirectory")
                {
                    m_ExcelExportDirectory = childElement.InnerText;
                }
                else if (childElement.Name == "XmlExportDirectory")
                {
                    m_XmlExportDirectory = childElement.InnerText;
                }
                else if (childElement.Name == "XmlBinExportDirectory")
                {
                    m_XmlBinExportDirectory = childElement.InnerText;
                }
                else if (childElement.Name == "CsExportDirectory")
                {
                    m_CsExportDirectory = childElement.InnerText;
                }
            }
        }
    }

    private void ApplyCsSettingConfig()
    {
        var configFilePath = Application.dataPath + "/Editor/ExcelExportTool/Config/SettingConfig.xml";

        var xmlDoc = new XmlDocument();
        xmlDoc.Load(configFilePath);
        var nodeList = xmlDoc.SelectSingleNode("root").ChildNodes;
        foreach (var childNode in nodeList)
        {
            var childElement = (XmlElement)childNode;
            if (childElement.Name == "ExcelExportDirectory")
            {
                childElement.InnerText = m_ExcelExportDirectory;
            }
            else if (childElement.Name == "XmlExportDirectory")
            {
                childElement.InnerText = m_XmlExportDirectory;
            }
            else if (childElement.Name == "XmlBinExportDirectory")
            {
                childElement.InnerText = m_XmlBinExportDirectory;
            }
            else if (childElement.Name == "CsExportDirectory")
            {
                childElement.InnerText = m_CsExportDirectory;
            }
        }
        xmlDoc.Save(configFilePath);
    }
}
