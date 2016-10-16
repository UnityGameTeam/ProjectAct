using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Aspose.Cells;
using UnityEditor;

public class SingleExcelConfigUI
{
    private bool m_ShowSingleExcelUI;
    public bool ShowSingleExcelUI
    {
        get { return m_ShowSingleExcelUI;}
        set { m_ShowSingleExcelUI = value; }
    }

    private string m_Title = "";
    private string m_ExcelPath = "";
    private int    m_WorksheetNum = 0;

    private List<bool> m_WorksheetShowList;
    private List<string> m_WorksheetNameList;
    private List<bool> m_WorksheetListObjectsList;
    private List<List<string>> m_WorksheetColumnList;
    private List<int> m_PriorityList;
    private List<int> m_OldPriorityList;

    private long m_ExcelFileChangeTime;
    private Vector2 m_ScrollPos;
    private ExcelExportEditor m_Window;

    public SingleExcelConfigUI(ExcelExportEditor window)
    {
        m_Window = window;
    }

    public void ShowSingleExcelConfigUI()
    {
        SetSingleExcelFileInfo(m_ExcelPath, m_Title);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(m_Title);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("文件路径:  " + m_ExcelPath);
        CreateOrExploreButton(m_ExcelPath);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("工作表数量:  " + m_WorksheetNum);
        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        for (int i = 0; i < m_WorksheetShowList.Count; i++)
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            m_WorksheetShowList[i] = EditorGUILayout.Foldout(m_WorksheetShowList[i], m_WorksheetNameList[i]);
            if (m_WorksheetShowList[i])
            {
                if (m_WorksheetListObjectsList[i])
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    foreach (var column in m_WorksheetColumnList[i])
                    {
                        GUILayout.Button(column);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("数据加载优先级:");

                    string[] priorityLabel = new string[]
                    {
                        "Preload","High","Medium","Normal","Low","DontLoad"
                    };
                    m_PriorityList[i] = GUILayout.Toolbar(m_PriorityList[i], priorityLabel);
                    if (m_Window.PriorityConfig.PriorityMap.ContainsKey(m_WorksheetNameList[i]))
                    {
                        m_Window.PriorityConfig.PriorityMap[m_WorksheetNameList[i]] = m_PriorityList[i];
                    }
                    else
                    {
                        m_Window.PriorityConfig.PriorityMap.Add(m_WorksheetNameList[i], m_PriorityList[i]);
                    }

                    if (m_OldPriorityList[i] != m_PriorityList[i])
                    {
                        m_OldPriorityList[i] = m_PriorityList[i];
                        m_Window.PriorityConfig.SaveSinglePriority(m_WorksheetNameList[i], m_Window.PriorityConfig.PriorityMap[m_WorksheetNameList[i]] + "");
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("工作表中没有可以可导出的表格");
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }
        
        GUILayout.FlexibleSpace();
        GUILayoutOption[] options =
        {
            GUILayout.Width(100),
            GUILayout.Height(36),
        };
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("返回", options))
        {
            m_ShowSingleExcelUI = false;
            m_ExcelFileChangeTime = 0;
        }
        GUILayout.EndHorizontal();
    }

    public void SetSingleExcelFileInfo(string excelPath,string title)
    {
        ReloadExcelFileInfo(excelPath, title);
    }

    private void ReloadExcelFileInfo(string excelPath, string title)
    {
        FileInfo fileInfo = new FileInfo(excelPath);
        var lastFileWriteTime = EditorTimeUtility.DateTimeToUnixTimesStamp(fileInfo.LastWriteTimeUtc);
        if (lastFileWriteTime != m_ExcelFileChangeTime)
        {
            m_Title = title;
            m_ExcelPath = excelPath;

            Workbook workbook = new Workbook(m_ExcelPath);
            m_WorksheetNum = workbook.Worksheets.Count;
            m_WorksheetShowList = new List<bool>(m_WorksheetNum);
            m_WorksheetNameList = new List<string>(m_WorksheetNum);
            for (int i = 0; i < m_WorksheetNum; i++)
            {
                m_WorksheetShowList.Add(true);
                m_WorksheetNameList.Add(workbook.Worksheets[i].Name);
            }

            m_WorksheetListObjectsList = new List<bool>(m_WorksheetNum);
            m_WorksheetColumnList = new List<List<string>>(m_WorksheetNum);
            for (int i = 0; i < m_WorksheetNum; i++)
            {
                List<string> columnNames = new List<string>();
                if (workbook.Worksheets[i].ListObjects.Count == 0)
                {
                    m_WorksheetListObjectsList.Add(false);
                }
                else
                {
                    m_WorksheetListObjectsList.Add(true);
                    foreach (var column in workbook.Worksheets[i].ListObjects[0].ListColumns)
                    {
                        columnNames.Add(column.Name);
                    }
                }
                m_WorksheetColumnList.Add(columnNames);
            }
            m_ExcelFileChangeTime = lastFileWriteTime;

            m_PriorityList = new List<int>(m_WorksheetNum);
            m_OldPriorityList = new List<int>(m_WorksheetNum);
            for (int i = 0; i < m_WorksheetNum; i++)
            {
                if (m_Window.PriorityConfig.PriorityMap.ContainsKey(m_WorksheetNameList[i]))
                {
                    m_PriorityList.Add(m_Window.PriorityConfig.PriorityMap[m_WorksheetNameList[i]]);
                    m_OldPriorityList.Add(m_Window.PriorityConfig.PriorityMap[m_WorksheetNameList[i]]);
                }
                else
                {
                    m_PriorityList.Add(3);
                    m_OldPriorityList.Add(3);
                }
            }
        }
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
            if (File.Exists(path))
            {
                if (GUILayout.Button("查看", options))
                {
                    WindowsOSUtility.ExploreFile(path);
                }
            }
        }
        return isExistExcelExportDir;
    }
}
