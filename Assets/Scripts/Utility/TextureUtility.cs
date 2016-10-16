using System.IO;
using UnityEngine;

public class TextureUtility
{
    public static void SavePng(string path, Texture2D texture2D)
    {
        byte[] pngData = texture2D.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
    }
}

