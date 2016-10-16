/*
	文件是由程序自动生成,请勿手动修改文件
*/

using System.Collections.Generic;
using GameLogic.LogicModules;
using UGCore;
using UnityEngine;

namespace Data.GameData
{
    public partial class Sheet1 : GameDataBase
    {
		protected readonly static string Name = "Sheet1";
        protected static Dictionary<int, Sheet1> m_DataMap = new Dictionary<int, Sheet1>(1);
        protected static bool m_DataIsLoaded;

		public static int Count
        {
            get { return m_DataMap.Count; }
        }

		private Sheet1()
        {

        }

		private static Sheet1 GetInstance()
        {
            return new Sheet1();
        }

        public static Sheet1 GetData(int key)
        {
			CheckDataValid();
            return m_DataMap[key];
        }

        public static Dictionary<int, Sheet1>.Enumerator GetEnumerator()
        {
			CheckDataValid();
            return m_DataMap.GetEnumerator();
        }

        public static Dictionary<int, Sheet1>.KeyCollection.Enumerator GetKeyEnumerator()
        {
			CheckDataValid();
            return m_DataMap.Keys.GetEnumerator();
        }

        public static Dictionary<int, Sheet1>.ValueCollection.Enumerator GetValueEnumerator()
        {
			CheckDataValid();
            return m_DataMap.Values.GetEnumerator();
        }

		public override void LoadDone()
        {
			m_DataIsLoaded = true;
        }

		public override bool IsLoadDone()
        {
			return m_DataIsLoaded;
        }

		public override GameDataBase NewData()
        {
			return new Sheet1();
        }

        protected static void CheckDataValid()
        {
            if (!m_DataIsLoaded)
            {
                var gameDataLoadModule = ModuleManager.Instance.GetGameModule(typeof(GameDataLoadModule).Name) as GameDataLoadModule;
                gameDataLoadModule.LoadGameDataSync(Name);
            }
        }
    }

    public partial class Sheet1
    {
		public string a { get; private set; }

		public override void SaveData()
        {
            m_DataMap.Add(m_DataMap.Count, this);
        }
		
		public override void SetString(string fieldName, string value)
		{
			switch (fieldName)
			{
				case "a": 
						this.a = value;
						break;
				default:
					Debug.LogError(string.Format("The data load error, The field does not exist, Field name: {0}, Value name : {1}", fieldName,value));
					break;
			}
		}	
		
    }
}