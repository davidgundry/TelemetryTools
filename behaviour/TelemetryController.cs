#if (!UNITY_WEBPLAYER)
#define LOCALSAVEENABLED
#endif

using UnityEngine;

namespace TelemetryTools.Behaviour
{
#if LOCALSAVEENABLED
    [RequireComponent(typeof(FileAccessor))]
#endif
    public class TelemetryController : MonoBehaviour
    {
        public bool showLogging;

        public Telemetry Telemetry { get; set; }
#if LOCALSAVEENABLED
        public FileAccessor FileAccessor { get; private set; }
#endif

        void Awake()
        {
            DontDestroyOnLoad(this);

            if (FindObjectsOfType(GetType()).Length > 1)
            {
                Destroy(gameObject);
            }
#if LOCALSAVEENABLED
            FileAccessor = GetComponent<FileAccessor>();
#endif
        }

        void Start()
        {
            
        }

        public void SetRemoteURLsFromPlayerPrefs()
        {
            SetRemoteURLs(PlayerPrefs.GetString("URL"));
        }

        public void SetRemoteURLs(string baseURL)
        {
            if (Telemetry != null)
            {
                Telemetry.DataConnection.SetURL(baseURL + "/import.php");
                Telemetry.KeyManager.KeyConnection.SetURL(baseURL + "/key.php");
                Telemetry.UserDataConnection.SetURL(baseURL + "/userdata.php");
            }
            else
                throw new TelemetryDoesntExistException();
        }

        public void ChangeKey()
        {
            if (Telemetry != null)
            {
                Telemetry.KeyManager.ChangeKey();
            }
            else
                throw new TelemetryDoesntExistException();
        }

        public void ChangeKey(int key)
        {
            if (Telemetry != null)
            {
                if (key < 0)
                    throw new System.ArgumentOutOfRangeException("All key IDs are positive integers");
                uint keyId = (uint)key;
                Telemetry.KeyManager.ChangeKey(keyId);
            }
            else 
                throw new TelemetryDoesntExistException();
        }

        public string GetKey()
        {
            if (Telemetry != null)
            {
                return Telemetry.KeyManager.CurrentKey;
            }
            throw new TelemetryDoesntExistException();
        }

        public string GetKey(int key)
        {
            if (Telemetry != null)
            {
                if (key < 0)
                    throw new System.ArgumentOutOfRangeException("All key IDs are positive integers");
                uint keyId = (uint)key;
                return Telemetry.KeyManager.GetKeyByID(keyId);
            }
            throw new TelemetryDoesntExistException();
        }

        public void WriteEverything()
        {
            if (Telemetry != null)
            {
                Telemetry.WriteEverythingOnQuit();
            }
            else
                throw new TelemetryDoesntExistException();
        }

        public void UpdateUserData(string key, string value)
        {
            if (Telemetry != null)
            {
                Telemetry.AddOrUpdateUserDataKeyValue(key, value);
            }
            else
                throw new TelemetryDoesntExistException();
        }

        void Update()
        {
            if (Telemetry != null)
            {
                Telemetry.Update(Time.deltaTime);

                if (showLogging)
                    Debug.Log(TelemetryTools.ConnectionLogger.GetPrettyLoggingRate());
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (Telemetry != null)
            {
                if (pauseStatus)
                    Telemetry.SendEvent(Strings.Event.ApplicationUnpause);
                else
                    Telemetry.SendEvent(Strings.Event.ApplicationPause);
            }
        }

        void OnApplicationQuit()
        {
            if (Telemetry != null)
            {
                Telemetry.WriteEverythingOnQuit();
                Telemetry.SendEvent(Strings.Event.ApplicationQuit);
            }
        }

    }
}