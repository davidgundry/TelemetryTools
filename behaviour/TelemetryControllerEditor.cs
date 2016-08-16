#if UNITY_EDITOR

#if (!UNITY_WEBPLAYER)
#define LOCALSAVEENABLED
#endif

using UnityEngine;
using System.Collections;
using UnityEditor;

namespace TelemetryTools.Behaviour
{
    [CustomEditor(typeof(TelemetryController))]
    public class TMonitorEditor : Editor
    {
        private int keyToChangeTo;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TelemetryController telemetryController = (TelemetryController)target;

            EditorGUILayout.LabelField("UploadURL", telemetryController.Telemetry.DataConnection.URL);
            EditorGUILayout.LabelField("Key Server", telemetryController.Telemetry.KeyManager.KeyConnection.URL);
            EditorGUILayout.LabelField("User Data URL", telemetryController.Telemetry.UserDataConnection.URL);

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
            EditorGUILayout.LabelField("Cached Files", telemetryController.Telemetry.CachedFiles.ToString());
#if LOCALSAVEENABLED
            EditorGUILayout.LabelField("User Data Files", telemetryMonitor.Telemetry.UserDataFiles.ToString());
#endif
            EditorGUILayout.LabelField("Lost Data", Mathf.Round(TelemetryTools.ConnectionLogger.Instance.LostData / 1024) + " KB");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Used Keys", telemetryController.Telemetry.KeyManager.NumberOfUsedKeys.ToString());
            EditorGUILayout.LabelField("Keys", telemetryController.Telemetry.KeyManager.NumberOfKeys.ToString());
            EditorGUILayout.LabelField("Current Key", "ID:" + telemetryController.Telemetry.KeyManager.CurrentKeyID.ToString() + " " + telemetryController.Telemetry.KeyManager.CurrentKey);

            /*EditorGUILayout.IntField("Key", keyToChangeTo);
            if (GUILayout.Button("Change Key"))
            {
                myScript.ChangeKey((uint) keyToChangeTo);
            }*/
            if (GUILayout.Button("New Key"))
            {
                telemetryController.ChangeKey();
                telemetryController.UpdateUserData("test", "test");
            }

            Repaint();

        }
    }
}
#endif