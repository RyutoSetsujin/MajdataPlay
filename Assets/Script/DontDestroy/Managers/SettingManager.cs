using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class SettingManager : MonoBehaviour
{
    
    public static SettingManager Instance;
    readonly string JsonPath = GameManager.SettingPath;
    public SettingFile SettingFile;
    private void Awake()
    {
        Instance = this;
        if (File.Exists(JsonPath))
        {
            var js = File.ReadAllText(JsonPath);
            SettingFile = JsonConvert.DeserializeObject<SettingFile>(js);
        }
        else
        {
            SettingFile = new SettingFile();
            var jsnew = JsonConvert.SerializeObject(SettingFile,Formatting.Indented);
            File.WriteAllText(JsonPath, jsnew);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnApplicationQuit()
    {
        SettingFile.lastSelectedSongDifficulty = GameManager.Instance.selectedDiff;
        SettingFile.lastSelectedSongIndex = GameManager.Instance.selectedIndex;
        var jsnew = JsonConvert.SerializeObject(SettingFile,Formatting.Indented);
        File.WriteAllText(JsonPath, jsnew);
    }
}

public enum SoundBackendType
{
    WaveOut,Asio,Unity
}

public class SettingFile
{
    public SoundBackendType SoundBackend { get; set; } = SoundBackendType.WaveOut;
    public int SoundOutputSamplerate { get; set; } = 44100;
    public float TapSpeed = 7.5f;
    public float TouchSpeed = 7.5f;
    public float BackgroundDim = 0.8f;
    public float AudioOffset = 0f;
    public float DisplayOffset = 0f;
    public int lastSelectedSongIndex = 0;
    public int lastSelectedSongDifficulty = 0;
    public int AsioDeviceIndex = 0;
    public bool DisplaySensorDebug = false;
    public float VolumeAnwser = 0.8f;
    public float VolumeBgm = 1f;
    public float VolumeJudge = 0.3f;
    public float VolumeSlide = 0.3f;
    public float VolumeBreak = 0.3f;
    public float VolumeTouch = 0.3f;
    public float VolumeVoice = 1f;
}
