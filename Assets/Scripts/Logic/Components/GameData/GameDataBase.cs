using System.Collections.Generic;

namespace Data.GameData
{
    public abstract class GameDataBase
    {

        public virtual void SetInt(string fieldName,int value)
        {

        }

        public virtual void SetString(string fieldName, string value)
        {

        }

        public virtual void SetLong(string fieldName, long value)
        {

        }

        public virtual void SetShort(string fieldName, short value)
        {

        }

        public virtual void SetFloat(string fieldName, float value)
        {

        }

        public virtual void SetListInt(string fieldName, List<int> value)
        {

        }

        public virtual void SetListInt2(string fieldName, List<List<int>> value)
        {

        }

        public virtual void SetListFloat(string fieldName, List<float> value)
        {

        }

        public virtual void SetListFloat2(string fieldName, List<List<float>> value)
        {

        }

        public virtual void SetListString(string fieldName, List<string> value)
        {

        }

        public virtual void SetListString2(string fieldName, List<List<string>> value)
        {

        }

        public virtual void SetDictionaryS2I(string fieldName, Dictionary<string, int> value)
        {

        }

        public virtual void SetDictionaryS2S(string fieldName, Dictionary<string, string> value)
        {

        }

        public virtual void SetDictionaryI2I(string fieldName, Dictionary<int, int> value)
        {

        }

        public virtual void SetDictionaryI2S(string fieldName, Dictionary<int, string> value)
        {

        }

        public virtual void SetHashSetInt(string fieldName, HashSet<int> value)
        {

        }

        public virtual void SetHashSetString(string fieldName, HashSet<string> value)
        {

        }

        public virtual void SaveData()
        {
            
        }

        public virtual GameDataBase NewData()
        {
            return null;
        }

        public virtual void LoadDone()
        {

        }

        public virtual bool IsLoadDone()
        {
            return false;
        }
    }
}