using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class RemoveBlankLine
{
    [MenuItem("Tools/File Tool/RemoveBlankLine")]
    public static void RemoveEmptyLine()
    {
        string path = EditorUtility.OpenFilePanel("Remove Blank Line Txt", "", "txt");
        if (path.Length != 0 && Path.GetExtension(path).ToLower() == ".txt" )
        {
            var lines = File.ReadAllLines(path);
            StringBuilder sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (line == "\n" || line == "\r\n" || line == "\r" || line == "")
                {
                    continue;
                }
                sb.AppendLine(line);
            }
            File.WriteAllText("F:/t.txt", sb.ToString());
        }
        else
        {
            Debug.LogError("不支持的文件");
        }
    }
}
