using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

public class PriorityConfig
{
    private bool m_NeedLoadConfig;
    private Dictionary<string,int> m_PriorityMap = new Dictionary<string, int>();
    private string m_ConfigFilePath;

    public Dictionary<string, int> PriorityMap
    {
        get { return m_PriorityMap; }
    }

    public string ConfigFilePath
    {
        get { return m_ConfigFilePath; }
    }

    public PriorityConfig()
    {
        m_ConfigFilePath = Application.dataPath + "/Editor/ExcelExportTool/Config/PriorityConfig.xml";
        m_NeedLoadConfig = true;
    }

    public void LoadPriorityConfig()
    {      
        if (File.Exists(m_ConfigFilePath))
        {
            if (m_NeedLoadConfig)
            {
                m_PriorityMap = new Dictionary<string, int>();
               
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(m_ConfigFilePath);
                var nodeList = xmlDoc.SelectSingleNode("root").ChildNodes;
                foreach (var childNode in nodeList)
                {
                    var childElement = (XmlElement)childNode;
                    m_PriorityMap.Add(childElement.Name, int.Parse(childElement.InnerText));
                }

                m_NeedLoadConfig = false;
            }
        }      
    }

    public string SavePriorityConfig(string xmlExportDir)
    {
        var scanXmlNum = 0;
        var dirInfo = new DirectoryInfo(xmlExportDir);
        foreach (var fi in dirInfo.GetFiles())
        {
            if ((fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            {
                continue;
            }
            if (fi.Extension.ToLower() == ".xml")
            {
                var fileName = Path.GetFileNameWithoutExtension(fi.FullName);
                if (!m_PriorityMap.ContainsKey(fileName))
                {
                    m_PriorityMap.Add(fileName,3);
                }
                ++scanXmlNum;
            }
        }

        if (File.Exists(m_ConfigFilePath))
        {
            File.Delete(m_ConfigFilePath);
        }

        if (File.Exists(m_ConfigFilePath +".meta"))
        {
            File.Delete(m_ConfigFilePath + ".meta");
        }

        XmlDocument xmlDoc = new XmlDocument();
        XmlDeclaration declaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
        XmlNode rootNode = xmlDoc.CreateElement("root");

        foreach (var priorityInfo in m_PriorityMap)
        {
            XmlNode node = xmlDoc.CreateElement(priorityInfo.Key);
            node.InnerText = priorityInfo.Value+"";
            rootNode.AppendChild(node);
        }

        xmlDoc.AppendChild(rootNode);
        xmlDoc.InsertBefore(declaration, xmlDoc.DocumentElement);
        xmlDoc.Save(m_ConfigFilePath);

        m_PriorityMap.Clear();
        m_NeedLoadConfig = true;
        return string.Format("<color=#bbbbbb>导出{0}个xml的加载优先级</color>",scanXmlNum);
    }

    public void SaveSinglePriority(string key,string value)
    {
        CreateConfig();

        bool isFind = false;
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(m_ConfigFilePath);
        var rootNode = xmlDoc.SelectSingleNode("root");
        var nodeList = rootNode.ChildNodes;
        foreach (var childNode in nodeList)
        {
            var childElement = (XmlElement)childNode;
            if(childElement.Name == key)
            {
                childElement.InnerText = value;
                isFind = true;
                break;
            }
        }

        if (!isFind)
        {
            var newNode = xmlDoc.CreateElement(key);
            newNode.InnerText = value; 
            rootNode.AppendChild(newNode);
        }

        xmlDoc.Save(m_ConfigFilePath);
    }

    private void CreateConfig()
    {
        if (!File.Exists(m_ConfigFilePath))
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration declaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
            XmlNode rootNode = xmlDoc.CreateElement("root");

            xmlDoc.AppendChild(rootNode);
            xmlDoc.InsertBefore(declaration, xmlDoc.DocumentElement);
            xmlDoc.Save(m_ConfigFilePath);
        }
    }
}
