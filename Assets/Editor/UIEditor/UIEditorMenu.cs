using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.Analytics;

public class UIEditorMenu : EditorWindow
{
    public static UIEditorMenu s_SpriteEditorMenu;
    private static Styles s_Styles;
    private static long s_LastClosedTime;
    private static int s_Selected;
    private static SpriteEditorMenuSetting s_Setting;
    public static UIEditorWindow s_SpriteEditor;

    private void Init(Rect buttonRect)
    {
        if (s_Setting == null)
            s_Setting = ScriptableObject.CreateInstance<SpriteEditorMenuSetting>();
        buttonRect = GUIUtilityWrap.GUIToScreenRect(buttonRect);
        Vector2 windowSize = new Vector2(300f, 145f);
        this.ShowAsDropDown(buttonRect, windowSize);
        Undo.undoRedoPerformed += new Undo.UndoRedoCallback(this.UndoRedoPerformed);
    }

    private void UndoRedoPerformed()
    {
        this.Repaint();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= new Undo.UndoRedoCallback(this.UndoRedoPerformed);
        s_LastClosedTime = DateTime.Now.Ticks / 10000L;
        s_SpriteEditorMenu = null;
    }

    internal static bool ShowAtPosition(Rect buttonRect)
    {
        if (DateTime.Now.Ticks / 10000L < s_LastClosedTime + 50L)
            return false;
        if (Event.current != null)
            Event.current.Use();
        if (s_SpriteEditorMenu == null)
            s_SpriteEditorMenu = ScriptableObject.CreateInstance<UIEditorMenu>();
        s_SpriteEditorMenu.Init(buttonRect);
        return true;
    }

    private void OnGUI()
    {
        if (s_Styles == null)
            s_Styles = new Styles();
        GUILayout.Space(4f);
        EditorGUIUtility.labelWidth = 124f;
        EditorGUIUtility.wideMode = true;
        GUI.Label(new Rect(0.0f, 0.0f, this.position.width, this.position.height), GUIContent.none, s_Styles.background);
        EditorGUI.BeginChangeCheck();
        SpriteEditorMenuSetting.SlicingType slicingType1 = s_Setting.slicingType;
        SpriteEditorMenuSetting.SlicingType slicingType2 = (SpriteEditorMenuSetting.SlicingType)EditorGUILayout.EnumPopup(Styles.typeLabel, (Enum)slicingType1, new GUILayoutOption[0]);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo(s_Setting, "Change slicing type");
            s_Setting.slicingType = slicingType2;
        }
        switch (slicingType2)
        {
            case SpriteEditorMenuSetting.SlicingType.Automatic:
                this.OnAutomaticGUI();
                break;
            case SpriteEditorMenuSetting.SlicingType.GridByCellSize:
            case SpriteEditorMenuSetting.SlicingType.GridByCellCount:
                this.OnGridGUI();
                break;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth + 4f);
        if (GUILayout.Button(Styles.sliceButtonLabel))
            this.DoSlicing();
        GUILayout.EndHorizontal();
    }

    private void DoSlicing()
    {
        this.DoAnalytics();
        switch (s_Setting.slicingType)
        {
            case SpriteEditorMenuSetting.SlicingType.Automatic:
                this.DoAutomaticSlicing();
                break;
            case SpriteEditorMenuSetting.SlicingType.GridByCellSize:
            case SpriteEditorMenuSetting.SlicingType.GridByCellCount:
                this.DoGridSlicing();
                break;
        }
    }

    private void DoAnalytics()
    {
      /*  Analytics.Event("Sprite Editor", "Slice", "Type", (int)s_Setting.slicingType);
        if (s_SpriteEditor.originalTexture != (UnityEngine.Object)null)
        {
            Analytics.Event("Sprite Editor", "Slice", "Texture Width", s_SpriteEditor.originalTexture.width);
            Analytics.Event("Sprite Editor", "Slice", "Texture Height", s_SpriteEditor.originalTexture.height);
        }
        if (s_Setting.slicingType == SpriteEditorMenuSetting.SlicingType.Automatic)
        {
            Analytics.Event("Sprite Editor", "Slice", "Auto Slicing Method", s_Setting.autoSlicingMethod);
        }
        else
        {
            Analytics.Event("Sprite Editor", "Slice", "Grid Slicing Size X", (int)s_Setting.gridSpriteSize.x);
            Analytics.Event("Sprite Editor", "Slice", "Grid Slicing Size Y", (int)s_Setting.gridSpriteSize.y);
            Analytics.Event("Sprite Editor", "Slice", "Grid Slicing Offset X", (int)s_Setting.gridSpriteOffset.x);
            Analytics.Event("Sprite Editor", "Slice", "Grid Slicing Offset Y", (int)s_Setting.gridSpriteOffset.y);
            Analytics.Event("Sprite Editor", "Slice", "Grid Slicing Padding X", (int)s_Setting.gridSpritePadding.x);
            Analytics.Event("Sprite Editor", "Slice", "Grid Slicing Padding Y", (int)s_Setting.gridSpritePadding.y);
        }*/
    }

    private void TwoIntFields(GUIContent label, GUIContent labelX, GUIContent labelY, ref int x, ref int y)
    {
        float num = 16f;
        Rect rect = GUILayoutUtility.GetRect(EditorGUILayoutWrap.kLabelFloatMinW, EditorGUILayoutWrap.kLabelFloatMaxW, num, num, EditorStyles.numberField);
        Rect position1 = rect;
        position1.width = EditorGUIUtility.labelWidth;
        position1.height = 16f;
        GUI.Label(position1, label);
        Rect position2 = rect;
        position2.width -= EditorGUIUtility.labelWidth;
        position2.height = 16f;
        position2.x += EditorGUIUtility.labelWidth;
        position2.width /= 2f;
        position2.width -= 2f;
        EditorGUIUtility.labelWidth = 12f;
        x = EditorGUI.IntField(position2, labelX, x);
        position2.x += position2.width + 3f;
        y = EditorGUI.IntField(position2, labelY, y);
        EditorGUIUtility.labelWidth = position1.width;
    }

    private void OnGridGUI()
    {
        int max1 = !((UnityEngine.Object)s_SpriteEditor.previewTexture != (UnityEngine.Object)null) ? 4096 : s_SpriteEditor.previewTexture.width;
        int max2 = !((UnityEngine.Object)s_SpriteEditor.previewTexture != (UnityEngine.Object)null) ? 4096 : s_SpriteEditor.previewTexture.height;
        if (s_Setting.slicingType == SpriteEditorMenuSetting.SlicingType.GridByCellCount)
        {
            int x = (int)s_Setting.gridCellCount.x;
            int y = (int)s_Setting.gridCellCount.y;
            EditorGUI.BeginChangeCheck();
            this.TwoIntFields(Styles.columnAndRowLabel, Styles.columnLabel, Styles.rowLabel, ref x, ref y);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo((UnityEngine.Object)s_Setting, "Change column & row");
                s_Setting.gridCellCount.x = (float)Mathf.Clamp(x, 1, max1);
                s_Setting.gridCellCount.y = (float)Mathf.Clamp(y, 1, max2);
            }
        }
        else
        {
            int x = (int)s_Setting.gridSpriteSize.x;
            int y = (int)s_Setting.gridSpriteSize.y;
            EditorGUI.BeginChangeCheck();
            this.TwoIntFields(Styles.pixelSizeLabel, Styles.xLabel, Styles.yLabel, ref x, ref y);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo((UnityEngine.Object)s_Setting, "Change grid size");
                s_Setting.gridSpriteSize.x = (float)Mathf.Clamp(x, 1, max1);
                s_Setting.gridSpriteSize.y = (float)Mathf.Clamp(y, 1, max2);
            }
        }
        int x1 = (int)s_Setting.gridSpriteOffset.x;
        int y1 = (int)s_Setting.gridSpriteOffset.y;
        EditorGUI.BeginChangeCheck();
        this.TwoIntFields(Styles.offsetLabel, Styles.xLabel, Styles.yLabel, ref x1, ref y1);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo((UnityEngine.Object)s_Setting, "Change grid offset");
            s_Setting.gridSpriteOffset.x = Mathf.Clamp((float)x1, 0.0f, (float)max1 - s_Setting.gridSpriteSize.x);
            s_Setting.gridSpriteOffset.y = Mathf.Clamp((float)y1, 0.0f, (float)max2 - s_Setting.gridSpriteSize.y);
        }
        int x2 = (int)s_Setting.gridSpritePadding.x;
        int y2 = (int)s_Setting.gridSpritePadding.y;
        EditorGUI.BeginChangeCheck();
        this.TwoIntFields(Styles.paddingLabel, Styles.xLabel, Styles.yLabel, ref x2, ref y2);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo((UnityEngine.Object)s_Setting, "Change grid padding");
            s_Setting.gridSpritePadding.x = (float)Mathf.Clamp(x2, 0, max1);
            s_Setting.gridSpritePadding.y = (float)Mathf.Clamp(y2, 0, max2);
        }
        this.DoPivotGUI();
        GUILayout.Space(2f);
    }

    private void OnAutomaticGUI()
    {
        float pixels = 38f;
        if ((UnityEngine.Object)s_SpriteEditor.originalTexture != (UnityEngine.Object)null && TextureUtilWrap.IsCompressedTextureFormat(s_SpriteEditor.originalTexture.format))
        {
            EditorGUILayout.LabelField(Styles.automaticSlicingHintLabel, s_Styles.notice, new GUILayoutOption[0]);
            pixels -= 31f;
        }
        this.DoPivotGUI();
        EditorGUI.BeginChangeCheck();
        int selectedIndex = s_Setting.autoSlicingMethod;
        int num = EditorGUILayout.Popup(Styles.methodLabel, selectedIndex, Styles.slicingMethodOptions, new GUILayoutOption[0]);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo(s_Setting, "Change Slicing Method");
            s_Setting.autoSlicingMethod = num;
        }
        GUILayout.Space(pixels);
    }

    private void DoPivotGUI()
    {
        EditorGUI.BeginChangeCheck();
        int selectedIndex = s_Setting.spriteAlignment;
        int num = EditorGUILayout.Popup(Styles.pivotLabel, selectedIndex, Styles.spriteAlignmentOptions, new GUILayoutOption[0]);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo(s_Setting, "Change Alignment");
            s_Setting.spriteAlignment = num;
            s_Setting.pivot = SpriteEditorUtilityWrap.GetPivotValue((SpriteAlignment)num, s_Setting.pivot);
        }
        Vector2 vector2 = s_Setting.pivot;
        EditorGUI.BeginChangeCheck();
        using (new EditorGUI.DisabledScope(num != 9))
            vector2 = EditorGUILayout.Vector2Field(Styles.customPivotLabel, vector2);
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RegisterCompleteObjectUndo(s_Setting, "Change custom pivot");
        s_Setting.pivot = vector2;
    }

    private void DoAutomaticSlicing()
    {
        s_SpriteEditor.DoAutomaticSlicing(4, s_Setting.spriteAlignment, s_Setting.pivot, (UIEditorWindow.AutoSlicingMethod)s_Setting.autoSlicingMethod);
    }

    private void DoGridSlicing()
    {
        if (s_Setting.slicingType == SpriteEditorMenuSetting.SlicingType.GridByCellCount)
            this.DetemineGridCellSizeWithCellCount();
        s_SpriteEditor.DoGridSlicing(s_Setting.gridSpriteSize, s_Setting.gridSpriteOffset, s_Setting.gridSpritePadding, s_Setting.spriteAlignment, s_Setting.pivot);
    }

    private void DetemineGridCellSizeWithCellCount()
    {
        int num1 = !((UnityEngine.Object)s_SpriteEditor.previewTexture != (UnityEngine.Object)null) ? 4096 : s_SpriteEditor.previewTexture.width;
        int num2 = !((UnityEngine.Object)s_SpriteEditor.previewTexture != (UnityEngine.Object)null) ? 4096 : s_SpriteEditor.previewTexture.height;
        s_Setting.gridSpriteSize.x = (float)((double)num1 - (double)s_Setting.gridSpriteOffset.x - (double)s_Setting.gridSpritePadding.x * (double)s_Setting.gridCellCount.x) / s_Setting.gridCellCount.x;
        s_Setting.gridSpriteSize.y = (float)((double)num2 - (double)s_Setting.gridSpriteOffset.y - (double)s_Setting.gridSpritePadding.y * (double)s_Setting.gridCellCount.y) / s_Setting.gridCellCount.y;
        s_Setting.gridSpriteSize.x = Mathf.Clamp(s_Setting.gridSpriteSize.x, 1f, (float)num1);
        s_Setting.gridSpriteSize.y = Mathf.Clamp(s_Setting.gridSpriteSize.y, 1f, (float)num2);
    }

    private class Styles
    {
        public GUIStyle background = (GUIStyle)"grey_border";
        public GUIStyle notice;
        public static readonly GUIContent[] spriteAlignmentOptions;
        public static readonly GUIContent[] slicingMethodOptions;
        public static readonly GUIContent methodLabel;
        public static readonly GUIContent pivotLabel;
        public static readonly GUIContent typeLabel;
        public static readonly GUIContent sliceButtonLabel;
        public static readonly GUIContent columnAndRowLabel;
        public static readonly GUIContent columnLabel;
        public static readonly GUIContent rowLabel;
        public static readonly GUIContent pixelSizeLabel;
        public static readonly GUIContent xLabel;
        public static readonly GUIContent yLabel;
        public static readonly GUIContent offsetLabel;
        public static readonly GUIContent paddingLabel;
        public static readonly GUIContent automaticSlicingHintLabel;
        public static readonly GUIContent customPivotLabel;

        static Styles()
        {
            GUIContent[] guiContentArray1 = new GUIContent[10];
            int index1 = 0;
            GUIContent guiContent1 = EditorGUIUtilityWrap.TextContent("Center");
            guiContentArray1[index1] = guiContent1;
            int index2 = 1;
            GUIContent guiContent2 = EditorGUIUtilityWrap.TextContent("Top Left");
            guiContentArray1[index2] = guiContent2;
            int index3 = 2;
            GUIContent guiContent3 = EditorGUIUtilityWrap.TextContent("Top");
            guiContentArray1[index3] = guiContent3;
            int index4 = 3;
            GUIContent guiContent4 = EditorGUIUtilityWrap.TextContent("Top Right");
            guiContentArray1[index4] = guiContent4;
            int index5 = 4;
            GUIContent guiContent5 = EditorGUIUtilityWrap.TextContent("Left");
            guiContentArray1[index5] = guiContent5;
            int index6 = 5;
            GUIContent guiContent6 = EditorGUIUtilityWrap.TextContent("Right");
            guiContentArray1[index6] = guiContent6;
            int index7 = 6;
            GUIContent guiContent7 = EditorGUIUtilityWrap.TextContent("Bottom Left");
            guiContentArray1[index7] = guiContent7;
            int index8 = 7;
            GUIContent guiContent8 = EditorGUIUtilityWrap.TextContent("Bottom");
            guiContentArray1[index8] = guiContent8;
            int index9 = 8;
            GUIContent guiContent9 = EditorGUIUtilityWrap.TextContent("Bottom Right");
            guiContentArray1[index9] = guiContent9;
            int index10 = 9;
            GUIContent guiContent10 = EditorGUIUtilityWrap.TextContent("Custom");
            guiContentArray1[index10] = guiContent10;

            Styles.spriteAlignmentOptions = guiContentArray1;
            GUIContent[] guiContentArray2 = new GUIContent[3];
            int index11 = 0;
            GUIContent guiContent11 = EditorGUIUtilityWrap.TextContent("Delete Existing|Delete all existing sprite assets before the slicing operation");
            guiContentArray2[index11] = guiContent11;
            int index12 = 1;
            GUIContent guiContent12 = EditorGUIUtilityWrap.TextContent("Smart|Try to match existing sprite rects to sliced rects from the slicing operation");
            guiContentArray2[index12] = guiContent12;
            int index13 = 2;
            GUIContent guiContent13 = EditorGUIUtilityWrap.TextContent("Safe|Keep existing sprite rects intact");
            guiContentArray2[index13] = guiContent13;
            Styles.slicingMethodOptions = guiContentArray2;
            Styles.methodLabel = EditorGUIUtilityWrap.TextContent("Method");
            Styles.pivotLabel = EditorGUIUtilityWrap.TextContent("Pivot");
            Styles.typeLabel = EditorGUIUtilityWrap.TextContent("Type");
            Styles.sliceButtonLabel = EditorGUIUtilityWrap.TextContent("Slice");
            Styles.columnAndRowLabel = EditorGUIUtilityWrap.TextContent("Column & Row");
            Styles.columnLabel = EditorGUIUtilityWrap.TextContent("C");
            Styles.rowLabel = EditorGUIUtilityWrap.TextContent("R");
            Styles.pixelSizeLabel = EditorGUIUtilityWrap.TextContent("Pixel Size");
            Styles.xLabel = EditorGUIUtilityWrap.TextContent("X");
            Styles.yLabel = EditorGUIUtilityWrap.TextContent("Y");
            Styles.offsetLabel = EditorGUIUtilityWrap.TextContent("Offset");
            Styles.paddingLabel = EditorGUIUtilityWrap.TextContent("Padding");
            Styles.automaticSlicingHintLabel = EditorGUIUtilityWrap.TextContent("To obtain more accurate slicing results, manual slicing is recommended!");
            Styles.customPivotLabel = EditorGUIUtilityWrap.TextContent("Custom Pivot");
        }

        public Styles()
        {
            this.notice = new GUIStyle(GUI.skin.label);
            this.notice.alignment = TextAnchor.MiddleCenter;
            this.notice.wordWrap = true;
        }
    }
}
