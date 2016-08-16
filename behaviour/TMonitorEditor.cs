#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace TelemetryTools.Behaviour
{
    [CustomEditor(typeof(TelemetryMonitor))]
    public class TMonitorEditor : Editor
    {
        private int keyToChangeTo;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TelemetryMonitor telemetryMonitor = (TelemetryMonitor)target;

            EditorGUILayout.LabelField("UploadURL", telemetryMonitor.Telemetry.DataConnection.URL);
            EditorGUILayout.LabelField("Key Server", telemetryMonitor.Telemetry.KeyManager.KeyConnection.URL);
            EditorGUILayout.LabelField("User Data URL", telemetryMonitor.Telemetry.UserDataConnection.URL);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Total HTTP Requests", TelemetryTools.ConnectionLogger.Instance.TotalHTTPRequestsSent.ToString());
            EditorGUILayout.LabelField("Total HTTP Success", TelemetryTools.ConnectionLogger.Instance.TotalHTTPSuccess.ToString());
            EditorGUILayout.LabelField("Total HTTP Errors", TelemetryTools.ConnectionLogger.Instance.TotalHTTPErrors.ToString());

            EditorGUILayout.LabelField("Total Key Server Requests", TelemetryTools.ConnectionLogger.Instance.TotalKeyServerRequestsSent.ToString());
            EditorGUILayout.LabelField("Total Key Server Success", TelemetryTools.ConnectionLogger.Instance.TotalKeyServerSuccess.ToString());
            EditorGUILayout.LabelField("Total Key Server Errors", TelemetryTools.ConnectionLogger.Instance.TotalKeyServerErrors.ToString());

            EditorGUILayout.Space();

            //EditorGUILayout.LabelField("Log Input", Mathf.Round(telemetryMonitor.Telemetry.LoggingRate / 1024) + " KB/s");
            //EditorGUILayout.LabelField("HTTP", Mathf.Round(telemetryMonitor.Telemetry.HTTPPostRate / 1024) + " KB/s");
            //EditorGUILayout.LabelField("File", Mathf.Round(telemetryMonitor.Telemetry.LocalFileSaveRate / 1024) + " KB/s");
            EditorGUILayout.LabelField("Total", Mathf.Round(TelemetryTools.ConnectionLogger.Instance.DataLogged / 1024) + " KB");
            EditorGUILayout.LabelField("Cached Files", telemetryMonitor.Telemetry.CachedFiles.ToString());
            EditorGUILayout.LabelField("User Data Files", telemetryMonitor.Telemetry.UserDataFiles.ToString());
            EditorGUILayout.LabelField("Lost Data", Mathf.Round(TelemetryTools.ConnectionLogger.Instance.LostData / 1024) + " KB");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Used Keys", telemetryMonitor.Telemetry.KeyManager.NumberOfUsedKeys.ToString());
            EditorGUILayout.LabelField("Keys", telemetryMonitor.Telemetry.KeyManager.NumberOfKeys.ToString());
            EditorGUILayout.LabelField("Current Key", "ID:" + telemetryMonitor.Telemetry.KeyManager.CurrentKeyID.ToString() + " " + telemetryMonitor.Telemetry.KeyManager.CurrentKey);

            /*EditorGUILayout.IntField("Key", keyToChangeTo);
            if (GUILayout.Button("Change Key"))
            {
                myScript.ChangeKey((uint) keyToChangeTo);
            }*/
            if (GUILayout.Button("New Key"))
            {
                telemetryMonitor.ChangeKey();
                telemetryMonitor.UpdateUserData("test", "test");
            }

            Repaint();

        }
    }
}
#endif