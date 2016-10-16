using System;
using System.Collections.Generic;
using System.Text;
using UGFoundation.Utility;

namespace GameLogic.Components
{
    public class AssetInfo
    {
        public string AssetBundlePath;
        public List<string> DependencesPath;
    }

    public class AssetDependencesParser
    {
        private int m_ItemCount;
        private int m_StringBuffCount;
        public Dictionary<string, AssetInfo> ParseAssetDependences(byte[] bytes)
        {
            Dictionary<string, AssetInfo> assetDependencesMap = new Dictionary<string, AssetInfo>();

            var offset = GetHeadInfo(bytes);
            if (m_ItemCount == 0)
            {
                assetDependencesMap = new Dictionary<string, AssetInfo>(0);
                return assetDependencesMap;
            }
            else
            {
                assetDependencesMap = new Dictionary<string, AssetInfo>(m_ItemCount);
            }

            List<string> stringBuff = new List<string>(m_StringBuffCount);
            offset = GetStringBuffInfo(stringBuff, bytes, offset);

            ParseAssetsInfo(assetDependencesMap, stringBuff, bytes, offset);

            return assetDependencesMap;
        }

        private int GetHeadInfo(byte[] bytes)
        {
            var offset = 0;
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            m_ItemCount = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            m_StringBuffCount = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            return offset;
        }

        private int GetStringBuffInfo(List<string> stringBuff, byte[] bytes, int offset)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var stringBuffSize = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            ParseStringCache(bytes, offset, offset += stringBuffSize, stringBuff);
            return offset;
        }

        private void ParseStringCache(byte[] bytes, int start, int end, List<string> stringCache)
        {
            UTF8Encoding converter = new UTF8Encoding();

            for (int i = start; i < end; ++i)
            {
                var offset = i;
                for (; i < end; ++i)
                {
                    if (bytes[i] == 0)
                    {
                        stringCache.Add(converter.GetString(bytes, offset, i - offset));
                        break;
                    }
                }
            }
        }

        private void ParseAssetsInfo(Dictionary<string, AssetInfo> assetDependencesMap, List<string> stringBuff, byte[] bytes, int offset)
        {
            for (int i = 0; i < m_ItemCount; ++i)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var index = BitConverter.ToInt32(bytes, offset);
                var path = stringBuff[index];
                offset += 4;

                var assetInfo = new AssetInfo();

                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                index = BitConverter.ToInt32(bytes, offset);
                assetInfo.AssetBundlePath = stringBuff[index];
                offset += 4;

                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var dependencesCount = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                var dependencesList = new List<string>(dependencesCount);
                for (int j = 0; j < dependencesCount; j++)
                {
                    BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                    index = BitConverter.ToInt32(bytes, offset);
                    dependencesList.Add(stringBuff[index]);
                    offset += 4;
                }
                assetInfo.DependencesPath = dependencesList;

                assetDependencesMap.Add(path, assetInfo);
            }
        }
    }
}