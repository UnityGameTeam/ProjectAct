using System.Reflection;

public class EditorResourcesUtilityWrap
{
    public static string folderIconName
    {
        get
        {
            var type = typeof (UnityEditorInternal.AssetStore);
            type = type.Assembly.GetType("UnityEditorInternal.EditorResourcesUtility");
            var p = type.GetProperty("folderIconName", BindingFlags.Static | BindingFlags.Public);
            return (string)p.GetValue(null,null);
        }
    }
}
