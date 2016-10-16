using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UGFoundation.Utility;

public class GameDataConfig
{
    private ExcelExportEditor m_Window;

    private List<short> m_PreloadList;
    private List<short> m_HighList;
    private List<short> m_MediumList;
    private List<short> m_NormalList;
    private List<short> m_LowList;
    private List<short> m_DontLoadList;

    public GameDataConfig(ExcelExportEditor window)
    {
        m_Window = window;
    }

    public string ExportConfig()
    {
        m_PreloadList = new List<short>();
        m_HighList = new List<short>();
        m_MediumList = new List<short>();
        m_NormalList = new List<short>();
        m_LowList = new List<short>();
        m_DontLoadList = new List<short>();

        var fileInfos = EditorFileUtility.FilterDirectory(m_Window.SettingUI.XmlExportDirectory, new string[] {".xml"},false);
        fileInfos.Sort(SortData);

        var priorityMap = new Dictionary<string,int>();
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(m_Window.PriorityConfig.ConfigFilePath);
        var nodeList = xmlDoc.SelectSingleNode("root").ChildNodes;
        foreach (var childNode in nodeList)
        {
            var childElement = (XmlElement)childNode;
            foreach (var file in fileInfos)
            {
                if (Path.GetFileNameWithoutExtension(file.FullName) == childElement.Name)
                {
                    priorityMap.Add(childElement.Name, int.Parse(childElement.InnerText));
                    break;
                }
            }
        }

        for (int i = 0; i < fileInfos.Count; i++)
        {
            var file = fileInfos[i];
            var name = Path.GetFileNameWithoutExtension(file.FullName);
            switch (priorityMap[name])
            {
                case 0:
                    m_PreloadList.Add((short)i);
                    break;
                case 1:
                    m_HighList.Add((short)i);
                    break;
                case 2:
                    m_MediumList.Add((short)i);
                    break;
                case 3:
                    m_NormalList.Add((short)i);
                    break;
                case 4:
                    m_LowList.Add((short)i);
                    break;
                case 5:
                    m_DontLoadList.Add((short)i);
                    break;
            }
        }

        WriteLoadConfig(priorityMap, fileInfos);
        return string.Format("<color=#bbbbbb>写入{0}个xml的加载优先级</color>", fileInfos.Count);
    }

    private void WriteLoadConfig(Dictionary<string, int> priorityMap, List<FileInfos> fileInfos)
    {
        if (Directory.Exists(m_Window.SettingUI.XmlBinExportDirectory))
        {
            Directory.Delete(m_Window.SettingUI.XmlBinExportDirectory,true);
        }
        if (!Directory.Exists(m_Window.SettingUI.XmlBinExportDirectory))
        {
            Directory.CreateDirectory(m_Window.SettingUI.XmlBinExportDirectory);
        }

        FileStream fs = File.Create(m_Window.SettingUI.XmlBinExportDirectory + "/GameDataConfig.xml");

        int flag = GetTrunkFlag();
        int count = priorityMap.Count;
        int bufferBytes = GetStringBufferSize(priorityMap);

        var bytes = BitConverter.GetBytes(flag);
        BitConverterUtility.ConvertToLittleEndian(bytes, 0, 4);
        fs.Write(bytes,0,bytes.Length);

        bytes = BitConverter.GetBytes(count);
        BitConverterUtility.ConvertToLittleEndian(bytes, 0, 4);
        fs.Write(bytes, 0, bytes.Length);

        bytes = BitConverter.GetBytes(bufferBytes);
        BitConverterUtility.ConvertToLittleEndian(bytes, 0, 4);
        fs.Write(bytes, 0, bytes.Length);

        WriteStringBuff(fileInfos, fs);

        WriteTrunk(m_PreloadList, fs);
        WriteTrunk(m_HighList, fs);
        WriteTrunk(m_MediumList, fs);
        WriteTrunk(m_NormalList, fs);
        WriteTrunk(m_LowList, fs);
        WriteTrunk(m_DontLoadList, fs);

        fs.Flush();
        fs.Close();
    }

    private int GetTrunkFlag()
    {
        int flag = 0;
        if (m_PreloadList.Count > 0)
        {
            flag += 1;
        }
        if (m_HighList.Count > 0)
        {
            flag += 1 << 1;
        }
        if (m_MediumList.Count > 0)
        {
            flag += 1 << 2;
        }
        if (m_NormalList.Count > 0)
        {
            flag += 1 << 3;
        }
        if (m_LowList.Count > 0)
        {
            flag += 1 << 4;
        }
        if (m_DontLoadList.Count > 0)
        {
            flag += 1 << 5;
        }
        return flag;
    }

    private int GetStringBufferSize(Dictionary<string, int> priorityMap)
    {
        int bytes = 0;
        foreach (var priority in priorityMap)
        {
            bytes += Encoding.UTF8.GetBytes(priority.Key).Length;
        }
        bytes += priorityMap.Count;
        return bytes;
    }

    private void WriteStringBuff(List<FileInfos> fileInfos ,FileStream fs)
    {
        foreach (var priority in fileInfos)
        {
            var bytes = Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(priority.FullName));
            fs.Write(bytes,0,bytes.Length);
            fs.WriteByte(0);
        }
    }

    private void WriteTrunk(List<short> loadList, FileStream fs)
    {
        if (loadList.Count > 0)
        {
            var bytes = BitConverter.GetBytes(loadList.Count);
            BitConverterUtility.ConvertToLittleEndian(bytes, 0, 4);
            fs.Write(bytes,0,bytes.Length);

            foreach (var priority in loadList)
            {
                bytes = BitConverter.GetBytes(priority);
                BitConverterUtility.ConvertToLittleEndian(bytes, 0, 2);
                fs.Write(bytes,0,bytes.Length);
            }
        }
    }

    private static int SortData(FileInfos obj1, FileInfos obj2)
    {
        return obj1.Name.CompareTo(obj2.Name);
    }
}
