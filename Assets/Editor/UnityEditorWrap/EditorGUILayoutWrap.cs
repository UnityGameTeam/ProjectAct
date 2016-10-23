using UnityEditor;

public class EditorGUILayoutWrap
{
    internal static float kLabelFloatMinW
    {
        get
        {
            return EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + 5.0f;
        }
    }

    internal static float kLabelFloatMaxW
    {
        get
        {
            return EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + 5.0f;
        }
    }
}
