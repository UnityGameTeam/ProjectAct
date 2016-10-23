using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

public class PatchImportSettingRecycleIDWrap
{
    public static void PatchMultiple(SerializedObject serializedObject, int classID, string[] oldNames, string[] newNames)
    {
        int length = oldNames.Length;
        foreach (SerializedProperty serializedProperty in serializedObject.FindProperty("m_FileIDToRecycleName"))
        {
            /*if (AssetImporter.LocalFileIDToClassID(serializedProperty.FindPropertyRelative("first").longValue) == classID)
            {
                SerializedProperty propertyRelative = serializedProperty.FindPropertyRelative("second");
                int index = Array.IndexOf<string>(oldNames, propertyRelative.stringValue);
                if (index >= 0)
                {
                    propertyRelative.stringValue = newNames[index];
                    if (--length == 0)
                        break;
                }
            }*/
        }
    }
}
