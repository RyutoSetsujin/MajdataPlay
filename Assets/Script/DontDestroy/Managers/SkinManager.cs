﻿using MajdataPlay.Types;
using MajdataPlay.Utils;
using MajSimaiDecode;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MajdataPlay
{
    public class SkinManager : MonoBehaviour
    {
        public CustomSkin SelectedSkin { get; set; }
        public CustomSkin[] LoadedSkins => loadedSkins.ToArray();
        List<CustomSkin> loadedSkins = new();

        public Sprite HoldEnd;
        public Sprite HoldEachEnd;
        public Sprite HoldBreakEnd;

        public Texture2D test;

        public Sprite[] TapLines;
        public Sprite[] StarLines;
        public Material BreakMaterial;
        public Material DefaultMaterial;
        public RuntimeAnimatorController JustBreak;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            MajInstances.SkinManager = this;
        }

        // Start is called before the first frame update
        private void Start()
        {
            var path = GameManager.SkinPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var selectedSkinName = MajInstances.Setting.Display.Skin;
            var dicts = Directory.GetDirectories(path);

            foreach (var skinPath in dicts)
                loadedSkins.Add(new CustomSkin(skinPath));

            var targetSkin = loadedSkins.Find(x => x.Name == selectedSkinName);
            if (targetSkin is null)
                targetSkin = new CustomSkin(Path.Combine(path, selectedSkinName));

            SelectedSkin = targetSkin;

            print(path);
            Debug.Log(test);
        }
        public JudgeTextSkin GetJudgeTextSkin()
        {
            return new()
            {
                CP_Break = SelectedSkin.CriticalPerfect_Break,
                P_Break = SelectedSkin.Perfect_Break,
                Break_2600_Shine = SelectedSkin.Break_2600_Shine,
                Break_2600 = SelectedSkin.Break_2600,
                Break_2550 = SelectedSkin.Break_2550,
                Break_2500 = SelectedSkin.Break_2500,
                Break_2000 = SelectedSkin.Break_2000,
                Break_1500 = SelectedSkin.Break_1500,
                Break_1250 = SelectedSkin.Break_1250,
                Break_1000 = SelectedSkin.Break_1000,
                Break_0 = SelectedSkin.Break_0,
                CriticalPerfect = SelectedSkin.JudgeText[4],
                Perfect = SelectedSkin.JudgeText[3],
                Great = SelectedSkin.JudgeText[2],
                Good = SelectedSkin.JudgeText[1],
                Miss = SelectedSkin.JudgeText[0],

                Fast = SelectedSkin.FastText,
                Late = SelectedSkin.LateText
            };
        }
        public TapSkin GetTapSkin()
        {
            return new()
            {
                Normal = SelectedSkin.Tap,
                Each = SelectedSkin.Tap_Each,
                Break = SelectedSkin.Tap_Break,
                Ex = SelectedSkin.Tap_Ex,

                DefaultMaterial = DefaultMaterial,
                BreakMaterial = BreakMaterial,
                NoteLines = TapLines,
                ExEffects = new Color[]
                {
                    new Color(255 / 255f,172 / 255f,225 / 255f), // Pink
                    new Color(255 / 255f,254 / 255f,119 / 255f), // Yellow
                    new Color(255 / 255f,254 / 255f,119 / 255f), // Yellow
                }
            };
        }
        public StarSkin GetStarSkin()
        {
            return new()
            {
                Normal = SelectedSkin.Star,
                Double = SelectedSkin.Star_Double,
                Each = SelectedSkin.Star_Each,
                EachDouble = SelectedSkin.Star_Each_Double,
                Break = SelectedSkin.Star_Break,
                BreakDouble = SelectedSkin.Star_Break_Double,
                Ex = SelectedSkin.Star_Ex,
                ExDouble = SelectedSkin.Star_Ex_Double,

                DefaultMaterial = DefaultMaterial,
                BreakMaterial = BreakMaterial,
                NoteLines = StarLines,
                ExEffects = new Color[]
                {
                    new Color(1,1,1), //White
                    new Color(255 / 255f,254 / 255f,119 / 255f), // Yellow
                    new Color(255 / 255f,254 / 255f,119 / 255f), // Yellow
                }
            };
        }
        public HoldSkin GetHoldSkin()
        {
            return new()
            {
                Normal = SelectedSkin.Hold,
                Off = SelectedSkin.Hold_Off,
                Normal_On = SelectedSkin.Hold_On,
                Each = SelectedSkin.Hold_Each,
                Each_On = SelectedSkin.Hold_Each_On,
                Break = SelectedSkin.Hold_Break,
                Break_On = SelectedSkin.Hold_Break_On,
                Ex = SelectedSkin.Hold_Ex,

                DefaultMaterial = DefaultMaterial,
                BreakMaterial = BreakMaterial,
                NoteLines = TapLines,
                Ends = new Sprite[3]
                {
                    HoldEnd,
                    HoldEachEnd,
                    HoldBreakEnd
                },
                ExEffects = new Color[]
                {
                    new Color(255 / 255f,172 / 255f,225 / 255f), // Pink
                    new Color(255 / 255f,254 / 255f,119 / 255f), // Yellow
                    new Color(255 / 255f,254 / 255f,119 / 255f), // Yellow
                }
            };
        }
        public SlideSkin GetSlideSkin()
        {
            return new SlideSkin()
            {
                Star = GetStarSkin(),
                Normal = SelectedSkin.Slide,
                Each = SelectedSkin.Slide_Each,
                Break = SelectedSkin.Slide_Break,
                BreakMaterial = BreakMaterial
            };
        }
        public WifiSkin GetWifiSkin()
        {
            return new WifiSkin()
            {
                Star = GetStarSkin(),
                Normal = SelectedSkin.Wifi,
                Each = SelectedSkin.Wifi_Each,
                Break = SelectedSkin.Wifi_Break,
                BreakMaterial = BreakMaterial
            };
        }
        public TouchHoldSkin GetTouchHoldSkin()
        {
            return new TouchHoldSkin()
            {
                Fans = new Sprite[4]
                {
                    SelectedSkin.TouchHold[0],
                    SelectedSkin.TouchHold[1],
                    SelectedSkin.TouchHold[2],
                    SelectedSkin.TouchHold[3],
                },
                Fans_Break = new Sprite[4]
                {
                    SelectedSkin.TouchHold_Break[0],
                    SelectedSkin.TouchHold_Break[1],
                    SelectedSkin.TouchHold_Break[2],
                    SelectedSkin.TouchHold_Break[3],
                },
                Boader = SelectedSkin.TouchHold[4],
                Boader_Break = SelectedSkin.TouchHold_Break[4],
                Point = SelectedSkin.TouchPoint,
                Point_Break = SelectedSkin.TouchPoint_Break,
                Off = SelectedSkin.TouchHold_Off,
                DefaultMaterial = DefaultMaterial,
                BreakMaterial = BreakMaterial,
            };
        }
        public TouchSkin GetTouchSkin()
        {
            return new TouchSkin()
            {
                Normal = SelectedSkin.Touch,
                Each = SelectedSkin.Touch_Each,
                Break = SelectedSkin.Touch_Break,
                Point_Normal = SelectedSkin.TouchPoint,
                Point_Each = SelectedSkin.TouchPoint_Each,
                Point_Break = SelectedSkin.TouchPoint_Break,
                Border_Each = SelectedSkin.TouchBorder_Each,
                Border_Normal = SelectedSkin.TouchBorder,
                Border_Break = SelectedSkin.TouchBorder_Break,
                DefaultMaterial = DefaultMaterial,
                BreakMaterial = BreakMaterial,
                JustBorder = SelectedSkin.TouchJust
            };
        }
    }
}