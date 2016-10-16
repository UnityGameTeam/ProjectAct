using System;
using System.Collections.Generic;
using System.Text;
using Data.GameData;
using UGFoundation.Utility;

namespace GameLogic.Components
{
    public abstract class GameDataParser
    {
        protected delegate int TypeParse(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName);

        protected static Dictionary<byte, TypeParse> TypeParseMap = new Dictionary<byte, TypeParse>(17)
        {
            {1, ParseInt},
            {2, ParseString},
            {3, ParseFloat},
            {4, ParseLong},
            {5, ParseShort},

            {6, ParseListInt},
            {7, ParseListInt2},
            {8, ParseListString},
            {9, ParseListString2},
            {10, ParseListFloat},
            {11, ParseListFloat2},

            {12, ParseMapI2I},
            {13, ParseMapI2S},
            {14, ParseMapS2S},
            {15, ParseMapS2I},

            {16, ParseHashSetInt},
            {17, ParseHashSetString}
        };

        protected List<byte>   m_FieldTypeList = new List<byte>();
        protected List<string> m_FieldNameList = new List<string>();

        protected List<string> m_StringBuffList = new List<string>();
        protected GameDataBase m_GameDataInstance;
        protected int          m_ItemCount;

        protected int GetHeadInfo(byte[] bytes)
        {
            var offset = 0;
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 2);
            var fieldCount = BitConverter.ToInt16(bytes, offset);
            offset += 2;

            for (int i = 0; i < fieldCount; i++)
            {
                m_FieldTypeList.Add(bytes[offset + i]);
            }
            offset += fieldCount;

            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var fieldNameBuffSize = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            ParseStringCache(bytes, offset, offset += fieldNameBuffSize, m_FieldNameList);

            return offset;
        }

        protected int GetStringBuffInfo(byte[] bytes, int offset)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            m_ItemCount = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            var hasStringBuff = bytes[offset] != 0;
            offset += 1;

            if (hasStringBuff)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var stringBuffSize = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                ParseStringCache(bytes, offset, offset += stringBuffSize, m_StringBuffList);
            }

            return offset;
        }

        protected int GetItems(byte[] bytes, int offset)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 2);
            var itemCount = BitConverter.ToInt16(bytes, offset);
            offset += 2;

            for (int i = 0; i < itemCount; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 2);
                short fieldIndex = BitConverter.ToInt16(bytes, offset);
                offset += 2;

                string fieldName = m_FieldNameList[fieldIndex];
                var type = m_FieldTypeList[fieldIndex];

                offset = TypeParseMap[type](this, bytes, offset, m_GameDataInstance, fieldName);
            }
            return offset;
        }

        protected void ParseStringCache(byte[] bytes, int start, int end, List<string> stringCache)
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

        protected static int ParseShort(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 2);
            var value = BitConverter.ToInt16(bytes, offset);
            offset += 2;
            gameDataObj.SetShort(fieldName,value);
            return offset;
        }

        protected static int ParseInt(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var value = BitConverter.ToInt32(bytes, offset);
            offset += 4;
            gameDataObj.SetInt(fieldName, value);
            return offset;
        }

        protected static int ParseLong(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 8);
            var value = BitConverter.ToInt64(bytes, offset);
            offset += 8;
            gameDataObj.SetLong(fieldName, value);
            return offset;
        }

        protected static int ParseFloat(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var value = BitConverter.ToSingle(bytes, offset);
            offset += 4;
            gameDataObj.SetFloat(fieldName, value);
            return offset;
        }

        protected static int ParseString(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var value = BitConverter.ToInt32(bytes, offset);
            offset += 4;
            gameDataObj.SetString(fieldName, parser.m_StringBuffList[value]);
            return offset;
        }

        protected static int ParseListInt(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            List<int> values = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var value = BitConverter.ToInt32(bytes, offset);
                offset += 4;
                values.Add(value);
            }

            gameDataObj.SetListInt(fieldName, values);
            return offset;
        }

        protected static int ParseListInt2(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var arrayCount = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            List<List<int>> array = new List<List<int>>(arrayCount);
            for (int i = 0; i < arrayCount; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var count = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                List<int> values = new List<int>(count);
                for (int j = 0; j < count; j++)
                {
                    BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                    var value = BitConverter.ToInt32(bytes, offset);
                    offset += 4;
                    values.Add(value);
                }
                array.Add(values);
            }

            gameDataObj.SetListInt2(fieldName, array);
            return offset;
        }

        protected static int ParseListString(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            List<string> values = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var value = BitConverter.ToInt32(bytes, offset);
                offset += 4;
                values.Add(parser.m_StringBuffList[value]);
            }

            gameDataObj.SetListString(fieldName, values);
            return offset;
        }

        protected static int ParseListString2(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var arrayCount = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            List<List<string>> array = new List<List<string>>(arrayCount);
            for (int i = 0; i < arrayCount; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var count = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                List<string> values = new List<string>(count);
                for (int j = 0; j < count; j++)
                {
                    BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                    var value = BitConverter.ToInt32(bytes, offset);
                    offset += 4;
                    values.Add(parser.m_StringBuffList[value]);
                }
                array.Add(values);
            }

            gameDataObj.SetListString2(fieldName, array);
            return offset;
        }

        protected static int ParseListFloat(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            List<float> values = new List<float>(count);
            for (int i = 0; i < count; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var value = BitConverter.ToSingle(bytes, offset);
                offset += 4;
                values.Add(value);
            }

            gameDataObj.SetListFloat(fieldName, values);
            return offset;
        }

        protected static int ParseListFloat2(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var arrayCount = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            List<List<float>> array = new List<List<float>>(arrayCount);
            for (int i = 0; i < arrayCount; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var count = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                List<float> values = new List<float>(count);
                for (int j = 0; j < count; j++)
                {
                    BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                    var value = BitConverter.ToSingle(bytes, offset);
                    offset += 4;
                    values.Add(value);
                }
                array.Add(values);
            }

            gameDataObj.SetListFloat2(fieldName, array);
            return offset;
        }

        protected static int ParseMapI2I(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            Dictionary<int, int> values = new Dictionary<int, int>(count);
            for (int i = 0; i < count; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var key = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var value = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                values.Add(key, value);
            }

            gameDataObj.SetDictionaryI2I(fieldName, values);
            return offset;
        }

        protected static int ParseMapI2S(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            Dictionary<int, string> values = new Dictionary<int, string>(count);
            for (int i = 0; i < count; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var key = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var value = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                values.Add(key, parser.m_StringBuffList[value]);
            }

            gameDataObj.SetDictionaryI2S(fieldName, values);
            return offset;
        }

        protected static int ParseMapS2S(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            Dictionary<string, string> values = new Dictionary<string, string>(count);
            for (int i = 0; i < count; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var key = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var value = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                values.Add(parser.m_StringBuffList[key], parser.m_StringBuffList[value]);
            }

            gameDataObj.SetDictionaryS2S(fieldName, values);
            return offset;
        }

        protected static int ParseMapS2I(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            Dictionary<string, int> values = new Dictionary<string, int>(count);
            for (int i = 0; i < count; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var key = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var value = BitConverter.ToInt32(bytes, offset);
                offset += 4;

                values.Add(parser.m_StringBuffList[key], value);
            }

            gameDataObj.SetDictionaryS2I(fieldName, values);
            return offset;
        }

        protected static int ParseHashSetInt(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            HashSet<int> values = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var value = BitConverter.ToInt32(bytes, offset);
                offset += 4;
                values.Add(value);
            }

            gameDataObj.SetHashSetInt(fieldName, values);
            return offset;
        }

        protected static int ParseHashSetString(GameDataParser parser, byte[] bytes, int offset, GameDataBase gameDataObj, string fieldName)
        {
            BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
            var count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            HashSet<string> values = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                BitConverterUtility.ConvertEndianFrom(bytes, true, offset, 4);
                var value = BitConverter.ToInt32(bytes, offset);
                offset += 4;
                values.Add(parser.m_StringBuffList[value]);
            }

            gameDataObj.SetHashSetString(fieldName, values);
            return offset;
        }
    }
}