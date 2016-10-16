using Data.GameData;

namespace GameLogic.Components
{
    public class GameDataSyncParser : GameDataParser
    {
        public void GetGameDataSync(byte[] bytes, string typeName, GameDataBase gameDataInstance)
        {
            m_FieldTypeList.Clear();
            m_FieldNameList.Clear();
            m_StringBuffList.Clear();
            m_GameDataInstance = gameDataInstance;
            ParseGameDataSync(bytes);
        }

        private void ParseGameDataSync(byte[] bytes)
        {
            var offset = GetHeadInfo(bytes);
            offset = GetStringBuffInfo(bytes, offset);

            for (int i = 0; i < m_ItemCount; i++)
            {
                offset = GetItems(bytes, offset);
                m_GameDataInstance.SaveData();

                if (i + 1 < m_ItemCount)
                {
                    m_GameDataInstance = m_GameDataInstance.NewData();
                }
            }
            m_GameDataInstance.LoadDone();
        }
    }
}