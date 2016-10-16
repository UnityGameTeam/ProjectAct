using UnityEditor;
using UnityEngine;

public class ExcelExportEditor : EditorWindow
{
    public enum ToolMode
    {
        Excel,
        Setting,
    }

    private ToolMode            m_ToolMode = ToolMode.Excel;
    private ToolMode            m_LastToolMode = ToolMode.Excel;

    public SettingUI           SettingUI { get; set; }
    public ExcelOneKeyExportUI OneKeyExportUI { get; set; }
    public ExportProcessUI     ExportProcessUI { get; set; }
    public PriorityConfig      PriorityConfig { get; set; }
    public SingleExcelConfigUI SingleExcelConfigUI { get; set; }

    public ExcelExportEditor()
    {
        SettingUI = new SettingUI(this);
        ExportProcessUI = new ExportProcessUI(this);
        PriorityConfig = new PriorityConfig();
        SingleExcelConfigUI = new SingleExcelConfigUI(this);
        OneKeyExportUI = new ExcelOneKeyExportUI(this);    
    }

    private void OnGUI()
    {
        if (ExportProcessUI.ExportProcessMode != ExportProcessUI.ProcessMode.None)
        {
            ExportProcessUI.ExportProcess();
            return;
        }

        PriorityConfig.LoadPriorityConfig();
        if (SingleExcelConfigUI.ShowSingleExcelUI)
        {
            SingleExcelConfigUI.ShowSingleExcelConfigUI();
            return;
        }

        var iconExcel = LoadTexture("Excel.png");
        var iconSetting = LoadTexture("Setting.png");

        GUIContent[] guiObjs =
        {
            new GUIContent(iconExcel, "Excel Export"),
            new GUIContent(iconSetting, "Excel Export Setting"),
        };

        GUILayoutOption[] options =
        {
            GUILayout.Width(100),
            GUILayout.Height(44),
        };
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        m_ToolMode = (ToolMode)GUILayout.Toolbar((int)m_ToolMode, guiObjs, options);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        SettingUI.LoadCSSettingConfig();

        if (m_LastToolMode != m_ToolMode)
        {
            EditorGUI.FocusTextInControl("");
            m_LastToolMode = m_ToolMode;
        }
        switch (m_ToolMode)
        {
            case ToolMode.Excel:
                OneKeyExportUI.ExcelOneKeyExportEditorUI();
                break;
            case ToolMode.Setting:
                SettingUI.SettingModeUI();
                break;
        }
    }

    void Update()
    {
        if (ExportProcessUI.ExportProcessMode != ExportProcessUI.ProcessMode.None)
        {
            if (ExportProcessUI.NeedUpdate())
            {
                Repaint();
            }
        }
    }

    void OnDestroy()
    {
        ExportProcessUI.Destory();
    }

    [MenuItem("Tools/Excel Export Tool")]
    static void OpenExcelExportEditor()
    {
        ExcelExportEditor window = (ExcelExportEditor)GetWindow(typeof(ExcelExportEditor));
        window.minSize = new Vector2(300, 300);
    }

    Texture LoadTexture(string name)
    {
        string path = "Assets/Editor/ExcelExportTool/Icons/";
        return (Texture)AssetDatabase.LoadAssetAtPath(path + name, typeof(Texture));
    }
}
