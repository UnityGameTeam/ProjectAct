using System;
using GameLogic.Components;
using GameLogic.Model;
using UguiExtensions;
using UGCore.Utility;
using UGFoundation.Utility;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    private ImageLoader imageLoader;
    private ImageLoader bimageLoader;

    public Button b;

    public InputFieldEx t;
    void Start ()
    {
        b.onClick.AddListener(() =>
        {
            t.ActivateInputField();
            t.onEndEdit.AddListener((str) =>
            {
                t.text = "这是回调啦";
            });
            //TouchScreenKeyboard.Open("1112", TouchScreenKeyboardType.Default,true, false, false, false, "1234567");
            //  AndroidUtility.ShowEditDialog("12323", "ddd", 3, 40, 1, 6, 1, 100, 1000, 0, 0, 0, 0, 0, 0, 0, 0, 30, 50, 300);
        });
    }
	
	// Update is called once per frame
	void Update () {

/*	    if (Input.GetKeyDown(KeyCode.A))
	    {
	        imageLoader.LoadImage("http://avatar.csdn.net/0/8/C/1_xoyojank.jpg", imageLoader.GetSavePathByUrl("http://avatar.csdn.net/0/8/C/1_xoyojank.jpg"),ri,true);
	    }

	    if (Input.GetKeyDown(KeyCode.D))
	    {
            imageLoader.LoadImage("http://avatar.csdn.net/0/8/C/1_xoyojank.jpg", imageLoader.GetSavePathByUrl("http://avatar.csdn.net/0/8/C/1_xoyojank.jpg"), ri, true,
                () =>
                {
                    bimageLoader.LoadImage("http://www.cppblog.com/images/cppblog_com/ylemzy/b_25F1665EFE7011E2D2EF878AB4C18939.jpg", imageLoader.GetSavePathByUrl("http://www.cppblog.com/imasdfsaages/cppblog_com/ylemzy/b_25F1665EFE7011E2D2EF878AB4C18939.jpg"), ri, false);
                    
                });
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            bimageLoader.LoadImage("http://images2015.cnblogs.com/blog/916005/201606/916005-20160612193502902-1720204858.png", null, ri, true);

        }*/
 
    }
}
