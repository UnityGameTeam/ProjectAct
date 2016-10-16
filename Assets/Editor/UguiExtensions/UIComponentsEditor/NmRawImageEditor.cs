using UnityEditor;
using UguiExtensions;

[CustomEditor(typeof(NmRawImage))]
public class NmRawImageEditor : UIDecoratorEditor
{
    private SerializedProperty m_Maskable;

    public NmRawImageEditor() : base("RawImageEditor", false)
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
