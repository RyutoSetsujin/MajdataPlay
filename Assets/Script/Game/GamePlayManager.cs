using MajdataPlay.IO;
using MajdataPlay.Types;
using MajSimaiDecode;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Scripting;

namespace MajdataPlay.Game
{
#nullable enable
    public class GamePlayManager : MonoBehaviour
    {
        /// <summary>
        /// 当前逻辑帧的时刻<para>Unit: Second</para>
        /// </summary>
        public float ThisFrameSec { get; private set; } = 0;
        public static GamePlayManager Instance { get; private set; }
        public ComponentState State { get; private set; } = ComponentState.Idle;
        public MaiScore? HistoryScore { get; private set; }
        public (float, float) BreakParams => (0.95f + Math.Max(Mathf.Sin(GetFrame() * 0.20f) * 0.8f, 0), 1f + Math.Min(Mathf.Sin(GetFrame() * 0.2f) * -0.15f, 0));

        AudioSampleWrap? audioSample = null;
        SimaiProcess Chart;
        SongDetail song;

        GameSetting gameSetting = GameManager.Instance.Setting;

        NoteLoader noteLoader;

        Text ErrorText;

        public GameObject notesParent;
        public GameObject tapPrefab;
        public GameObject loadingMask;

        public float noteSpeed = 9f;
        public float touchSpeed = 7.5f;

        public float AudioTime = -114514f;
        public float AudioTimeNoOffset = -114514f;
        public bool IsStart => audioSample?.GetPlayState() ?? false;

        public float CurrentSpeed = 1f;

        private float AudioStartTime = -114514f;
        CancellationTokenSource allTaskTokenSource = new();
        List<AnwserSoundPoint> AnwserSoundList = new List<AnwserSoundPoint>();

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);
        float timeSource
        {
            get
            {
                if (GameManager.Instance.UseUnityTimer)
                    return Time.unscaledTime;

                GetSystemTimePreciseAsFileTime(out var filetime);
                filetime = filetime - fileTimeAtStart;
                //print(filetime);
                return (float)(filetime / 10000000d);
            }
        }
        long fileTimeAtStart = 0;
        Task sfxGeneratingTask;

        private void Awake()
        {
            Instance = this;
            //print(GameManager.Instance.SelectedIndex);
            song = GameManager.Instance.Collection.Current;
            HistoryScore = ScoreManager.Instance.GetScore(song, GameManager.Instance.SelectedDiff);
            GetSystemTimePreciseAsFileTime(out fileTimeAtStart);
        }

        private void OnPauseButton(object sender, InputEventArgs e)
        {
            if (e.IsButton && e.IsClick && e.Type == SensorType.P1)
            {
                print("Pause!!");
                BackToList();
            }
        }

        void Start()
        {
            State = ComponentState.Loading;
            InputManager.Instance.BindAnyArea(OnPauseButton);
            DumpOnlineChart().Forget();
        }

        async UniTask DumpOnlineChart()
        {
            if (song.isOnline)
            {
                LightManager.Instance.SetAllLight(Color.red);
                var loadingText = loadingMask.transform.GetChild(0).GetComponent<TextMeshPro>();
                loadingText.text = "Downloading...";
                var dumpTask = song.DumpToLocal();
                while (!dumpTask.IsCompleted)
                {
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                }
                song = dumpTask.Result;
            }
            await LoadAudioAndChart();
        }

        async UniTask LoadAudioAndChart()
        {
            audioSample = AudioManager.Instance.LoadMusic(song.TrackPath ?? string.Empty);
            if (audioSample is null)
            {
                var loadingText = loadingMask.transform.GetChild(0).GetComponent<TextMeshPro>();
                loadingText.text = "\r\nFailed to load chart\r\n\r\nAudio track not found.";
                loadingText.color = Color.red;
                return;
            }
            audioSample.SetVolume(gameSetting.Audio.Volume.BGM);
            ErrorText = GameObject.Find("ErrText").GetComponent<Text>();
            LightManager.Instance.SetAllLight(Color.white);
            try
            {

                var maidata = song.LoadInnerMaidata((int)GameManager.Instance.SelectedDiff);
                if (maidata == "" || maidata == null)
                {
                    BackToList();
                    return;
                }

                var loadingText = loadingMask.transform.GetChild(0).GetComponent<TextMeshPro>();
                loadingText.text = "Deserialization...";

                Chart = new SimaiProcess(maidata);
                if (Chart.notelist.Count == 0)
                {
                    BackToList();
                    return;
                }
                else
                    DelayPlay().Forget();

                sfxGeneratingTask = Task.Run(() =>
                {
                    //Generate ClockSounds
                    var countnum = song.ClockCount == null ? 4 : song.ClockCount;
                    var firstBpm = Chart.notelist.FirstOrDefault().currentBpm;
                    var interval = 60 / firstBpm;
                    if (Chart.notelist.Any(o => o.time < countnum * interval))
                    {
                        //if there is something in first measure, we add clock before the bgm
                        for (int i = 0; i < countnum; i++)
                        {
                            AnwserSoundList.Add(new AnwserSoundPoint()
                            {
                                time = -(i + 1) * interval,
                                isClock = true,
                                isPlayed = false
                            });
                        }
                    }
                    else
                    {
                        //if nothing there, we can add it with bgm
                        for (int i = 0; i < countnum; i++)
                        {
                            AnwserSoundList.Add(new AnwserSoundPoint()
                            {
                                time = i * interval,
                                isClock = true,
                                isPlayed = false
                            });
                        }
                    }


                    //Generate AnwserSounds
                    foreach (var timingPoint in Chart.notelist)
                    {
                        if (timingPoint.noteList.All(o => o.isSlideNoHead)) continue;

                        AnwserSoundList.Add(new AnwserSoundPoint()
                        {
                            time = timingPoint.time,
                            isClock = false,
                            isPlayed = false
                        });
                        var holds = timingPoint.noteList.FindAll(o => o.noteType == SimaiNoteType.Hold || o.noteType == SimaiNoteType.TouchHold);
                        if (holds.Count == 0) continue;
                        foreach (var hold in holds)
                        {
                            var newtime = timingPoint.time + hold.holdTime;
                            if (!Chart.notelist.Any(o => Math.Abs(o.time - newtime) < 0.001) &&
                                !AnwserSoundList.Any(o => Math.Abs(o.time - newtime) < 0.001)
                                )
                                AnwserSoundList.Add(new AnwserSoundPoint()
                                {
                                    time = newtime,
                                    isClock = false,
                                    isPlayed = false
                                });
                        }
                    }
                    AnwserSoundList = AnwserSoundList.OrderBy(o => o.time).ToList();
                });
            }
            catch (Exception ex)
            {
                State = ComponentState.Failed;
                ErrorText.text = "加载note时出错了哟\n" + ex.Message;
                Debug.LogError(ex);
            }
        }

        /// <summary>
        /// 背景加载
        /// </summary>
        /// <returns></returns>
        async UniTask InitBackground()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            var BGManager = GameObject.Find("Background").GetComponent<BGManager>();
            if (!string.IsNullOrEmpty(song.VideoPath))
                BGManager.SetBackgroundMovie(song.VideoPath);
            else
            {
                var task = song.GetSpriteAsync();
                while (!task.IsCompleted)
                {
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                }
                BGManager.SetBackgroundPic(task.Result);
            }


            BGManager.SetBackgroundDim(gameSetting.Game.BackgroundDim);
        }
        /// <summary>
        /// 初始化NoteLoader与实例化Note对象
        /// </summary>
        /// <returns></returns>
        async UniTask LoadNotes()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            noteLoader = GameObject.Find("NoteLoader").GetComponent<NoteLoader>();
            noteLoader.noteSpeed = (float)(107.25 / (71.4184491 * Mathf.Pow(gameSetting.Game.TapSpeed + 0.9975f, -0.985558604f)));
            noteLoader.touchSpeed = gameSetting.Game.TouchSpeed;

            //var loaderTask = noteLoader.LoadNotes(Chart);
            var loaderTask = noteLoader.LoadNotesIntoPool(Chart);
            var loadingText = loadingMask.transform.GetChild(0).GetComponent<TextMeshPro>();
            var timer = 1f;
            var loadingImage = loadingMask.GetComponent<Image>();

            while (noteLoader.State < NoteLoaderStatus.Finished)
            {
                if (noteLoader.State == NoteLoaderStatus.Error)
                {
                    var e = loaderTask.AsTask().Exception;
                    ErrorText.text = "加载note时出错了哟\n" + e.Message;
                    loadingText.text = $"\r\nFailed to load chart\r\n\r\n{e.Message}%";
                    Debug.LogError(e);
                    StopAllCoroutines();
                    throw e;
                }
                loadingText.text = $"\r\nLoading Chart...\r\n\r\n{noteLoader.Process * 100:F2}%";
                await UniTask.Yield();
            }
            loadingText.text = $"\r\nLoading Chart...\r\n\r\n100.00%";

            while (timer > 0)
            {
                await UniTask.Yield();
                timer -= Time.deltaTime;
                var textColor = Color.white;
                var maskColor = Color.black;
                textColor.a = timer / 1f;
                maskColor.a = timer / 1f * 0.75f;
                loadingImage.color = maskColor;
                loadingText.color = textColor;
            }

            loadingMask.SetActive(false);
            loadingText.gameObject.SetActive(false);
        }
        async UniTaskVoid DelayPlay()
        {
            if (audioSample is null)
                return;
            AudioTime = -5f;

            await InitBackground();
            var noteLoaderTask = LoadNotes().AsTask();

            while (!noteLoaderTask.IsCompleted)
            {
                if (noteLoaderTask.IsFaulted)
                    throw noteLoaderTask.Exception;
                await UniTask.Yield();
            }
            if (!sfxGeneratingTask.IsCompleted)
                await sfxGeneratingTask;

            GameManager.Instance.DisableGC();
            Time.timeScale = 1f;
            var firstClockTiming = AnwserSoundList[0].time;
            float extraTime = 5f;
            if (firstClockTiming < -5f)
                extraTime += (-(float)firstClockTiming - 5f) + 2f;
            AudioStartTime = timeSource + (float)audioSample.GetCurrentTime() + extraTime;
            StartToPlayAnswer();
            audioSample.Play();
            audioSample.Pause();

            State = ComponentState.Running;

            while (timeSource - AudioStartTime < 0)
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            audioSample.Play();
            AudioStartTime = timeSource;

        }

        private void OnDestroy()
        {
            print("GPManagerDestroy");
            DisposeAudioTrack();
            audioSample = null;
            State = ComponentState.Finished;
            allTaskTokenSource.Cancel();
            GameManager.Instance.EnableGC();
        }
        // Update is called once per frame
        void Update()
        {
            if (audioSample is null)
                return;
            else if (State != ComponentState.Running)
                return;
            //Do not use this!!!! This have connection with sample batch size
            //AudioTime = (float)audioSample.GetCurrentTime();
            if (AudioStartTime == -114514f)
                return;
            var chartOffset = (float)song.First + gameSetting.Judge.AudioOffset;
            AudioTime = timeSource - AudioStartTime - chartOffset;
            AudioTimeNoOffset = timeSource - AudioStartTime;

            var realTimeDifference = (float)audioSample.GetCurrentTime() - (timeSource - AudioStartTime);
            if (Math.Abs(realTimeDifference) > 0.04f && AudioTime > 0)
            {
                ErrorText.text = "音频错位了哟\n" + realTimeDifference;
            }
            else if (Math.Abs(realTimeDifference) > 0.02f && AudioTime > 0 && GameManager.Instance.Setting.Debug.TryFixAudioSync)
            {
                ErrorText.text = "修正音频哟\n" + realTimeDifference;
                AudioStartTime -= realTimeDifference * 0.8f;
            }


        }
        void FixedUpdate()
        {
            ThisFrameSec = AudioTime;
        }
        async void StartToPlayAnswer()
        {
            int i = 0;
            await Task.Run(async () =>
            {
                while (!allTaskTokenSource.IsCancellationRequested)
                {
                    if (i >= AnwserSoundList.Count)
                        return;

                    var noteToPlay = AnwserSoundList[i].time;
                    var delta = AudioTime - noteToPlay;

                    if (delta > 0)
                    {
                        if (AnwserSoundList[i].isClock)
                            AudioManager.Instance.PlaySFX(SFXSampleType.CLOCK);
                        else
                            AudioManager.Instance.PlaySFX(SFXSampleType.ANSWER);
                        AnwserSoundList[i].isPlayed = true;
                        i++;
                    }
                    await Task.Delay(1);
                }
            });
        }
        public float GetFrame()
        {
            var _audioTime = AudioTime * 1000;

            return _audioTime / 16.6667f;
        }
        void DisposeAudioTrack()
        {
            if (audioSample is not null)
                audioSample.Dispose();
        }
        public void BackToList()
        {
            StopAllCoroutines();
            DisposeAudioTrack();
            //AudioManager.Instance.UnLoadMusic();
            InputManager.Instance.UnbindAnyArea(OnPauseButton);
            GameManager.Instance.EnableGC();
            DelayBackToList().Forget();

        }
        async UniTaskVoid DelayBackToList()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            GameObject.Find("Notes").GetComponent<NoteManager>().DestroyAllNotes();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            SceneManager.LoadScene(1);
        }


        public void EndGame(float acc)
        {
            print("GameResult: " + acc);
            var objectCounter = FindFirstObjectByType<ObjectCounter?>();
            if (objectCounter != null)
                GameManager.LastGameResult = objectCounter.GetPlayRecord(song, GameManager.Instance.SelectedDiff);
            GameManager.Instance.EnableGC();
            DelayEndGame().Forget();
            State = ComponentState.Finished;
        }

        async UniTaskVoid DelayEndGame()
        {
            await UniTask.Delay(2000);
            DisposeAudioTrack();
            InputManager.Instance.UnbindAnyArea(OnPauseButton);
            SceneManager.LoadScene(3);
        }

        class AnwserSoundPoint
        {
            public double time;
            public bool isClock;
            public bool isPlayed;
        }
    }
}