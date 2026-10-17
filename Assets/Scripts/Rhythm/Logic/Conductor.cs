using RhythmCore.Data;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmCore.Logic
{
    public class Conductor : MonoBehaviour
    {
        public static Conductor Instance { get; private set; }

        [Header("Audio Source")]
        [SerializeField] private AudioSource musicSource;

        public double SongPosition { get; private set; }
        public double CurrentBeat { get; private set; }
        public bool IsPlaying { get; private set; }

        public float startDelay = 3.0f;

        private double _dspSongStartTime;
        private double _secPerBeat;
        private double _bpm;
        private double _offsetSec;

        public double bpm => _bpm;
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Initialize(ChartMetadata metadata)
        {
            _bpm = metadata.bpm;
            _secPerBeat = 60.0 / _bpm;
            _offsetSec = metadata.offset_ms / 1000.0;

            // Reset state
            SongPosition = -startDelay;
            CurrentBeat = -startDelay / _secPerBeat;
            IsPlaying = false;
        }

        public void SetMusic(AudioClip clip)
        {
            if (musicSource != null)
            {
                musicSource.clip = clip;
            }
        }

        public void PlayMusic()
        {
            double initialDsp = AudioSettings.dspTime;

            _dspSongStartTime = initialDsp + startDelay;

            // 3. Lên lịch phát
            musicSource.PlayScheduled(_dspSongStartTime);

            IsPlaying = true;
        }

        public void PauseMusic()
        {
            musicSource.Pause();
            IsPlaying = false;
        }

        private void Update()
        {
            if (!IsPlaying) return;

            SongPosition = AudioSettings.dspTime - _dspSongStartTime - _offsetSec;

            if (_secPerBeat > 0)
            {
                CurrentBeat = SongPosition / _secPerBeat;
            }
        }

        public double BeatToTime(double beat)
        {
            return beat * _secPerBeat;
        }

        public double TimeToBeat(double time)
        {
            if (_secPerBeat <= 0) return 0;
            return time / _secPerBeat;
        }
    }
}