using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

public class CsBuilder
{
    private static string[] PrimaryKeySuffix = new string[]
    {
        "_pk", "_primarykey"
    };

    private static Dictionary<string, string> TypeMap = new Dictionary<string, string>()
    {
        { "_i","int" },
        { "_s","string"},
        { "_f","float"},
        { "_long","long"},
        { "_short","short"},

        { "_i1","List<int>"},
        { "_i2","List<List<int>>"},
        { "_s1","List<string>"},
        { "_s2","List<List<string>>"},
        { "_f1","List<float>"},
        { "_f2","List<List<float>>"},

        { "_di2i","Dictionary<int,int>"},
        { "_di2s","Dictionary<int,string>"},
        { "_ds2s","Dictionary<string,string>"},
        { "_ds2i","Dictionary<string,int>"},

        { "_hsi","HashSet<int>"},
        { "_hss","HashSet<string>"},
    };

    private string m_ClassName;
    private int m_DataCount;
    private string m_PrimaryKeyType = "int";
    private string m_PrimaryKey = "m_DataMap.Add(m_DataMap.Count, this);";

    private string m_Properties;
    private string m_Fields;

    private List<string> m_FieldNameList = new List<string>();
    private List<string> m_FieldTypeList = new List<string>();

    public static void CreateCsCode(string xmlExportDir, string csExportDir, string templateFile)
    {
        if (Directory.Exists(csExportDir))
        {
            Directory.Delete(csExportDir, true);
        }

        Directory.CreateDirectory(csExportDir);

        var fileInfos = EditorFileUtility.FilterDirectory(xmlExportDir, new string[] { ".xml" }, false);
        foreach (var file in fileInfos)
        {
            ParseXml(file.FullName, csExportDir, templateFile);
        }
    }

    private static CsBuilder ParseXml(string path, string csExportDir, string templateFile)
    {
        CsBuilder csBuilder = new CsBuilder();
        var xmlDoc = new XmlDocument();

        xmlDoc.Load(path);
        var rootNode = xmlDoc.SelectSingleNode("root");

        csBuilder.m_ClassName = Path.GetFileNameWithoutExtension(path);
        csBuilder.m_DataCount = rootNode.SelectSingleNode("Items").ChildNodes.Count;

        var nodeList = rootNode.SelectSingleNode("Props").ChildNodes;
        foreach (var childNode in nodeList)
        {
            var childElement = (XmlElement)childNode;
            var fieldName = childElement.InnerText;
            var hasPrimaryKey = IsPrimaryKey(ref fieldName);
            var type = GetFieldType(ref fieldName);
            if (hasPrimaryKey)
            {
                csBuilder.m_PrimaryKeyType = type;
                csBuilder.m_PrimaryKey = string.Format("m_DataMap.Add(data.{0}, this);", fieldName);
            }
            csBuilder.m_FieldNameList.Add(fieldName);
            csBuilder.m_FieldTypeList.Add(type);

            csBuilder.m_Properties = GetProperties(csBuilder);
            csBuilder.m_Fields = GetFields(csBuilder);
            SaveCs(csBuilder, csExportDir, templateFile);
        }

        return csBuilder;
    }

    public static bool IsPrimaryKey(ref string name)
    {
        foreach (var suffix in PrimaryKeySuffix)
        {
            if (name.ToLower().EndsWith(suffix))
            {
                name = name.Substring(0, name.LastIndexOf('_'));
                return true;
            }
        }
        return false;
    }

    public static string GetFieldType(ref string name)
    {
        var type = "string";
        if (name.IndexOf('_') == -1)
        {
            return type;
        }

        var suffix = name.Substring(name.LastIndexOf('_'), name.Length - name.LastIndexOf('_'));
        if (TypeMap.ContainsKey(suffix.ToLower()))
        {
            type = TypeMap[suffix.ToLower()];
        }

        name = name.Substring(0, name.IndexOf('_'));
        return type;
    }

    private static string GetProperties(CsBuilder csBuilder)
    {
        string format = "\t\tpublic {0} {1} ";
        string suffix = "{ get; private set; }\n";
        var result = new StringBuilder();
        for (int i = 0; i < csBuilder.m_FieldNameList.Count; i++)
        {
            result.Append(string.Format(format, csBuilder.m_FieldTypeList[i], csBuilder.m_FieldNameList[i]));
            result.Append(suffix);
        }
        return result.ToString();
    }

    private static void SaveCs(CsBuilder csBuilder, string csExportDir, string templateFile)
    {
        var codeTemplate = File.ReadAllText(templateFile,Encoding.UTF8);
        FileStream fs = File.Create(csExportDir + "/" + csBuilder.m_ClassName + ".cs");

        codeTemplate = codeTemplate.Replace("$$ClassName$$", csBuilder.m_ClassName);
        codeTemplate = codeTemplate.Replace("$$PrimaryKeyType$$", csBuilder.m_PrimaryKeyType);
        codeTemplate = codeTemplate.Replace("$$DataCount$$", csBuilder.m_DataCount + "");
        codeTemplate = codeTemplate.Replace("$$properties$$", csBuilder.m_Properties);
        codeTemplate = codeTemplate.Replace("$$fields$$", csBuilder.m_Fields);
        codeTemplate = codeTemplate.Replace("$$primaryKeyOp$$", csBuilder.m_PrimaryKey);

        var bytes = Encoding.UTF8.GetBytes(codeTemplate);
        fs.Write(bytes, 0, bytes.Length);
        fs.Flush();
        fs.Close();
    }

    struct FieldInfo
    {
        public string fieldName;
        public string fieldType;
    }


    private static Dictionary<string, string> TypeFunctionHeadMap = new Dictionary<string, string>()
    {
        { "int","\t\tpublic override void SetInt(string fieldName,int value)\n" },
        { "string","\t\tpublic override void SetString(string fieldName, string value)\n"},
        { "float","\t\tpublic override void SetFloat(string fieldName, float value)\n"},
        { "long","\t\tpublic override void SetLong(string fieldName, long value)\n"},
        { "short","\t\tpublic override void SetShort(string fieldName, short value)\n"},

        { "List<int>","\t\tpublic override void SetListInt(string fieldName, List<int> value)\n"},
        { "List<List<int>>","\t\tpublic override void SetListInt2(string fieldName, List<List<int>> value)\n"},
        { "List<string>","\t\tpublic override void SetListString(string fieldName, List<string> value)\n"},
        { "List<List<string>>","\t\tpublic override void SetListString2(string fieldName, List<List<string>> value)\n"},
        { "List<float>","\t\tpublic override void SetListFloat(string fieldName, List<float> value)\n"},
        { "List<List<float>>","\t\tpublic override void SetListFloat2(string fieldName, List<List<float>> value)\n"},

        { "Dictionary<int,int>","\t\tpublic override void SetDictionaryI2I(string fieldName, Dictionary<int, int> value)\n"},
        { "Dictionary<int,string>","\t\tpublic override void SetDictionaryI2S(string fieldName, Dictionary<int, string> value)\n"},
        { "Dictionary<string,string>","\t\tpublic override void SetDictionaryS2S(string fieldName, Dictionary<string, string> value)\n"},
        { "Dictionary<string,int>","\t\tpublic override void SetDictionaryS2I(string fieldName, Dictionary<string, int> value)\n"},

        { "HashSet<int>","\t\tpublic override void SetHashSetInt(string fieldName, HashSet<int> value)\n"},
        { "HashSet<string>","\t\tpublic override void SetHashSetString(string fieldName, HashSet<string> value)\n"},
    }; 
     
    private static string GetFields(CsBuilder csBuilder)
    {
        string format = "\t\t\t\tcase \"{0}\": \n\t\t\t\t\t\tthis.{1} = value;\n\t\t\t\t\t\tbreak;\n";
        var result = new StringBuilder();

        List<FieldInfo> fieldsList = new List<FieldInfo>();
        for (int i = 0; i < csBuilder.m_FieldNameList.Count; i++)
        {
            FieldInfo fi = new FieldInfo();
            fi.fieldName = csBuilder.m_FieldNameList[i];
            fi.fieldType = csBuilder.m_FieldTypeList[i];
            fieldsList.Add(fi);
        }

        fieldsList.Sort((delegate(FieldInfo info, FieldInfo fieldInfo)
        {
            return info.fieldType.CompareTo(fieldInfo.fieldType);
        }));

        Dictionary<string,bool> typeMap = new Dictionary<string, bool>();
         
        for (int i = 0; i < fieldsList.Count; i++)
        {
            if (!typeMap.ContainsKey(fieldsList[i].fieldType))
            {
                result.Append(TypeFunctionHeadMap[fieldsList[i].fieldType]);
                result.Append("\t\t{\n");
                result.Append("\t\t\tswitch (fieldName)\n");
                result.Append("\t\t\t{\n");
                typeMap.Add(fieldsList[i].fieldType,true);
            }

            result.Append(string.Format(format, fieldsList[i].fieldName, fieldsList[i].fieldName));

            if ((i + 1 >= fieldsList.Count) || (fieldsList[i].fieldType != fieldsList[i + 1].fieldType))
            {
                result.Append("\t\t\t\tdefault:\n");
                result.Append("\t\t\t\t\tDebug.LogError(string.Format(\"The data load error, The field does not exist, Field name: {0}, Value name : {1}\", fieldName,value));\n");
                result.Append("\t\t\t\t\tbreak;\n");
                result.Append("\t\t\t}\n");
                result.Append("\t\t}");
                if (!(i + 1 >= fieldsList.Count))
                {
                    result.Append("\n\n");
                }
            }
        }
        return result.ToString();
    }
}