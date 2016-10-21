using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;

namespace UGCore
{
    public class LoadingLanguageData
    {
        private static LoadingLanguageData instance;

        public static LoadingLanguageData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LoadingLanguageData();
                }
                return instance;
            }
        }

        protected Dictionary<string, string> m_LanguageData; 

        protected LoadingLanguageData()
        {
            var languageDataText = Resources.Load("ReadonlyData/LoadingLanguageData") as TextAsset;
            m_LanguageData = JsonReader.Deserialize<Dictionary<string, string>>(languageDataText.text);
            Resources.UnloadAsset(languageDataText);
        }

        public string GetString(int key)
        {
            var keyStr = key.ToString();
            if (m_LanguageData.ContainsKey(keyStr))
            {
                return m_LanguageData[keyStr];
            }
            return string.Empty;
        }
    }
}
