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
            if (telemetryController.Telemetry != null)
            {
                /*EditorGUILayout.LabelField("UploadURL", telemetryController.Telemetry.DataConnection.url);
                EditorGUILayout.LabelField("Key Server", telemetryController.Telemetry.KeyManager.KeyConnection.url);
                EditorGUILayout.LabelField("User Data URL", telemetryController.Telemetry.UserDataConnection.url);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Total HTTP Requests", telemetryController.Telemetry.DataConnection.Requests.ToString());
                EditorGUILayout.LabelField("Total HTTP Success", telemetryController.Telemetry.DataConnection.Successes.ToString());
                EditorGUILayout.LabelField("Total HTTP Errors", telemetryController.Telemetry.DataConnection.Errors.ToString());

                EditorGUILayout.LabelField("Total Key Server Requests", telemetryController.Telemetry.KeyManager.KeyConnection.Requests.ToString());
                EditorGUILayout.LabelField("Total Key Server Success", telemetryController.Telemetry.KeyManager.KeyConnection.Successes.ToString());
                EditorGUILayout.LabelField("Total Key Server Errors", telemetryController.Telemetry.KeyManager.KeyConnection.Errors.ToString());

                EditorGUILayout.LabelField("Total User Data Requests", telemetryController.Telemetry.UserDataConnection.Requests.ToString());
                EditorGUILayout.LabelField("Total User Data Success", telemetryController.Telemetry.UserDataConnection.Successes.ToString());
                EditorGUILayout.LabelField("Total User Data Errors", telemetryController.Telemetry.UserDataConnection.Errors.ToString());
                */
                EditorGUILayout.Space();

                //EditorGUILayout.LabelField("Log Input", Mathf.Round(telemetryMonitor.Telemetry.LoggingRate / 1024) + " KB/s");
                //EditorGUILayout.LabelField("HTTP", Mathf.Round(telemetryMonitor.Telemetry.HTTPPostRate / 1024) + " KB/s");
                //EditorGUILayout.LabelField("File", Mathf.Round(telemetryMonitor.Telemetry.LocalFileSaveRate / 1024) + " KB/s");
                EditorGUILayout.LabelField("Total", Mathf.Round(TelemetryTools.ConnectionLogger.Instance.DataLogged / 1024) + " KB");
                EditorGUILayout.LabelField("Cached Files", telemetryController.Telemetry.CachedFiles.ToString());
#if LOCALSAVEENABLED
                EditorGUILayout.LabelField("User Data Files", telemetryController.Telemetry.UserDataFilesCount.ToString());
#endif
                EditorGUILayout.LabelField("Lost Data", Mathf.Round(TelemetryTools.ConnectionLogger.Instance.LostData / 1024) + " KB");

                EditorGUILayout.Space();

                /*EditorGUILayout.LabelField("Used Keys", telemetryController.Telemetry.KeyManager.NumberOfUsedKeys.ToString());
                EditorGUILayout.LabelField("Keys", telemetryController.Telemetry.KeyManager.NumberOfKeys.ToString());
                EditorGUILayout.LabelField("Current Key", "ID:" + telemetryController.Telemetry.KeyManager.CurrentKeyID.ToString() + " " + telemetryController.Telemetry.KeyManager.CurrentKey);*/

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
            }

            Repaint();

        }
    }
}
#endif