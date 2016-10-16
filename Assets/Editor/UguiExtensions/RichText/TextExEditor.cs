using UnityEditor;
using UnityEditor.UI;

namespace UguiExtensions
{
    [CustomEditor(typeof (TextEx), true)]
    [CanEditMultipleObjects]
    public class TextExEditor : GraphicEditor
    {
        private SerializedProperty m_Text;
        private SerializedProperty m_FontData;
        private SerializedProperty m_SpacingX;
        private SerializedProperty m_EllipsizeEnd;
        private SerializedProperty m_EmojiConfigList;
        private SerializedProperty m_UrlClickEvent;

        private SerializedProperty m_ColorInfluenceEmoji;
        private SerializedProperty m_ParseEmoji;
        private SerializedProperty m_ParseColor;
        private SerializedProperty m_ParseBold;
        private SerializedProperty m_ParseItatic;
        private SerializedProperty m_ParseUnderline;
        private SerializedProperty m_ParseStrikethrough;
        private SerializedProperty m_ParseUrl;
        private SerializedProperty m_ParseSub;
        private SerializedProperty m_ParseSup;
        private SerializedProperty m_ParseSize;

        private SerializedProperty m_RaycastTarget;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Text = serializedObject.FindProperty("m_Text");
            m_SpacingX = serializedObject.FindProperty("m_SpacingX");
            m_EllipsizeEnd = serializedObject.FindProperty("m_EllipsizeEnd");
            m_FontData = serializedObject.FindProperty("m_FontData");
            m_EmojiConfigList = serializedObject.FindProperty("m_EmojiConfigList");
            m_UrlClickEvent = serializedObject.FindProperty("m_UrlClickEvent");

            m_ColorInfluenceEmoji = serializedObject.FindProperty("m_ColorInfluenceEmoji");
            m_ParseEmoji = serializedObject.FindProperty("m_ParseEmoji");
            m_ParseColor = serializedObject.FindProperty("m_ParseColor");
            m_ParseBold = serializedObject.FindProperty("m_ParseBold");
            m_ParseItatic = serializedObject.FindProperty("m_ParseItatic");
            m_ParseUnderline = serializedObject.FindProperty("m_ParseUnderline");
            m_ParseStrikethrough = serializedObject.FindProperty("m_ParseStrikethrough");
            m_ParseUrl = serializedObject.FindProperty("m_ParseUrl");
            m_ParseSub = serializedObject.FindProperty("m_ParseSub");
            m_ParseSup = serializedObject.FindProperty("m_ParseSup");
            m_ParseSize = serializedObject.FindProperty("m_ParseSize");

            m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Text);
            EditorGUILayout.PropertyField(m_ColorInfluenceEmoji);
            EditorGUILayout.PropertyField(m_ParseEmoji);
            EditorGUILayout.PropertyField(m_ParseColor);
            EditorGUILayout.PropertyField(m_ParseBold);
            EditorGUILayout.PropertyField(m_ParseItatic);
            EditorGUILayout.PropertyField(m_ParseUnderline);
            EditorGUILayout.PropertyField(m_ParseStrikethrough);
            EditorGUILayout.PropertyField(m_ParseUrl);
            EditorGUILayout.PropertyField(m_ParseSub);
            EditorGUILayout.PropertyField(m_ParseSup);
            EditorGUILayout.PropertyField(m_ParseSize);
            EditorGUILayout.PropertyField(m_SpacingX);
            EditorGUILayout.PropertyField(m_EllipsizeEnd);
            EditorGUILayout.PropertyField(m_FontData);
            EditorGUILayout.PropertyField(m_UrlClickEvent);
            EditorGUILayout.PropertyField(m_EmojiConfigList, true);
            EditorGUILayout.PropertyField(m_RaycastTarget);
            AppearanceControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}