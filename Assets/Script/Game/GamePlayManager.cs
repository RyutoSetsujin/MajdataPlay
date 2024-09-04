using MajdataPlay.Game.Notes;
using MajdataPlay.IO;
using MajdataPlay.Types;
using MajSimaiDecode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#nullable enable
public class GamePlayManager : MonoBehaviour
{
    public static GamePlayManager Instance;
    public (float,float) BreakParams => (0.95f + Math.Max(Mathf.Sin(GetFrame() * 0.20f) * 0.8f, 0), 1f + Math.Min(Mathf.Sin(GetFrame() * 0.2f) * -0.15f, 0));

    AudioSampleWrap audioSample;
    SimaiProcess Chart;
    SongDetail song;
    GameManager settingManager => GameManager.Instance;

    NoteLoader noteLoader;

    Text ErrorText;

    public GameObject notesParent;
    public GameObject tapPrefab;

    public float noteSpeed = 9f;
    public float touchSpeed = 7.5f;

    public float AudioTime = 0f;
    public bool isStart => audioSample.GetPlayState();
    public float CurrentSpeed = 1f;

    private float AudioStartTime = -114514f;
    List<SimaiTimingPoint> AnwserSoundList = new List<SimaiTimingPoint>();
    // Start is called before the first frame update
    private void Awake()
    {
        Instance = this;
        print(GameManager.Instance.SelectedIndex);
        song = GameManager.Instance.SongList[GameManager.Instance.SelectedIndex];
    }

    private void OnPauseButton(object sender,InputEventArgs e)
    {
        if (e.IsButton && e.IsClick && e.Type == SensorType.P1) {
            print("Pause!!");
            BackToList();
        }
    }
    
    void Start()
    {
        InputManager.Instance.BindAnyArea(OnPauseButton);
        audioSample = AudioManager.Instance.LoadMusic(song.TrackPath);
        audioSample.SetVolume(settingManager.Setting.Audio.Volume.BGM);
        ErrorText = GameObject.Find("ErrText").GetComponent<Text>();
        LightManager.Instance.SetAllLight(Color.white);
        try
        {
            var maidata = song.InnerMaidata[(int)GameManager.Instance.SelectedDiff];
            if (maidata == "" || maidata == null) {
                BackToList();
                return;
            }
                
            Chart = new SimaiProcess(maidata);
            if (Chart.notelist.Count == 0)
            {
                BackToList();
                return;
            }
            else
            {
                StartCoroutine(DelayPlay());
            }

            //Generate AnwserSounds
            foreach (var timingPoint in Chart.notelist)
            {
                timingPoint.havePlayed = false;
                if (timingPoint.noteList.All(o => o.isSlideNoHead)) continue;

                AnwserSoundList.Add(timingPoint);
                var holds = timingPoint.noteList.FindAll(o => o.noteType == SimaiNoteType.Hold || o.noteType == SimaiNoteType.TouchHold);
                if (holds.Count == 0) continue;
                foreach (var hold in holds)
                {
                    var newtime = timingPoint.time + hold.holdTime;
                    if(!Chart.notelist.Any(o=>Math.Abs(o.time-newtime) < 0.001)&&
                        !AnwserSoundList.Any(o => Math.Abs(o.time - newtime) < 0.001)
                        )
                        AnwserSoundList.Add(new SimaiTimingPoint(newtime));
                }
            }
            AnwserSoundList = AnwserSoundList.OrderBy(o=>o.time).ToList();
        }
        catch (Exception ex)
        {
            ErrorText.text = "�ָ�note������Ӵ\n" + ex.Message;
            Debug.LogError(ex);
        }
    }

    IEnumerator DelayPlay()
    {
        var settings = settingManager.Setting;

        yield return new WaitForEndOfFrame();
        var BGManager = GameObject.Find("Background").GetComponent<BGManager>();
        if (song.VideoPath != null)
        {
            BGManager.SetBackgroundMovie(song.VideoPath);
        }
        else
        {
            BGManager.SetBackgroundPic(song.SongCover);
        }
        BGManager.SetBackgroundDim(settings.Game.BackgroundDim);

        yield return new WaitForEndOfFrame();
        noteLoader = GameObject.Find("NoteLoader").GetComponent<NoteLoader>();
        noteLoader.noteSpeed = (float)(107.25 / (71.4184491 * Mathf.Pow(settings.Game.TapSpeed + 0.9975f, -0.985558604f)));
        noteLoader.touchSpeed = settings.Game.TouchSpeed;
        try
        {
            noteLoader.LoadNotes(Chart);
        }
        catch (Exception ex)
        {
            ErrorText.text = "����note������Ӵ\n" + ex.Message;
            Debug.LogError(ex);
            StopAllCoroutines();
        }


        yield return new WaitForEndOfFrame();

        GameObject.Find("Notes").GetComponent<NoteManager>().Refresh();
        yield return new WaitForSeconds(2);
        audioSample.Play();
        AudioStartTime = Time.unscaledTime + (float)audioSample.GetCurrentTime();
    }

    private void OnDestroy()
    {
        print("GPManagerDestroy");
        audioSample = null;
        GC.Collect();
    }
    int i = 0;
    // Update is called once per frame
    void Update()
    {
        if (audioSample == null) return;
        //Do not use this!!!! This have connection with sample batch size
        //AudioTime = (float)audioSample.GetCurrentTime();
        if (AudioStartTime == -114514f) return;

        AudioTime = Time.unscaledTime - AudioStartTime - (float)song.First - settingManager.Setting.Judge.DisplayOffset;
        var realTimeDifference = (float)audioSample.GetCurrentTime() - (Time.unscaledTime - AudioStartTime);
        if (i >= AnwserSoundList.Count)
            return;
        if (Math.Abs(realTimeDifference) > 0.04f)
        {
            ErrorText.text = "��⵽��Ƶ��λ��Ӵ\n" + realTimeDifference;
        }
        else if (Math.Abs(realTimeDifference) > 0.02f)
        {
            ErrorText.text = "������Ƶ\n" + realTimeDifference;
            AudioStartTime -= realTimeDifference;
        }



        var noteToPlay = AnwserSoundList[i].time;
        var delta = AudioTime - (noteToPlay) + settingManager.Setting.Judge.DisplayOffset - settingManager.Setting.Judge.AudioOffset;
        //print(delta);
        /*        if(!AnwserSoundList[i].havePlayed && delta > 0)
                {
                    AudioManager.Instance.PlaySFX("answer.wav");
                    AnwserSoundList[i].havePlayed = true;
                    print("lateplay");
                    i++;
                }*/
        if (delta > 0)
        {
            AudioManager.Instance.PlaySFX("answer.wav");
            AnwserSoundList[i].havePlayed = true;
            i++;
        }
    }

    public float GetFrame()
    {
        var _audioTime = AudioTime * 1000;

        return _audioTime / 16.6667f;
    }

    public void BackToList()
    {
        StopAllCoroutines();
        audioSample.Pause();
        audioSample = null;
        //AudioManager.Instance.UnLoadMusic();
        InputManager.Instance.UnbindAnyArea(OnPauseButton);
        StartCoroutine(delayBackToList());

    }
    IEnumerator delayBackToList()
    {
        yield return new WaitForEndOfFrame();
        GameObject.Find("Notes").GetComponent<NoteManager>().DestroyAllNotes();
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene(1);
    }


    public void EndGame(float acc)
    {
        print("GameResult: "+acc);
        var objectCounter = FindFirstObjectByType<ObjectCounter?>();
        if(objectCounter != null)
            GameManager.LastGameResult = objectCounter.GetPlayRecord(song,GameManager.Instance.SelectedDiff);
        StartCoroutine(delayEndGame());
    }

    IEnumerator delayEndGame()
    {
        yield return new WaitForSeconds(2f);
        audioSample.Pause();
        audioSample = null;
        InputManager.Instance.UnbindAnyArea(OnPauseButton);
        SceneManager.LoadScene(3);
    }

}
