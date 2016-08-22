#if (!UNITY_WEBPLAYER)
#define LOCALSAVEENABLED
#endif

using UnityEngine;
using TelemetryTools.Upload;

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

        public Telemetry CreateTelemetry(string baseURL)
        {
            Buffer buffer = new Buffer();
            BufferUploadConnection dataConnection = new BufferUploadConnection(baseURL + "/import.php");
            UserDataUploadConnection userDataConnection = new UserDataUploadConnection(baseURL + "/userdata.php");
            KeyManager keyManager = new KeyManager(new KeyUploadConnection(baseURL + "/key.php"));
            
#if LOCALSAVEENABLED
            FileAccessor fileAccessor = telemetryController.FileAccessor;
            Telemetry telemetry = new Telemetry(fileAccessor, buffer, keyManager, dataConnection, userDataConnection);
#else
            Telemetry telemetry = new Telemetry(buffer, keyManager, dataConnection, userDataConnection);
#endif

            return telemetry;
        }

        public void ChangeKey()
        {
            if (Telemetry != null)
            {
                Telemetry.NewKey();
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
                Telemetry.ChangeKey(keyId);
            }
            else 
                throw new TelemetryDoesntExistException();
        }

        public string GetKey()
        {
            if (Telemetry != null)
            {
                return Telemetry.CurrentKey;
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
                return Telemetry.GetKeyByID(keyId);
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