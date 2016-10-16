using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Aspose.Cells.Tables;

public class ExcelToXmlUtility
{
    private static string[] PrimaryKeySuffix = new string[]
    {
        "_pk", "_primarykey"
    };

    private static string[] IgnoreSuffix = new string[]
    {
        "_g", "_ignore"
    };

    public static string ExoportXml(string excelPath,string path, string sheetName, ListObject listObject, ref string convertResult)
    {
        var result = ValidListObject(listObject, sheetName);
        if (!string.IsNullOrEmpty(result))
        {
            return result;
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        if (File.Exists(path + "/" + sheetName))
        {
            File.Delete(path + "/" + sheetName);
        }

        convertResult = CreateXml(excelPath,path + "/" + sheetName + ".xml", listObject);
        return null;
    }

    private static string ValidListObject(ListObject listObject, string sheetName)
    {
        HashSet<string> hs = new HashSet<string>();
        HashSet<string> fullName = new HashSet<string>();
        foreach (var column in listObject.ListColumns)
        {
            fullName.Add(column.Name);
            if (hs.Contains(CheckName(column.Name)))
            {
                if (CheckSuffix(column.Name) == null)
                {
                    continue;
                }
                return "        <color=#ff0000>-->插入表格中列名重复,生成终止,跳过生成</color>";
            }
            hs.Add(CheckName(column.Name));
        }

        var result = "";
        foreach (var str in hs)
        {
            if (str == sheetName)
            {
                return "        <color=#ff0000>-->插入表格中列名和工作表名相同,生成终止,跳过生成</color>";
            }

            result = CheckVariable(str);
            if (result != null)
            {
                return result;
            }
        }

        string primaryKey = null;
        result = CheckPrimaryKey(fullName, ref primaryKey);
        if (!string.IsNullOrEmpty(result))
        {
            return result;
        }

        if (!string.IsNullOrEmpty(primaryKey))
        {
            result = CheckDuplicateDataInPk(primaryKey, listObject);
            if (!string.IsNullOrEmpty(result))
            {

                return result;

            }
        }



        return null;
    }

    private static string CheckName(string text)
    {
        var index = text.IndexOf("_");
        if (index > 0)
        {
            return text.Substring(0, index);
        }
        return text;
    }

    private static string CreateXml(string excelPath,string path, ListObject listObject)
    {
        XmlDocument xmlDoc = new XmlDocument();
        XmlDeclaration declaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

        XmlNode rootNode = xmlDoc.CreateElement("root");
        XmlNode pathNode = xmlDoc.CreateElement("Path");
        XmlNode propsNode = xmlDoc.CreateElement("Props");
        XmlNode itemsNode = xmlDoc.CreateElement("Items");

        pathNode.InnerText = excelPath;

        List<int> exportIndex = new List<int>();
        for (int i = 0; i < listObject.ListColumns.Count; ++i)
        {
            var lcn = listObject.ListColumns[i].Name;
            var name = CheckSuffix(lcn);
            if (string.IsNullOrEmpty(name))
            {
                continue;
                ;
            }

            XmlNode propNode = xmlDoc.CreateElement("Prop");
            propNode.InnerText = name;
            propsNode.AppendChild(propNode);
            exportIndex.Add(i);
        }

        if (exportIndex.Count <= 0)
        {
            return "        <color=#cca200>-->没有需要导出的数据,跳过生成</color>";
        }

        var dataTable = listObject.DataRange.ExportDataTable();
        for (int i = 0; i < dataTable.Rows.Count; ++i)
        {
            XmlNode itemNode = xmlDoc.CreateElement("Item");
            itemsNode.AppendChild(itemNode);
            foreach (var index in exportIndex)
            {
                if (string.IsNullOrEmpty(dataTable.Rows[i][index].ToString()))
                {
                    continue;
                }
                var xmlNode = xmlDoc.CreateElement(listObject.ListColumns[index].Name);
                xmlNode.InnerText = dataTable.Rows[i][index].ToString();
                itemNode.AppendChild(xmlNode);
            }
        }

        rootNode.AppendChild(pathNode);
        rootNode.AppendChild(propsNode);
        rootNode.AppendChild(itemsNode);
        xmlDoc.AppendChild(rootNode);
        xmlDoc.InsertBefore(declaration, xmlDoc.DocumentElement);

        xmlDoc.Save(path);
        return null;
    }

    private static string CheckSuffix(string text)
    {
        foreach (var gs in IgnoreSuffix)
        {
            if (text.ToLower().EndsWith(gs))
            {
                return null;
            }
        }
        return text;
    }

    private static string CheckVariable(string text)
    {
        var result = "        <color=#ff0000>-->插入表格列名命名必须满足第一个字符是字母开头(a-z,A-Z),其他字符需要以字母(a-z,A-Z),数字(0-9)组成,导出终止,请修复Excel表 -->错误列名：{0}</color>";
        if (string.IsNullOrEmpty(text))
        {
            return string.Format(result, text);
        }

        if (!(text[0] >= 'a' && text[0] <= 'z'))
        {
            return string.Format(result, text);
        }

        for (int i = 1; i < text.Length; ++i)
        {
            var c = text[i];
            if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9'))
            {
                return string.Format(result, text);
            }
        }
        return null;
    }

    private static string CheckPrimaryKey(HashSet<string> columnNames, ref string primaryKey)
    {
        List<string> primaryKeys = new List<string>();
        foreach (var columnName in columnNames)
        {
            foreach (var suffix in PrimaryKeySuffix)
            {
                if (columnName.ToLower().EndsWith(suffix))
                {
                    primaryKeys.Add(columnName);
                }
            }
        }

        if (primaryKeys.Count > 1)
        {
            var result = "        <color=#ff0000>-->Excel表格列名不能包含多个主键,只能有一个主键,导出终止,请修复Excel表 -->列名：{0}</color>";
            var sb = new StringBuilder();
            foreach (var key in primaryKeys)
            {
                sb.Append(key);
                sb.Append(",");
            }
            sb.Remove(sb.Length - 1, 1);
            return string.Format(result, sb);
        }

        if (primaryKeys.Count == 1)
        {
            primaryKey = primaryKeys[0];
        }
        return null;
    }

    private static string CheckDuplicateDataInPk(string primaryKey, ListObject listObject)
    {
        var index = -1;
        for (int i = 0; i < listObject.ListColumns.Count; ++i)
        {
            var lcn = listObject.ListColumns[i].Name;
            if (lcn == primaryKey)
            {
                index = i;
                break;
            }
        }
        if (index > -1)
        {

            HashSet<string> pkData = new HashSet<string>();

            var dataTable = listObject.DataRange.ExportDataTable();

            for (int i = 0; i < dataTable.Rows.Count; ++i)
            {
                if (pkData.Contains(dataTable.Rows[i][index].ToString()))
                {
                    return string.Format("        <color=#ff0000>-->主键列中不能包含重复数据,导出终止,请修复Excel表 --主键>列名：{0}</color>", primaryKey);
                }
                if (string.IsNullOrEmpty(dataTable.Rows[i][index].ToString()))
                {
                    return string.Format("        <color=#ff0000>-->主键列中不能包含空数据,导出终止,请修复Excel表 --主键>列名：{0}</color>", primaryKey);
                }
                pkData.Add(dataTable.Rows[i][index].ToString());
            }
        }

        return null;
    }
}
