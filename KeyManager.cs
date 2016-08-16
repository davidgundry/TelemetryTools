#define POSTENABLED

using System;
using UnityEngine;

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
using System.Collections.Generic;

using TelemetryTools.Upload;

namespace TelemetryTools
{
    public class KeyManager
    {
        private Telemetry telemetry;

        private UniqueKey[] keys;
        public UniqueKey[] Keys { get { return keys; } }
        public int NumberOfKeys { get { if (keys != null) return keys.Length; else return 0; } }
        public uint NumberOfUsedKeys { get; private set; }
        public KeyID LatestUsedKey { get { if (NumberOfUsedKeys > 0) return NumberOfUsedKeys - 1; else return null; } }
        public KeyID CurrentKeyID { get; private set; }
        public UniqueKey CurrentKey { get { if (keys != null) if (CurrentKeyID != null) if (CurrentKeyID < keys.Length) return keys[(int)CurrentKeyID]; return ""; } }

        public KeyUploadConnection KeyConnection { get; set; }

        private const Milliseconds requestKeyDelayOnFailure = 10000;

        /// <summary>
        /// Returns true if the we have a CurrentKeyID set.
        /// </summary>
        public bool KeyInUse { get { return CurrentKeyID != null; } }

        /// <summary>
        /// Returns true if the CurrentKeyID corresponds to a key we have fetched.
        /// </summary>
        public bool KeyInUseIsFetched
        {
            get
            {
                if (KeyInUse)
                    return CurrentKeyID < NumberOfKeys;
                return false;
            }
        }

        public KeyManager(Telemetry telemetry, URL keyServer)
        {
            this.telemetry = telemetry;
            keys = new string[0];

#if POSTENABLED
            KeyConnection = new KeyUploadConnection(keyServer);
            KeyConnection.OnSuccess += new UploadSuccessHandler(HandleKeySuccess);
            LoadFromPlayerPrefs();
#endif
        }

        private void LoadFromPlayerPrefs()
        {
            int numKeys = 0;
            if (Int32.TryParse(PlayerPrefs.GetString("numkeys"), out numKeys))
                keys = new string[numKeys];

            int usedKeysParsed = 0;
            Int32.TryParse(PlayerPrefs.GetString("usedkeys"), out usedKeysParsed);
            NumberOfUsedKeys = (uint)usedKeysParsed;

            CurrentKeyID = null;

            for (int i = 0; i < NumberOfKeys; i++)
                keys[i] = PlayerPrefs.GetString("key" + i);
        }

        public void Update(float deltaTime, bool httpPostEnabled)
        {
            if (httpPostEnabled)
            {
                KeyConnection.Update(deltaTime);
                if (KeyConnection.NoRequestDelay)
                    RequestKeyIfNone(UserProperties);
            }
        }

        public static KeyValuePair<UserDataKey, string>[] UserProperties
        {
            get
            {
                List<KeyValuePair<UserDataKey, string>> userData = new List<KeyValuePair<UserDataKey, string>>();
                userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.Platform, Application.platform.ToString()));
                userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.Version, Application.version));
                userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.UnityVersion, Application.unityVersion));
                userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.Genuine, Application.genuine.ToString()));
                if (Application.isWebPlayer)
                    userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.WebPlayerURL, Application.absoluteURL));

                return userData.ToArray();
            }
        }

        public UniqueKey GetKeyByID(KeyID id)
        {
            return keys[(uint) id];
        }

        public bool KeyIsValid(KeyID id)
        {
            return id < NumberOfKeys;
        }

        public void ReuseOrCreateKey()
        {
            if (LatestUsedKey != null)
                ChangeKey((uint)LatestUsedKey);
            else
                ChangeKey();
        }
                
        public void ChangeKey()
        {
            NumberOfUsedKeys++;
            ChangeKey(NumberOfUsedKeys - 1, newKey: true);
        }

        public void ChangeKey(uint key, bool newKey = false)
        {
            if (key < NumberOfUsedKeys)
            {
                if (CurrentKeyID != null)
                {
#if LOCALSAVEENABLED
                    telemetry.SaveUserData();
#endif
                    telemetry.SendAllBuffered();
                }

                CurrentKeyID = key;
                if (!newKey)
                {
#if LOCALSAVEENABLED
                    telemetry.UserData = Telemetry.LoadUserData(CurrentKeyID);
#endif
                }
                else
                    telemetry.UserData = new Dictionary<UserDataKey, string>();

                PlayerPrefs.SetString("currentkeyid", CurrentKeyID.ToString());
                PlayerPrefs.SetString("usedkeys", NumberOfUsedKeys.ToString());
                PlayerPrefs.Save();

                telemetry.Restart();
            }
        }

        private void RequestKeyIfNone(KeyValuePair<UserDataKey, string>[] userData)
        {
            if (!KeyConnection.Busy)
                if (NumberOfUsedKeys > NumberOfKeys)
                {
                    KeyConnection.RequestUniqueKey(userData, (uint) NumberOfUsedKeys);
                }
        }






        private void HandleKeySuccess(UploadRequest uploadRequest, string message)
        {
            UniqueKey newKey = GetReturnedKey(message);;
            if (newKey != null)
            {
                ConnectionLogger.Instance.KeyServerSuccess();
                Array.Resize(ref keys, NumberOfKeys + 1);
                keys[NumberOfKeys - 1] = newKey;
                PlayerPrefs.SetString("key" + (NumberOfKeys - 1), newKey);
                PlayerPrefs.SetString("numkeys", NumberOfKeys.ToString());
                PlayerPrefs.Save();
                ConnectionLogger.Instance.UploadUserDataDelay = 0;
                ConnectionLogger.Instance.UploadCacheFilesDelay = 0;
            }
            else
            {
                ConnectionLogger.Instance.KeyServerError();
                KeyConnection.ResetRequestDelay();
            }
        }

        private UniqueKey GetReturnedKey(string message)
        {
            if (message.StartsWith("key:"))
            {
                UniqueKey uniqueKey = message.Substring(4);
                Debug.Log("Key retrieved: " + uniqueKey);
                return uniqueKey;
            }
            else
            {
                Debug.LogWarning("Invalid key retrieved: " + message);
                return null;
            }
        }

    }
}