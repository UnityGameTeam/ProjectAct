using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public struct FileInfos
{
    public string FullName;
    public string Name;
}

public class EditorFileUtility
{
    public static List<FileInfos> FilterDirectory(string path, string[] extNames,bool includeChilrenDir = true)
    {
        List<FileInfos> result = new List<FileInfos>();
        if (!Directory.Exists(path))
        {
            return null;
        }
        
        Queue<DirectoryInfo> dirQueue = new Queue<DirectoryInfo>();
        DirectoryInfo dirInfo = new DirectoryInfo(path);
        dirQueue.Enqueue(dirInfo);
        while (dirQueue.Count > 0)
        {
            dirInfo = dirQueue.Dequeue();

            if (includeChilrenDir)
            {
                foreach (var di in dirInfo.GetDirectories())
                {
                    dirQueue.Enqueue(di);
                }
            }

            foreach (var fi in dirInfo.GetFiles())
            {
                if ((fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }
                foreach (var extName in extNames)
                {
                    if (extName.ToLower() == fi.Extension.ToLower())
                    {
                        FileInfos efi = new FileInfos();
                        efi.FullName = fi.FullName;
                        efi.Name = fi.Name;
                        result.Add(efi);
                        break;
                    }
                }
            }
        }
        return result;
    }

    public static List<string> FilterDirectoryIgnoreExt(string path, string[] extNames, bool includeChilrenDir = true)
    {
        List<string> result = new List<string>();
        if (!Directory.Exists(path))
        {
            return null;
        }

        Queue<DirectoryInfo> dirQueue = new Queue<DirectoryInfo>();
        DirectoryInfo dirInfo = new DirectoryInfo(path);
        dirQueue.Enqueue(dirInfo);
        while (dirQueue.Count > 0)
        {
            dirInfo = dirQueue.Dequeue();

            if (includeChilrenDir)
            {
                foreach (var di in dirInfo.GetDirectories())
                {
                    dirQueue.Enqueue(di);
                }
            }

            foreach (var fi in dirInfo.GetFiles())
            {
                if ((fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }

                bool isIgnore = false;
                foreach (var extName in extNames)
                {
                    if (extName.ToLower() == fi.Extension.ToLower())
                    {                       
                        isIgnore = true;
                        break;
                    }
                }

                if (!isIgnore)
                {
                    result.Add(fi.FullName);
                }
            }
        }
        return result;
    }

    public static string GetMD5HashFromFile(string fileName)
    {
        try
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
        return null;
    }

    public static void CopyDirectory(string sourceDirectory, string destDirectory)
    {
        //判断源目录和目标目录是否存在，如果不存在，则创建一个目录
        if (!Directory.Exists(sourceDirectory))
        {
            Directory.CreateDirectory(sourceDirectory);
        }
        if (!Directory.Exists(destDirectory))
        {
            Directory.CreateDirectory(destDirectory);
        }
        //拷贝文件
        CopyFile(sourceDirectory, destDirectory);

        //拷贝子目录       
        //获取所有子目录名称
        string[] directionName = Directory.GetDirectories(sourceDirectory);

        foreach (string directionPath in directionName)
        {
            //根据每个子目录名称生成对应的目标子目录名称
            string directionPathTemp = destDirectory + "\\" + directionPath.Substring(sourceDirectory.Length + 1);

            //递归下去
            CopyDirectory(directionPath, directionPathTemp);
        }
    }
    public static void CopyFile(string sourceDirectory, string destDirectory)
    {
        //获取所有文件名称
        string[] fileName = Directory.GetFiles(sourceDirectory);

        foreach (string filePath in fileName)
        {
            //根据每个文件名称生成对应的目标文件名称
            string filePathTemp = destDirectory + "\\" + filePath.Substring(sourceDirectory.Length + 1);

            //若不存在，直接复制文件；若存在，覆盖复制
            if (File.Exists(filePathTemp))
            {
                File.Copy(filePath, filePathTemp, true);
            }
            else
            {
                File.Copy(filePath, filePathTemp);
            }
        }
    }

    public static void ClearDirectory(string path)
    {
        DeleteDirectory(path);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    public static void CreateFile(string path)
    {
        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }
        File.Create(path);
    }

    public static void CreateParentDirecotry(string filePath)
    {
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }
    }

    public static string GetPathWithoutExt(string filePath)
    {
        var path = Path.GetDirectoryName(filePath);
        path = path.Replace("\\", "/");
        return path + "/" + Path.GetFileNameWithoutExtension(filePath);
    }

    public static string GetPath(string dirPath)
    {
        var path = Path.GetDirectoryName(dirPath);
        path = path.Replace("\\", "/");
        return path;
    }
}