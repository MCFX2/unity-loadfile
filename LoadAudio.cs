using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SerializedAudio
{
    /// <summary>
    /// Create a new SerializedAudio object. Does NOT actually load the audio!
    /// </summary>
    /// <param name="path">The path to the audio file.
    /// Can be a URL (like http://example.com/example.mp3) or a file path (like C:\demo.mp3).</param>
    /// <param name="isWeb">True if the path represents a web URL. False if the path represents a file URL.</param>
    public SerializedAudio(string path, bool isWeb)
    {
        this.isWeb = isWeb;
        this.path = path;
    }

    [field: NonSerialized] public AudioClip Audio { get; private set; } = null;

    [SerializeField] private bool isWeb;
    [SerializeField] private string path;

    /// <summary>
    /// Loads the backing audio file based on the configured filename.
    /// Use as a Unity coroutine.
    /// </summary>
    /// <param name="onLoadFinished">Called when the audio has successfully finished loading.
    /// The Audio member is now ready to use.</param>
    /// <param name="onError">Called when the audio has failed to load for some reason,
    /// with the reason and error message as args. If null, the operation will silently fail and no callback will happen.
    /// In either case, the Audio member will be null (even if it was populated before).</param>
    public IEnumerator Load(Action onLoadFinished, Action<UnityWebRequest.Result, string> onError = null)
    {
        var format = GetAudioTypeFromExtension(path);

        if (format == AudioType.UNKNOWN)
        {
            Audio = null;
            onError?.Invoke(UnityWebRequest.Result.DataProcessingError,
                "Unrecognized file format. Does the filename have the correct extension?");
            yield break;
        }
        
        using var www =
            UnityWebRequestMultimedia.GetAudioClip(
                isWeb ? path : ("file://" + path), AudioType.MPEG);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Audio = DownloadHandlerAudioClip.GetContent(www);
            onLoadFinished();
            yield break;
        }
        else
        {
            Audio = null;
            onError?.Invoke(www.result, www.error);
            yield break;
        }
    }

    /// <summary>
    /// Get the audiotype associated with a given file extension.
    /// </summary>
    /// <param name="filename">The filename or web URL to resolve.</param>
    /// <returns>The AudioType associated with the extension.</returns>
    public static AudioType GetAudioTypeFromExtension(string filename)
    {
        var extension = filename[(filename.LastIndexOf('.') + 1)..].ToLower();

        switch (extension)
        {
            case "mp3":
            case "mp2":
            case "mpeg":
                return AudioType.MPEG;
            case "ogg":
                return AudioType.OGGVORBIS;
            case "wav":
                return AudioType.WAV;
            case "aiff":
                return AudioType.AIFF;
            case "xma":
                return AudioType.XMA;
            case "xm":
                return AudioType.XM;
            case "it":
                return AudioType.IT;
            case "mod":
                return AudioType.MOD;
            case "alac":
            case "aac":
                return AudioType.AUDIOQUEUE;
            case "s3m":
                return AudioType.S3M;
            case "vag":
                return AudioType.VAG;
            default:
                return AudioType.UNKNOWN;
        }
    }
}
