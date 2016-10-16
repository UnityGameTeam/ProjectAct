using UnityEngine;
using System.Collections;
using GameLogic.Components;

public class SceneManager
{
    private static SceneManager _instance;
    public static SceneManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SceneManager();
            }
            return _instance;
        }
    }

    protected SceneManager()
    {
        
    }

    public IEnumerator LoadSceneAsync(string sceneName)
    {
        var loadSceneOperation = Application.LoadLevelAsync(sceneName);
        yield return loadSceneOperation;

        LoadSceneConfig();
    }

    private void LoadSceneConfig()
    {
        float mapSize = 300;
        var t = GameObject.Find("Main Camera").transform;
        t.localPosition = new Vector3(mapSize, mapSize,-100);

       /* AssetManager.Instance.LoadAssetSync("Prefabs/RoomBg", (obj) =>
        {
            var go = GameObject.Instantiate(obj) as GameObject;
            
            go.transform.localScale = new Vector3(mapSize * 2, mapSize * 2,1);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = new Vector3(mapSize, mapSize);
        });*/
    }
}
