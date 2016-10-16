using System;
using System.Collections.Generic;
using UGFoundation.Utility;

namespace GameLogic.Components
{
    public class GameDataConfigParser
    {
        protected struct HeadInfo
        {
            public int TrunkFlags;
            public int StringCacheCount;
            public int StringCacheSize;
            public int StringCacheStartIndex;

            public int ParseHeadInfo(byte[] data)
            {
                int readBytes = 0;

                BitConverterUtility.ConvertEndianFrom(data, true, readBytes, 4);
                TrunkFlags = BitConverter.ToInt32(data, readBytes);
                readBytes += 4;

                BitConverterUtility.ConvertEndianFrom(data, true, readBytes, 4);
                StringCacheCount = BitConverter.ToInt32(data, readBytes);
                readBytes += 4;

                BitConverterUtility.ConvertEndianFrom(data, true, readBytes, 4);
                StringCacheSize = BitConverter.ToInt32(data, readBytes);
                readBytes += 4;

                StringCacheStartIndex = readBytes;
                return readBytes;
            }
        }

        protected struct TrunkInfo
        {
            public int DataCount;
            public int DataBytesStartIndex;

            public int ParseHeadInfo(byte[] data, int offset)
            {
                int readBytes = 0;
                BitConverterUtility.ConvertEndianFrom(data, true, offset, 4);
                DataCount = BitConverter.ToInt32(data, offset);
                readBytes += 4;

                DataBytesStartIndex = readBytes;

                return readBytes;
            }
        }

        protected enum Trunk
        {
            Preload = 1,
            High = 2,
            Medium = 4,
            Normal = 8,
            Low = 16,
            DontLoad = 32,
        }

        private Dictionary<short, string> m_StringCache; //所有数据文件名的字符串集合 

        private List<string> m_PreloadList = new List<string>(0);
        private List<string> m_HighPriorityList = new List<string>(0);
        private List<string> m_MediumPriorityList = new List<string>(0);
        private List<string> m_NormalPriorityList = new List<string>(0);
        private List<string> m_LowPriorityList = new List<string>(0);
        private List<string> m_DontLoadList = new List<string>(0);

        public GameDataConfigParser(byte[] bytes)
        {
            ParseConfig(bytes);
        }

        public List<List<string>> GetLoadPriorityList()
        {
            List<List<string>> loadList = new List<List<string>>(6);
            loadList.Add(m_PreloadList);
            loadList.Add(m_HighPriorityList);
            loadList.Add(m_MediumPriorityList);
            loadList.Add(m_NormalPriorityList);
            loadList.Add(m_LowPriorityList);
            loadList.Add(m_DontLoadList);
            return loadList;
        }

        private void ParseConfig(byte[] bytes)
        {
            int offset = 0;

            HeadInfo headInfo = new HeadInfo();
            offset += headInfo.ParseHeadInfo(bytes);
            m_StringCache = new Dictionary<short, string>(headInfo.StringCacheCount);

            ParseStringCache(bytes, headInfo.StringCacheStartIndex, offset += headInfo.StringCacheSize, m_StringCache);
            for (int i = 1; i <= (int) Trunk.DontLoad; i = i << 1)
            {
                if ((headInfo.TrunkFlags & i) == i)
                {
                    TrunkInfo trunk = new TrunkInfo();
                    offset += trunk.ParseHeadInfo(bytes, offset);
                    var list = GetList(i, trunk.DataCount);
                    ParseLoadList(bytes, offset, offset += (trunk.DataCount*2), list);
                }
            }
        }

        private void ParseStringCache(byte[] bytes, int start, int end, Dictionary<short, string> stringCache)
        {
            System.Text.UTF8Encoding converter = new System.Text.UTF8Encoding();

            short index = 0;
            for (int i = start; i < end; ++i)
            {
                var offset = i;
                for (; i < end; ++i)
                {
                    if (bytes[i] == 0)
                    {
                        stringCache.Add(index, converter.GetString(bytes, offset, i - offset));
                        ++index;
                        break;
                    }
                }
            }
        }

        private void ParseLoadList(byte[] bytes, int start, int end, List<string> loadList)
        {
            for (int i = start; i < end; i += 2)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, i, 2);
                loadList.Add(m_StringCache[BitConverter.ToInt16(bytes, i)]);
            }
        }

        private List<string> GetList(int trunk, int count)
        {
            switch ((Trunk) trunk)
            {
                case Trunk.Preload:
                    m_PreloadList = new List<string>(count);
                    return m_PreloadList;

                case Trunk.High:
                    m_HighPriorityList = new List<string>(count);
                    return m_HighPriorityList;

                case Trunk.Medium:
                    m_MediumPriorityList = new List<string>(count);
                    return m_MediumPriorityList;

                case Trunk.Normal:
                    m_NormalPriorityList = new List<string>(count);
                    return m_NormalPriorityList;

                case Trunk.Low:
                    m_LowPriorityList = new List<string>(count);
                    return m_LowPriorityList
                        ;
                case Trunk.DontLoad:
                    m_DontLoadList = new List<string>(count);
                    return m_DontLoadList;

                default:
                    return null;
            }
        }
    }
}