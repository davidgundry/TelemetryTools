using UnityEngine;

namespace TelemetryTools
{
    public class TelemetryMonitor : MonoBehaviour
    {
        public bool showLogging;

        public Telemetry Telemetry { get { return Telemetry.Instance; } }

        void Awake()
        {
            DontDestroyOnLoad(this);

            if (FindObjectsOfType(GetType()).Length > 1)
            {
                Destroy(gameObject);
            }
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
            if (Telemetry.Exists)
            {
                Telemetry.DataConnection.SetURL(baseURL + "/import.php");
                Telemetry.KeyManager.KeyServer = baseURL + "/key.php";
                Telemetry.UserDataConnection.SetURL(baseURL + "/userdata.php");
            }
            else
                throw new TelemetryDoesntExistException();
        }

        public void ChangeKey()
        {
            if (Telemetry.Exists)
            {
                Telemetry.KeyManager.ChangeKey();
            }
            else
                throw new TelemetryDoesntExistException();
        }

        public void ChangeKey(int key)
        {
            if (Telemetry.Exists)
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
            if (Telemetry.Exists)
            {
                return Telemetry.KeyManager.CurrentKey;
            }
            throw new TelemetryDoesntExistException();
        }

        public string GetKey(int key)
        {
            if (Telemetry.Exists)
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
            if (Telemetry.Exists)
            {
                Telemetry.WriteEverything();
            }
            else
                throw new TelemetryDoesntExistException();
        }

        public void UpdateUserData(string key, string value)
        {
            if (Telemetry.Exists)
            {
                Telemetry.UpdateUserData(key, value);
            }
            else
                throw new TelemetryDoesntExistException();
        }

        void Update()
        {
            if (Telemetry.Exists)
            {
                Telemetry.Update();

                if (showLogging)
                    Debug.Log(TelemetryTools.ConnectionLogger.GetPrettyLoggingRate());
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (Telemetry.Exists)
            {
                if (pauseStatus)
                    Telemetry.SendEvent(TelemetryTools.Event.ApplicationUnpause);
                else
                    Telemetry.SendEvent(TelemetryTools.Event.ApplicationPause);
            }
        }

        void OnApplicationQuit()
        {
            if (Telemetry.Exists)
            {
                Telemetry.WriteEverything();
                Telemetry.SendEvent(TelemetryTools.Event.ApplicationQuit);
            }
        }

    }
}