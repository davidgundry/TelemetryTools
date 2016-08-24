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
using SequenceID = System.Nullable<System.UInt32>;
using SessionID = System.Nullable<System.UInt32>;
using FrameID = System.UInt32;
using UserDataKey = System.String;


namespace TelemetryTools
{
    public class Telemetry
    {
        public bool CurrentKeyIsFetched { get { return KeyManager.CurrentKeyIsFetched; } }
        public UniqueKey CurrentKey { get { return KeyManager.CurrentKey; } }

        private Buffer Buffer { get; set; }
        private KeyManager KeyManager { get; set; }

        private Milliseconds startTime = 0;
        private readonly SessionID sessionID = 0;
        private SequenceID sequenceID = 0;
        private FrameID frameID = 0;

#if LOCALSAVEENABLED
        private List<FilePath> cachedFilesList;
        private bool cachedFilesListDirty;
        private FileAccessor FileAccessor { get; set; }
#else
        private object FileAccessor { get; set; }
#endif

#if POSTENABLED 
        private bool HTTPPostEnabled { get; set; }
        private BufferUploadConnection DataConnection { get; set; }
        private UserDataUploadConnection UserDataConnection { get; set; }
#endif

        private Dictionary<UserDataKey, string> UserData { get; set; }
#if LOCALSAVEENABLED

        public List<FilePath> UserDataFilesList { get; private set; }
        public int UserDataFilesCount { get { if (UserDataFilesList != null) return UserDataFilesList.Count; return 0; } }
        private bool UserDataFilesListDirty;

        float cacheFileBacklogDelay;
        float totalCacheFileBacklogDelay;
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
        public Telemetry(FileAccessor fileAccessor, Buffer buffer, KeyManager keyManager, BufferUploadConnection dataConnection, UserDataUploadConnection userDataConnection)
        {
            FileAccessor = fileAccessor;
#else
        public Telemetry(Buffer buffer, KeyManager keyManager, BufferUploadConnection dataConnection, UserDataUploadConnection userDataConnection)
        {
#endif
            Buffer = buffer;
            KeyManager = keyManager;
#if POSTENABLED
            DataConnection = dataConnection;
            DataConnection.OnError += new UploadConnection.ErrorHandler(SaveDataOnBufferUploadErrorIfWeCan);
            UserDataConnection = userDataConnection;
            UserDataConnection.OnSuccess += new UploadConnection.SuccessHandler(RemoveLocalCopyOfUploadedUserData);
#endif
            sessionID = LoadSessionIDFromPlayerPrefs();
            UserData = new Dictionary<UserDataKey, string>();
#if LOCALSAVEENABLED
            cachedFilesList = FileAccessor.GetCacheDataFilesList();
            UserDataFilesList = FileAccessor.GetUserDataFilesList();
#endif
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
                ReduceCacheFileBacklogDelay(deltaTime);
                if ((UserDataConnection.ReadyToSend))
                    UploadBacklogOfCachedUserData();

                if ((!Buffer.FullBufferReadyToSend) && (DataConnection.ReadyToSend))
                    UploadBacklogOfCacheFiles();
#endif
                if ((!Buffer.FullBufferReadyToSend) && (DataConnection.ReadyToSend) && (Buffer.PartialBufferReadyToSend))
                    SendPartialBuffer();
            }
#if LOCALSAVEENABLED
            if (UserDataFilesListDirty)
                SaveUserDataFilesList();

            if (cachedFilesListDirty)
                SaveCachedFilesList();
#endif
        }

        public void ChangeToNewKey()
        {
            SaveDataIfWeHaveKey();
            KeyManager.ChangeToNewKey();
            UserData = new Dictionary<UserDataKey, string>();
            Restart();
        }

        public void ChangeToKey(uint key)
        {
            SaveDataIfWeHaveKey();
#if LOCALSAVEENABLED
            UserData = Telemetry.LoadUserData(CurrentKeyID);
#else
            UserData = new Dictionary<UserDataKey, string>();
#endif
            Restart();
        }

        private void SaveDataIfWeHaveKey()
        {
            if (KeyManager.CurrentKeyIsSet)
            {
#if LOCALSAVEENABLED
                SaveUserData();
#endif
                SendAllBuffered();
            }
        }

        private void Restart()
        {
            SendFrame();
            startTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            SendKeyValuePair(Strings.Event.TelemetryStart, System.DateTime.UtcNow.ToString("u"));
            SendEvent(Strings.Event.TelemetryStart);
        }

        public void WriteEverythingOnQuit()
        {
#if LOCALSAVEENABLED
            SaveUserData(KeyManager.CurrentKeyID);
#endif
            SendFrame();
            WriteDataInActiveBufferToFile();

#if POSTENABLED

            if (DataConnection.ConnectionActive)
            {
#if LOCALSAVEENABLED
                if (DataConnection.ConnectionActive)
                    WriteCacheFileAndAddToList(((BufferUploadRequest)DataConnection.UploadRequest).GetKeyAssociatedData());
#endif
                DataConnection.DisposeRequest();
            }
#endif
        }

        public bool IsAllDataUploaded()
        {
            if (!DataConnection.ConnectionActive)
                if (!UserDataConnection.ConnectionActive)
                    if (UserData.Count == 0)
                        if (!KeyManager.ConnectionActive)
                            if (!Buffer.FullBufferReadyToSend)
                                if (Utility.RemoveTrailingNulls(Buffer.GetDataInActiveBuffer()).Length == 0)
                                    return true;
            return false;
        }

        public void AddOrUpdateUserDataKeyValue(UserDataKey key, string value)
        {
            if (KeyManager.CurrentKeyID != null)
                UserData[key] = value;
            else
                Debug.LogWarning("Cannot log user data without a unique key.");
        }

        public void UploadOrSaveCurrentUserData()
        {
            UploadUserData(KeyManager.CurrentKeyID);
#if LOCALSAVEENABLED
            SaveUserData(KeyManager.CurrentKeyID);
#endif
        }

        public void SendAllBuffered()
        {
            SendFrame();

            if (Buffer.FullBufferReadyToSend)
            {
                bool success = !SendBuffer(Buffer.GetDataInFullBuffer());
                Buffer.FullBufferReadyToSend = success;
            }

            SendBuffer(Buffer.GetDataInActiveBuffer());
            Buffer.ResetBufferPosition();
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
        private void SaveUserData(KeyID keyID)
        {
            FileAccessor.SaveUserData(keyID, UserData, UserDataFilesList);
        }

        private void UploadBacklogOfCachedUserData()
        {
            if (UserDataFilesList.Count > 0)
            {
                int lineNumber = 0;
                if (UserDataConnection.ReadyToSend)
                {
                    KeyID key = UserDataFileName(lineNumber);
                    if (key != null)
                        UploadUserData(key);
                    else
                    {
                        UserDataFilesList.RemoveAt(lineNumber);
                        UserDataFilesListDirty = true;
                    }
                }
            }
        }

        private KeyID UserDataFileName(int lineNumber)
        {
            string[] separators = new string[1];
            separators[0] = ".";
            string[] strs = UserDataFilesList[lineNumber].Split(separators, 2, System.StringSplitOptions.None);
            uint result = 0;
            if (UInt32.TryParse(strs[0], out result))
                return result;
            return null;
        }

        private void UploadBacklogOfCacheFiles()
        {
            bool success = false;
            if (cachedFilesList.Count > 0)
            {
                if (DataConnection.ReadyToSend)
                {
                    int cacheFileLineIndex = 0;
                    KeyAssociatedData cacheData = FileAccessor.LoadDataFromCacheFile(cachedFilesList[cacheFileLineIndex]);
                    if (cacheData != null)
                    {
                        if (KeyManager.KeyHasBeenFetched(cacheData.KeyID))
                        {
                            DataConnection.UploadData(cacheData.Data, cacheData.SessionID, cacheData.SequenceID, FileAccessor.fileExtension, KeyManager.GetKeyByID(cacheData.KeyID), cacheData.KeyID);
                            RemoveLocalCopyOfCacheFile(cacheFileLineIndex);
                            success = true;
                        }
                        else
                            Debug.LogWarning("Cannot upload cache file because KeyID " + cacheData.KeyID.ToString() + " has not been retrieved from the key server.");
                    }
                    else
                    {
                        string keyIDString = (cacheData.KeyID == null ? "null" : cacheData.KeyID.ToString());
                        Debug.LogWarning("Error loading from cache file for KeyID:  " + keyIDString);
                        RemoveLocalCopyOfCacheFile(cacheFileLineIndex);
                    }
                }
            }

            if (!success)
                ResetCacheFileBacklogDelay();
        }

        private void ResetCacheFileBacklogDelay()
        {
            cacheFileBacklogDelay = totalCacheFileBacklogDelay;
        }

        private void RemoveLocalCopyOfCacheFile(int cacheFileLineIndex)
        {
            FileAccessor.DeleteCacheFile(cachedFilesList[cacheFileLineIndex]);
            cachedFilesList.RemoveAt(cacheFileLineIndex);
            cachedFilesListDirty = true;
        }
#endif
        private void SendFullBuffer()
        {
            bool success = SendBuffer(Buffer.GetDataInFullBuffer());
            Buffer.FullBufferReadyToSend = !success;
        }

        private void SendPartialBuffer()
        {
            if (SendBuffer(Buffer.GetDataInActiveBuffer(), httpOnly: true))
                Buffer.ResetBufferPosition();
        }

        private KeyAssociatedData KeyData(byte[] data)
        {
            return new KeyAssociatedData(data, sessionID, sequenceID, KeyManager.CurrentKey, KeyManager.CurrentKeyID);
        }

        private bool SendBuffer(byte[] data, bool httpOnly = false)
        {
#if POSTENABLED
            if (HTTPPostEnabled)
            {
                if (DataConnection.ReadyToSend)
                {
                    if (KeyManager.CurrentKeyIsFetched)
                    {
                        DataConnection.UploadData(KeyData(data));
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
                if (WriteCacheFileAndAddToList(KeyData(data)))
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

        private long GetTimeFromStart()
        {
            return (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - startTime;
        }

        private void BufferToFrameBuffer(byte[] data)
        {
            if (KeyManager.CurrentKeyIsSet)
            {
                ConnectionLogger.Instance.AddDataLoggedSinceUpdate((uint)data.Length);
                Buffer.BufferInFrameBuffer(data);
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

        // Used as delegate for BufferUploadConnection
        private void SaveDataOnBufferUploadErrorIfWeCan(UploadRequest uploadRequest,string error)
        {
            BufferUploadRequest bufferUploadRequest = (BufferUploadRequest)uploadRequest;
#if LOCALSAVEENABLED
            WriteCacheFileAndAddToList(((BufferUploadRequest) uploadRequest).GetKeyAssociatedData());                        
#else
            DataConnection.LostData += (uint)bufferUploadRequest.Data.Length;
            DataConnection.DisposeRequest();
#endif
        }

#if LOCALSAVEENABLED
        private bool WriteCacheFileAndAddToList(KeyAssociatedData cacheFileData)
        {
            if (cacheFileData.Data.Length == 0)
                return true;

            FileInfo file = FileAccessor.WriteCacheFile(cacheFileData);
            if (file != null)
            {
                ConnectionLogger.Instance.AddDataSavedToFileSinceUpdate((uint)cacheFileData.Data.Length);
                cachedFilesList.Add(file.Name);
                FileAccessor.WriteCacheFilesList(cachedFilesList);
                cachedFilesListDirty = true;
                return true;
            }
            return false;
        }
#endif

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
            FileAccessor.DeleteUserDataFile(key.ToString() + "." + FileAccessor.userDataFileExtension);
            UserDataFilesList.Remove(key.ToString() + "." + FileAccessor.userDataFileExtension);
            UserDataFilesListDirty = true;
        }

        private void ReduceCacheFileBacklogDelay(float deltaTime)
        {
            if (cacheFileBacklogDelay > 0)
                cacheFileBacklogDelay -= deltaTime;
        }

        private void SaveUserDataFilesList()
        {
            UserDataFilesListDirty = !FileAccessor.WriteUserDataFilesList(UserDataFilesList);
        }

        private void SaveCachedFilesList()
        {
            cachedFilesListDirty = !FileAccessor.WriteCacheFilesList(cachedFilesList);
        }
#endif

        private void WriteDataInActiveBufferToFile()
        {
            byte[] dataInBuffer = Buffer.GetDataInActiveBuffer();
            if (dataInBuffer.Length > 0)
            {
                bool savedBuffer = false;
#if LOCALSAVEENABLED
                WriteCacheFileAndAddToList(new KeyAssociatedData(dataInBuffer, sessionID, sequenceID, KeyManager.CurrentKeyID));
                savedBuffer = true;
#endif

                if (savedBuffer)
                {
                    Buffer.ResetBufferPosition();
                    sequenceID++;
                }
                else
                    Debug.LogWarning(dataInBuffer.Length + " bytes lost on close: " + Utility.BytesToString(dataInBuffer));
            }
        }

        private SessionID LoadSessionIDFromPlayerPrefs()
        {
            SessionID sessionID = (SessionID)PlayerPrefs.GetInt("sessionID");
            PlayerPrefs.SetInt("sessionID", (int)sessionID + 1);
            PlayerPrefs.Save();
            return sessionID;
        }

        private bool UploadUserData(KeyID key)
        {
            if (KeyManager.CurrentKeyIsFetched)
            {
                if (key == KeyManager.CurrentKeyID)
                    UserDataConnection.SendUserData(UserData, KeyManager.GetKeyByID(key), key);
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

        public UniqueKey GetKeyByID(KeyID keyId)
        {
            return KeyManager.GetKeyByID(keyId);
        }

        public void ReuseOrCreateKey()
        {
            KeyManager.ReuseOrCreateKey();
        }
    }
}
