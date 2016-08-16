#if (!UNITY_WEBPLAYER)
#define LOCALSAVEENABLED
#endif

#define POSTENABLED

using System;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using TelemetryTools.Upload;

#if LOCALSAVEENABLED
using System.IO;
#endif

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

namespace TelemetryTools
{
    public class Telemetry
    {
        private Milliseconds startTime = 0;
        private readonly SessionID sessionID = 0;
        private SequenceID sequenceID = 0;
        private FrameID frameID = 0;

        private const FilePath fileExtension = "telemetry";
#if LOCALSAVEENABLED
        // Local
        private const FilePath cacheDirectory = "cache";
        private const FilePath cacheListFilename = "cache.txt";
        private List<FilePath> cachedFilesList;
        private bool cachedFilesListDirty;
        private FileAccessor fileAccessor;
        public FileAccessor FileAccessor { get { return fileAccessor; } set { fileAccessor = value; } }
#else
        public object fileAccessor;
#endif


        private const Milliseconds uploadCachedFilesDelayOnFailure = 10000;

#if POSTENABLED 
        public bool HTTPPostEnabled { get; set; }
        public BufferUploadConnection DataConnection { get; set; }
        public UserDataUploadConnection UserDataConnection { get; set; }
#endif

        private Dictionary<UserDataKey,string> userData;
        public Dictionary<UserDataKey, string> UserData { get { return userData; } set { userData = value; } }
        private const FilePath userDataDirectory = "userdata";
        private const FilePath userDataFileExtension = "userdata";
        private const FilePath userDataListFilename = "userdata.txt";
        private List<FilePath> userDataFilesList;
        public List<FilePath> UserDataFilesList { get { return userDataFilesList; } }
        public int UserDataFiles { get { if (userDataFilesList != null) return userDataFilesList.Count; return 0; } }
        private bool userDataFilesListDirty;

        private const Milliseconds uploadUserDataDelayOnFailure = 10000;



        private Buffer buffer;
        public KeyManager KeyManager { get; private set; }


        public int CachedFiles
        {
            get
            {
#if LOCALSAVEENABLED
                if (cachedFilesList != null)
                    return cachedFilesList.Count;
                else
#endif
                    return 0;
            }
        }

        public Telemetry(URL uploadURL = "", URL keyServer = "", URL userDataURL = "")
        {
            buffer = new Buffer();

            KeyManager = new KeyManager(this, keyServer);

#if LOCALSAVEENABLED
            cachedFilesList = ReadStringsFromFile(GetFileInfo(cacheDirectory, cacheListFilename));
            userDataFilesList = ReadStringsFromFile(GetFileInfo(userDataDirectory, userDataListFilename));
            Debug.Log("Persistant Data Path: " + Application.persistentDataPath);
#endif

            sessionID = (SessionID)PlayerPrefs.GetInt("sessionID");
            PlayerPrefs.SetInt("sessionID", (int)sessionID+1);
            PlayerPrefs.Save();

#if POSTENABLED
            DataConnection = new BufferUploadConnection(uploadURL);
            DataConnection.OnError += new UploadErrorHandler(SaveDataOnWWWErrorIfWeCan);
            UserDataConnection = new UserDataUploadConnection(userDataURL);
            UserDataConnection.OnSuccess += new UploadSuccessHandler(HandleUserDataSuccess);
#endif
        }

        public void Update(float deltaTime)
        {
            if (buffer.OffBufferFull)
                if (!DataConnection.Busy)
                    buffer.OffBufferFull = !SendBuffer(Utility.RemoveTrailingNulls(buffer.OffBuffer));
#if POSTENABLED
            UserDataConnection.Update(deltaTime);
            DataConnection.Update(deltaTime);
            KeyManager.Update(deltaTime, HTTPPostEnabled);

            if (HTTPPostEnabled)
            {
#if LOCALSAVEENABLED
                if ((!userDatawwwBusy) && (ConnectionLogger.Instance.UploadUserDataDelay <= 0))
                    if (!UploadBacklogOfUserData())
                        ConnectionLogger.Instance.UploadUserDataDelay = uploadUserDataDelayOnFailure;
                if ((!offBufferFull) && (!wwwBusy) && (ConnectionLogger.Instance.UploadCacheFilesDelay <= 0))
                    if (!UploadBacklogOfCacheFiles())
                        ConnectionLogger.Instance.UploadCacheFilesDelay = uploadCachedFilesDelayOnFailure;
#endif

                if ((!buffer.OffBufferFull) && (!DataConnection.Busy))
                    if (buffer.ReadyToSend)
                        if (SendBuffer(buffer.GetDataInActiveBuffer(), httpOnly: true))
                            buffer.ResetBufferPosition();
            }
#endif
#if LOCALSAVEENABLED
            if (userDataFilesListDirty)
            {
                userDataFilesListDirty = !fileAccessor.WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename),append: false); ;
            }
            if (cachedFilesListDirty)
            {
                cachedFilesListDirty = !fileAccessor.WriteStringsToFile(cachedFilesList.ToArray(), GetFileInfo(cacheDirectory, cacheListFilename), append: false); ;
            }
#endif
            ConnectionLogger.Instance.Update();
        }

        public void WriteEverything()
        {
#if LOCALSAVEENABLED
            SaveUserData(KeyManager.CurrentKeyID, userData, userDataFilesList, fileAccessor);
#endif

            SendFrame();
            byte[] dataInBuffer = buffer.GetDataInActiveBuffer();
            bool savedBuffer = false;

#if LOCALSAVEENABLED
            if (KeyManager.CurrentKeyID != null)
            {
                if (dataInBuffer.Length > 0)
                    WriteCacheFile(dataInBuffer, sessionID, sequenceID, KeyManager.CurrentKeyID);
                savedBuffer = true;
            }
#endif

            buffer.ResetBufferPosition();
            sequenceID++;

#if POSTENABLED

            if (DataConnection.Initialised())
            {
    #if LOCALSAVEENABLED
                if (!www.isDone)
                    WriteCacheFile(wwwData, wwwSessionID, wwwSequenceID, wwwKeyID);
    #endif
                DataConnection.Dispose();
            }


            /*if ((httpPostEnabled) && (!savedBuffer))
            {
                if (currentKeyID != null)
                    if (currentKeyID < NumberOfKeys)
                    {
                        WWW stopwww = null;
                        SendByHTTPPost(dataInBuffer, sessionID, sequenceID, fileExtension, keys[(uint) currentKeyID], currentKeyID, uploadURL, ref stopwww, out wwwData, out wwwSequenceID, out wwwSessionID, out wwwBusy, out wwwKey, out wwwKeyID, ref totalHTTPRequestsSent);
                        savedBuffer = true;
                    }
            }*/
#endif

            //if (!savedBuffer)
            //    lostData += (uint) dataInBuffer.Length;
        }


        public bool AllDataUploaded()
        {
            return false;
        }


        public void UpdateUserData(UserDataKey key, string value)
        {
            if (KeyManager.CurrentKeyID != null)
                userData[key] = value;
            else
                Debug.LogWarning("Cannot log user data without a unique key.");
        }

        private bool UploadUserData(KeyID key)
        {
            if (KeyManager.KeyInUseIsFetched)
            {
                if (key == KeyManager.CurrentKeyID)
                    UserDataConnection.SendByHTTPPost(userData, KeyManager.GetKeyByID(key), key);
#if LOCALSAVEENABLED
                else
                    userDataConnection.SendUserDataByHTTPPost(LoadUserData(key), KeyManager.GetKeyByID(key), key);
#endif
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot upload user data of keyID " + key + " because we have not yet fetched that key.");
                return false;
            }
        }

        public bool UploadBacklogOfUserData()
        {
            if (userDataFilesList.Count > 0)
            {
                int i = 0;
                if (!UserDataConnection.Busy)
                {
                    string[] separators = new string[1];
                    separators[0] = ".";
                    string[] strs = userDataFilesList[i].Split(separators, 2, System.StringSplitOptions.None);
                    uint result = 0;
                    if (UInt32.TryParse(strs[0], out result))
                        UploadUserData(result);
                    else
                    {
                        userDataFilesList.RemoveAt(i);
                        //fileAccessor.WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                        userDataFilesListDirty = true;
                    }

                }
            }
            return UserDataConnection.Busy; // If www is busy, we successfully found something to upload
        }

        #if LOCALSAVEENABLED
        private bool UploadBacklogOfCacheFiles()
        {
            if (cachedFilesList.Count > 0)
            {
                byte[] data;
                SessionID snID;
                SequenceID sqID;
                KeyID keyID;
                int i = 0;
                if (!wwwBusy)
                {
                    ParseCacheFileName(cacheDirectory, cachedFilesList[i], out snID, out sqID, out keyID);
                    if (KeyManager.KeyIsValid(keyID))
                    {
                        if (LoadFromCacheFile(cacheDirectory, cachedFilesList[i], out data))
                        {
                            if ((data.Length > 0) && (snID != null) && (sqID != null) && (keyID != null)) // key here could be empty because it was not known when the file was saved
                            {
                                SendByHTTPPost(data, snID, sqID, fileExtension, KeyManager.GetKeyByID(keyID), keyID, uploadURL, ref www, out wwwData, out wwwSequenceID, out wwwSessionID, out wwwBusy, out wwwKey, out wwwKeyID);
                                File.Delete(GetFileInfo(cacheDirectory, cachedFilesList[i]).FullName);
                                cachedFilesList.RemoveAt(i);
                                //fileAccessor.WriteStringsToFile(cachedFilesList.ToArray(), GetFileInfo(cacheDirectory, cacheListFilename));
                                cachedFilesListDirty = true;
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
                                return false;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Error loading from cache file for KeyID:  " + (keyID == null ? "null" : keyID.ToString()));
                            File.Delete(GetFileInfo(cacheDirectory, cachedFilesList[i]).FullName);
                            cachedFilesList.RemoveAt(i);
                            //fileAccessor.WriteStringsToFile(cachedFilesList.ToArray(), GetFileInfo(cacheDirectory, cacheListFilename));
                            cachedFilesListDirty = true;
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Cannot upload cache file because KeyID " + keyID.ToString() + " has not been retrieved from the key server.");
                        return false;
                    }
                }
            }
            return true; // If www is busy, we successfully found something to upload
        }
#endif

        public bool SendBuffer(byte[] data, bool httpOnly = false)
        {
#if POSTENABLED
            if (HTTPPostEnabled)
            {
                if (!DataConnection.Busy)
                {
                    if (KeyManager.KeyInUseIsFetched)
                    {
                        DataConnection.SendByHTTPPost(data, sessionID, sequenceID, fileExtension, KeyManager.CurrentKey, KeyManager.CurrentKeyID);
                        sequenceID++;
                        return true;
                    }
                }
                else
                    Debug.LogWarning("Cannot send buffer: WWW object busy");
            }
#endif
#if LOCALSAVEENABLED
            if (!httpOnly)
            {
                if (WriteCacheFile(data, sessionID, sequenceID, KeyManager.CurrentKeyID))
                {
                    sequenceID++;
                    return true;
                }
            }
            if (!httpOnly)
#endif
            Debug.LogWarning("Could not deal with buffer: " + Utility.BytesToString(data));

            return false;
        }

#if LOCALSAVEENABLED
        public void SaveUserData()
        {
            SaveUserData(KeyManager.CurrentKeyID, userData, userDataFilesList, fileAccessor);
        }
#endif

        #if LOCALSAVEENABLED
        private static void SaveUserData(KeyID currentKeyID, Dictionary<UserDataKey,string> userData, List<FilePath> userDataFilesList, FileAccessor fileAccessor)
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

                        FileInfo file = GetFileInfo(userDataDirectory, currentKeyID.ToString() + "." + userDataFileExtension);
                        fileAccessor.WriteStringsToFile(stringList, file);
                        userDataFilesList.Remove(file.Name);
                        userDataFilesList.Add(file.Name);
                        //TODO: Append rather than rewrite everything
                        fileAccessor.WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                    }
                    else
                    {
                        File.Delete(GetFileInfo(userDataDirectory, currentKeyID.ToString() + "." + userDataFileExtension).FullName);
                        userDataFilesList.Remove(currentKeyID.ToString() + "." + userDataFileExtension);
                        fileAccessor.WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                    }
                }
                else
                {
                    File.Delete(GetFileInfo(userDataDirectory, currentKeyID.ToString() + "." + userDataFileExtension).FullName);
                    userDataFilesList.Remove(currentKeyID.ToString() + "." + userDataFileExtension);
                    fileAccessor.WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                }
            }
            else
                Debug.LogWarning("UserKeyID not valid. You probably have not set a user key.");
        }
#endif
            
        #if LOCALSAVEENABLED
        public static Dictionary<UserDataKey, string> LoadUserData(KeyID keyIDToLoad)
        {
            if (keyIDToLoad != null)
            {
                List<string> strings = ReadStringsFromFile(GetFileInfo(userDataDirectory, keyIDToLoad.ToString() + "." + userDataFileExtension));
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
#endif



        public void SendAllBuffered()
        {
            SendFrame();

            if (buffer.OffBufferFull)
            {
                bool success = !SendBuffer(Utility.RemoveTrailingNulls(buffer.OffBuffer));
                buffer.OffBufferFull = success;
            }

            SendBuffer(buffer.GetDataInActiveBuffer());
            buffer.ResetBufferPosition();
        }

        public void Restart()
        {
            SendFrame();
            //SendStreamValue(TelemetryTools.Stream.FrameTime, System.DateTime.UtcNow.Ticks);
            startTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            SendKeyValuePair(TelemetryTools.Event.TelemetryStart, System.DateTime.UtcNow.ToString("u"));
            SendEvent(Event.TelemetryStart);
        }


        private long GetTimeFromStart()
        {
            return (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) -startTime;
        }

        private void BufferToFrameBuffer(byte[] data)
        {
            if (KeyManager.KeyInUse)
            {
                ConnectionLogger.Instance.AddDataLoggedSinceUpdate((uint)data.Length);
                buffer.BufferToFrameBuffer(data);
            }
            else
                Debug.LogWarning("Cannot buffer data without it being associated with a unique key. Create a new key.");
        }

        private void BufferInNewFrame(byte[] data)
        {
            if (KeyManager.KeyInUse)
            {
                ConnectionLogger.Instance.AddDataLoggedSinceUpdate((uint)data.Length);
                bool firstFrame = frameID != 0;
                buffer.BufferInNewFrame(data, firstFrame);
            }
            else
                Debug.LogWarning("Cannot buffer data without it being associated with a unique key. Create a new key.");
        }


        public void SendFrame()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"id\":");
            sb.Append(frameID);
            sb.Append("");
            BufferInNewFrame(Utility.StringToBytes(sb.ToString()));
        }

        public void SendEvent(string name, Milliseconds time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(GetTimeFromStart().ToString());
            sb.Append("\":\"");
            sb.Append(name);
            sb.Append("\"");
            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendEvent(string name)
        {
            SendEvent(name, GetTimeFromStart());
        }

        public void SendKeyValuePair(string key, string value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(value);
            sb.Append("\"");
            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendValue(string key, ValueType value, Milliseconds time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":{\"v\":");
            sb.Append(value.ToString());
            sb.Append(",\"t\":");
            sb.Append(GetTimeFromStart().ToString());
            sb.Append("}");
            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendValue(string key, ValueType value)
        {
            SendValue(key, value, GetTimeFromStart());
        }

        public void SendStreamString(string key, string value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(value);
            sb.Append("\"");
            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }


        public void SendStreamValue(string key, ValueType value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":");
            sb.Append(value.ToString());
            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendStreamValue(string key, bool value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":");
            if (value)
                sb.Append("true");
            else
                sb.Append("false");
            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, byte[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, short[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, int[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, long[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i<values.Length-1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, float[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, double[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        public void SendByteDataBase64(string key, byte[] data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(data.Length);
            sb.Append(",");
            sb.Append(System.Convert.ToBase64String(data));
            sb.Append("\"");

            BufferToFrameBuffer(Utility.StringToBytes(sb.ToString()));
        }

        //Looking at the Mongo spec, it seems that it doesn't support this sort of encoding, use SendByteDataBase64 instead.
        public void SendByteDataBinary(string key, byte[] data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            byte[] b1 = Utility.StringToBytes(sb.ToString());

            byte[] b2 = BitConverter.GetBytes((System.Int32) data.Length);
            byte nullByte = (byte) 0;
            byte[] b3 = Utility.StringToBytes("\"");

            byte[] output = new byte[data.Length + b1.Length + b2.Length + 1 + b3.Length];
            System.Buffer.BlockCopy(b1,0,output,0,b1.Length);
            System.Buffer.BlockCopy(b2,0,output,b1.Length,b2.Length);
            output[b1.Length] = nullByte;
            System.Buffer.BlockCopy(data,0,output,b1.Length+b2.Length+1,data.Length);
            System.Buffer.BlockCopy(b3,0,output,b1.Length+b2.Length+data.Length+1,b3.Length);

            BufferToFrameBuffer(output);
        }



#if LOCALSAVEENABLED

        private static void ParseCacheFileName(FilePath directory, FilePath filename, out SessionID sessionID, out SequenceID sequenceID, out KeyID keyID)
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

        private static bool IsFileLocked(FilePath file)
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

        private static bool LoadFromCacheFile(FilePath directory, FilePath filename, out byte[] data)
        {
            data = new byte[0];

            FilePath cacheFile = GetFileInfo(cacheDirectory, filename).FullName;

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

        private static List<FilePath> ReadStringsFromFile(FileInfo file)
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
                    string s = BytesToString(bytes);
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

        private static T ReadValueFromFile<T>(FileInfo file)
        {
            if (file.Exists)
            {

                FileStream fileStream = null;
                try
                {
                    fileStream = file.Open(FileMode.Open);

                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    string s = BytesToString(bytes);

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

        private static void WriteValueToFile(ValueType value, FileInfo file)
        {
            BackgroundWorker bgWorker = new BackgroundWorker();

            bgWorker.DoWork += (o,a) =>
            {
                FileStream fileStream = null;
                try
                {
                    fileStream = file.Open(FileMode.Create);

                    byte[] bytes = StringToBytes(value.ToString() + "\n");
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

        private static bool IsFileOpen(FileInfo file)
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

        private static FileInfo GetFileInfo(FilePath directory, FilePath filename)
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

        private static FileInfo GetFileInfo(FilePath directory,
                                            SessionID sessionID,
                                            SequenceID sequenceID,
                                            KeyID keyID,
                                            long time,
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
            sb.Append(time);
            sb.Append(".");
            sb.Append(fileExtension);

            FilePath filePath = directory + "/" +sb.ToString();
            return new FileInfo(filePath);
        }

        private static FilePath LocalFilePath(FilePath filename)
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

        private bool WriteCacheFile(byte[] data, SessionID sessionID, SequenceID sequenceID, KeyID key)
        {
            if (data.Length > 0)
            {
                FileInfo file = GetFileInfo(cacheDirectory, sessionID, sequenceID, key, GetTimeFromStart(), fileExtension);
                if ((!File.Exists(file.FullName)) || (!IsFileOpen(file)))
                {
                    if (fileAccessor != null)
                    {
                        fileAccessor.WriteDataToFile(data, file);
                        ConnectionLogger.Instance.AddDataSavedToFileSinceUpdate((uint)data.Length);

                        cachedFilesList.Add(file.Name);
                        //TODO: Append rather than rewrite everything
                        fileAccessor.WriteStringsToFile(cachedFilesList.ToArray(), GetFileInfo(cacheDirectory, cacheListFilename));
                        cachedFilesListDirty = true;
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning("Couldn't write cache file because fileAccessor is null");
                        return false;
                    }
                }
                else
                {
                    Debug.LogWarning("Couldn't write cache file because it was open or it already exists");
                    return false;
                }
            }

            return true;
        }

#endif

        // Used as delegate for BufferUploadConnection
        private void SaveDataOnWWWErrorIfWeCan(UploadRequest uploadRequest,string error)
        {
            BufferUploadRequest bufferUploadRequest = (BufferUploadRequest)uploadRequest;
#if LOCALSAVEENABLED
            WriteCacheFile(wwwData, wwwSessionID, wwwSequenceID, wwwKeyID);
            DisposeWWW(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy, ref wwwKey, ref wwwKeyID);
                        
#else
            ConnectionLogger.Instance.AddLostData((uint)bufferUploadRequest.Data.Length);
            DataConnection.Dispose();
#endif

        }

        // Used as delegate for UserDataUploadConnection
        public void HandleUserDataSuccess(UploadRequest uploadRequest, string response)
        {
            if (uploadRequest.KeyID == KeyManager.CurrentKeyID)
                UserData.Clear();
            else
            {
#if LOCALSAVEENABLED
                File.Delete(GetFileInfo(userDataDirectory, wwwKeyID.ToString() + "." + userDataFileExtension).FullName);
                userDataFilesList.Remove(wwwKeyID.ToString() + "." + userDataFileExtension);
                //fileAccessor.WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                userDataFilesListDirty = true;
#endif
            }
        }

    }
}
