using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmCore.Data
{
    [Serializable]
    public class ChartData
    {
        public ChartMetadata metadata;
        public List<LaneData> laneDefinitions = new List<LaneData>();
        public List<NoteData> notes;
        public List<SVEvent> sv_events;
        public List<LaneEvent> lane_events;
    }

    [Serializable]
    public class ChartMetadata
    {
        public string id; // để phân loại nhạc/gom các chart có cùng bài nhạc 
        public string difficultyType; //Type of difficulty (để phân loại)
        public string difficultyName; //Name of difficulty 
        public int level;

        public string title;
        public string artist;
        public string charter;
        public double bpm;     
        public double offset_ms;

        public string audioPath = "";
        public string backgroundPath = "";
        public string ArtCoverPath = "";
        public double previewStartBeat = 0;
        public double previewDurationBeats = 0;
    }

    [Serializable]
    public class NoteData
    {
        public double beat;
        public int lane;
        public NoteType type;

        public float alpha = 1.0f;
        public double duration; 
        public int warningCount;
    }

    [Serializable]
    public class LaneData
    {
        public int lane_id;
        public float pos_x = 0;
        public float pos_y = 0;
        public float rotation = 0;
        public float alpha = 1f;
        public bool autoplay = false;
    }

    [Serializable]
    public enum NoteType
    {
        Tap,
        Hold,
        Blast,
        Drag
    }

    [Serializable]
    public class SVEvent
    {
        public double beat;
        public float target_speed;
        public double duration;
        public EasingType ease;
    }

    [Serializable]
    public class LaneEvent
    {
        public double beat;
        public int lane_id;
        public float pos_x;
        public float pos_y;
        public float rotation;
        public float alpha = 1.0f;
        public double duration = 0.5f;
        public bool autoplay = false;
        public EasingType ease;
    }

    public enum EasingType
    {
        Linear,
        EaseOutQuad, 
        EaseInQuad,
        EaseInOutQuad
    }
}