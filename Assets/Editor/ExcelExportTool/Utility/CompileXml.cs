using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UGFoundation.Utility;
using UnityEngine;

public class CompileXml
{
    private delegate byte[] CompileType(short type, string value);

    private static readonly Dictionary<string, byte> TypeMap = new Dictionary<string, byte>()
    {
        {"int", 1},
        {"string", 2},
        {"float", 3},
        {"long", 4},
        {"short", 5},

        {"List<int>", 6},
        {"List<List<int>>", 7},
        {"List<string>", 8},
        {"List<List<string>>", 9},
        {"List<float>", 10},
        {"List<List<float>>", 11},

        {"Dictionary<int,int>", 12},
        {"Dictionary<int,string>", 13},
        {"Dictionary<string,string>", 14},
        {"Dictionary<string,int>", 15},

        {"HashSet<int>", 16},
        {"HashSet<string>", 17},
    };

    private Dictionary<byte, CompileType> ParseTypeMap;

    public CompileXml()
    {
        ParseTypeMap = new Dictionary<byte, CompileType>()
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

            {12,ParseMapI2I},
            {13,ParseMapI2S},
            {14,ParseMapS2S},
            {15,ParseMapS2I},

            {16,ParseHashSetInt},
            {17,ParseHashSetString }
        };
    }


    private short m_FieldCount;
    private List<byte> m_FieldTypeList = new List<byte>();

    private int m_FieldNameBufferSzie;
    private List<string> m_FieldNameList = new List<string>();

    private int m_ItemCount;
    private byte m_HasStringBuffer;

    private int m_StringBuffSize;

    private Dictionary<string, int> m_FieldNameBufferToIndexMap = new Dictionary<string, int>();

    public static void ComplieXmlFile(string xmlExportDir, string xmlBinExportDir)
    {
        var fileInfos = EditorFileUtility.FilterDirectory(xmlExportDir, new string[] {".xml"}, false);
        foreach (var file in fileInfos)
        {
            ParseXml(file.FullName, xmlBinExportDir);
        }
    }

    private static void ParseXml(string path, string xmlBinExportDir)
    {
        try
        {
            var complieXml = new CompileXml();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(path);
            var rootNode = xmlDoc.SelectSingleNode("root");

            var nodeList = rootNode.SelectSingleNode("Props").ChildNodes;
            foreach (var childNode in nodeList)
            {

                var childElement = (XmlElement) childNode;
                var fieldName = childElement.InnerText;

                CsBuilder.IsPrimaryKey(ref fieldName);
                var fieldType = CsBuilder.GetFieldType(ref fieldName);

                complieXml.m_FieldNameList.Add(fieldName);
                complieXml.m_FieldNameBufferToIndexMap.Add(fieldName, complieXml.m_FieldNameBufferToIndexMap.Count);
                complieXml.m_FieldTypeList.Add(TypeMap[fieldType]);
            }

            complieXml.m_FieldNameBufferSzie = GetStringBufferSize(complieXml.m_FieldNameList);
            complieXml.m_FieldCount = (short) complieXml.m_FieldTypeList.Count;

            string name = Path.GetFileNameWithoutExtension(path);

            FileStream fs = File.Create(xmlBinExportDir + "/" + name + ".xml");

            complieXml.WriteHead(fs);

            GetStringBuffer(complieXml, rootNode);
            complieXml.WriteStringBuffer(fs);

            WriteItems(fs, complieXml, rootNode);

            fs.Flush();
            fs.Close();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void WriteHead(FileStream fs)
    {
        var bytes = BitConverter.GetBytes(this.m_FieldCount);
        BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
        fs.Write(bytes, 0, bytes.Length);

        foreach (var type in this.m_FieldTypeList)
        {
            fs.WriteByte(type);
        }

        bytes = BitConverter.GetBytes(m_FieldNameBufferSzie);
        BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
        fs.Write(bytes, 0, bytes.Length);

        foreach (var fieldName in m_FieldNameList)
        {
            bytes = Encoding.UTF8.GetBytes(fieldName);
            fs.Write(bytes, 0, bytes.Length);
            fs.WriteByte(0);
        }
    }

    private void WriteStringBuffer(FileStream fs)
    {
        var bytes = BitConverter.GetBytes(this.m_ItemCount);
        BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
        fs.Write(bytes, 0, bytes.Length);

        fs.WriteByte(m_HasStringBuffer);

        if (m_HasStringBuffer != 0)
        {
            var buffsize = GetStringBufferSize(m_StringBufferList);

            bytes = BitConverter.GetBytes(buffsize);
            BitConverterUtility.ConvertToLittleEndian(bytes, 0, bytes.Length);
            fs.Write(bytes, 0, bytes.Length);
        }

        foreach (var strBuff in m_StringBufferList)
        {
            bytes = Encoding.UTF8.GetBytes(strBuff);
            fs.Write(bytes, 0, bytes.Length);
            fs.WriteByte(0);
        }
    }

    private static int GetStringBufferSize(List<string> stringBuffList)
    {
        int bytes = 0;
        foreach (var priority in stringBuffList)
        {
            bytes += System.Text.Encoding.UTF8.GetBytes(priority).Length;
        }
        bytes += stringBuffList.Count;
        return bytes;
    }

    private Dictionary<string, int> m_StringBufferToIndexMap = new Dictionary<string, int>();
    private List<string> m_StringBufferList = new List<string>();

    private static void GetStringBuffer(CompileXml compileXml, XmlNode rootNode)
    {
        var itemsNode = rootNode.SelectSingleNode("Items");
        compileXml.m_ItemCount = itemsNode.ChildNodes.Count;

        foreach (var itemNode in itemsNode)
        {
            var itemElement = (XmlElement) itemNode;
            var dataNodes = itemElement.ChildNodes;
            foreach (var dataNode in dataNodes)
            {
                var dataElement = (XmlElement) dataNode;
                var name = dataElement.Name;
                var _index = name.IndexOf('_');
                if (_index > -1)
                {
                    name = name.Substring(0, _index);
                }

                var fieldType = compileXml.m_FieldTypeList[compileXml.m_FieldNameBufferToIndexMap[name]];
                if (fieldType == 2)
                {
                    if (!compileXml.m_StringBufferToIndexMap.ContainsKey(dataElement.InnerText) &&
                        !string.IsNullOrEmpty(dataElement.InnerText))
                    {
                        compileXml.m_StringBufferList.Add(dataElement.InnerText);
                        compileXml.m_StringBufferToIndexMap.Add(dataElement.InnerText,
                            compileXml.m_StringBufferList.Count - 1);
                    }
                }
                if (fieldType == 8 || fieldType == 17)
                {
                    var strings = dataElement.InnerText.Split(',');
                    foreach (var str in strings)
                    {
                        if (!compileXml.m_StringBufferToIndexMap.ContainsKey(str) && !string.IsNullOrEmpty(str))
                        {
                            compileXml.m_StringBufferList.Add(str);
                            compileXml.m_StringBufferToIndexMap.Add(str, compileXml.m_StringBufferList.Count - 1);
                        }
                    }
                }
                if (fieldType == 9)
                {
                    string[] valueSplit = dataElement.InnerText.Split(',');

                    for (int i = 0; i < valueSplit.Length; i++)
                    {
                        var parseResult = valueSplit[i].Split('_');
                        for (int j = 0; j < parseResult.Length; j++)
                        {
                            if (!compileXml.m_StringBufferToIndexMap.ContainsKey(parseResult[j]) &&
                                !string.IsNullOrEmpty(parseResult[j]))
                            {
                                compileXml.m_StringBufferList.Add(parseResult[j]);
                                compileXml.m_StringBufferToIndexMap.Add(parseResult[j],
                                    compileXml.m_StringBufferList.Count - 1);
                            }
                        }
                    }
                }
                if (fieldType == 13)
                {
                    string[] valueSplit = dataElement.InnerText.Split(',');

                    for (int i = 0; i < valueSplit.Length; i++)
                    {
                        var parseResult = valueSplit[i].Split(':');

                        if (!compileXml.m_StringBufferToIndexMap.ContainsKey(parseResult[1]) &&
                                !string.IsNullOrEmpty(parseResult[1]))
                        {
                            compileXml.m_StringBufferList.Add(parseResult[1]);
                            compileXml.m_StringBufferToIndexMap.Add(parseResult[1],
                                compileXml.m_StringBufferList.Count - 1);
                        }
                    }
                }
                if (fieldType == 14)
                {
                    string[] valueSplit = dataElement.InnerText.Split(',');
                    for (int i = 0; i < valueSplit.Length; i++)
                    {
                        var parseResult = valueSplit[i].Split(':');
                        for (int j = 0; j < 2; j++)
                        {
                            if (!compileXml.m_StringBufferToIndexMap.ContainsKey(parseResult[j]) &&
                                !string.IsNullOrEmpty(parseResult[j]))
                            {
                                compileXml.m_StringBufferList.Add(parseResult[j]);
                                compileXml.m_StringBufferToIndexMap.Add(parseResult[j],
                                    compileXml.m_StringBufferList.Count - 1);
                            }
                        }
                    }
                }
                if (fieldType == 15)
                {
                    string[] valueSplit = dataElement.InnerText.Split(',');

                    for (int i = 0; i < valueSplit.Length; i++)
                    {
                        var parseResult = valueSplit[i].Split(':');

                        if (!compileXml.m_StringBufferToIndexMap.ContainsKey(parseResult[0]) &&
                                !string.IsNullOrEmpty(parseResult[0]))
                        {
                            compileXml.m_StringBufferList.Add(parseResult[0]);
                            compileXml.m_StringBufferToIndexMap.Add(parseResult[0],
                                compileXml.m_StringBufferList.Count - 1);
                        }
                    }
                }
            }
        }

        if (compileXml.m_StringBufferList.Count > 0)
        {
            compileXml.m_HasStringBuffer = 1;
        }
    }

    private static void WriteItems(FileStream fs, CompileXml compileXml, XmlNode rootNode)
    {
        var itemsNode = rootNode.SelectSingleNode("Items");

        foreach (var itemNode in itemsNode)
        {
            var itemElement = (XmlElement) itemNode;
            var dataNodes = itemElement.ChildNodes;
            compileXml.writeItemCount(fs, (short) dataNodes.Count);

            foreach (var dataNode in dataNodes)
            {
                var dataElement = (XmlElement) dataNode;
                var name = dataElement.Name;
                var _index = name.IndexOf('_');
                if (_index > -1)
                {
                    name = name.Substring(0, _index);
                }

                var fieldType = compileXml.m_FieldTypeList[compileXml.m_FieldNameBufferToIndexMap[name]];
                var bytes = compileXml.ParseTypeMap[fieldType]((short) compileXml.m_FieldNameBufferToIndexMap[name],
                    dataElement.InnerText);
                fs.Write(bytes, 0, bytes.Length);
            }
        }
    }

    private void writeItemCount(FileStream fs, short count)
    {
        var data = BitConverter.GetBytes(count);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        fs.Write(data, 0, data.Length);
    }

    private void AddBytesData(List<byte> bytes, byte[] data)
    {
        foreach (var b in data)
        {
            bytes.Add(b);
        }
    }

    private byte[] ParseShort(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        var result = short.Parse(value);
        data = BitConverter.GetBytes(result);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        return bytes.ToArray();
    }

    private byte[] ParseInt(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        var result = int.Parse(value);
        data = BitConverter.GetBytes(result);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        return bytes.ToArray();
    }

    private byte[] ParseLong(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        var result = long.Parse(value);
        data = BitConverter.GetBytes(result);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        return bytes.ToArray();
    }

    private byte[] ParseFloat(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        var result = float.Parse(value);
        data = BitConverter.GetBytes(result);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        return bytes.ToArray();
    }

    private byte[] ParseString(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        var sbIndex = m_StringBufferToIndexMap[value];
        data = BitConverter.GetBytes(sbIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        return bytes.ToArray();
    }

    private byte[] ParseListInt(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        var dataCount = valueSplit.Length;
        data = BitConverter.GetBytes(dataCount);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        List<int> listValues = new List<int>(dataCount);
        foreach (var v in valueSplit)
        {
            listValues.Add(int.Parse(v));
        }

        foreach (var v in listValues)
        {
            data = BitConverter.GetBytes(v);
            BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
            AddBytesData(bytes, data);
        }

        return bytes.ToArray();
    }

    private byte[] ParseListInt2(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        data = BitConverter.GetBytes(valueSplit.Length);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        for (int i = 0; i < valueSplit.Length; i++)
        {
            var parseResult = valueSplit[i].Split('_');

            data = BitConverter.GetBytes(parseResult.Length);
            BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
            AddBytesData(bytes, data);

            List<int> listValues = new List<int>(parseResult.Length);
            foreach (var v in parseResult)
            {
                listValues.Add(int.Parse(v));
            }

            foreach (var v in listValues)
            {
                data = BitConverter.GetBytes(v);
                BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
                AddBytesData(bytes, data);
            }
        }

        return bytes.ToArray();
    }

    private byte[] ParseListString(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        var dataCount = valueSplit.Length;
        data = BitConverter.GetBytes(dataCount);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        List<int> listValues = new List<int>(dataCount);
        foreach (var v in valueSplit)
        {
            listValues.Add(m_StringBufferToIndexMap[v]);
        }

        foreach (var v in listValues)
        {
            data = BitConverter.GetBytes(v);
            BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
            AddBytesData(bytes, data);
        }

        return bytes.ToArray();
    }

    private byte[] ParseListString2(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        data = BitConverter.GetBytes(valueSplit.Length);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        for (int i = 0; i < valueSplit.Length; i++)
        {
            var parseResult = valueSplit[i].Split('_');
            data = BitConverter.GetBytes(parseResult.Length);
            BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
            AddBytesData(bytes, data);

            List<int> listValues = new List<int>(parseResult.Length);
            foreach (var v in parseResult)
            {
                listValues.Add(m_StringBufferToIndexMap[v]);
            }

            foreach (var v in listValues)
            {
                data = BitConverter.GetBytes(v);
                BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
                AddBytesData(bytes, data);
            }
        }

        return bytes.ToArray();
    }

    private byte[] ParseListFloat(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        var dataCount = valueSplit.Length;
        data = BitConverter.GetBytes(dataCount);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        List<float> listValues = new List<float>(dataCount);
        foreach (var v in valueSplit)
        {
            listValues.Add(float.Parse(v));
        }

        foreach (var v in listValues)
        {
            data = BitConverter.GetBytes(v);
            BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
            AddBytesData(bytes, data);
        }

        return bytes.ToArray();
    }

    private byte[] ParseListFloat2(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        data = BitConverter.GetBytes(valueSplit.Length);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        for (int i = 0; i < valueSplit.Length; i++)
        {
            var parseResult = valueSplit[i].Split('_');

            data = BitConverter.GetBytes(parseResult.Length);
            BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
            AddBytesData(bytes, data);

            List<float> listValues = new List<float>(parseResult.Length);
            foreach (var v in parseResult)
            {
                listValues.Add(float.Parse(v));
            }

            foreach (var v in listValues)
            {
                data = BitConverter.GetBytes(v);
                BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
                AddBytesData(bytes, data);
            }
        }

        return bytes.ToArray();
    }

    private byte[] ParseMapI2I(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        data = BitConverter.GetBytes(valueSplit.Length);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        Dictionary<int,int> keySet = new Dictionary<int, int>();

        for (int i = 0; i < valueSplit.Length; i++)
        {
            var parseResult = valueSplit[i].Split(':');

            List<int> listValues = new List<int>(2);

            keySet.Add(int.Parse(parseResult[0]), int.Parse(parseResult[1])); //有重复的健会报错

            listValues.Add(int.Parse(parseResult[0]));
            listValues.Add(int.Parse(parseResult[1]));

            foreach (var v in listValues)
            {
                data = BitConverter.GetBytes(v);
                BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
                AddBytesData(bytes, data);
            }
        }

        return bytes.ToArray();
    }

    private byte[] ParseMapI2S(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        data = BitConverter.GetBytes(valueSplit.Length);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        Dictionary<int, int> keySet = new Dictionary<int, int>();

        for (int i = 0; i < valueSplit.Length; i++)
        {
            var parseResult = valueSplit[i].Split(':');

            List<int> listValues = new List<int>(2);

            keySet.Add(int.Parse(parseResult[0]),m_StringBufferToIndexMap[parseResult[1]]); //有重复的健会报错

            listValues.Add(int.Parse(parseResult[0]));
            listValues.Add(m_StringBufferToIndexMap[parseResult[1]]);

            foreach (var v in listValues)
            {
                data = BitConverter.GetBytes(v);
                BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
                AddBytesData(bytes, data);
            }
        }

        return bytes.ToArray();
    }

    private byte[] ParseMapS2S(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        data = BitConverter.GetBytes(valueSplit.Length);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        Dictionary<int, int> keySet = new Dictionary<int, int>();

        for (int i = 0; i < valueSplit.Length; i++)
        {
            var parseResult = valueSplit[i].Split(':');

            List<int> listValues = new List<int>(2);

            keySet.Add(m_StringBufferToIndexMap[parseResult[0]], m_StringBufferToIndexMap[parseResult[1]]); //有重复的健会报错

            listValues.Add(m_StringBufferToIndexMap[parseResult[0]]);
            listValues.Add(m_StringBufferToIndexMap[parseResult[1]]);

            foreach (var v in listValues)
            {
                data = BitConverter.GetBytes(v);
                BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
                AddBytesData(bytes, data);
            }
        }

        return bytes.ToArray();
    }

    private byte[] ParseMapS2I(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        data = BitConverter.GetBytes(valueSplit.Length);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        Dictionary<int, int> keySet = new Dictionary<int, int>();

        for (int i = 0; i < valueSplit.Length; i++)
        {
            var parseResult = valueSplit[i].Split(':');

            List<int> listValues = new List<int>(2);

            keySet.Add(m_StringBufferToIndexMap[parseResult[0]], int.Parse(parseResult[1])); //有重复的健会报错

            listValues.Add(m_StringBufferToIndexMap[parseResult[0]]);
            listValues.Add(int.Parse(parseResult[1]));

            foreach (var v in listValues)
            {
                data = BitConverter.GetBytes(v);
                BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
                AddBytesData(bytes, data);
            }
        }

        return bytes.ToArray();
    }

    private byte[] ParseHashSetInt(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        var dataCount = valueSplit.Length;
        data = BitConverter.GetBytes(dataCount);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        Dictionary<int,bool> keyMap = new Dictionary<int, bool>();

        List<int> listValues = new List<int>(dataCount);
        foreach (var v in valueSplit)
        {
            keyMap.Add(int.Parse(v),true);
            listValues.Add(int.Parse(v));
        }

        foreach (var v in listValues)
        {
            data = BitConverter.GetBytes(v);
            BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
            AddBytesData(bytes, data);
        }

        return bytes.ToArray();
    }

    private byte[] ParseHashSetString(short fieldIndex, string value)
    {
        List<byte> bytes = new List<byte>();

        var data = BitConverter.GetBytes(fieldIndex);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        string[] valueSplit = value.Split(',');
        var dataCount = valueSplit.Length;
        data = BitConverter.GetBytes(dataCount);
        BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
        AddBytesData(bytes, data);

        Dictionary<int, bool> keyMap = new Dictionary<int, bool>();

        List<int> listValues = new List<int>(dataCount);
        foreach (var v in valueSplit)
        {
            keyMap.Add(m_StringBufferToIndexMap[v], true);
            listValues.Add(m_StringBufferToIndexMap[v]);
        }

        foreach (var v in listValues)
        {
            data = BitConverter.GetBytes(v);
            BitConverterUtility.ConvertToLittleEndian(data, 0, data.Length);
            AddBytesData(bytes, data);
        }

        return bytes.ToArray();
    }
}
