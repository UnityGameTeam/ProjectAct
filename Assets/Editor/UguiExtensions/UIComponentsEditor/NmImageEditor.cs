using UnityEditor;
using UguiExtensions;

[CustomEditor(typeof(NmImage))]
public class NmImageEditor : UIDecoratorEditor
{
    private SerializedProperty m_Maskable;

    public NmImageEditor(): base("ImageEditor",false)
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
