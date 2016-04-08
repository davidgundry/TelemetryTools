using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class FileAccessor : MonoBehaviour {

    void Start()
    {
        TelemetryTools.Telemetry.Instance.FileAccessor = this;
    }

	// Update is called once per frame
	void Update () {
	
	}

    public void WriteDataToFile(byte[] data, FileInfo file)
    {
        StartCoroutine(WriteDataToFileCoroutine(data,file));
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
}
