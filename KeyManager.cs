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
using TelemetryTools.Strings;

namespace TelemetryTools
{
    public class KeyManager
    {
        private KeyUploadConnection KeyConnection { get; set; }
        public bool ConnectionActive { get { return KeyConnection.ConnectionActive; } }

        private UniqueKey[] Keys { get; set; }

        public int NumberOfKeys { get { if (Keys != null) return Keys.Length; else return 0; } }
        public uint NumberOfUsedKeys { get; private set; }
        public KeyID LatestUsedKey { get { if (NumberOfUsedKeys > 0) return NumberOfUsedKeys - 1; else return null; } }
        public UniqueKey CurrentKey { get { if (Keys != null) if (CurrentKeyID != null) if (CurrentKeyID < Keys.Length) return Keys[(int)CurrentKeyID]; return ""; } }
        public KeyID CurrentKeyID { get; private set; }
        public bool CurrentKeyIsSet { get { return CurrentKeyID != null; } }
        public bool CurrentKeyIsFetched { get { if (CurrentKeyIsSet) return CurrentKeyID < NumberOfKeys;  return false; } }

        public KeyManager(KeyUploadConnection keyUploadConnection)
        {
            Keys = new string[0];

#if POSTENABLED
            KeyConnection = keyUploadConnection;
            KeyConnection.OnKeyReturned += new KeyUploadConnection.KeyReturnedHandler(HandleKeyReturned);
            LoadKeysFromPlayerPrefs();
#endif
        }

        private void LoadKeysFromPlayerPrefs()
        {
            int numKeys = 0;
            if (Int32.TryParse(PlayerPrefs.GetString("numkeys"), out numKeys))
                Keys = new string[numKeys];

            int usedKeysParsed = 0;
            Int32.TryParse(PlayerPrefs.GetString("usedkeys"), out usedKeysParsed);
            NumberOfUsedKeys = (uint)usedKeysParsed;

            CurrentKeyID = null;

            for (int i = 0; i < NumberOfKeys; i++)
                Keys[i] = PlayerPrefs.GetString("key" + i);
        }

        public void Update(float deltaTime)
        {
            KeyConnection.Update(deltaTime);
            CheckForAndFetchNewKeys();
        }

        public UniqueKey GetKeyByID(KeyID id)
        {
            return Keys[(uint)id];
        }

        public bool KeyHasBeenFetched(KeyID id)
        {
            return id < NumberOfKeys;
        }

        public void ReuseOrCreateKey()
        {
            if (LatestUsedKey != null)
                ChangeToKey((uint)LatestUsedKey);
            else
                ChangeToNewKey();
        }
                
        public void ChangeToNewKey()
        {
            NumberOfUsedKeys++;
            CurrentKeyID = NumberOfUsedKeys - 1;
            SaveCurrentKeyToUserPrefs();
        }

        public void ChangeToKey(uint key)
        {
            if (key < NumberOfUsedKeys)
            {
                CurrentKeyID = key;
                SaveCurrentKeyToUserPrefs();
            }
            else
                throw new System.ArgumentOutOfRangeException("Tried to change to a key that has not been created");
        }

        private void SaveCurrentKeyToUserPrefs()
        {
            PlayerPrefs.SetString("currentkeyid", CurrentKeyID.ToString());
            PlayerPrefs.SetString("usedkeys", NumberOfUsedKeys.ToString());
            PlayerPrefs.Save();
        }

        private void CheckForAndFetchNewKeys()
        {
            if (NumberOfUsedKeys > NumberOfKeys)
                if (KeyConnection.ReadyToSend)
                    RequestKey();
        }

        private void RequestKey()
        {
            if (NumberOfUsedKeys > NumberOfKeys)
            {
                KeyConnection.RequestUniqueKey(UserProperties, (uint) NumberOfUsedKeys);
            }
        }

        private static KeyValuePair<UserDataKey, string>[] UserProperties
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

        private void HandleKeyReturned(UniqueKey keyReturned)
        {
            AddNewKey(keyReturned);
        }

        private void AddNewKey(UniqueKey newKey)
        {
            AddKeyToKeys(newKey);
            SaveNewKeyToPlayerPrefs(newKey);
        }

        private void AddKeyToKeys(UniqueKey newKey)
        {
            UniqueKey[] keyArray = Keys;
            Array.Resize(ref keyArray, NumberOfKeys + 1);
            Keys = keyArray;
            Keys[NumberOfKeys - 1] = newKey;
        }

        private void SaveNewKeyToPlayerPrefs(UniqueKey newKey)
        {
            PlayerPrefs.SetString("key" + (NumberOfKeys - 1), newKey);
            PlayerPrefs.SetString("numkeys", NumberOfKeys.ToString());
            PlayerPrefs.Save();
        }
    }
}