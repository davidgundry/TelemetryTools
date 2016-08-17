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
        private Buffer Buffer { get; set; }
        public KeyManager KeyManager { get; private set; }

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
        public FileAccessor FileAccessor { get; set; }
#else
        public object FileAccessor;
#endif

#if POSTENABLED 
        public bool HTTPPostEnabled { get; set; }
        public BufferUploadConnection DataConnection { get; set; }
        public UserDataUploadConnection UserDataConnection { get; set; }
#endif

        private Dictionary<UserDataKey,string> userData;
        public Dictionary<UserDataKey, string> UserData { get { return userData; } set { userData = value; } }
#if LOCALSAVEENABLED

        private List<FilePath> userDataFilesList;
        public List<FilePath> UserDataFilesList { get { return userDataFilesList; } }
        public int UserDataFiles { get { if (userDataFilesList != null) return userDataFilesList.Count; return 0; } }
        private bool userDataFilesListDirty;
#endif

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

#if LOCALSAVEENABLED
        public Telemetry(FileAccessor fileAccessor, URL uploadURL = "", URL keyServer = "", URL userDataURL = "")
        {
            FileAccessor = fileAccessor;
#else
        public Telemetry(URL uploadURL = "", URL keyServer = "", URL userDataURL = "")
        {
#endif
            Buffer = new Buffer();
            KeyManager = new KeyManager(this, keyServer);
            sessionID = LoadSessionIDFromPlayerPrefs();

#if LOCALSAVEENABLED
            cachedFilesList = FileUtility.ReadStringsFromFile(FileUtility.GetFileInfo(cacheDirectory, cacheListFilename));
            userDataFilesList = FileAccessor.GetUserDataFilesList();
            Debug.Log("Persistant Data Path: " + Application.persistentDataPath);
#endif

#if POSTENABLED
            DataConnection = new BufferUploadConnection(uploadURL);
            DataConnection.OnError += new UploadConnection.ErrorHandler(SaveDataOnBufferUploadErrorIfWeCan);
            UserDataConnection = new UserDataUploadConnection(userDataURL);
            UserDataConnection.OnSuccess += new UploadConnection.SuccessHandler(RemoveLocalCopyOfUploadedUserData);
#endif
        }

        private SessionID LoadSessionIDFromPlayerPrefs()
        {
            SessionID sessionID = (SessionID)PlayerPrefs.GetInt("sessionID");
            PlayerPrefs.SetInt("sessionID", (int)sessionID + 1);
            PlayerPrefs.Save();
            return sessionID;
        }

        public void Update(float deltaTime)
        {
            UserDataConnection.Update(deltaTime);
            DataConnection.Update(deltaTime);
            KeyManager.Update(deltaTime);
            ConnectionLogger.Instance.Update();

            if ((Buffer.FullBufferReadyToSend) && (DataConnection.ReadyToSend))
                SendFullBuffer();

            if (HTTPPostEnabled)
            {
#if LOCALSAVEENABLED
                if ((UserDataConnection.ReadyToSend))
                    UploadBacklogOfCachedUserData();

                if ((!Buffer.FullBufferReadyToSend) && (DataConnection.ReadyToSend))
                    UploadBacklogOfCacheFiles();
#endif
                if ((!Buffer.FullBufferReadyToSend) && (DataConnection.ReadyToSend) && (Buffer.PartialBufferReadyToSend))
                    SendPartialBuffer();
            }
#if LOCALSAVEENABLED
            if (userDataFilesListDirty)
                SaveUserDataFilesList();

            if (cachedFilesListDirty)
                SaveCachedFilesList();
#endif
            
        }

        private void SaveUserDataFilesList()
        {
            userDataFilesListDirty = !FileAccessor.WriteUserDataFilesList(userDataFilesList);
        }

        private void SaveCachedFilesList()
        {
            cachedFilesListDirty = !FileAccessor.WriteStringsToFile(cachedFilesList.ToArray(), FileUtility.GetFileInfo(cacheDirectory, cacheListFilename), append: false); ;
        }

        public void WriteEverything()
        {
#if LOCALSAVEENABLED
            SaveUserData();
#endif

            SendFrame();
            byte[] dataInBuffer = Buffer.GetDataInActiveBuffer();
            bool savedBuffer = false;

#if LOCALSAVEENABLED
            if (KeyManager.CurrentKeyID != null)
            {
                if (dataInBuffer.Length > 0)
                    WriteCacheFile(new KeyAssociatedData(dataInBuffer, sessionID, sequenceID, KeyManager.CurrentKeyID));
                savedBuffer = true;
            }
#endif

            Buffer.ResetBufferPosition();
            sequenceID++;

#if POSTENABLED

            if (DataConnection.ConnectionActive)
            {
    #if LOCALSAVEENABLED
                if (DataConnection.ConnectionActive)
                    WriteCacheFile(((BufferUploadRequest) DataConnection.UploadRequest).GetKeyAssociatedData());
    #endif
                DataConnection.DisposeRequest();
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

            if (!savedBuffer)
                Debug.LogWarning(dataInBuffer.Length + " bytes lost on close: " + Utility.BytesToString(dataInBuffer));
        }


        public bool AllDataUploaded()
        {
            throw new System.NotImplementedException();
        }

        public void UpdateUserData(UserDataKey key, string value)
        {
            if (KeyManager.CurrentKeyID != null)
                userData[key] = value;
            else
                Debug.LogWarning("Cannot log user data without a unique key.");
        }

        public void UploadCurrentUserData()
        {
            UploadUserData(KeyManager.CurrentKeyID);
        }

        private bool UploadUserData(KeyID key)
        {
            if (KeyManager.CurrentKeyIsFetched)
            {
                if (key == KeyManager.CurrentKeyID)
                    UserDataConnection.SendUserData(userData, KeyManager.GetKeyByID(key), key);
#if LOCALSAVEENABLED
                else
                    UserDataConnection.SendUserData(FileAccessor.LoadUserData(key), KeyManager.GetKeyByID(key), key);
#endif
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot upload user data of keyID " + key + " because we have not yet fetched that key.");
                return false;
            }
        }

#if LOCALSAVEENABLED
        public void UploadBacklogOfCachedUserData()
        {
            if (userDataFilesList.Count > 0)
            {
                int lineNumber = 0;
                if (UserDataConnection.ReadyToSend)
                {
                    KeyID key = UserDataFileName(lineNumber);
                    if (key != null)
                        UploadUserData(key);
                    else
                    {
                        userDataFilesList.RemoveAt(lineNumber);
                        userDataFilesListDirty = true;
                    }
                }
            }
        }


        private KeyID UserDataFileName(int lineNumber)
        {
            string[] separators = new string[1];
            separators[0] = ".";
            string[] strs = userDataFilesList[lineNumber].Split(separators, 2, System.StringSplitOptions.None);
            uint result = 0;
            if (UInt32.TryParse(strs[0], out result))
                return result;
            return null;
        }

        private void UploadBacklogOfCacheFiles()
        {
            if (cachedFilesList.Count > 0)
            {
                byte[] data;
                SessionID snID;
                SequenceID sqID;
                KeyID keyID;
                int i = 0;
                if (DataConnection.ReadyToSend)
                {
                    FileUtility.ParseCacheFileName(cacheDirectory, cachedFilesList[i], out snID, out sqID, out keyID);
                    if (KeyManager.KeyIsValid(keyID))
                    {
                        if (FileUtility.LoadFromCacheFile(cacheDirectory, cachedFilesList[i], out data))
                        {
                            if ((data.Length > 0) && (snID != null) && (sqID != null) && (keyID != null)) // key here could be empty because it was not known when the file was saved
                            {
                                DataConnection.UploadData(data, snID, sqID, fileExtension, KeyManager.GetKeyByID(keyID), keyID);
                                File.Delete(FileUtility.GetFileInfo(cacheDirectory, cachedFilesList[i]).FullName);
                                cachedFilesList.RemoveAt(i);
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
                                DataConnection.ResetRequestDelay();
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Error loading from cache file for KeyID:  " + (keyID == null ? "null" : keyID.ToString()));
                            File.Delete(FileUtility.GetFileInfo(cacheDirectory, cachedFilesList[i]).FullName);
                            cachedFilesList.RemoveAt(i);
                            cachedFilesListDirty = true;
                            DataConnection.ResetRequestDelay();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Cannot upload cache file because KeyID " + keyID.ToString() + " has not been retrieved from the key server.");
                        DataConnection.ResetRequestDelay();
                    }
                }
            }
        }
#endif

        private void SendFullBuffer()
        {
            Buffer.FullBufferReadyToSend = !SendBuffer(Utility.RemoveTrailingNulls(Buffer.OffBuffer));
        }

        private void SendPartialBuffer()
        {
            if (SendBuffer(Buffer.GetDataInActiveBuffer(), httpOnly: true))
                Buffer.ResetBufferPosition();
        }

        public bool SendBuffer(byte[] data, bool httpOnly = false)
        {
#if POSTENABLED
            if (HTTPPostEnabled)
            {
                if (DataConnection.ReadyToSend)
                {
                    if (KeyManager.CurrentKeyIsFetched)
                    {
                        DataConnection.UploadData(data, sessionID, sequenceID, fileExtension, KeyManager.CurrentKey, KeyManager.CurrentKeyID);
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
                if (WriteCacheFile(new KeyAssociatedData(data, sessionID, sequenceID, KeyManager.CurrentKeyID)))
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
            FileAccessor.SaveUserData(KeyManager.CurrentKeyID, userData, userDataFilesList);
        }


#endif

        public void SendAllBuffered()
        {
            SendFrame();

            if (Buffer.FullBufferReadyToSend)
            {
                bool success = !SendBuffer(Utility.RemoveTrailingNulls(Buffer.OffBuffer));
                Buffer.FullBufferReadyToSend = success;
            }

            SendBuffer(Buffer.GetDataInActiveBuffer());
            Buffer.ResetBufferPosition();
        }

        public void Restart()
        {
            SendFrame();
            //SendStreamValue(TelemetryTools.Stream.FrameTime, System.DateTime.UtcNow.Ticks);
            startTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            SendKeyValuePair(Strings.Event.TelemetryStart, System.DateTime.UtcNow.ToString("u"));
            SendEvent(Strings.Event.TelemetryStart);
        }

        private long GetTimeFromStart()
        {
            return (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) -startTime;
        }

        private void BufferToFrameBuffer(byte[] data)
        {
            if (KeyManager.CurrentKeyIsSet)
            {
                ConnectionLogger.Instance.AddDataLoggedSinceUpdate((uint)data.Length);
                Buffer.BufferToFrameBuffer(data);
            }
            else
                Debug.LogWarning("Cannot buffer data without it being associated with a unique key. Create a new key.");
        }

        private void BufferInNewFrame(byte[] data)
        {
            if (KeyManager.CurrentKeyIsSet)
            {
                ConnectionLogger.Instance.AddDataLoggedSinceUpdate((uint)data.Length);
                bool firstFrame = frameID == 0;
                Buffer.BufferInNewFrame(data, firstFrame);
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
            frameID++;
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
        private bool WriteCacheFile(KeyAssociatedData keyAssociatedData)
        {
            if (keyAssociatedData.Data.Length > 0)
            {
                FileInfo file = FileUtility.GetFileInfo(cacheDirectory, sessionID, sequenceID, keyAssociatedData.KeyID, GetTimeFromStart(), fileExtension);
                if ((!File.Exists(file.FullName)) || (!FileUtility.IsFileOpen(file)))
                {
                    if (FileAccessor != null)
                    {
                        FileAccessor.WriteDataToFile(keyAssociatedData.Data, file);
                        ConnectionLogger.Instance.AddDataSavedToFileSinceUpdate((uint)keyAssociatedData.Data.Length);

                        cachedFilesList.Add(file.Name);
                        //TODO: Append rather than rewrite everything
                        FileAccessor.WriteStringsToFile(cachedFilesList.ToArray(), FileUtility.GetFileInfo(cacheDirectory, cacheListFilename));
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
        private void SaveDataOnBufferUploadErrorIfWeCan(UploadRequest uploadRequest,string error)
        {
            BufferUploadRequest bufferUploadRequest = (BufferUploadRequest)uploadRequest;
#if LOCALSAVEENABLED
            WriteCacheFile(((BufferUploadRequest) uploadRequest).GetKeyAssociatedData());                        
#else
            DataConnection.LostData += (uint)bufferUploadRequest.Data.Length;
            DataConnection.DisposeRequest();
#endif

        }

        // Used as delegate for UserDataUploadConnection
        private void RemoveLocalCopyOfUploadedUserData(UploadRequest uploadRequest, string response)
        {
            if (uploadRequest.KeyID == KeyManager.CurrentKeyID)
                UserData.Clear();
            else
            {
#if LOCALSAVEENABLED
                DeleteUserDataFile(uploadRequest.KeyID);
#endif
            }
        }

#if LOCALSAVEENABLED
        private void DeleteUserDataFile(KeyID key)
        {
            FileAccessor.DeleteFile(key.ToString() + "." + FileAccessor.userDataFileExtension);
            userDataFilesList.Remove(key.ToString() + "." + FileAccessor.userDataFileExtension);
            userDataFilesListDirty = true;
        }
#endif

    }
}
