using UnityEditor;
using UguiExtensions;

[CustomEditor(typeof(NmText))]
public class NmTextEditor : UIDecoratorEditor
{
    private SerializedProperty m_Maskable;

    public NmTextEditor() : base("TextEditor", false)
    {

    }

    protected void OnEnable()
    {
        m_Maskable = serializedObject.FindProperty("Maskable");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.PropertyField(m_Maskable);
    }
}
