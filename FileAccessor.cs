using UnityEngine;
using System.Collections;
using System.IO;
using System;

using FilePath = System.String;
using System.Collections.Generic;

namespace TelemetryTools
{
    public class FileAccessor : MonoBehaviour
    {

        private Dictionary<string, IEnumerator> ienumerators = new Dictionary<FilePath, IEnumerator>();

        void Start()
        {
            TelemetryTools.Telemetry.Instance.FileAccessor = this;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void WriteDataToFile(byte[] data, FileInfo file)
        {
            StartCoroutine(WriteDataToFileCoroutine(data, file));
        }

        IEnumerator WriteDataToFileCoroutine(byte[] data, FileInfo file)
        {
            FileStream fileStream = null;
            try
            {
                fileStream = file.Open(FileMode.Create);
                for (int i = 0; i < data.Length; i += 1024)
                {
                    fileStream.Write(data, i, Math.Min(data.Length - i, 1024));
                    yield return false;
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }
            yield return true;
        }

        /// <summary>
        /// Uses an IEnumerator to write strings to file. Cancels previous writes if need to re-access file. If this is called too frequently there is the chance that the file will never be fully written!
        /// </summary>
        /// <param name="stringList"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool WriteStringsToFile(string[] stringList, FileInfo file)
        {
            if (ienumerators.ContainsKey(file.FullName))
            {
                IEnumerator value;
                ienumerators.TryGetValue(file.FullName, out value);
                StopCoroutine(value);
                ((IDisposable)value).Dispose();
                ienumerators.Remove(file.FullName);
                return false;
            }
            else
            {
                IEnumerator coroutine = WriteStringsToFileCoroutine(stringList, file);
                StartCoroutine(coroutine);
                ienumerators.Add(file.FullName, coroutine);
                return true;
            }
        }

        private IEnumerator WriteStringsToFileCoroutine(string[] stringList, FileInfo file)
        {
            FileStream fileStream = null;
            byte[] newLine = Telemetry.StringToBytes("\n");
            try
            {
                fileStream = file.Open(FileMode.Create);

                int i = 0;
                foreach (FilePath str in stringList)
                {
                    byte[] bytes = Telemetry.StringToBytes(str);
                    fileStream.Write(bytes, 0, bytes.Length);
                    fileStream.Write(newLine, 0, newLine.Length);
                    i++;
                    if (i % 10 == 0)
                        yield return false;
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }
            yield return true;
        }

    }
}