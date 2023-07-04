using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public class SerializedJson<T> where T : new()
{
    public SerializedJson(string path)
    {
        _path = path;
    }

    private string _path;
    
    public T Data { get; set; } = default;
    
    /// <summary>
    /// Attempt to asynchronously load a json file.
    /// If the file does not exist, this will fail.
    /// Use as Unity coroutine.
    /// </summary>
    /// <param name="jsonPath">The full path to the json file.</param>
    /// <param name="onComplete">A callback run once the file is successfully read and parsed.
    /// It is not called if this function fails for any reason.</param>
    /// <param name="onError">A callback run if it fails for any reason, with the reason passed as an argument.</param>
    public IEnumerator LoadSoft(Action onComplete, Action<string> onError = null)
    {
        if (!File.Exists(_path))
        {
            onError?.Invoke("File: [" + _path + "] does not exist!");
            yield break;
        }

        Task<string> jsonTask;
        
        try
        {
            jsonTask = File.OpenText(_path).ReadToEndAsync();
        }
        catch (Exception e)
        {
            Data = default;
            onError?.Invoke(e.Message);
            yield break;
        }

        while (!jsonTask.IsCompleted)
        {
            yield return null;
        }

        if (jsonTask.Exception != null)
        {
            Data = default;
            onError?.Invoke(jsonTask.Exception.Message);
            yield break;
        }

        if (!jsonTask.IsCompletedSuccessfully)
        {
            Data = default;
            onError?.Invoke("Loading file " + _path + " failed for unknown reason");
            yield break;
        }

        Data = JsonUtility.FromJson<SerializeWrapper<T>>(jsonTask.Result).Content;
        
        // we call onComplete out here in case the callback throws
        onComplete();
        yield break;
    }

    /// <summary>
    /// Attempt to asynchronously load a json file.
    /// If the file does not exist, this will attempt to default-construct the wrapped type
    /// and immediately serialize it to the target file, rather than report as an error.
    /// Useful for files you don't necessarily expect to exist already.
    /// Use as a Unity coroutine.
    /// </summary>
    /// <param name="jsonPath">The full path to the json file.</param>
    /// <param name="onComplete">A callback run once the file is successfully read and parsed.
    /// It is not called if this function fails for any reason.</param>
    /// <param name="onError">A callback run if it fails for any reason, with the reason passed as an argument.</param>
    public IEnumerator LoadHard(Action onComplete, Action<string> onError = null)
    {
        if (!File.Exists(_path))
        {
            Data = new T();
            yield return Write(null, onError);
        }

        yield return LoadSoft(onComplete, onError);
    }

    /// <summary>
    /// Attempt to asynchronously write the current Data to a json file.
    /// If the file does not exist, this will attempt to create it.
    /// If the file does exist, it will be overwritten.
    /// </summary>
    /// <param name="onComplete">Called when the file is successfully written to,
    /// and ready to be read from.</param>
    /// <param name="onError">Called if it fails for any reason.</param>
    public IEnumerator Write(Action onComplete = null,
        Action<string> onError = null)
    {
        Task fileWriteTask;
        StreamWriter newFile;
        try
        {
            newFile = File.CreateText(_path);
            var newJson = JsonUtility.ToJson(new SerializeWrapper<T>(Data), true);

            fileWriteTask = newFile.WriteAsync(newJson);
        }
        catch (Exception e)
        {
            onError?.Invoke(e.Message);
            yield break;
        }

        while (!fileWriteTask.IsCompleted)
        {
            yield return null;
        }
        
        if (fileWriteTask.Exception != null)
        {
            newFile.Close();
            onError?.Invoke(fileWriteTask.Exception.Message);
            yield break;
        }

        if (!fileWriteTask.IsCompletedSuccessfully)
        {
            newFile.Close();
            onError?.Invoke("Failed to create a file at " + _path + " for an unknown reason");
            yield break;
        }
        
        // wait for disk write to finish before taking the W
        var writeStatus = newFile.FlushAsync();
        while (!writeStatus.IsCompleted)
        {
            yield return null;
        }
        
        newFile.Close();
        if (writeStatus.Exception != null)
        {
            onError?.Invoke(writeStatus.Exception.Message);
            yield break;
        }

        if (!writeStatus.IsCompletedSuccessfully)
        {
            onError?.Invoke("Failed to create a file at " + _path + " for an unknown reason (flush failed)");
            yield break;
        }
        
        onComplete?.Invoke();
        yield break;
    }
}

[Serializable]
public class SerializeWrapper<T>
{
    public SerializeWrapper(T data)
    {
        content = data;
    }

    [SerializeField] private T content;
    public T Content => content;
}