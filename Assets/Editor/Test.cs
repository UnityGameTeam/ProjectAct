using UnityEngine;
using System.Collections;
using UnityEditor;

public class Test 
{
    [MenuItem("Tools/导出精灵")]
    static void SaveSprite()
    {
     
        //string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        //Object[] assets = AssetDatabase.LoadAllAssetsAtPath(selectionPath);

        //foreach (var asset in assets)
        //{
        //    Debug.LogError(AssetDatabase.GetAssetPath(asset));
        //    Debug.LogError(asset is Sprite);
        //}

      //  TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Selection.activeObject)) as TextureImporter;

       // var assetsSp = textureImporter.spritesheet;
      //  foreach (var as1 in assetsSp)
      //  {
      //      Debug.LogError(as1.name);
      //  }

        // 必须最上级是"Assets/Resources/"
        // if (selectionPath.StartsWith(resourcesPath))
     //   {
                /*string selectionExt = System.IO.Path.GetExtension(selectionPath);
                if (selectionExt.Length == 0)
                {
                    continue;
                }
                // 从路径"Assets/Resources/UI/testUI.png"得到路径"UI/testUI"
                string loadPath = selectionPath.Remove(selectionPath.Length - selectionExt.Length);
                loadPath = loadPath.Substring(resourcesPath.Length);
                // 加载此文件下的所有资源
                Sprite[] sprites = Resources.LoadAll<Sprite>(loadPath);
                if (sprites.Length > 0)
                {
                    // 创建导出文件夹
                    string outPath = Application.dataPath + "/outSprite/" + loadPath;
                    System.IO.Directory.CreateDirectory(outPath);
                    foreach (Sprite sprite in sprites)
                    {
                        // 创建单独的纹理
                        Texture2D tex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, sprite.texture.format, false);
                        tex.SetPixels(sprite.texture.GetPixels((int)sprite.rect.xMin, (int)sprite.rect.yMin,
                        (int)sprite.rect.width, (int)sprite.rect.height));
                        tex.Apply();
                        // 写入成PNG文件
                        System.IO.File.WriteAllBytes(outPath + "/" + sprite.name + ".png", tex.EncodeToPNG());
                    }
                    Debug.Log("SaveSprite to " + outPath);
                }*/
          
     //   }
    }
}
