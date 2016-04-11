using UnityEngine;
using System.Collections;
using System.IO;
using System;

using FilePath = System.String;
using Bytes = System.UInt32;
using System.Collections.Generic;

namespace TelemetryTools
{
    public class FileAccessor : MonoBehaviour
    {

        private Bytes dataToWritePerFrame = 1024*5;
        public Bytes DataToWritePerFrame { get { return dataToWritePerFrame; } set { dataToWritePerFrame = value; } }
        private Dictionary<string, IEnumerator> ienumerators = new Dictionary<FilePath, IEnumerator>();

        void Start()
        {
            TelemetryTools.Telemetry.Instance.FileAccessor = this;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void WriteDataToFile(byte[] data, FileInfo file, bool append = false)
        {
            StartCoroutine(WriteDataToFileCoroutine(data, file, append));
        }

        IEnumerator WriteDataToFileCoroutine(byte[] data, FileInfo file, bool append = false)
        {
            FileStream fileStream = null;
            try
            {
                if (append)
                    fileStream = file.Open(FileMode.Append);
                else
                    fileStream = file.Open(FileMode.Create);
                for (int i = 0; i < data.Length; i += (int) dataToWritePerFrame)
                {
                    fileStream.Write(data, i, Math.Min(data.Length - i, (int) dataToWritePerFrame));
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
        public bool WriteStringsToFile(string[] stringList, FileInfo file, bool append = false)
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
                IEnumerator coroutine = WriteStringsToFileCoroutine(stringList, file, append);
                StartCoroutine(coroutine);
                ienumerators.Add(file.FullName, coroutine);
                return true;
            }
        }

        private IEnumerator WriteStringsToFileCoroutine(string[] stringList, FileInfo file, bool append = false)
        {
            FileStream fileStream = null;
            byte[] newLine = Telemetry.StringToBytes("\n");
            try
            {
                
                if ((append) && (file.Exists))
                    fileStream = file.Open(FileMode.Append, FileAccess.Write);
                else
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