﻿using MajdataPlay.Extensions;
using MajdataPlay.Interfaces;
using MajdataPlay.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#nullable enable
namespace MajdataPlay.Game
{
    public class NotePool<TInfo,TMember> :INotePool<TInfo,TMember>
        where TInfo : NotePoolingInfo where TMember : NoteQueueInfo
    {
        public int Capacity { get; private set; } = 64;

        protected TimingPoint<TInfo>?[] timingPoints;
        protected List<IPoolableNote<TInfo, TMember>> idleNotes;
        protected List<IPoolableNote<TInfo, TMember>> inUseNotes;
        
        public NotePool(GameObject prefab, Transform parent, TInfo[] noteInfos,int capacity)
        {
            Capacity = capacity;
            idleNotes = new(capacity);
            inUseNotes = new(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var obj = GameObject.Instantiate(prefab, parent);
                obj.SetActive(false);
                var noteObj = obj.GetComponent<IPoolableNote<TInfo, TMember>>();
                if (noteObj is null)
                    throw new NotSupportedException();
                idleNotes.Add(noteObj);
            }
            var timingPoints = noteInfos.GroupBy(x => x.AppearTiming)
                                        .OrderBy(x => x.Key);
            this.timingPoints = new TimingPoint<TInfo>[timingPoints.Count()];
            foreach (var (i, timingPoint) in timingPoints.WithIndex())
            {
                this.timingPoints[i] = new TimingPoint<TInfo>()
                {
                    Timing = timingPoint.Key,
                    Infos = timingPoint.ToArray()
                };
            }
        }
        public NotePool(GameObject prefab,Transform parent, TInfo[] noteInfos) : this(prefab, parent, noteInfos,64)
        {
            
        }
        public virtual void Update(float currentSec)
        {
            if (idleNotes.IsEmpty())
                return;
            foreach(var (i, tp) in timingPoints.WithIndex())
            {
                if (tp is null)
                    continue;
                var timeDiff = currentSec - tp.Timing;
                if(timeDiff > -0.15f)
                {
                    if (!Dequeue(tp.Infos))
                        return;
                    timingPoints[i] = null;
                }
            }
        }
        bool Dequeue(TInfo?[] infos)
        {
            foreach(var (i,info) in infos.WithIndex())
            {
                if (info is null)
                    continue;
                else if (!Dequeue(info))
                    return false;
                infos[i] = null;
            }
            return true;
        }
        bool Dequeue(TInfo info)
        {

            if (idleNotes.IsEmpty())
            {
                Debug.LogWarning($"No more Note can use");
                return false;
            }
            var idleNote = idleNotes[0];
            var obj = idleNote.GameObject;
            info.Instance = obj;
            inUseNotes.Add(idleNote);
            idleNotes.RemoveAt(0);
            idleNote.Initialize(info);
            if (!obj.activeSelf)
                obj.SetActive(true);
            return true;
        }
        public virtual void Collect(IPoolableNote<TInfo, TMember> endNote)
        {
            inUseNotes.Remove(endNote);
            idleNotes.Add(endNote);
        }
        public virtual void Destroy()
        {
            foreach (var note in idleNotes)
            {
                try
                {
                    note.End(true);
                    GameObject.Destroy(note.GameObject);
                }
                catch(Exception e)
                {
                    Debug.LogError($"Cannot destroy note:\n{e}");
                }
            }
            foreach (var note in inUseNotes)
            {
                try
                {
                    note.End(true);
                    GameObject.Destroy(note.GameObject);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Cannot destroy note:\n{e}");
                }
            }
        }
        protected class TimingPoint<T> where T: NotePoolingInfo
        {
            public float Timing { get; init; }
            public T?[] Infos { get; init; } = Array.Empty<T>();
        }
    }
}
