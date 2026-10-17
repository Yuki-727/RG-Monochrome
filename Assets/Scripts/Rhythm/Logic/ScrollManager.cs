using RhythmCore.Data;
using RhythmCore.Logic;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmCore.Logic
{
    public class ScrollManager : MonoBehaviour
    {
        public static ScrollManager Instance { get; private set; }

        private class SVSegment
        {
            public double startBeat;
            public double endBeat;
            public float startSpeed;
            public float endSpeed;
            public EasingType ease;

            public double accumulatedVisualBeatAtStart;
        }

        private List<SVSegment> _segments = new List<SVSegment>();

        [Header("Settings")]
        public float BaseNoteSpeed = 10.0f;
        public float PixelScale = 120.0f;

        [Header("Calibration")]
        [Tooltip("Chỉnh cái này nếu cảm thấy hình và tiếng không khớp. Đơn vị Millisecond.")]
        public float visualOffsetMs = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void Initialize(List<SVEvent> svEvents)
        {
            _segments.Clear();

            var sortedEvents = new List<SVEvent>(svEvents);

            sortedEvents.Sort((a, b) => a.beat.CompareTo(b.beat));

            float currentSpeed = 1.0f;
            double currentAccumulatedVB = 0.0;
            double lastBeat = 0.0;

            foreach (var ev in sortedEvents)
            {
                if (ev.beat > lastBeat)
                {
                    double gapDuration = ev.beat - lastBeat;
                    _segments.Add(new SVSegment
                    {
                        startBeat = lastBeat,
                        endBeat = ev.beat,
                        startSpeed = currentSpeed,
                        endSpeed = currentSpeed,
                        ease = EasingType.Linear,
                        accumulatedVisualBeatAtStart = currentAccumulatedVB
                    });

                    currentAccumulatedVB += gapDuration * currentSpeed;
                }

                double duration = ev.duration > 0 ? ev.duration : 0.001;
                _segments.Add(new SVSegment
                {
                    startBeat = ev.beat,
                    endBeat = ev.beat + duration,
                    startSpeed = currentSpeed,
                    endSpeed = ev.target_speed,
                    ease = ev.ease,
                    accumulatedVisualBeatAtStart = currentAccumulatedVB
                });

                float integratedVal = EasingHelper.GetIntegratedProgress(currentSpeed, ev.target_speed, 1.0f, ev.ease);

                currentAccumulatedVB += duration * integratedVal;

                lastBeat = ev.beat + duration;
                currentSpeed = ev.target_speed;
            }

            _segments.Add(new SVSegment
            {
                startBeat = lastBeat,
                endBeat = double.MaxValue,
                startSpeed = currentSpeed,
                endSpeed = currentSpeed,
                ease = EasingType.Linear,
                accumulatedVisualBeatAtStart = currentAccumulatedVB
            });
        }

        public double GetVisualBeat(double beat)
        {
            if (beat < 0) return beat;

            SVSegment seg = null;
            for (int i = 0; i < _segments.Count; i++)
            {
                if (beat >= _segments[i].startBeat && beat < _segments[i].endBeat)
                {
                    seg = _segments[i];
                    break;
                }
            }

            if (seg == null) seg = _segments[_segments.Count - 1];

            double timeInSegment = beat - seg.startBeat;
            double segmentDuration = seg.endBeat - seg.startBeat;

            float t = (float)(timeInSegment / segmentDuration);
            if (segmentDuration > 100000) t = (float)timeInSegment;

            float integralPart = EasingHelper.GetIntegratedProgress(seg.startSpeed, seg.endSpeed, t, seg.ease);

            double localVisualBeat = (segmentDuration > 100000 ? 1 : segmentDuration) * integralPart;

            return seg.accumulatedVisualBeatAtStart + localVisualBeat;
        }

        public float GetNotePosition(double targetBeat)
        {
            double currentSongTime = Conductor.Instance.SongPosition;

            double effectiveTime = currentSongTime + (visualOffsetMs / 1000.0);
            double effectiveCurrentBeat = Conductor.Instance.TimeToBeat(effectiveTime);

            double targetVisualBeat = GetVisualBeat(targetBeat);
            double currentVisualBeat = GetVisualBeat(effectiveCurrentBeat);

            double visualDistance = targetVisualBeat - currentVisualBeat;

            return (float)(visualDistance * BaseNoteSpeed * PixelScale);
        }
    }
}