using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Aspose.Cells;
using UnityEditor;

public class ExportProcessUI
{
    public enum ProcessMode
    {
        None,
        AllExportXml,
    }

    private int m_ShowCount;
    private Vector2 m_ProcessScrollPos;
    private Thread m_ProcessThread;
    private List<string> m_ProcessResults = new List<string>();
    private ProcessMode m_ExportProcessMode = ProcessMode.None;
 
    private int m_ResultShowCount;
    private SaveGameDataConfig m_SaveGameDataConfig;
    private ExcelExportEditor m_Window;

    private string m_CodeTemplateFilePath;

    public ProcessMode ExportProcessMode
    {
        get { return m_ExportProcessMode; }
    }

    public ExportProcessUI(ExcelExportEditor window)
    {
        m_Window = window;
        m_SaveGameDataConfig = new SaveGameDataConfig(window);
        m_CodeTemplateFilePath = Application.dataPath + "/Editor/ExcelExportTool/Templates/CSTemplate.txt";
    }

    void AllExportToXml()
    {
        GUIStyle guiStyle = new GUIStyle();
        guiStyle.richText = true;
        EditorGUILayout.Space();

        m_ProcessScrollPos = EditorGUILayout.BeginScrollView(m_ProcessScrollPos);
        if (Event.current.type == EventType.Layout)
        {
            m_ResultShowCount = m_ProcessResults.Count;
        }
        for (int i = 0; i < m_ResultShowCount; ++i)
        {
            if (m_ProcessResults[i].StartsWith("###"))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(m_ProcessResults[i].Substring(3), guiStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            else
            {
                GUILayout.Label(m_ProcessResults[i], guiStyle);
                EditorGUILayout.Space();
            }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndScrollView();

        GUILayoutOption[] options =
        {
            GUILayout.Width(100),
            GUILayout.Height(36),
        };

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("终止", options))
        {
            if (m_ProcessThread != null)
            {
                m_ProcessThread.Abort();
                m_ProcessThread = null;
            }
            m_ExportProcessMode = ProcessMode.None;
        }
        EditorGUILayout.EndHorizontal();
    }

    public void ExportProcess()
    {
        switch (m_ExportProcessMode)
        {
            case ProcessMode.AllExportXml:
                AllExportToXml();
                break;
        }
    }

    public void AllExportXml()
    {
        if (Directory.Exists(m_Window.SettingUI.XmlExportDirectory))
        {
            Directory.Delete(m_Window.SettingUI.XmlExportDirectory, true);
            AssetDatabase.Refresh();
        }
        if (!Directory.Exists(m_Window.SettingUI.XmlExportDirectory))
        {
            Directory.CreateDirectory(m_Window.SettingUI.XmlExportDirectory);
            AssetDatabase.Refresh();
        }

        m_ExportProcessMode = ProcessMode.AllExportXml;
        m_ProcessResults.Clear();
        m_ShowCount = 0;
        m_ProcessResults.Add("###<color=#ffffff>导出Excel为Xml文件</color>");

        if (m_ProcessThread != null)
        {
            m_ProcessThread.Abort();
            m_ProcessThread = null;
        }

        m_ProcessThread = new Thread(ExportXmlProcess);
        m_ProcessThread.Start();
    }

    public void Destory()
    {
        if (m_ProcessThread != null)
        {
            m_ProcessThread.Abort();
            m_ProcessThread = null;
        }
    }

    public bool NeedUpdate()
    {
        if (m_ShowCount != m_ProcessResults.Count)
        {
            for (int i = m_ShowCount; i < m_ProcessResults.Count; ++i)
            {
                if (m_ProcessResults[i].StartsWith("###"))
                {
                    AssetDatabase.Refresh();
                }
            }
            m_ShowCount = m_ProcessResults.Count;
            return true;
        }
        return false;
    }

    private void ExportXmlProcess()
    {
        lock (m_ProcessResults)
        {
            bool hasError = false;
            int warningNum = 0;
            int exportXmlNum = 0;
            for (int i = 0; i < m_Window.OneKeyExportUI.ExcelFileList.Count; ++i)
            {
                var efi = m_Window.OneKeyExportUI.ExcelFileList[i];
                var result = string.Format("<color=#ffffff>{0}、</color><color=#22b454>处理第{1}个Excel文件</color> <color=#bbbbbb>名称:{2}</color> <color=#888888>路径:{3}</color>",i + 1, i + 1, efi.Name, efi.FullName);

                m_ProcessResults.Add(result);
                Workbook workbook = new Workbook(efi.FullName);
                foreach (var worksheet in workbook.Worksheets)
                {
                    result = string.Format("    <color=#bbbbbb>-->处理工作表:{0}</color>", worksheet.Name);
                    m_ProcessResults.Add(result);
                    if (worksheet.ListObjects.Count <= 0)
                    {
                        result = string.Format("        <color=#cca200>-->工作表中不包含插入表格,跳过处理</color>");
                        m_ProcessResults.Add(result);
                        ++warningNum;
                    }
                    else
                    {
                        if (worksheet.ListObjects.Count > 1)
                        {
                            result = string.Format("        <color=#cca200>-->工作表中包含多个插入表格,只处理第一个表格，其他忽略</color>");
                            m_ProcessResults.Add(result);
                            ++warningNum;
                        }

                        var convertResult = "";
                        result = ExcelToXmlUtility.ExoportXml(efi.FullName, m_Window.SettingUI.XmlExportDirectory, worksheet.Name, worksheet.ListObjects[0],ref convertResult);

                        if (!string.IsNullOrEmpty(result))
                        {
                            m_ProcessResults.Add(result);
                            hasError = true;
                            break;
                        }
                        else
                        {
                            if (convertResult != null)
                            {
                                m_ProcessResults.Add(convertResult);
                                ++warningNum;
                            }
                            else
                            {
                                m_ProcessResults.Add("        <color=#bbbbbb>导出xml成功</color>");
                                ++exportXmlNum;
                            }
                        }
                    }
                }

                if (hasError)
                    break;
            }

            if (hasError)
            {
                m_ProcessResults.Add("###<color=#ff0000>================有错误发生,导出xml结束,请先修复错误,再导出xml================</color>");
            }
            else
            {
                m_ProcessResults.Add(string.Format("###<color=#22b454>================导出xml结束,导出成功,导出{0}个xml文件,{1}个警告================</color>", exportXmlNum,warningNum));
                SavePriorityConfig();
            }           
        }
    }

    private void SavePriorityConfig()
    {
        m_ProcessResults.Add("###<color=#ffffff>保存数据优先级配置</color>");
        m_ProcessResults.Add(m_Window.PriorityConfig.SavePriorityConfig(m_Window.SettingUI.XmlExportDirectory));
        m_ProcessResults.Add("###<color=#22b454>=======================保存数据优先级配置成功=======================</color>");

        try
        {
            m_ProcessResults.Add("###<color=#ffffff>写入数据优先级配置</color>");
            m_ProcessResults.Add(m_SaveGameDataConfig.ExportConfig());
            m_ProcessResults.Add("###<color=#22b454>=======================写入数据优先级配置成功=======================</color>");

        }
        catch (Exception e)
        {
           Debug.LogError(e);
        }

        CompileXml.ComplieXmlFile(m_Window.SettingUI.XmlExportDirectory, m_Window.SettingUI.XmlBinExportDirectory);
        CsBuilder.CreateCsCode(m_Window.SettingUI.XmlExportDirectory,m_Window.SettingUI.CsExportDirectory, m_CodeTemplateFilePath);
    }
}
