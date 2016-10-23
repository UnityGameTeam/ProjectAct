using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;

public class UIEditorWindow : ZoomUtilityWindow
{
    internal static PrefKey k_SpriteEditorTrim = new PrefKey("Sprite Editor/Trim", "#t");
    public static bool s_OneClickDragStarted = false;
    private Rect m_PolygonChangeShapeWindowRect = new Rect(0.0f, 17f, 150f, 45f);
    private const float maxSnapDistance = 14f;
    private const float marginForFraming = 0.05f;
    private const float k_WarningMessageWidth = 250f;
    private const float k_WarningMessageHeight = 40f;
    private const int k_PolygonChangeShapeWindowMargin = 17;
    private const int k_PolygonChangeShapeWindowWidth = 150;
    private const int k_PolygonChangeShapeWindowHeight = 45;
    private const int k_PolygonChangeShapeWindowWarningHeight = 65;
    protected const float k_InspectorHeight = 160f;
    public static UIEditorWindow s_Instance;
    public bool m_ResetOnNextRepaint;
    public bool m_IgnoreNextPostprocessEvent;
    public Texture2D m_OriginalTexture;
    private int m_PolygonSides;
    private bool m_ShowPolygonChangeShapeWindow;
    private SpriteRectCache m_RectsCache;
    private SerializedObject m_TextureImporterSO;
    private TextureImporter m_TextureImporter;
    private SerializedProperty m_TextureImporterSprites;
    private SerializedProperty m_SpriteSheetOutline;
    private bool m_TextureIsDirty;
    private static bool[] s_AlphaPixelCache;
    public string m_SelectedAssetPath;
    private GizmoMode m_GizmoMode;
    [SerializeField]
    private SpriteRect m_Selected;

    public Texture2D originalTexture
    {
        get
        {
            return this.m_OriginalTexture;
        }
    }

    internal Texture2D previewTexture
    {
        get
        {
            return this.m_Texture;
        }
    }

    [MenuItem("Tools/Test1")]
    public static void EditorTest1()
    {
        GetWindow<UIEditorWindow>();
    }


    internal SpriteRect selected
    {
        get
        {
            if (this.IsEditingDisabled())
                return (SpriteRect)null;
            return this.m_Selected;
        }
        set
        {
            if (value == this.m_Selected)
                return;
            this.m_Selected = value;
        }
    }

    private int defaultColliderAlphaCutoff
    {
        get
        {
            return 254;
        }
    }

    private float defaultColliderDetail
    {
        get
        {
            return 0.25f;
        }
    }

    private Rect inspectorRect
    {
        get
        {
            return new Rect((float)((double)this.position.width - 330.0 - 8.0 - 16.0), (float)((double)this.position.height - 160.0 - 8.0 - 16.0), 330f, 160f);
        }
    }

    private Rect warningMessageRect
    {
        get
        {
            return new Rect((float)((double)this.position.width - 250.0 - 8.0 - 16.0), 24f, 250f, 40f);
        }
    }

    private bool multipleSprites
    {
        get
        {
            if ((UnityEngine.Object)this.m_TextureImporter != (UnityEngine.Object)null)
                return this.m_TextureImporter.spriteImportMode == SpriteImportMode.Multiple;
            return false;
        }
    }

    private bool validSprite
    {
        get
        {
            if ((UnityEngine.Object)this.m_TextureImporter != (UnityEngine.Object)null)
                return this.m_TextureImporter.spriteImportMode != SpriteImportMode.None;
            return false;
        }
    }

    private bool activeTextureSelected
    {
        get
        {
            if ((UnityEngine.Object)this.m_TextureImporter != (UnityEngine.Object)null && (UnityEngine.Object)this.m_Texture != (UnityEngine.Object)null)
                return (UnityEngine.Object)this.m_OriginalTexture != (UnityEngine.Object)null;
            return false;
        }
    }

    public bool textureIsDirty
    {
        get
        {
            return this.m_TextureIsDirty;
        }
        set
        {
            this.m_TextureIsDirty = value;
        }
    }

    public bool selectedTextureChanged
    {
        get
        {
            Texture2D selectedTexture2D = this.GetSelectedTexture2D();
            if ((UnityEngine.Object)selectedTexture2D != (UnityEngine.Object)null)
                return (UnityEngine.Object)this.m_OriginalTexture != (UnityEngine.Object)selectedTexture2D;
            return false;
        }
    }

    private bool polygonSprite
    {
        get
        {
            if ((UnityEngine.Object)this.m_TextureImporter != (UnityEngine.Object)null)
                return this.m_TextureImporter.spriteImportMode == SpriteImportMode.Polygon;
            return false;
        }
    }

    private bool isSidesValid
    {
        get
        {
            if (this.m_PolygonSides == 0)
                return true;
            if (this.m_PolygonSides >= 3)
                return this.m_PolygonSides <= 128;
            return false;
        }
    }

    public static void GetWindow()
    {
        EditorWindow.GetWindow<UIEditorWindow>();
    }

    private void ModifierKeysChanged()
    {
        if (!((UnityEngine.Object)EditorWindow.focusedWindow == (UnityEngine.Object)this))
            return;
        this.Repaint();
    }

    public static void TextureImporterApply(SerializedObject so)
    {
        if (s_Instance == null)
            return;
        s_Instance.ApplyCacheSettingsToInspector(so);
    }

    private void ApplyCacheSettingsToInspector(SerializedObject so)
    {
        if (this.m_TextureImporterSO == null || !(this.m_TextureImporterSO.targetObject == so.targetObject))
            return;
        if (so.FindProperty("m_SpriteMode").intValue == this.m_TextureImporterSO.FindProperty("m_SpriteMode").intValue)
        {
            s_Instance.m_IgnoreNextPostprocessEvent = true;
        }
        else
        {
            if (!this.textureIsDirty || !EditorUtility.DisplayDialog(UIEditorWindowStyles.spriteEditorWindowTitle.text, UIEditorWindowStyles.pendingChangesDialogContent.text, UIEditorWindowStyles.yesButtonLabel.text, UIEditorWindowStyles.noButtonLabel.text))
                return;
            this.DoApply(so);
        }
    }

    public void RefreshPropertiesCache()
    {
        this.m_OriginalTexture = this.GetSelectedTexture2D();
        if ((UnityEngine.Object)this.m_OriginalTexture == (UnityEngine.Object)null)
            return;
        this.m_TextureImporter = AssetImporter.GetAtPath(this.m_SelectedAssetPath) as TextureImporter;
        if ((UnityEngine.Object)this.m_TextureImporter == (UnityEngine.Object)null)
            return;
        this.m_TextureImporterSO = new SerializedObject((UnityEngine.Object)this.m_TextureImporter);
        this.m_TextureImporterSprites = this.m_TextureImporterSO.FindProperty("m_SpriteSheet.m_Sprites");
        this.m_SpriteSheetOutline = this.m_TextureImporterSO.FindProperty("m_SpriteSheet.m_Outline");
        if ((UnityEngine.Object)this.m_RectsCache != (UnityEngine.Object)null)
            this.selected = this.m_TextureImporterSprites.arraySize <= 0 ? null : this.m_RectsCache.RectAt(0);
        int width = 0;
        int height = 0;
        //this.m_TextureImporter.GetWidthAndHeight(ref width, ref height);
        TextureImporterWrap.GetWidthAndHeight(m_TextureImporter,ref width, ref height);
        this.m_Texture = this.CreateTemporaryDuplicate(AssetDatabase.LoadMainAssetAtPath(this.m_TextureImporter.assetPath) as Texture2D, width, height);
        if ((UnityEngine.Object)this.m_Texture == (UnityEngine.Object)null)
            return;
        this.m_Texture.filterMode = UnityEngine.FilterMode.Point;
    }

    public void InvalidatePropertiesCache()
    {
        if ((bool)((UnityEngine.Object)this.m_RectsCache))
        {
            this.m_RectsCache.ClearAll();
            UnityEngine.Object.DestroyImmediate((UnityEngine.Object)this.m_RectsCache);
        }
        if ((bool)((UnityEngine.Object)this.m_Texture))
            UnityEngine.Object.DestroyImmediate((UnityEngine.Object)this.m_Texture);
        this.m_OriginalTexture = (Texture2D)null;
        this.m_TextureImporter = (TextureImporter)null;
        this.m_TextureImporterSO = (SerializedObject)null;
        this.m_TextureImporterSprites = (SerializedProperty)null;
        s_AlphaPixelCache = (bool[])null;
    }

    private void InitializeAnimVariables()
    {

    }

    private void DeterminePolygonSides()
    {
        if (this.selected != null && this.selected.m_Outline != null && this.selected.m_Outline.Count == 1)
            this.m_PolygonSides = this.selected.m_Outline[0].Count;
        else
            this.m_PolygonSides = 0;
    }

    private static void AcquireOutline(SerializedProperty outlineSP, SpriteRect spriteRect)
    {
        for (int index1 = 0; index1 < outlineSP.arraySize; ++index1)
        {
            List<Vector2> list = new List<Vector2>();
            SerializedProperty arrayElementAtIndex = outlineSP.GetArrayElementAtIndex(index1);
            for (int index2 = 0; index2 < arrayElementAtIndex.arraySize; ++index2)
            {
                Vector2 vector2Value = arrayElementAtIndex.GetArrayElementAtIndex(index2).vector2Value;
                list.Add(vector2Value);
            }
            spriteRect.m_Outline.Add(list);
        }
    }

    private static void ApplyOutlineChanges(SerializedProperty outlineSP, SpriteRect spriteRect)
    {
        outlineSP.ClearArray();
        for (int index1 = 0; index1 < spriteRect.m_Outline.Count; ++index1)
        {
            outlineSP.InsertArrayElementAtIndex(index1);
            SerializedProperty arrayElementAtIndex = outlineSP.GetArrayElementAtIndex(index1);
            arrayElementAtIndex.ClearArray();
            List<Vector2> list = spriteRect.m_Outline[index1];
            for (int index2 = 0; index2 < list.Count; ++index2)
            {
                arrayElementAtIndex.InsertArrayElementAtIndex(index2);
                arrayElementAtIndex.GetArrayElementAtIndex(index2).vector2Value = list[index2];
            }
        }
    }

    public bool IsEditingDisabled()
    {
        return EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private void OnSelectionChange()
    {
        if ((UnityEngine.Object)this.GetSelectedTexture2D() == (UnityEngine.Object)null || this.selectedTextureChanged)
            this.HandleApplyRevertDialog();
        this.InvalidatePropertiesCache();
        this.Reset();
        this.UpdateSelectedSprite();
        this.Repaint();
    }

    public void Reset()
    {
        this.InvalidatePropertiesCache();
        this.selected = (SpriteRect)null;
        this.textureIsDirty = false;
        this.m_Zoom = -1f;
        this.RefreshPropertiesCache();
        this.RefreshRects();
        this.m_ShowPolygonChangeShapeWindow = this.polygonSprite;
        if (this.m_ShowPolygonChangeShapeWindow)
            this.DeterminePolygonSides();
        this.Repaint();
    }

    private void OnEnable()
    {
        this.minSize = new Vector2(360f, 200f);
        this.titleContent = UIEditorWindowStyles.spriteEditorWindowTitle;
        s_Instance = this;
        Undo.undoRedoPerformed += new Undo.UndoRedoCallback(this.UndoRedoPerformed);
        EditorApplication.modifierKeysChanged -= new EditorApplication.CallbackFunction(this.ModifierKeysChanged);
        EditorApplication.modifierKeysChanged += new EditorApplication.CallbackFunction(this.ModifierKeysChanged);
        this.Reset();
    }

    private void UndoRedoPerformed()
    {
        Texture2D selectedTexture2D = this.GetSelectedTexture2D();
        if ((UnityEngine.Object)selectedTexture2D != (UnityEngine.Object)null && (UnityEngine.Object)this.m_OriginalTexture != (UnityEngine.Object)selectedTexture2D)
            this.OnSelectionChange();
        if ((UnityEngine.Object)this.m_RectsCache != (UnityEngine.Object)null && !this.m_RectsCache.Contains(this.selected))
            this.selected = (SpriteRect)null;
        this.Repaint();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= new Undo.UndoRedoCallback(this.UndoRedoPerformed);
        if ((UnityEngine.Object)this.m_RectsCache != (UnityEngine.Object)null)
            Undo.ClearUndo((UnityEngine.Object)this.m_RectsCache);
        this.HandleApplyRevertDialog();
        this.InvalidatePropertiesCache();
        EditorApplication.modifierKeysChanged -= new EditorApplication.CallbackFunction(this.ModifierKeysChanged);
        s_Instance = null;
    }

    private void HandleApplyRevertDialog()
    {
        if (!this.textureIsDirty || !((UnityEngine.Object)this.m_TextureImporter != (UnityEngine.Object)null))
            return;
        if (EditorUtility.DisplayDialog(UIEditorWindowStyles.applyRevertDialogTitle.text, string.Format(UIEditorWindowStyles.applyRevertDialogContent.text, (object)this.m_TextureImporter.assetPath), UIEditorWindowStyles.applyButtonLabel.text, UIEditorWindowStyles.revertButtonLabel.text))
            this.DoApply();
        else
            this.DoRevert();
    }

    private void RefreshRects()
    {
        if (this.m_TextureImporterSprites == null)
            return;
        if ((bool)((UnityEngine.Object)this.m_RectsCache))
        {
            this.m_RectsCache.ClearAll();
            Undo.ClearUndo((UnityEngine.Object)this.m_RectsCache);
            UnityEngine.Object.DestroyImmediate((UnityEngine.Object)this.m_RectsCache);
        }
        this.m_RectsCache = ScriptableObject.CreateInstance<SpriteRectCache>();
        if (this.multipleSprites)
        {
            for (int index = 0; index < this.m_TextureImporterSprites.arraySize; ++index)
            {
                SpriteRect spriteRect = new SpriteRect();
                spriteRect.m_Rect = this.m_TextureImporterSprites.GetArrayElementAtIndex(index).FindPropertyRelative("m_Rect").rectValue;
                spriteRect.m_Name = this.m_TextureImporterSprites.GetArrayElementAtIndex(index).FindPropertyRelative("m_Name").stringValue;
                spriteRect.m_Alignment = (SpriteAlignment)this.m_TextureImporterSprites.GetArrayElementAtIndex(index).FindPropertyRelative("m_Alignment").intValue;
                spriteRect.m_Border = this.m_TextureImporterSprites.GetArrayElementAtIndex(index).FindPropertyRelative("m_Border").vector4Value;
                spriteRect.m_Pivot = SpriteEditorUtilityWrap.GetPivotValue(spriteRect.m_Alignment, this.m_TextureImporterSprites.GetArrayElementAtIndex(index).FindPropertyRelative("m_Pivot").vector2Value);
                spriteRect.m_TessellationDetail = this.m_TextureImporterSprites.GetArrayElementAtIndex(index).FindPropertyRelative("m_TessellationDetail").floatValue;
                AcquireOutline(this.m_TextureImporterSprites.GetArrayElementAtIndex(index).FindPropertyRelative("m_Outline"), spriteRect);
                this.m_RectsCache.AddRect(spriteRect);
            }
        }
        else if (this.validSprite)
        {
            SpriteRect spriteRect = new SpriteRect();
            spriteRect.m_Rect = new Rect(0.0f, 0.0f, (float)this.m_Texture.width, (float)this.m_Texture.height);
            spriteRect.m_Name = this.m_OriginalTexture.name;
            spriteRect.m_Alignment = (SpriteAlignment)this.m_TextureImporterSO.FindProperty("m_Alignment").intValue;
            spriteRect.m_Border = this.m_TextureImporter.spriteBorder;
            spriteRect.m_Pivot = SpriteEditorUtilityWrap.GetPivotValue(spriteRect.m_Alignment, this.m_TextureImporter.spritePivot);
            spriteRect.m_TessellationDetail = this.m_TextureImporterSO.FindProperty("m_SpriteTessellationDetail").floatValue;
            AcquireOutline(this.m_SpriteSheetOutline, spriteRect);
            this.m_RectsCache.AddRect(spriteRect);
        }
        if (this.m_RectsCache.Count <= 0)
            return;
        this.selected = this.m_RectsCache.RectAt(0);
    }

   // HorizontalSplitLine m_HSL = new HorizontalSplitLine(100,100);

    private void OnGUI()
    {
        if (m_ResetOnNextRepaint || selectedTextureChanged)
        {
            Reset();
            m_ResetOnNextRepaint = false;
        }

        Matrix4x4 matrix = Handles.matrix;
        if (!activeTextureSelected)
        {
            using (new EditorGUI.DisabledScope(true))
                GUILayout.Label(Styles.s_NoSelectionWarning);
        }
        else
        {
            InitStyles();

           // m_HSL.ResizeHandling(16, position.width, position.height);
            //m_HSL.OnGUI(0,16, position.height);

            Rect rect = EditorGUILayout.BeginHorizontal("Toolbar");
            DoToolbarGUI();
            GUILayout.FlexibleSpace();
            DoApplyRevertGUI();
            DoAlphaZoomToolbarGUI();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            m_TextureViewRect = new Rect(0.0f, rect.yMax, position.width - 16f, position.height - 16f - rect.height);
            GUILayout.FlexibleSpace();
            DoTextureGUI();
            EditorGUILayout.EndHorizontal();
            DoPolygonChangeShapeWindow();
            DoEditingDisabledMessage();
            DoSelectedFrameInspector();
            Handles.matrix = matrix;
        }
    }

    protected override void DoTextureGUIExtras()
    {
        if (this.IsEditingDisabled())
            return;
        this.HandleGizmoMode();
        if (this.multipleSprites)
            this.HandleRectCornerScalingHandles();
        this.HandleBorderCornerScalingHandles();
        this.HandleBorderSidePointScalingSliders();
        if (this.multipleSprites)
            this.HandleRectSideScalingHandles();
        this.HandleBorderSideScalingHandles();
        this.HandlePivotHandle();
        if (this.multipleSprites)
            this.HandleDragging();
        this.HandleSelection();
        this.HandleFrameSelected();
        if (!this.multipleSprites)
            return;
        this.HandleCreate();
        this.HandleDelete();
        this.HandleDuplicate();
    }

    private void HandleGizmoMode()
    {
        this.m_GizmoMode = !Event.current.control ? GizmoMode.RectEditing : GizmoMode.BorderEditing;
        Event current = Event.current;
        if (current.type != EventType.KeyDown && current.type != EventType.KeyUp || current.keyCode != KeyCode.LeftControl && current.keyCode != KeyCode.RightControl && (current.keyCode != KeyCode.LeftAlt && current.keyCode != KeyCode.RightAlt))
            return;
        this.Repaint();
    }

    private void DoToolbarGUI()
    {
        if (polygonSprite)
        {
            using (new EditorGUI.DisabledScope(IsEditingDisabled()))
                m_ShowPolygonChangeShapeWindow = GUILayout.Toggle(m_ShowPolygonChangeShapeWindow, UIEditorWindowStyles.changeShapeLabel, EditorStyles.toolbarButton);
        }
        else
        {
            using (new EditorGUI.DisabledScope(!multipleSprites || IsEditingDisabled()))
            {
                Rect buttonRect = EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(UIEditorWindowStyles.sliceButtonLabel, "toolbarPopup"))
                {
                    UIEditorMenu.s_SpriteEditor = this;
                    if (UIEditorMenu.ShowAtPosition(buttonRect))
                        GUIUtility.ExitGUI();
                }
                using (new EditorGUI.DisabledScope(selected == null))
                {
                    if (!GUILayout.Button(UIEditorWindowStyles.trimButtonLabel, EditorStyles.toolbarButton))
                    {
                        if (string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
                        {
                            if (!k_SpriteEditorTrim.activated)
                                goto label_19;
                        }
                        else
                            goto label_19;
                    }
                    Rect rect = TrimAlpha(selected.m_Rect);
                    if (rect.width <= 0.0 && rect.height <= 0.0)
                    {
                        m_RectsCache.RemoveRect(selected);
                        selected = null;
                    }
                    else
                    {
                        rect = ClampSpriteRect(rect);
                        if (selected.m_Rect != rect)
                            textureIsDirty = true;
                        selected.m_Rect = rect;
                    }
                    Repaint();
                }
                label_19:
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void DoPolygonChangeShapeWindow()
    {
        if (!this.m_ShowPolygonChangeShapeWindow)
            return;
        bool flag = false;
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 45f;
        GUILayout.BeginArea(this.m_PolygonChangeShapeWindowRect);
        GUILayout.BeginVertical(GUI.skin.box, new GUILayoutOption[0]);
        Event current = Event.current;
        if (this.isSidesValid && current.type == EventType.KeyDown && current.keyCode == KeyCode.Return)
        {
            flag = true;
            current.Use();
        }
        EditorGUI.FocusTextInControl("PolygonSidesInput");
        GUI.SetNextControlName("PolygonSidesInput");
        EditorGUI.BeginChangeCheck();
        this.m_PolygonSides = EditorGUILayout.IntField(UIEditorWindowStyles.sidesLabel, this.m_PolygonSides, new GUILayoutOption[0]);
        if (EditorGUI.EndChangeCheck())
            this.m_PolygonChangeShapeWindowRect.height = this.isSidesValid ? 45f : 65f;
        GUILayout.FlexibleSpace();
        if (!this.isSidesValid)
        {
            EditorGUILayout.HelpBox(UIEditorWindowStyles.polygonChangeShapeHelpBoxContent.text, MessageType.Warning, true);
        }
        else
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!this.isSidesValid))
            {
                if (GUILayout.Button(UIEditorWindowStyles.changeButtonLabel))
                    flag = true;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        if (flag)
        {
            if (this.isSidesValid)
                this.GeneratePolygonOutline(this.m_PolygonSides);
            this.m_ShowPolygonChangeShapeWindow = false;
        }
        EditorGUIUtility.labelWidth = labelWidth;
        GUILayout.EndArea();
    }

    private void FourIntFields(GUIContent label, GUIContent labelX, GUIContent labelY, GUIContent labelZ, GUIContent labelW, ref int x, ref int y, ref int z, ref int w)
    {
        Rect rect = GUILayoutUtility.GetRect(322f, 322f, 32f, 32f);
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
        GUI.SetNextControlName("FourIntFields_x");
        x = EditorGUI.IntField(position2, labelX, x);
        position2.x += position2.width + 3f;
        GUI.SetNextControlName("FourIntFields_y");
        y = EditorGUI.IntField(position2, labelY, y);
        position2.y += 16f;
        position2.x -= position2.width + 3f;
        GUI.SetNextControlName("FourIntFields_z");
        z = EditorGUI.IntField(position2, labelZ, z);
        position2.x += position2.width + 3f;
        GUI.SetNextControlName("FourIntFields_w");
        w = EditorGUI.IntField(position2, labelW, w);
        EditorGUIUtility.labelWidth = 135f;
    }

    private void DoEditingDisabledMessage()
    {
        if (!this.IsEditingDisabled())
            return;
        GUILayout.BeginArea(this.warningMessageRect);
        EditorGUILayout.HelpBox(UIEditorWindowStyles.editingDiableMessageLabel.text, MessageType.Warning);
        GUILayout.EndArea();
    }

    private void DoSelectedFrameInspector()
    {
        if (this.selected == null)
            return;
        EditorGUIUtility.wideMode = true;
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 135f;
        GUILayout.BeginArea(this.inspectorRect);
        GUILayout.BeginVertical(UIEditorWindowStyles.spriteLabel, GUI.skin.window, new GUILayoutOption[0]);
        using (new EditorGUI.DisabledScope(!this.multipleSprites))
        {
            this.DoNameField();
            this.DoPositionField();
        }
        this.DoBorderFields();
        this.DoPivotFields();
        GUILayout.EndVertical();
        GUILayout.EndArea();
        EditorGUIUtility.labelWidth = labelWidth;
    }

    private void DoPivotFields()
    {
        EditorGUI.BeginChangeCheck();
        this.selected.m_Alignment = (SpriteAlignment)EditorGUILayout.Popup(Styles.s_PivotLabel, (int)this.selected.m_Alignment, Styles.spriteAlignmentOptions, new GUILayoutOption[0]);
        Vector2 vector2 = this.selected.m_Pivot;
        Vector2 customOffset = vector2;
        using (new EditorGUI.DisabledScope(this.selected.m_Alignment != SpriteAlignment.Custom))
        {
            Rect rect = GUILayoutUtility.GetRect(322f, 322f, 32f, 32f);
            GUI.SetNextControlName("PivotField");
            customOffset = EditorGUI.Vector2Field(rect, UIEditorWindowStyles.customPivotLabel, vector2);
        }
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Change Sprite Pivot");
        this.textureIsDirty = true;
        this.selected.m_Pivot = SpriteEditorUtilityWrap.GetPivotValue(this.selected.m_Alignment, customOffset);
    }

    private void DoBorderFields()
    {
        EditorGUI.BeginChangeCheck();
        Vector4 vector4 = this.ClampSpriteBorder(this.selected.m_Border);
        int x = Mathf.RoundToInt(vector4.x);
        int w = Mathf.RoundToInt(vector4.y);
        int z = Mathf.RoundToInt(vector4.z);
        int y = Mathf.RoundToInt(vector4.w);
        this.FourIntFields(UIEditorWindowStyles.borderLabel, UIEditorWindowStyles.lLabel, UIEditorWindowStyles.tLabel, UIEditorWindowStyles.rLabel, UIEditorWindowStyles.bLabel, ref x, ref y, ref z, ref w);
        Vector4 border = new Vector4((float)x, (float)w, (float)z, (float)y);
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Change Sprite Border");
        this.textureIsDirty = true;
        this.selected.m_Border = this.ClampSpriteBorder(border);
    }

    private void DoPositionField()
    {
        EditorGUI.BeginChangeCheck();
        Rect rect1 = this.selected.m_Rect;
        int x = Mathf.RoundToInt(rect1.x);
        int y = Mathf.RoundToInt(rect1.y);
        int z = Mathf.RoundToInt(rect1.width);
        int w = Mathf.RoundToInt(rect1.height);
        this.FourIntFields(UIEditorWindowStyles.positionLabel, UIEditorWindowStyles.xLabel, UIEditorWindowStyles.yLabel, UIEditorWindowStyles.wLabel, UIEditorWindowStyles.hLabel, ref x, ref y, ref z, ref w);
        Rect rect2 = new Rect((float)x, (float)y, (float)z, (float)w);
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Change Sprite Position");
        this.textureIsDirty = true;
        this.selected.m_Rect = this.ClampSpriteRect(rect2);
    }

    private void DoNameField()
    {
        EditorGUI.BeginChangeCheck();
        string text = this.selected.m_Name;
        GUI.SetNextControlName("SpriteName");
        string filename = EditorGUILayout.TextField(UIEditorWindowStyles.nameLabel, text, new GUILayoutOption[0]);
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Change Sprite Name");
        this.textureIsDirty = true;
        string str = InternalEditorUtility.RemoveInvalidCharsFromFileName(filename, true);
        if (string.IsNullOrEmpty(this.selected.m_OriginalName) && str != text)
            this.selected.m_OriginalName = text;
        if (string.IsNullOrEmpty(str))
            str = text;
        using (List<SpriteRect>.Enumerator enumerator = this.m_RectsCache.m_Rects.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.m_Name == str)
                {
                    str = this.selected.m_OriginalName;
                    break;
                }
            }
        }
        this.selected.m_Name = str;
    }

    private void DoApplyRevertGUI()
    {
        using (new EditorGUI.DisabledScope(!this.textureIsDirty))
        {
            if (GUILayout.Button(UIEditorWindowStyles.revertButtonLabel, EditorStyles.toolbarButton, new GUILayoutOption[0]))
                this.DoRevert();
            if (!GUILayout.Button(UIEditorWindowStyles.applyButtonLabel, EditorStyles.toolbarButton, new GUILayoutOption[0]))
                return;
            this.DoApply();
        }
    }

    private void DoApply(SerializedObject so)
    {
        if (this.multipleSprites)
        {
            List<string> list1 = new List<string>();
            List<string> list2 = new List<string>();
            SerializedProperty property = so.FindProperty("m_SpriteSheet.m_Sprites");
            property.ClearArray();
            for (int index = 0; index < this.m_RectsCache.Count; ++index)
            {
                SpriteRect spriteRect = this.m_RectsCache.RectAt(index);
                if (string.IsNullOrEmpty(spriteRect.m_Name))
                    spriteRect.m_Name = "Empty";
                if (!string.IsNullOrEmpty(spriteRect.m_OriginalName))
                {
                    list1.Add(spriteRect.m_OriginalName);
                    list2.Add(spriteRect.m_Name);
                }
                property.InsertArrayElementAtIndex(index);
                SerializedProperty arrayElementAtIndex = property.GetArrayElementAtIndex(index);
                arrayElementAtIndex.FindPropertyRelative("m_Rect").rectValue = spriteRect.m_Rect;
                arrayElementAtIndex.FindPropertyRelative("m_Border").vector4Value = spriteRect.m_Border;
                arrayElementAtIndex.FindPropertyRelative("m_Name").stringValue = spriteRect.m_Name;
                arrayElementAtIndex.FindPropertyRelative("m_Alignment").intValue = (int)spriteRect.m_Alignment;
                arrayElementAtIndex.FindPropertyRelative("m_Pivot").vector2Value = spriteRect.m_Pivot;
                arrayElementAtIndex.FindPropertyRelative("m_TessellationDetail").floatValue = spriteRect.m_TessellationDetail;
                ApplyOutlineChanges(arrayElementAtIndex.FindPropertyRelative("m_Outline"), spriteRect);
            }
            if (list1.Count <= 0)
                return;
            PatchImportSettingRecycleIDWrap.PatchMultiple(so, 213, list1.ToArray(), list2.ToArray());
        }
        else
        {
            if (this.m_RectsCache.Count <= 0)
                return;
            SpriteRect spriteRect = this.m_RectsCache.RectAt(0);
            so.FindProperty("m_Alignment").intValue = (int)spriteRect.m_Alignment;
            so.FindProperty("m_SpriteBorder").vector4Value = spriteRect.m_Border;
            so.FindProperty("m_SpritePivot").vector2Value = spriteRect.m_Pivot;
            so.FindProperty("m_SpriteTessellationDetail").floatValue = spriteRect.m_TessellationDetail;
            this.m_SpriteSheetOutline.ClearArray();
            ApplyOutlineChanges(this.m_SpriteSheetOutline, spriteRect);
        }
    }

    private void DoApply()
    {
        Undo.ClearUndo((UnityEngine.Object)this.m_RectsCache);
        this.DoApply(this.m_TextureImporterSO);
        this.m_TextureImporterSO.ApplyModifiedPropertiesWithoutUndo();
        this.m_IgnoreNextPostprocessEvent = true;
        this.DoTextureReimport(this.m_TextureImporter.assetPath);
        this.textureIsDirty = false;
        this.selected = (SpriteRect)null;
    }

    private void DoRevert()
    {
        this.m_TextureIsDirty = false;
        this.selected = (SpriteRect)null;
        this.RefreshRects();
        GUI.FocusControl(string.Empty);
    }

    private void HandleDuplicate()
    {
        if (Event.current.type != EventType.ValidateCommand && Event.current.type != EventType.ExecuteCommand || !(Event.current.commandName == "Duplicate"))
            return;
        if (Event.current.type == EventType.ExecuteCommand)
        {
            Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Duplicate sprite");
            this.selected = this.AddSprite(this.selected.m_Rect, (int)this.selected.m_Alignment, this.selected.m_Pivot, this.defaultColliderAlphaCutoff, this.defaultColliderDetail);
        }
        Event.current.Use();
    }

    private void HandleCreate()
    {
        if (this.MouseOnTopOfInspector() || Event.current.alt)
            return;
        EditorGUI.BeginChangeCheck();
        Rect rect = SpriteEditorHandlesWrap.RectCreator((float)this.m_Texture.width, (float)this.m_Texture.height, s_Styles.createRect);
        if (!EditorGUI.EndChangeCheck() || (double)rect.width <= 0.0 || (double)rect.height <= 0.0)
            return;
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Create sprite");
        this.selected = this.AddSprite(rect, 0, Vector2.zero, this.defaultColliderAlphaCutoff, this.defaultColliderDetail);
        GUIUtility.keyboardControl = 0;
    }

    private void HandleDelete()
    {
        if (Event.current.type != EventType.ValidateCommand && Event.current.type != EventType.ExecuteCommand || !(Event.current.commandName == "SoftDelete") && !(Event.current.commandName == "Delete"))
            return;
        if (Event.current.type == EventType.ExecuteCommand)
        {
            Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Delete sprite");
            this.m_RectsCache.RemoveRect(this.selected);
            this.selected = (SpriteRect)null;
            this.textureIsDirty = true;
        }
        Event.current.Use();
    }

    private void HandleDragging()
    {
        if (this.selected == null || this.MouseOnTopOfInspector())
            return;
        Rect clamp = new Rect(0.0f, 0.0f, (float)this.m_Texture.width, (float)this.m_Texture.height);
        EditorGUI.BeginChangeCheck();
        SpriteRect selected = this.selected;
        Rect rect = SpriteEditorUtilityWrap.ClampedRect(SpriteEditorUtilityWrap.RoundedRect(SpriteEditorHandlesWrap.SliderRect(this.selected.m_Rect)), clamp, true);
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Move sprite");
        selected.m_Rect = rect;
        this.textureIsDirty = true;
    }

    private void HandleSelection()
    {
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || (GUIUtility.hotControl != 0 || Event.current.alt) || this.MouseOnTopOfInspector())
            return;
        SpriteRect selected = this.selected;
        this.selected = this.TrySelect(Event.current.mousePosition);
        if (this.selected != null)
            s_OneClickDragStarted = true;
        else
            this.Repaint();
        if (selected == this.selected || this.selected == null)
            return;
        Event.current.Use();
    }

    private void HandleFrameSelected()
    {
        if (Event.current.type != EventType.ValidateCommand && Event.current.type != EventType.ExecuteCommand || !(Event.current.commandName == "FrameSelected"))
            return;
        if (Event.current.type == EventType.ExecuteCommand)
        {
            if (this.selected == null)
                return;
            Rect rect = this.selected.m_Rect;
            float num = this.m_Zoom;
            this.m_Zoom = (double)rect.width >= (double)rect.height ? this.m_TextureViewRect.width / (rect.width + this.m_TextureViewRect.width * 0.05f) : this.m_TextureViewRect.height / (rect.height + this.m_TextureViewRect.height * 0.05f);
            this.m_ScrollPosition.x = (rect.center.x - (float)this.m_Texture.width * 0.5f) * this.m_Zoom;
            this.m_ScrollPosition.y = (float)(((double)rect.center.y - (double)this.m_Texture.height * 0.5) * (double)this.m_Zoom * -1.0);
            this.Repaint();
        }
        Event.current.Use();
    }

    private bool ShouldShowRectScaling()
    {
        if (this.selected != null)
            return this.m_GizmoMode == GizmoMode.RectEditing;
        return false;
    }

    private void HandlePivotHandle()
    {
        if (this.selected == null)
            return;
        EditorGUI.BeginChangeCheck();
        SpriteRect selected = this.selected;
        selected.m_Pivot = this.ApplySpriteAlignmentToPivot(selected.m_Pivot, selected.m_Rect, selected.m_Alignment);
        Vector2 pivot = SpriteEditorHandlesWrap.PivotSlider(selected.m_Rect, selected.m_Pivot, s_Styles.pivotdot, s_Styles.pivotdotactive);
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Move sprite pivot");
        if (Event.current.control)
        {
            this.selected.m_Pivot = this.SnapPivot(pivot);
        }
        else
        {
            this.selected.m_Pivot = pivot;
            this.selected.m_Alignment = SpriteAlignment.Custom;
        }
        this.textureIsDirty = true;
    }

    private Rect ClampSpriteRect(Rect rect)
    {
        Rect rect1 = new Rect();
        rect1.xMin = Mathf.Clamp(rect.xMin, 0.0f, (float)(this.m_Texture.width - 1));
        rect1.yMin = Mathf.Clamp(rect.yMin, 0.0f, (float)(this.m_Texture.height - 1));
        rect1.xMax = Mathf.Clamp(rect.xMax, 1f, (float)this.m_Texture.width);
        rect1.yMax = Mathf.Clamp(rect.yMax, 1f, (float)this.m_Texture.height);
        if (Mathf.RoundToInt(rect1.width) == 0)
            rect1.width = 1f;
        if (Mathf.RoundToInt(rect1.height) == 0)
            rect1.height = 1f;
        return SpriteEditorUtilityWrap.RoundedRect(rect1);
    }

    private Rect FlipNegativeRect(Rect rect)
    {
        return new Rect()
        {
            xMin = Mathf.Min(rect.xMin, rect.xMax),
            yMin = Mathf.Min(rect.yMin, rect.yMax),
            xMax = Mathf.Max(rect.xMin, rect.xMax),
            yMax = Mathf.Max(rect.yMin, rect.yMax)
        };
    }

    private Vector4 ClampSpriteBorder(Vector4 border)
    {
        Rect rect = this.FlipNegativeRect(this.selected.m_Rect);
        float width = rect.width;
        float height = rect.height;
        return new Vector4()
        {
            x = (float)Mathf.RoundToInt(Mathf.Clamp(border.x, 0.0f, Mathf.Min(width - border.z, width))),
            z = (float)Mathf.RoundToInt(Mathf.Clamp(border.z, 0.0f, Mathf.Min(width - border.x, width))),
            y = (float)Mathf.RoundToInt(Mathf.Clamp(border.y, 0.0f, Mathf.Min(height - border.w, height))),
            w = (float)Mathf.RoundToInt(Mathf.Clamp(border.w, 0.0f, Mathf.Min(height - border.y, height)))
        };
    }

    private Vector2 SnapPivot(Vector2 pivot)
    {
        Rect rect = this.selected.m_Rect;
        Vector2 texturePos = new Vector2(rect.xMin + rect.width * pivot.x, rect.yMin + rect.height * pivot.y);
        Vector2[] snapPointsArray = this.GetSnapPointsArray(rect);
        SpriteAlignment spriteAlignment = SpriteAlignment.Custom;
        float num1 = float.MaxValue;
        for (int index = 0; index < snapPointsArray.Length; ++index)
        {
            float num2 = (texturePos - snapPointsArray[index]).magnitude * this.m_Zoom;
            if ((double)num2 < (double)num1)
            {
                spriteAlignment = (SpriteAlignment)index;
                num1 = num2;
            }
        }
        this.selected.m_Alignment = spriteAlignment;
        return this.ConvertFromTextureToNormalizedSpace(texturePos, rect);
    }

    public Vector2 ApplySpriteAlignmentToPivot(Vector2 pivot, Rect rect, SpriteAlignment alignment)
    {
        Vector2[] snapPointsArray = this.GetSnapPointsArray(rect);
        if (alignment != SpriteAlignment.Custom)
            return this.ConvertFromTextureToNormalizedSpace(snapPointsArray[(int)alignment], rect);
        return pivot;
    }

    private Vector2 ConvertFromTextureToNormalizedSpace(Vector2 texturePos, Rect rect)
    {
        return new Vector2((texturePos.x - rect.xMin) / rect.width, (texturePos.y - rect.yMin) / rect.height);
    }

    private Vector2[] GetSnapPointsArray(Rect rect)
    {
        Vector2[] vector2Array = new Vector2[9];
        vector2Array[1] = new Vector2(rect.xMin, rect.yMax);
        vector2Array[2] = new Vector2(rect.center.x, rect.yMax);
        vector2Array[3] = new Vector2(rect.xMax, rect.yMax);
        vector2Array[4] = new Vector2(rect.xMin, rect.center.y);
        vector2Array[0] = new Vector2(rect.center.x, rect.center.y);
        vector2Array[5] = new Vector2(rect.xMax, rect.center.y);
        vector2Array[6] = new Vector2(rect.xMin, rect.yMin);
        vector2Array[7] = new Vector2(rect.center.x, rect.yMin);
        vector2Array[8] = new Vector2(rect.xMax, rect.yMin);
        return vector2Array;
    }

    private void UpdateSelectedSprite()
    {
        if (Selection.activeObject is Sprite)
        {
            this.SelectSpriteIndex(Selection.activeObject as Sprite);
        }
        else
        {
            if (!((UnityEngine.Object)Selection.activeGameObject != (UnityEngine.Object)null) || !(bool)((UnityEngine.Object)Selection.activeGameObject.GetComponent<SpriteRenderer>()))
                return;
            this.SelectSpriteIndex(Selection.activeGameObject.GetComponent<SpriteRenderer>().sprite);
        }
    }

    private void SelectSpriteIndex(Sprite sprite)
    {
        if ((UnityEngine.Object)sprite == (UnityEngine.Object)null)
            return;
        this.selected = (SpriteRect)null;
        for (int i = 0; i < this.m_RectsCache.Count; ++i)
        {
            if (sprite.rect == this.m_RectsCache.RectAt(i).m_Rect)
            {
                this.selected = this.m_RectsCache.RectAt(i);
                break;
            }
        }
    }

    private Texture2D GetSelectedTexture2D()
    {
        Texture2D texture2D = (Texture2D)null;
        if (Selection.activeObject is Texture2D)
            texture2D = Selection.activeObject as Texture2D;
        else if (Selection.activeObject is Sprite)
            texture2D = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(Selection.activeObject as Sprite, false);
        else if ((bool)((UnityEngine.Object)Selection.activeGameObject) && (bool)((UnityEngine.Object)Selection.activeGameObject.GetComponent<SpriteRenderer>()) && (bool)((UnityEngine.Object)Selection.activeGameObject.GetComponent<SpriteRenderer>().sprite))
            texture2D = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(Selection.activeGameObject.GetComponent<SpriteRenderer>().sprite, false);
        if ((UnityEngine.Object)texture2D != (UnityEngine.Object)null)
            this.m_SelectedAssetPath = AssetDatabase.GetAssetPath((UnityEngine.Object)texture2D);
        return texture2D;
    }

    protected override void DrawGizmos()
    {
        SpriteEditorUtilityWrap.BeginLines(new Color(0.0f, 0.0f, 0.0f, 0.25f));
        for (int i = 0; i < this.m_RectsCache.Count; ++i)
        {
            Rect rect = this.m_RectsCache.RectAt(i).m_Rect;
            if (this.m_RectsCache.RectAt(i) != this.selected)
                SpriteEditorUtilityWrap.DrawBox(new Rect(rect.xMin + 1f / this.m_Zoom, rect.yMin + 1f / this.m_Zoom, rect.width, rect.height));
        }
        SpriteEditorUtilityWrap.EndLines();
        SpriteEditorUtilityWrap.BeginLines(new Color(1f, 1f, 1f, 0.5f));
        for (int i = 0; i < this.m_RectsCache.Count; ++i)
        {
            if (this.m_RectsCache.RectAt(i) != this.selected)
                SpriteEditorUtilityWrap.DrawBox(this.m_RectsCache.RectAt(i).m_Rect);
        }
        SpriteEditorUtilityWrap.EndLines();
        if (this.polygonSprite)
        {
            for (int i = 0; i < this.m_RectsCache.Count; ++i)
            {
                SpriteRect spriteRect = this.m_RectsCache.RectAt(i);
                Vector2 vector2 = spriteRect.m_Rect.size * 0.5f;
                if (spriteRect.m_Outline.Count > 0)
                {
                    SpriteEditorUtilityWrap.BeginLines(new Color(0.75f, 0.75f, 0.75f, 0.75f));
                    for (int index1 = 0; index1 < spriteRect.m_Outline.Count; ++index1)
                    {
                        int index2;
                        for (index2 = 0; index2 < spriteRect.m_Outline[index1].Count - 1; ++index2)
                            SpriteEditorUtilityWrap.DrawLine((Vector3)(spriteRect.m_Outline[index1][index2] + vector2), (Vector3)(spriteRect.m_Outline[index1][index2 + 1] + vector2));
                        SpriteEditorUtilityWrap.DrawLine((Vector3)(spriteRect.m_Outline[index1][index2] + vector2), (Vector3)(spriteRect.m_Outline[index1][0] + vector2));
                    }
                    SpriteEditorUtilityWrap.EndLines();
                }
            }
        }
        SpriteEditorUtilityWrap.BeginLines(new Color(0.0f, 1f, 0.0f, 0.7f));
        for (int i = 0; i < this.m_RectsCache.Count; ++i)
        {
            SpriteRect currentRect = this.m_RectsCache.RectAt(i);
            if (this.ShouldDrawBorders(currentRect))
            {
                Vector4 vector4 = currentRect.m_Border;
                Rect rect = currentRect.m_Rect;
                SpriteEditorUtilityWrap.DrawLine(new Vector3(rect.xMin + vector4.x, rect.yMin), new Vector3(rect.xMin + vector4.x, rect.yMax));
                SpriteEditorUtilityWrap.DrawLine(new Vector3(rect.xMax - vector4.z, rect.yMin), new Vector3(rect.xMax - vector4.z, rect.yMax));
                SpriteEditorUtilityWrap.DrawLine(new Vector3(rect.xMin, rect.yMin + vector4.y), new Vector3(rect.xMax, rect.yMin + vector4.y));
                SpriteEditorUtilityWrap.DrawLine(new Vector3(rect.xMin, rect.yMax - vector4.w), new Vector3(rect.xMax, rect.yMax - vector4.w));
            }
        }
        SpriteEditorUtilityWrap.EndLines();
        if (!this.ShouldShowRectScaling())
            return;
        Rect position = this.selected.m_Rect;
        SpriteEditorUtilityWrap.BeginLines(new Color(0.0f, 0.1f, 0.3f, 0.25f));
        SpriteEditorUtilityWrap.DrawBox(new Rect(position.xMin + 1f / this.m_Zoom, position.yMin + 1f / this.m_Zoom, position.width, position.height));
        SpriteEditorUtilityWrap.EndLines();
        SpriteEditorUtilityWrap.BeginLines(new Color(0.25f, 0.5f, 1f, 0.75f));
        SpriteEditorUtilityWrap.DrawBox(position);
        SpriteEditorUtilityWrap.EndLines();
    }

    private bool ShouldDrawBorders(SpriteRect currentRect)
    {
        if (!Mathf.Approximately(currentRect.m_Border.sqrMagnitude, 0.0f))
            return true;
        if (currentRect == this.selected)
            return this.m_GizmoMode == GizmoMode.BorderEditing;
        return false;
    }

    private SpriteRect TrySelect(Vector2 mousePosition)
    {
        float num1 = 1E+07f;
        SpriteRect spriteRect = (SpriteRect)null;
        for (int i = 0; i < this.m_RectsCache.Count; ++i)
        {
            if (this.m_RectsCache.RectAt(i).m_Rect.Contains(HandlesWrap.s_InverseMatrix.MultiplyPoint((Vector3)mousePosition)))
            {
                if (this.m_RectsCache.RectAt(i) == this.selected)
                    return this.m_RectsCache.RectAt(i);
                float width = this.m_RectsCache.RectAt(i).m_Rect.width;
                float height = this.m_RectsCache.RectAt(i).m_Rect.height;
                float num2 = width * height;
                if ((double)width > 0.0 && (double)height > 0.0 && (double)num2 < (double)num1)
                {
                    spriteRect = this.m_RectsCache.RectAt(i);
                    num1 = num2;
                }
            }
        }
        return spriteRect;
    }

    public SpriteRect AddSprite(Rect rect, int alignment, Vector2 pivot, int colliderAlphaCutoff, float colliderDetail)
    {
        SpriteRect r = new SpriteRect();
        r.m_Rect = rect;
        r.m_Alignment = (SpriteAlignment)alignment;
        r.m_Pivot = pivot;
        string withoutExtension = Path.GetFileNameWithoutExtension(this.m_TextureImporter.assetPath);
        r.m_Name = this.GetUniqueName(withoutExtension);
        r.m_OriginalName = r.m_Name;
        this.textureIsDirty = true;
        this.m_RectsCache.AddRect(r);
        return r;
    }

    private string GetUniqueName(string prefix)
    {
        int num = 0;
        string str;
        bool flag;
        do
        {
            str = prefix + "_" + num++;
            flag = false;
            using (List<SpriteRect>.Enumerator enumerator = this.m_RectsCache.m_Rects.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.m_Name == str)
                        flag = true;
                }
            }
        }
        while (flag);
        return str;
    }

    private Rect TrimAlpha(Rect rect)
    {
        int a1 = (int)rect.xMax;
        int a2 = (int)rect.xMin;
        int a3 = (int)rect.yMax;
        int a4 = (int)rect.yMin;
        for (int index1 = (int)rect.yMin; index1 < (int)rect.yMax; ++index1)
        {
            for (int index2 = (int)rect.xMin; index2 < (int)rect.xMax; ++index2)
            {
                if (this.PixelHasAlpha(index2, index1))
                {
                    a1 = Mathf.Min(a1, index2);
                    a2 = Mathf.Max(a2, index2);
                    a3 = Mathf.Min(a3, index1);
                    a4 = Mathf.Max(a4, index1);
                }
            }
        }
        if (a1 > a2 || a3 > a4)
            return new Rect(0.0f, 0.0f, 0.0f, 0.0f);
        return new Rect((float)a1, (float)a3, (float)(a2 - a1 + 1), (float)(a4 - a3 + 1));
    }

    public void DoTextureReimport(string path)
    {
        if (this.m_TextureImporterSO == null)
            return;
        try
        {
            AssetDatabase.StartAssetEditing();
            AssetDatabase.ImportAsset(path);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        this.textureIsDirty = false;
    }

    private void HandleRectCornerScalingHandles()
    {
        if (this.selected == null)
            return;
        GUIStyle dragDot = s_Styles.dragdot;
        GUIStyle dragDotActive = s_Styles.dragdotactive;
        Color white = Color.white;
        Rect rect = new Rect(this.selected.m_Rect);
        float xMin = rect.xMin;
        float xMax = rect.xMax;
        float yMax = rect.yMax;
        float yMin = rect.yMin;
        EditorGUI.BeginChangeCheck();
        this.HandleBorderPointSlider(ref xMin, ref yMax, MouseCursor.ResizeUpLeft, false, dragDot, dragDotActive, white);
        this.HandleBorderPointSlider(ref xMax, ref yMax, MouseCursor.ResizeUpRight, false, dragDot, dragDotActive, white);
        this.HandleBorderPointSlider(ref xMin, ref yMin, MouseCursor.ResizeUpRight, false, dragDot, dragDotActive, white);
        this.HandleBorderPointSlider(ref xMax, ref yMin, MouseCursor.ResizeUpLeft, false, dragDot, dragDotActive, white);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Scale sprite");
            rect.xMin = xMin;
            rect.xMax = xMax;
            rect.yMax = yMax;
            rect.yMin = yMin;
            this.selected.m_Rect = this.ClampSpriteRect(rect);
            this.selected.m_Border = this.ClampSpriteBorder(this.selected.m_Border);
            this.textureIsDirty = true;
        }
        if (GUIUtility.hotControl != 0)
            return;
        this.selected.m_Rect = this.FlipNegativeRect(this.selected.m_Rect);
        this.selected.m_Border = this.ClampSpriteBorder(this.selected.m_Border);
    }

    private void HandleRectSideScalingHandles()
    {
        if (this.selected == null)
            return;
        Rect rect = new Rect(this.selected.m_Rect);
        float xMin = rect.xMin;
        float xMax = rect.xMax;
        float yMax = rect.yMax;
        float yMin = rect.yMin;
        Vector2 vector2_1 = (Vector2)Handles.matrix.MultiplyPoint(new Vector3(rect.xMin, rect.yMin));
        Vector2 vector2_2 = (Vector2)Handles.matrix.MultiplyPoint(new Vector3(rect.xMax, rect.yMax));
        float width = Mathf.Abs(vector2_2.x - vector2_1.x);
        float height = Mathf.Abs(vector2_2.y - vector2_1.y);
        EditorGUI.BeginChangeCheck();
        float num1 = this.HandleBorderScaleSlider(xMin, rect.yMax, width, height, true);
        float num2 = this.HandleBorderScaleSlider(xMax, rect.yMax, width, height, true);
        float num3 = this.HandleBorderScaleSlider(rect.xMin, yMax, width, height, false);
        float num4 = this.HandleBorderScaleSlider(rect.xMin, yMin, width, height, false);
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Scale sprite");
        rect.xMin = num1;
        rect.xMax = num2;
        rect.yMax = num3;
        rect.yMin = num4;
        this.selected.m_Rect = this.ClampSpriteRect(rect);
        this.selected.m_Border = this.ClampSpriteBorder(this.selected.m_Border);
        this.textureIsDirty = true;
    }

    private void HandleBorderSidePointScalingSliders()
    {
        if (this.selected == null)
            return;
        GUIStyle dragDot = s_Styles.dragBorderdot;
        GUIStyle dragDotActive = s_Styles.dragBorderDotActive;
        Color color = new Color(0.0f, 1f, 0.0f);
        Rect rect = this.selected.m_Rect;
        Vector4 border = this.selected.m_Border;
        float x1 = rect.xMin + border.x;
        float x2 = rect.xMax - border.z;
        float y1 = rect.yMax - border.w;
        float y2 = rect.yMin + border.y;
        EditorGUI.BeginChangeCheck();
        float num1 = y2 - (float)(((double)y2 - (double)y1) / 2.0);
        float num2 = x1 - (float)(((double)x1 - (double)x2) / 2.0);
        float y3 = num1;
        this.HandleBorderPointSlider(ref x1, ref y3, MouseCursor.ResizeHorizontal, false, dragDot, dragDotActive, color);
        float y4 = num1;
        this.HandleBorderPointSlider(ref x2, ref y4, MouseCursor.ResizeHorizontal, false, dragDot, dragDotActive, color);
        float x3 = num2;
        this.HandleBorderPointSlider(ref x3, ref y1, MouseCursor.ResizeVertical, false, dragDot, dragDotActive, color);
        x3 = num2;
        this.HandleBorderPointSlider(ref x3, ref y2, MouseCursor.ResizeVertical, false, dragDot, dragDotActive, color);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Scale sprite border");
            border.x = x1 - rect.xMin;
            border.z = rect.xMax - x2;
            border.w = rect.yMax - y1;
            border.y = y2 - rect.yMin;
            this.textureIsDirty = true;
        }
        this.selected.m_Border = this.ClampSpriteBorder(border);
    }

    private void HandleBorderCornerScalingHandles()
    {
        if (this.selected == null)
            return;
        GUIStyle dragDot = s_Styles.dragBorderdot;
        GUIStyle dragDotActive = s_Styles.dragBorderDotActive;
        Color color = new Color(0.0f, 1f, 0.0f);
        Rect rect = new Rect(this.selected.m_Rect);
        Vector4 border = this.selected.m_Border;
        float x1 = rect.xMin + border.x;
        float x2 = rect.xMax - border.z;
        float y1 = rect.yMax - border.w;
        float y2 = rect.yMin + border.y;
        EditorGUI.BeginChangeCheck();
        this.HandleBorderPointSlider(ref x1, ref y1, MouseCursor.ResizeUpLeft, (double)border.x < 1.0 && (double)border.w < 1.0, dragDot, dragDotActive, color);
        this.HandleBorderPointSlider(ref x2, ref y1, MouseCursor.ResizeUpRight, (double)border.z < 1.0 && (double)border.w < 1.0, dragDot, dragDotActive, color);
        this.HandleBorderPointSlider(ref x1, ref y2, MouseCursor.ResizeUpRight, (double)border.x < 1.0 && (double)border.y < 1.0, dragDot, dragDotActive, color);
        this.HandleBorderPointSlider(ref x2, ref y2, MouseCursor.ResizeUpLeft, (double)border.z < 1.0 && (double)border.y < 1.0, dragDot, dragDotActive, color);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Scale sprite border");
            border.x = x1 - rect.xMin;
            border.z = rect.xMax - x2;
            border.w = rect.yMax - y1;
            border.y = y2 - rect.yMin;
            this.textureIsDirty = true;
        }
        this.selected.m_Border = this.ClampSpriteBorder(border);
    }

    private void HandleBorderSideScalingHandles()
    {
        if (this.selected == null)
            return;
        Rect rect = new Rect(this.selected.m_Rect);
        Vector4 border = this.selected.m_Border;
        float x1 = rect.xMin + border.x;
        float x2 = rect.xMax - border.z;
        float y1 = rect.yMax - border.w;
        float y2 = rect.yMin + border.y;
        Vector2 vector2_1 = (Vector2)Handles.matrix.MultiplyPoint(new Vector3(rect.xMin, rect.yMin));
        Vector2 vector2_2 = (Vector2)Handles.matrix.MultiplyPoint(new Vector3(rect.xMax, rect.yMax));
        float width = Mathf.Abs(vector2_2.x - vector2_1.x);
        float height = Mathf.Abs(vector2_2.y - vector2_1.y);
        EditorGUI.BeginChangeCheck();
        float num1 = this.HandleBorderScaleSlider(x1, rect.yMax, width, height, true);
        float num2 = this.HandleBorderScaleSlider(x2, rect.yMax, width, height, true);
        float num3 = this.HandleBorderScaleSlider(rect.xMin, y1, width, height, false);
        float num4 = this.HandleBorderScaleSlider(rect.xMin, y2, width, height, false);
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Scale sprite border");
        border.x = num1 - rect.xMin;
        border.z = rect.xMax - num2;
        border.w = rect.yMax - num3;
        border.y = num4 - rect.yMin;
        this.selected.m_Border = this.ClampSpriteBorder(border);
        this.textureIsDirty = true;
    }

    private void HandleBorderPointSlider(ref float x, ref float y, MouseCursor mouseCursor, bool isHidden, GUIStyle dragDot, GUIStyle dragDotActive, Color color)
    {
        Color color1 = GUI.color;
        GUI.color = !isHidden ? color : new Color(0.0f, 0.0f, 0.0f, 0.0f);
        Vector2 vector2 = SpriteEditorHandlesWrap.PointSlider(new Vector2(x, y), mouseCursor, dragDot, dragDotActive);
        x = vector2.x;
        y = vector2.y;
        GUI.color = color1;
    }

    private float HandleBorderScaleSlider(float x, float y, float width, float height, bool isHorizontal)
    {
        float fixedWidth = s_Styles.dragBorderdot.fixedWidth;
        Vector2 pos = (Vector2)Handles.matrix.MultiplyPoint((Vector3)new Vector2(x, y));
        EditorGUI.BeginChangeCheck();
        float num;
        if (isHorizontal)
        {
            Rect cursorRect = new Rect(pos.x - fixedWidth * 0.5f, pos.y, fixedWidth, height);
            num = SpriteEditorHandlesWrap.ScaleSlider(pos, MouseCursor.ResizeHorizontal, cursorRect).x;
        }
        else
        {
            Rect cursorRect = new Rect(pos.x, pos.y - fixedWidth * 0.5f, width, fixedWidth);
            num = SpriteEditorHandlesWrap.ScaleSlider(pos, MouseCursor.ResizeVertical, cursorRect).y;
        }
        if (EditorGUI.EndChangeCheck())
            return num;
        if (isHorizontal)
            return x;
        return y;
    }

    public void DoAutomaticSlicing(int minimumSpriteSize, int alignment, Vector2 pivot, AutoSlicingMethod slicingMethod)
    {
        Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Automatic Slicing");
        if (slicingMethod == AutoSlicingMethod.DeleteAll)
            this.m_RectsCache.ClearAll();
        using (List<Rect>.Enumerator enumerator = this.SortRects(new List<Rect>((IEnumerable<Rect>)InternalSpriteUtility.GenerateAutomaticSpriteRectangles(this.m_Texture, minimumSpriteSize, 0))).GetEnumerator())
        {
            while (enumerator.MoveNext())
                this.AddSprite(enumerator.Current, alignment, pivot, slicingMethod);
        }
        this.selected = (SpriteRect)null;
        this.textureIsDirty = true;
        this.Repaint();
    }

    public void DoGridSlicing(Vector2 size, Vector2 offset, Vector2 padding, int alignment, Vector2 pivot)
    {
        Rect[] rectArray = InternalSpriteUtility.GenerateGridSpriteRectangles(this.m_Texture, offset, size, padding);
        bool flag = true;
        if (rectArray.Length > 1000 && !EditorUtility.DisplayDialog(UIEditorWindowStyles.creatingMultipleSpriteDialogTitle.text, string.Format(UIEditorWindowStyles.creatingMultipleSpriteDialogContent.text, (object)rectArray.Length), UIEditorWindowStyles.okButtonLabel.text, UIEditorWindowStyles.cancelButtonLabel.text))
            flag = false;
        if (flag)
        {
            Undo.RegisterCompleteObjectUndo((UnityEngine.Object)this.m_RectsCache, "Grid Slicing");
            this.m_RectsCache.ClearAll();
            foreach (Rect rect in rectArray)
                this.AddSprite(rect, alignment, pivot, this.defaultColliderAlphaCutoff, this.defaultColliderDetail);
            this.selected = (SpriteRect)null;
            this.textureIsDirty = true;
        }
        this.Repaint();
    }

    public void GeneratePolygonOutline(int sides)
    { 
    }

    private List<Rect> SortRects(List<Rect> rects)
    {
        List<Rect> list1 = new List<Rect>();
        while (rects.Count > 0)
        {
            Rect rect = rects[rects.Count - 1];
            Rect sweepRect = new Rect(0.0f, rect.yMin, m_Texture.width, rect.height);
            List<Rect> list2 = RectSweep(rects, sweepRect);
            if (list2.Count > 0)
            {
                list1.AddRange(list2);
            }
            else
            {
                list1.AddRange(rects);
                break;
            }
        }
        return list1;
    }

    private List<Rect> RectSweep(List<Rect> rects, Rect sweepRect)
    {
        if (rects == null || rects.Count == 0)
            return new List<Rect>();
        List<Rect> list = new List<Rect>();
        using (List<Rect>.Enumerator enumerator = rects.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                Rect current = enumerator.Current;
                if (this.Overlap(current, sweepRect))
                    list.Add(current);
            }
        }
        using (List<Rect>.Enumerator enumerator = list.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                Rect current = enumerator.Current;
                rects.Remove(current);
            }
        }
        list.Sort(((a, b) => a.x.CompareTo(b.x)));
        return list;
    }

    private void AddSprite(Rect frame, int alignment, Vector2 pivot, AutoSlicingMethod slicingMethod)
    {
        if (slicingMethod != AutoSlicingMethod.DeleteAll)
        {
            SpriteRect overlappingSprite = GetExistingOverlappingSprite(frame);
            if (overlappingSprite != null)
            {
                if (slicingMethod != AutoSlicingMethod.Smart)
                    return;
                overlappingSprite.m_Rect = frame;
                overlappingSprite.m_Alignment = (SpriteAlignment)alignment;
                overlappingSprite.m_Pivot = pivot;
            }
            else
                AddSprite(frame, alignment, pivot, defaultColliderAlphaCutoff, defaultColliderDetail);
        }
        else
            AddSprite(frame, alignment, pivot, defaultColliderAlphaCutoff, defaultColliderDetail);
    }

    private SpriteRect GetExistingOverlappingSprite(Rect rect)
    {
        for (int i = 0; i < m_RectsCache.Count; ++i)
        {
            if (Overlap(m_RectsCache.RectAt(i).m_Rect, rect))
                return m_RectsCache.RectAt(i);
        }
        return null;
    }

    private bool Overlap(Rect a, Rect b)
    {
        if (a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax)
            return a.yMax > b.yMin;
        return false;
    }

    private bool MouseOnTopOfInspector()
    {
        if (selected == null)
            return false;
        return inspectorRect.Contains(GUIClipWrap.Unclip(Event.current.mousePosition) + new Vector2(0.0f, -22f));
    }

    private bool PixelHasAlpha(int x, int y)
    {
        if (m_Texture == null)
            return false;

        if (s_AlphaPixelCache == null)
        {
            s_AlphaPixelCache = new bool[m_Texture.width * m_Texture.height];
            Color32[] pixels32 = m_Texture.GetPixels32();
            for (int index = 0; index < pixels32.Length; ++index)
                s_AlphaPixelCache[index] = pixels32[index].a != 0;
        }
        int index1 = y * m_Texture.width + x;
        return s_AlphaPixelCache[index1];
    }

    private Texture2D CreateTemporaryDuplicate(Texture2D original, int width, int height)
    {
        if (!ShaderUtil.hardwareSupportsRectRenderTexture || !original)
            return null;
        RenderTexture active = RenderTexture.active;
        bool flag1 = !TextureUtilWrap.GetLinearSampled(original);
        RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, !flag1 ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
        GL.sRGBWrite = flag1 && QualitySettings.activeColorSpace == ColorSpace.Linear;
        Graphics.Blit(original, temporary);
        GL.sRGBWrite = false;
        RenderTexture.active = temporary;
        bool flag2 = width >= SystemInfo.maxTextureSize || height >= SystemInfo.maxTextureSize;
        Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, original.mipmapCount > 1 || flag2);
        texture2D.ReadPixels(new Rect(0.0f, 0.0f, width, height), 0, 0);
        texture2D.Apply();
        RenderTexture.ReleaseTemporary(temporary);
        EditorGUIUtilityWrap.SetRenderTextureNoViewport(active);
        texture2D.alphaIsTransparency = original.alphaIsTransparency;
        return texture2D;
    }

    private enum GizmoMode
    {
        BorderEditing,
        RectEditing,
    }

    private class UIEditorWindowStyles
    {
        public static readonly GUIContent changeShapeLabel = EditorGUIUtilityWrap.TextContent("Change Shape");
        public static readonly GUIContent sliceButtonLabel = EditorGUIUtilityWrap.TextContent("Slice");
        public static readonly GUIContent trimButtonLabel = EditorGUIUtilityWrap.TextContent("Trim|Trims selected rectangle (T)");
        public static readonly GUIContent sidesLabel = EditorGUIUtilityWrap.TextContent("Sides");
        public static readonly GUIContent polygonChangeShapeHelpBoxContent = EditorGUIUtilityWrap.TextContent("Sides can only be either 0 or anything between 3 and 128");
        public static readonly GUIContent changeButtonLabel = EditorGUIUtilityWrap.TextContent("Change|Change to the new number of sides");
        public static readonly GUIContent editingDiableMessageLabel = EditorGUIUtilityWrap.TextContent("Editing is disabled during play mode");
        public static readonly GUIContent spriteLabel = EditorGUIUtilityWrap.TextContent("Sprite");
        public static readonly GUIContent customPivotLabel = EditorGUIUtilityWrap.TextContent("Custom Pivot");
        public static readonly GUIContent borderLabel = EditorGUIUtilityWrap.TextContent("Border");
        public static readonly GUIContent lLabel = EditorGUIUtilityWrap.TextContent("L");
        public static readonly GUIContent tLabel = EditorGUIUtilityWrap.TextContent("T");
        public static readonly GUIContent rLabel = EditorGUIUtilityWrap.TextContent("R");
        public static readonly GUIContent bLabel = EditorGUIUtilityWrap.TextContent("B");
        public static readonly GUIContent positionLabel = EditorGUIUtilityWrap.TextContent("Position");
        public static readonly GUIContent xLabel = EditorGUIUtilityWrap.TextContent("X");
        public static readonly GUIContent yLabel = EditorGUIUtilityWrap.TextContent("Y");
        public static readonly GUIContent wLabel = EditorGUIUtilityWrap.TextContent("W");
        public static readonly GUIContent hLabel = EditorGUIUtilityWrap.TextContent("H");
        public static readonly GUIContent nameLabel = EditorGUIUtilityWrap.TextContent("Name");
        public static readonly GUIContent revertButtonLabel = EditorGUIUtilityWrap.TextContent("Revert");
        public static readonly GUIContent applyButtonLabel = EditorGUIUtilityWrap.TextContent("Apply");
        public static readonly GUIContent spriteEditorWindowTitle = EditorGUIUtilityWrap.TextContent("Sprite Editor");
        public static readonly GUIContent pendingChangesDialogContent = EditorGUIUtilityWrap.TextContent("You have pending changes in the Sprite Editor Window.\nDo you want to apply these changes?");
        public static readonly GUIContent yesButtonLabel = EditorGUIUtilityWrap.TextContent("Yes");
        public static readonly GUIContent noButtonLabel = EditorGUIUtilityWrap.TextContent("No");
        public static readonly GUIContent applyRevertDialogTitle = EditorGUIUtilityWrap.TextContent("Unapplied import settings");
        public static readonly GUIContent applyRevertDialogContent = EditorGUIUtilityWrap.TextContent("Unapplied import settings for '{0}'");
        public static readonly GUIContent creatingMultipleSpriteDialogTitle = EditorGUIUtilityWrap.TextContent("Creating multiple sprites");
        public static readonly GUIContent creatingMultipleSpriteDialogContent = EditorGUIUtilityWrap.TextContent("Creating {0} sprites. \nThis can take up to several minutes");
        public static readonly GUIContent okButtonLabel = EditorGUIUtilityWrap.TextContent("Ok");
        public static readonly GUIContent cancelButtonLabel = EditorGUIUtilityWrap.TextContent("Cancel");
    }

    public enum AutoSlicingMethod
    {
        DeleteAll,
        Smart,
        Safe,
    }
}
