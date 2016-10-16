using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Aspose.Cells;
using UnityEditor;

public class ExcelOneKeyExportUI
{
    private string          m_SearchText = "";
    private List<FileInfos> m_ExcelFileList;

    private List<string>    m_ExcelFileShowList = new List<string>();
    private List<string>    m_FullExcelFileShowList = new List<string>();

    private Vector2         m_ScrollPos;
    private int             m_SelectionGridIndex = -1;
    private int             m_LastSelectionGridIndex = -1;

    private string m_CacheExcelExportDir = "";
    private long m_ExcelExportDirChange;

    private ExcelExportEditor m_Window;
    public List<FileInfos> ExcelFileList
    {
        get { return m_ExcelFileList; }
    }

    public ExcelOneKeyExportUI(ExcelExportEditor window)
    {
        m_Window = window;
    }

    public void ExcelOneKeyExportEditorUI()
    {
        EditorGUILayout.BeginHorizontal();
        m_SearchText = EditorGUILayout.TextField("Excel文件搜索:", m_SearchText);
        EditorGUILayout.EndHorizontal();


        if (!Directory.Exists(m_Window.SettingUI.ExcelExportDirectory))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("Excel文件导出目录不存在，请到设置页面修改", MessageType.Error);
            EditorGUILayout.EndHorizontal();
            if (m_ExcelFileList != null)
                m_ExcelFileList.Clear();
            m_CacheExcelExportDir = "";
            m_ExcelExportDirChange = 0;
        }

        if (Directory.Exists(m_Window.SettingUI.ExcelExportDirectory) && m_CacheExcelExportDir != m_Window.SettingUI.ExcelExportDirectory)
        {
            m_CacheExcelExportDir = m_Window.SettingUI.ExcelExportDirectory;
            m_ExcelExportDirChange = 0;
        }

        var result = ListExcelFiles();
        if (!string.IsNullOrEmpty(result))
        {
            m_ExcelExportDirChange = 0;
            m_SelectionGridIndex = -1;
            m_LastSelectionGridIndex = -1;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.HelpBox(result, MessageType.Error);
            EditorGUILayout.EndVertical();
        }
        else
        {
            ShowExcelFiles();

            GUILayoutOption[] options =
            {
                GUILayout.Width(100),
                GUILayout.Height(36),
            };

            if (m_ExcelFileList != null && m_ExcelFileList.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("全部导出Xml", options))
                {
                    m_Window.ExportProcessUI.AllExportXml();
                }
                GUILayout.EndHorizontal();
            }
        }
    }

    private void ShowExcelFiles()
    {
        if (string.IsNullOrEmpty(m_SearchText))
        {
            m_ExcelFileShowList.Clear();
            m_FullExcelFileShowList.Clear();
            if (m_ExcelFileList != null)
            {
                foreach (var efi in m_ExcelFileList)
                {
                    m_ExcelFileShowList.Add(efi.Name);
                    m_FullExcelFileShowList.Add(efi.FullName);
                }
            }
        }
        else
        {
            m_ExcelFileShowList.Clear();
            m_FullExcelFileShowList.Clear();
            if (m_ExcelFileList != null)
            {
                foreach (var efi in m_ExcelFileList)
                {
                    if (efi.Name.ToLower().Contains(m_SearchText.ToLower()))
                    {
                        m_ExcelFileShowList.Add(efi.Name);
                        m_FullExcelFileShowList.Add(efi.FullName);
                    }
                }
            }
        }

        if (m_ExcelFileShowList.Count > 0)
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            m_SelectionGridIndex = GUILayout.SelectionGrid(m_SelectionGridIndex, m_ExcelFileShowList.ToArray(), 3);
            if (m_LastSelectionGridIndex != m_SelectionGridIndex)
            {
                m_LastSelectionGridIndex = m_SelectionGridIndex;
                m_Window.SingleExcelConfigUI.SetSingleExcelFileInfo(m_FullExcelFileShowList[m_LastSelectionGridIndex], m_ExcelFileShowList[m_LastSelectionGridIndex]);
                m_Window.SingleExcelConfigUI.ShowSingleExcelUI = true;
                m_LastSelectionGridIndex = m_SelectionGridIndex = -1;
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("没有检索到Excel文件");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
    }

    private string ListExcelFiles()
    {
        if (Directory.Exists(m_CacheExcelExportDir) && !string.IsNullOrEmpty(m_CacheExcelExportDir))
        {
            FileInfo fileInfo = new FileInfo(m_CacheExcelExportDir);
            var lastFileWriteTime = EditorTimeUtility.DateTimeToUnixTimesStamp(fileInfo.LastWriteTimeUtc);
            if (lastFileWriteTime != m_ExcelExportDirChange)
            {
                m_ExcelFileList = EditorFileUtility.FilterDirectory(m_CacheExcelExportDir,
                    new string[] { ".xlsx", ".xls", ".xlsm" }, false);

                m_ExcelExportDirChange = lastFileWriteTime;
                m_SelectionGridIndex = -1;
                m_LastSelectionGridIndex = -1;
                return CheckDuplicateWorkSheet();
            }
        }
        return "";
    }

    private string CheckDuplicateWorkSheet()
    {
        Dictionary<string, string> sheetMap = new Dictionary<string, string>();
        foreach (var efi in m_ExcelFileList)
        {
            Workbook workbook = new Workbook(efi.FullName);
            foreach (var worksheet in workbook.Worksheets)
            {
                if (sheetMap.ContainsKey(worksheet.Name))
                {
                    var result = "";
                    if (sheetMap[worksheet.Name] == efi.FullName)
                    {
                        result = "错误源:" + efi.FullName + "\n错误原因：文件中存在同名的Worksheet";
                    }
                    else
                    {
                        result = "错误源:" + efi.FullName + "\n错误源:" + sheetMap[worksheet.Name] +
                                 "\n错误原因: 两个文件中存在同名的Worksheet";
                    }
                    return result;
                }
                sheetMap.Add(worksheet.Name, efi.FullName);
            }
        }
        return "";
    }
}
