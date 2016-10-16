/*
	文件是由程序自动生成,请勿手动修改文件
*/

using System.Collections.Generic;
using GameLogic.LogicModules;
using UGCore;
using UnityEngine;

namespace Data.GameData
{
    public partial class Sheet2 : GameDataBase
    {
		protected readonly static string Name = "Sheet2";
        protected static Dictionary<int, Sheet2> m_DataMap = new Dictionary<int, Sheet2>(15831);
        protected static bool m_DataIsLoaded;

		public static int Count
        {
            get { return m_DataMap.Count; }
        }

		private Sheet2()
        {

        }

		private static Sheet2 GetInstance()
        {
            return new Sheet2();
        }

        public static Sheet2 GetData(int key)
        {
			CheckDataValid();
            return m_DataMap[key];
        }

        public static Dictionary<int, Sheet2>.Enumerator GetEnumerator()
        {
			CheckDataValid();
            return m_DataMap.GetEnumerator();
        }

        public static Dictionary<int, Sheet2>.KeyCollection.Enumerator GetKeyEnumerator()
        {
			CheckDataValid();
            return m_DataMap.Keys.GetEnumerator();
        }

        public static Dictionary<int, Sheet2>.ValueCollection.Enumerator GetValueEnumerator()
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
			return new Sheet2();
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

    public partial class Sheet2
    {
		public string a { get; private set; }
		public HashSet<string> b { get; private set; }
		public List<List<string>> c { get; private set; }
		public long d { get; private set; }

		public override void SaveData()
        {
            m_DataMap.Add(m_DataMap.Count, this);
        }
		
		public override void SetHashSetString(string fieldName, HashSet<string> value)
		{
			switch (fieldName)
			{
				case "b": 
						this.b = value;
						break;
				default:
					Debug.LogError(string.Format("The data load error, The field does not exist, Field name: {0}, Value name : {1}", fieldName,value));
					break;
			}
		}

		public override void SetListString2(string fieldName, List<List<string>> value)
		{
			switch (fieldName)
			{
				case "c": 
						this.c = value;
						break;
				default:
					Debug.LogError(string.Format("The data load error, The field does not exist, Field name: {0}, Value name : {1}", fieldName,value));
					break;
			}
		}

		public override void SetLong(string fieldName, long value)
		{
			switch (fieldName)
			{
				case "d": 
						this.d = value;
						break;
				default:
					Debug.LogError(string.Format("The data load error, The field does not exist, Field name: {0}, Value name : {1}", fieldName,value));
					break;
			}
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