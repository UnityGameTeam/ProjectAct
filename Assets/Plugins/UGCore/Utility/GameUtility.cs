namespace UGCore.Utility
{
    public static class GameUtility
    {
        public static void QuitGame()
        {
            AndroidUtility.QuitGame();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif

#if UNITY_STANDALONE
            UnityEngine.Application.Quit();
#endif
        }
    }
}