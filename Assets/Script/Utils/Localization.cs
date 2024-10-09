﻿using MajdataPlay.Extensions;
using MajdataPlay.Types;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
#nullable enable
namespace MajdataPlay.Utils
{
    public static class Localization
    {
        public static event EventHandler<Language>? OnLanguageChanged;
        public static Language Current 
        {
            get => _current; 
            set
            {
                _current = value;
                if (OnLanguageChanged is not null)
                    OnLanguageChanged(null, value);
            }
        }
        readonly static JsonSerializerOptions jsonReaderOption = new()
        {

            Converters =
            {
                new JsonStringEnumConverter()
            },
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };
        public static Language[] Available { get; private set; } = Array.Empty<Language>();
        public static void Initialize()
        {
            var path = GameManager.LangPath;
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }
            var files = new DirectoryInfo(path).GetFiles()
                                               .Where(x => x.Extension == ".json");
            List<Language> loadedLangs = new();
            foreach(var fileInfo in files)
            {
                var filePath = fileInfo.FullName;
                var json = File.ReadAllText(filePath);
                Language? lang = null;
                if (Serializer.Json.TryDeserialize(json, out lang,jsonReaderOption) && lang is not null)
                    loadedLangs.Add(lang);
                else
                    continue;
            }
            if (loadedLangs.IsEmpty())
                return;
            var grouped = loadedLangs.GroupBy(x => x.ToString());
            Available = new Language[grouped.Count()];
            foreach(var (i, grouping) in grouped.WithIndex())
                Available[i] = grouping.First();
        }
        /// <summary>
        /// Set language by code and author<para>such like: "zh-CN - Majdata"</para>
        /// </summary>
        /// <param name="langInfo"></param>
        public static void SetLang(string langInfo)
        {
            if (Available.IsEmpty())
                return;
            var result = Available.Find(x => x.ToString() == langInfo);
            if (result is null)
                return;
            Current = result;
        }
        public static void SetLangByCode(string code)
        {
            if (Available.IsEmpty())
                return;
            var result = Available.Find(x => x.Code == code);
            if (result is null)
                return;
            Current = result;
        }
        public static string GetLocalizedText(MajText textType)
        {
            var table = Current.MappingTable;
            var result = table.Find(x => x.Type == textType);

            return result?.Content ?? textType.ToString();
        }
        public static string GetLocalizedText(string origin)
        {
            var table = Current.MappingTable;
            var result = table.Find(x => x.Origin == origin);

            return result?.Content ?? origin;
        }
        public static void GetLocalizedText(MajText textType,out string origin)
        {
            origin = GetLocalizedText(textType);
        }
        static Language _current = Language.Default;
    }
}