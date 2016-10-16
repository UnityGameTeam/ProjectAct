using UnityEditor;

namespace UguiExtensions
{
    [CustomEditor(typeof (NmTextEx), true)]
    [CanEditMultipleObjects]
    public class NmTextExEditor : TextExEditor
    {
        private SerializedProperty m_Maskable;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Maskable = serializedObject.FindProperty("Maskable");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(m_Maskable);
        }
    }
}