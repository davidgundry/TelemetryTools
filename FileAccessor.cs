#if (!UNITY_WEBPLAYER)
#define LOCALSAVEENABLED
#endif

#if LOCALSAVEENABLED

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;

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


using TelemetryTools.Behaviour;
using System.Text;

namespace TelemetryTools
{
    public class FileAccessor : MonoBehaviour
    {
        private Bytes dataToWritePerFrame = 1024*5;
        public Bytes DataToWritePerFrame { get { return dataToWritePerFrame; } set { dataToWritePerFrame = value; } }
        private Dictionary<string, IEnumerator> ienumerators = new Dictionary<FilePath, IEnumerator>();

        public const FilePath userDataDirectory = "userdata";
        public const FilePath userDataFileExtension = "userdata";
        public const FilePath userDataListFilename = "userdata.txt";

        private const FilePath cacheDirectory = "cache";
        private const FilePath cacheListFilename = "cache.txt";
        public const FilePath fileExtension = "telemetry";

        public void WriteDataToFile(byte[] data, FileInfo file, bool append = false)
        {
            StartCoroutine(WriteDataToFileCoroutine(data, file, append));
        }

        IEnumerator WriteDataToFileCoroutine(byte[] data, FileInfo file, bool append = false)
        {
            FileStream fileStream = null;
            try
            {
                if (append)
                    fileStream = file.Open(FileMode.Append);
                else
                    fileStream = file.Open(FileMode.Create);
                for (int i = 0; i < data.Length; i += (int) dataToWritePerFrame)
                {
                    fileStream.Write(data, i, Math.Min(data.Length - i, (int) dataToWritePerFrame));
                    yield return false;
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }
            yield return true;
        }

        /// <summary>
        /// Uses an IEnumerator to write strings to file. Cancels previous writes if need to re-access file. If this is called too frequently there is the chance that the file will never be fully written!
        /// </summary>
        /// <param name="stringList"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool WriteStringsToFile(string[] stringList, FileInfo file, bool append = false)
        {
            if (ienumerators.ContainsKey(file.FullName))
            {
                IEnumerator value;
                ienumerators.TryGetValue(file.FullName, out value);
                StopCoroutine(value);
                ((IDisposable)value).Dispose();
                ienumerators.Remove(file.FullName);
                return false;
            }
            else
            {
                IEnumerator coroutine = WriteStringsToFileCoroutine(stringList, file, append);
                StartCoroutine(coroutine);
                ienumerators.Add(file.FullName, coroutine);
                return true;
            }
        }

        private IEnumerator WriteStringsToFileCoroutine(string[] stringList, FileInfo file, bool append = false)
        {
            FileStream fileStream = null;
            byte[] newLine = Utility.StringToBytes("\n");
            try
            {
                
                if ((append) && (file.Exists))
                    fileStream = file.Open(FileMode.Append, FileAccess.Write);
                else
                    fileStream = file.Open(FileMode.Create);

                int i = 0;
                foreach (FilePath str in stringList)
                {
                    byte[] bytes = Utility.StringToBytes(str);
                    fileStream.Write(bytes, 0, bytes.Length);
                    fileStream.Write(newLine, 0, newLine.Length);
                    i++;
                    if (i % 10 == 0)
                        yield return false;
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }
            yield return true;
        }


        public void SaveUserData(KeyID currentKeyID, Dictionary<UserDataKey, string> userData, List<FilePath> userDataFilesList)
        {
            if (currentKeyID != null)
            {
                if (userData != null)
                {
                    if (userData.Count > 0)
                    {
                        string[] stringList = new string[userData.Keys.Count];
                        int i = 0;
                        foreach (string key in userData.Keys)
                        {
                            stringList[i] = key + "," + userData[key];
                            i++;
                        }

                        FileInfo file = FileUtility.GetFileInfo(userDataDirectory, currentKeyID.ToString() + "." + userDataFileExtension);
                        WriteStringsToFile(stringList, file);
                        userDataFilesList.Remove(file.Name);
                        userDataFilesList.Add(file.Name);
                        //TODO: Append rather than rewrite everything
                        WriteStringsToFile(userDataFilesList.ToArray(), FileUtility.GetFileInfo(userDataDirectory, userDataListFilename));
                    }
                    else
                    {
                        File.Delete(FileUtility.GetFileInfo(userDataDirectory, currentKeyID.ToString() + "." + userDataFileExtension).FullName);
                        userDataFilesList.Remove(currentKeyID.ToString() + "." + userDataFileExtension);
                        WriteStringsToFile(userDataFilesList.ToArray(), FileUtility.GetFileInfo(userDataDirectory, userDataListFilename));
                    }
                }
                else
                {
                    File.Delete(FileUtility.GetFileInfo(userDataDirectory, currentKeyID.ToString() + "." + userDataFileExtension).FullName);
                    userDataFilesList.Remove(currentKeyID.ToString() + "." + userDataFileExtension);
                    WriteStringsToFile(userDataFilesList.ToArray(), FileUtility.GetFileInfo(userDataDirectory, userDataListFilename));
                }
            }
            else
                Debug.LogWarning("UserKeyID not valid. You probably have not set a user key.");
        }


        public FileInfo WriteCacheFile(KeyAssociatedData keyAssociatedData)
        {
            if (keyAssociatedData.Data.Length > 0)
            {
                FileInfo file = FileUtility.GetFileInfo(cacheDirectory, keyAssociatedData.SessionID, keyAssociatedData.SequenceID, keyAssociatedData.KeyID, fileExtension);
                if ((!File.Exists(file.FullName)) || (!FileUtility.IsFileOpen(file)))
                {
                    WriteDataToFile(keyAssociatedData.Data, file);

                    return file;
                }
                else
                {
                    Debug.LogWarning("Couldn't write cache file because it was open or it already exists");
                    return null;
                }
            }

            return null;
        }

        public List<FilePath> GetCacheDataFilesList()
        {
            return FileUtility.ReadStringsFromFile(FileUtility.GetFileInfo(cacheDirectory, cacheListFilename));
        }

        public List<FilePath> GetUserDataFilesList()
        {
            return FileUtility.ReadStringsFromFile(FileUtility.GetFileInfo(userDataDirectory, userDataListFilename));
        }

        public bool WriteUserDataFilesList(List<FilePath> userDataFilesList)
        {
            return WriteStringsToFile(userDataFilesList.ToArray(), FileUtility.GetFileInfo(userDataDirectory, userDataListFilename), append: false); ;
        }

        public bool WriteCacheFilesList(List<FilePath> cachedFilesList)
        {
            return WriteStringsToFile(cachedFilesList.ToArray(), FileUtility.GetFileInfo(cacheDirectory, cacheListFilename), append: false); ;
        }

        public static Dictionary<UserDataKey, string> LoadUserData(KeyID keyIDToLoad)
        {
            if (keyIDToLoad != null)
            {
                List<string> strings = FileUtility.ReadStringsFromFile(FileUtility.GetFileInfo(userDataDirectory, keyIDToLoad.ToString() + "." + userDataFileExtension));
                Dictionary<UserDataKey, string> userData = new Dictionary<UserDataKey, string>();
                foreach (string str in strings)
                {
                    string[] separators = new string[1];
                    separators[0] = ",";
                    string[] keyAndValue = str.Split(separators, 2, System.StringSplitOptions.None);
                    userData.Add(keyAndValue[0], keyAndValue[1]);
                }
                return userData;
            }
            return null;
        }

        public static void DeleteUserDataFile(FilePath filePath)
        {
            File.Delete(FileUtility.GetFileInfo(FileAccessor.userDataDirectory, filePath).FullName);
        }
        public static void DeleteCacheFile(FilePath filePath)
        {
            File.Delete(FileUtility.GetFileInfo(cacheDirectory, filePath).FullName);
        }

        public KeyAssociatedData LoadDataFromCacheFile(FilePath filepath)
        {
            byte[] data;
            SessionID snID;
            SequenceID sqID;
            KeyID keyID;

            FileUtility.ParseCacheFileName(cacheDirectory, filepath, out snID, out sqID, out keyID);

            if (FileUtility.LoadFromCacheFile(cacheDirectory, filepath, out data))
            {
                if ((data.Length > 0) && (snID != null) && (sqID != null) && (keyID != null)) // key here could be empty because it was not known when the file was saved
                {
                    return new KeyAssociatedData(data, snID, sqID, keyID);
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Values loaded from cache file seem to be invalid:");
                    if (data.Length <= 0)
                        sb.Append("\n* Data loaded is empty");
                    if (snID == null)
                        sb.Append("\n* Session ID is null");
                    if (sqID == null)
                        sb.Append("\n* Sequence ID is null");
                    if (keyID == null)
                        sb.Append("\n* Key ID is null");

                    Debug.LogWarning(sb.ToString());
                }
            }
            return null;
        }

    }
}
#endif