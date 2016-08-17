#if (!UNITY_WEBPLAYER)
#define LOCALSAVEENABLED
#endif

#if LOCALSAVEENABLED

using BytesPerSecond = System.Single;
using Bytes = System.UInt32;
using Megabytes = System.UInt32;
using Milliseconds = System.Int64;
using FilePath = System.String;
using URL = System.String;
using SequenceID = System.Nullable<System.UInt32>;
using SessionID = System.Nullable<System.UInt32>;
using KeyID = System.Nullable<System.UInt32>;
using FrameID = System.UInt32;
using UserDataKey = System.String;
using UniqueKey = System.String;

using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;

namespace TelemetryTools
{
    public class FileUtility
    {
        public static void ParseCacheFileName(FilePath directory, FilePath filename, out SessionID sessionID, out SequenceID sequenceID, out KeyID keyID)
        {
            sessionID = null;
            sequenceID = null;
            keyID = null;
            directory = LocalFilePath(directory);
            if (Directory.Exists(directory))
            {
                uint sqID = 0;
                uint snID = 0;
                uint kID = 0;
                string[] separators = new string[1];
                separators[0] = ".";
                string[] fileDetails = filename.Split(separators, 4, System.StringSplitOptions.None);
                bool parsed = UInt32.TryParse(fileDetails[0], out snID) && UInt32.TryParse(fileDetails[1], out sqID) && UInt32.TryParse(fileDetails[2], out kID);

                if (parsed)
                {
                    sessionID = snID;
                    sequenceID = sqID;
                    keyID = kID;
                }
                else
                    Debug.LogWarning("Failed to parse filename. List of cache files may be corrputed.");
            }
        }

        public static bool IsFileLocked(FilePath file)
        {
            FileStream stream = null;

            try
            {
                File.Open(file,FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }

        public static bool LoadFromCacheFile(FilePath directory, FilePath filename, out byte[] data)
        {
            data = new byte[0];

            FilePath cacheFile = GetFileInfo(directory, filename).FullName;

            if (File.Exists(cacheFile))
            {
                if (!IsFileLocked(cacheFile))
                {
                    data = File.ReadAllBytes(cacheFile);
                    return true;
                }
            }
            else
                Debug.LogWarning("Attempted to load from from non-existant cache file: " + cacheFile);

            return false;
        }

        public static List<FilePath> ReadStringsFromFile(FileInfo file)
        {
            List<FilePath> list = new List<FilePath>();

            if (file.Exists)
            {
                FileStream fileStream = null;
                try
                {
                    fileStream = file.Open(FileMode.Open);

                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    string s = Utility.BytesToString(bytes);
                    string[] separators = new string[1];
                    separators[0] = "\n";
                    string[] lines = s.Split(separators, Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries);
                    list = new List<FilePath>(lines);
                    return list;
                }
                catch (IOException)
                {
                    return list;
                }
                finally
                {
                    if (fileStream != null)
                    {
                        fileStream.Close();
                        fileStream = null;
                    }
                }
            }
            Debug.LogWarning("Attempted to read strings from non-existant file: " + file.FullName);
            return list;
        }

        public static T ReadValueFromFile<T>(FileInfo file)
        {
            if (file.Exists)
            {

                FileStream fileStream = null;
                try
                {
                    fileStream = file.Open(FileMode.Open);

                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    string s = Utility.BytesToString(bytes);

                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(s);
                }
                catch
                {
                    return default(T);
                }
                finally
                {
                    if (fileStream != null)
                    {
                        fileStream.Close();
                        fileStream = null;
                    }
                }
            }

            Debug.LogWarning("Attempted to read value from non-existant file: " + file.FullName);

            return default(T);
        }

        public static void WriteValueToFile(ValueType value, FileInfo file)
        {
            BackgroundWorker bgWorker = new BackgroundWorker();

            bgWorker.DoWork += (o, a) =>
            {
                FileStream fileStream = null;
                try
                {
                    fileStream = file.Open(FileMode.Create);

                    byte[] bytes = Utility.StringToBytes(value.ToString() + "\n");
                    fileStream.Write(bytes, 0, bytes.Length);
                }
                finally
                {
                    if (fileStream != null)
                    {
                        fileStream.Close();
                        fileStream = null;
                    }
                }
            };
            //new DoWorkEventHandler(BGWorker_WriteValueToFile);
            bgWorker.RunWorkerAsync();
        }

        public static bool IsFileOpen(FileInfo file)
        {
            if (file != null)
            {
                FileStream stream = null;

                try
                {
                    stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    return true;
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }
            }
            return false;
        }

        public static FileInfo GetFileInfo(FilePath directory, FilePath filename)
        {
            directory = LocalFilePath(directory);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log("Created directory " + directory);
            }

            FilePath filePath = LocalFilePath(directory + "/" + filename);
            return new FileInfo(filePath);
        }

        public static FileInfo GetFileInfo(FilePath directory,
                                            SessionID sessionID,
                                            SequenceID sequenceID,
                                            KeyID keyID,
                                            FilePath fileExtension)
        {
            directory = LocalFilePath(directory);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log("Created directory " + directory);
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(sessionID);
            sb.Append(".");
            sb.Append(sequenceID);
            sb.Append(".");
            sb.Append(keyID);
            sb.Append(".");
            sb.Append(fileExtension);

            FilePath filePath = directory + "/" + sb.ToString();
            return new FileInfo(filePath);
        }

        public static FilePath LocalFilePath(FilePath filename)
        {
            /* if (Application.platform == RuntimePlatform.IPhonePlayer)
             {
                 FilePath path = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
                 path = path.Substring(0, path.LastIndexOf('/'));
                 return Path.Combine(Path.Combine(path, "Documents"), filename);
             }
             else if (Application.platform == RuntimePlatform.Android)
             {
                 FilePath path = Application.persistentDataPath;
                 path = path.Substring(0, path.LastIndexOf('/'));
                 return Path.Combine(path, filename);
             }
             else
             {*/
            //TODO: check what's going on here.
            FilePath path = Application.persistentDataPath + "/";
            //path = path.Substring(0, path.LastIndexOf('/')+1);
            //return path + filename;
            return Path.Combine(path, filename);
            //}
        }
    }
}
#endif