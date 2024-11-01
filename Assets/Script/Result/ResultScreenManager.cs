﻿using MajdataPlay.Types;
using MajdataPlay.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;
using MajdataPlay.Utils;

namespace MajdataPlay.Result
{
    public class ResultScreenManager : MonoBehaviour
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI artist;
        public TextMeshProUGUI designer;
        public TextMeshProUGUI level;

        public TextMeshProUGUI accDX;
        public TextMeshProUGUI accClassic;
        public TextMeshProUGUI dxScore;

        public TextMeshProUGUI perfectCount;
        public TextMeshProUGUI greatCount;
        public TextMeshProUGUI goodCount;
        public TextMeshProUGUI missCount;

        public TextMeshProUGUI fastCount;
        public TextMeshProUGUI lateCount;

        public TextMeshProUGUI omg;

        public TextMeshProUGUI subMonitor;


        public GameObject clearLogo;
        public GameObject xxlb;

        public Image coverImg;


        void Start()
        {
            var gameManager = MajInstances.GameManager;
            var result = (GameResult)GameManager.LastGameResult;
            GameManager.LastGameResult = null;

            LightManager.Instance.SetAllLight(Color.white);
            LightManager.Instance.SetButtonLight(Color.green, 3);

            var totalJudgeRecord = UnpackJudgeRecord(result.JudgeRecord.TotalJudgeInfo);
            var song = result.SongInfo;

            GetComponent<OnlineInteractionSender>().Init(song);

            if (result.Acc.DX < 70)
            {
                omg.text = "您输了";
                xxlb.GetComponent<Animator>().SetTrigger("Bad");
            }
            else
            {
                omg.text = "您赢了";
                xxlb.GetComponent<Animator>().SetTrigger("Good");
            }

            title.text = song.Title;
            artist.text = song.Artist;
            designer.text = song.Designers[(int)gameManager.SelectedDiff];
            level.text = gameManager.SelectedDiff.ToString() + " " + song.Levels[(int)gameManager.SelectedDiff];

            accDX.text = $"{result.Acc.DX:F4}%";
            accClassic.text = $"{result.Acc.Classic:F2}%";
            var dxScoreRank = new DXScoreRank(result.DXScore, result.TotalDXScore);
            if (dxScoreRank.Rank > 0)
                dxScore.text = $"*{dxScoreRank.Rank} {result.DXScore}/{result.TotalDXScore}";
            else
                dxScore.text = $"{result.DXScore}/{result.TotalDXScore}";

            perfectCount.text = $"{totalJudgeRecord.CriticalPerfect + totalJudgeRecord.Perfect}";
            greatCount.text = $"{totalJudgeRecord.Great}";
            goodCount.text = $"{totalJudgeRecord.Good}";
            missCount.text = $"{totalJudgeRecord.Miss}";

            fastCount.text = $"{result.Fast}";
            lateCount.text = $"{result.Late}";

            subMonitor.text = BuildSubDisplayText(result.JudgeRecord);

            LoadCover(song).Forget();

            var breakJudgeInfo = UnpackJudgeRecord(result.JudgeRecord[ScoreNoteType.Break]);

            if (!totalJudgeRecord.IsNoMiss)
                clearLogo.SetActive(false);
            else if (totalJudgeRecord.IsAllPerfect)
            {
                if (breakJudgeInfo.Perfect == 0)
                    clearLogo.GetComponentInChildren<TextMeshProUGUI>().text = "AP+";
                else
                    clearLogo.GetComponentInChildren<TextMeshProUGUI>().text = "AP";
            }
            else if (totalJudgeRecord.IsNoGood)
                clearLogo.GetComponentInChildren<TextMeshProUGUI>().text = "FC+";


            MajInstances.InputManager.BindAnyArea(OnAreaDown);
            MajInstances.AudioManager.PlaySFX(SFXSampleType.SUGOI);
            MajInstances.AudioManager.PlaySFX(SFXSampleType.RESULT_BGM, true);
            MajInstances.ScoreManager.SaveScore(result, result.ChartLevel);
        }

        async UniTask LoadCover(SongDetail song)
        {
            var task = song.GetSpriteAsync();
            while (!task.IsCompleted)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }
            coverImg.sprite = task.Result;
        }

        string BuildSubDisplayText(JudgeDetail judgeRecord)
        {
            var tapJudge = UnpackJudgeRecord(judgeRecord[ScoreNoteType.Tap]);
            var holdJudge = UnpackJudgeRecord(judgeRecord[ScoreNoteType.Hold]);
            var slideJudge = UnpackJudgeRecord(judgeRecord[ScoreNoteType.Slide]);
            var touchJudge = UnpackJudgeRecord(judgeRecord[ScoreNoteType.Touch]);
            var breakJudge = UnpackJudgeRecord(judgeRecord[ScoreNoteType.Break]);
            string[] nmsl = new string[]
            {
            "NOTES\t\tCP    \t\tP    \t\tGr    \t\tGd   \t\tM",
            $"Tap  \t\t{tapJudge.CriticalPerfect}\t\t{tapJudge.Perfect}\t\t{tapJudge.Great}\t\t{tapJudge.Good}\t\t{tapJudge.Miss}",
            $"Hold\t\t{holdJudge.CriticalPerfect}\t\t{holdJudge.Perfect}\t\t{holdJudge.Great}\t\t{holdJudge.Good}\t\t{holdJudge.Miss}",
            $"Slide\t\t{slideJudge.CriticalPerfect}\t\t{slideJudge.Perfect}\t\t{slideJudge.Great}\t\t{slideJudge.Good}\t\t{slideJudge.Miss}",
            $"Touch\t\t{touchJudge.CriticalPerfect}\t\t{touchJudge.Perfect}\t\t{touchJudge.Great}\t\t{touchJudge.Good}\t\t{touchJudge.Miss}",
            $"Break\t\t{breakJudge.CriticalPerfect}\t\t{breakJudge.Perfect}\t\t{breakJudge.Great}\t\t{breakJudge.Good}\t\t{breakJudge.Miss}"
            };
            return string.Join("\n", nmsl);
        }
        UnpackJudgeInfo UnpackJudgeRecord(JudgeInfo judgeInfo)
        {
            long cPerfect = 0;
            long perfect = 0;
            long great = 0;
            long good = 0;
            long miss = 0;

            long fast = 0;
            long late = 0;

            foreach (var kv in judgeInfo)
            {
                if (kv.Key > JudgeType.Perfect)
                    fast += kv.Value;
                else if (kv.Key is not (JudgeType.Miss or JudgeType.Perfect))
                    late += kv.Value;
                switch (kv.Key)
                {
                    case JudgeType.Miss:
                        miss += kv.Value;
                        break;
                    case JudgeType.FastGood:
                    case JudgeType.LateGood:
                        good += kv.Value;
                        break;
                    case JudgeType.LateGreat2:
                    case JudgeType.LateGreat1:
                    case JudgeType.LateGreat:
                    case JudgeType.FastGreat:
                    case JudgeType.FastGreat1:
                    case JudgeType.FastGreat2:
                        great += kv.Value;
                        break;
                    case JudgeType.LatePerfect2:
                    case JudgeType.LatePerfect1:
                    case JudgeType.FastPerfect1:
                    case JudgeType.FastPerfect2:
                        perfect += kv.Value;
                        break;
                    case JudgeType.Perfect:
                        cPerfect += kv.Value;
                        break;
                }
            }
            return new UnpackJudgeInfo()
            {
                CriticalPerfect = cPerfect,
                Perfect = perfect,
                Great = great,
                Good = good,
                Miss = miss,
                Fast = fast,
                Late = late,
            };

        }
        private void OnAreaDown(object sender, InputEventArgs e)
        {
            if (e.IsClick && e.IsButton && e.Type == SensorType.A4)
            {
                MajInstances.InputManager.UnbindAnyArea(OnAreaDown);
                MajInstances.AudioManager.StopSFX(SFXSampleType.SUGOI);
                MajInstances.AudioManager.StopSFX(SFXSampleType.RESULT_BGM);
                MajInstances.SceneSwitcher.SwitchScene("List");
            }
        }

        readonly ref struct UnpackJudgeInfo
        {
            public long CriticalPerfect { get; init; }
            public long Perfect { get; init; }
            public long Great { get; init; }
            public long Good { get; init; }
            public long Miss { get; init; }
            public long Fast { get; init; }
            public long Late { get; init; }
            public bool IsNoMiss => Miss == 0;
            public bool IsNoGood => Good == 0;
            public bool IsAllPerfect => IsNoMiss && IsNoGood && Great == 0;
        }
    }
}