using UnityEngine;
using System.Collections.Generic;
using RhythmCore.Data;
using RhythmCore.Gameplay;
using RhythmCore.Logic; // Đảm bảo using namespace này

namespace RhythmCore.DebugTools
{
    public class DebugChartLoader : MonoBehaviour
    {
        [Header("Prefabs")]
        public LaneController lanePrefab;
        public NoteController notePrefab;

        [Header("Containers")]
        public Transform trackRoot;

        [Header("Config")]
        public float laneDistanceY = 2.0f;
        public bool autoStart = true; // Tick vào đây để nhạc tự chạy

        // Dùng IEnumerator Start thay vì void Start để chờ 1 frame cho chắc ăn
        // (Một trick nhỏ trong Unity để đợi các Manager Awake xong)
        System.Collections.IEnumerator Start()
        {
            // Chờ 1 frame để đảm bảo ScrollManager.Awake() đã chạy
            yield return null;

            // --- 1. Safety Check ---
            if (ScrollManager.Instance == null)
            {
                Debug.LogError("CRITICAL: Không tìm thấy ScrollManager trong Scene!");
                yield break;
            }
            if (Conductor.Instance == null)
            {
                Debug.LogError("CRITICAL: Không tìm thấy Conductor trong Scene!");
                yield break;
            }

            // --- 2. Setup Lanes ---
            List<LaneController> lanes = new List<LaneController>();
            for (int i = 0; i < 2; i++)
            {
                var laneObj = Instantiate(lanePrefab, trackRoot);
                laneObj.transform.position = new Vector3(-4f, i * laneDistanceY - 0.5f, 0);
                //laneObj.Initialize(i);
                lanes.Add(laneObj);
            }

            // --- 3. Setup Scroll System ---
            Debug.Log("Initializing Scroll System...");
            List<SVEvent> svEvents = new List<SVEvent>
            {
                //new SVEvent { beat = 0, target_speed = 1.0f, duration = 0, ease = EasingType.Linear },
                //new SVEvent { beat = 4, target_speed = 0.2f, duration = 2, ease = EasingType.EaseOutQuad },
                //new SVEvent { beat = 8, target_speed = 3.0f, duration = 1, ease = EasingType.Linear }
            };

            // Fix lỗi sort list lúc trước (nếu chưa fix trong ScrollManager)
            svEvents.Sort((a, b) => a.beat.CompareTo(b.beat));
            ScrollManager.Instance.Initialize(svEvents);

            // --- 4. Setup Notes ---
            SpawnNote(lanes[0], 0.0, NoteType.Tap);
            SpawnNote(lanes[0], 4.0, NoteType.Tap); 
            SpawnNote(lanes[1], 5.0, NoteType.Tap);
            SpawnNote(lanes[0], 6.0, NoteType.Tap);

            SpawnNote(lanes[1], 8.0, NoteType.Hold, duration: 4.0);
            //SpawnNote(lanes[0], 20.0, NoteType.Blast, warningCount: 4);
            //SpawnNote(lanes[1], 21.0, NoteType.Blast, warningCount: 4);
            //SpawnNote(lanes[0], 22.0, NoteType.Blast, warningCount: 4);
            //SpawnNote(lanes[1], 23.0, NoteType.Blast, warningCount: 4);

            SpawnNote(lanes[1], 14.0, NoteType.Tap);
            SpawnNote(lanes[0], 16.0, NoteType.Drag);
            SpawnNote(lanes[0], 16.0, NoteType.Drag);
            SpawnNote(lanes[0], 16.0, NoteType.Drag);
            SpawnNote(lanes[0], 16.0, NoteType.Drag);
            SpawnNote(lanes[1], 16.0, NoteType.Drag);
            SpawnNote(lanes[1], 16.0, NoteType.Drag);
            SpawnNote(lanes[1], 16.0, NoteType.Drag);
            SpawnNote(lanes[1], 16.0, NoteType.Drag);
            // --- 5. Setup Conductor & Audio ---
            var meta = new ChartMetadata { bpm = 170, offset_ms = 0 };
            Conductor.Instance.Initialize(meta);

            if (autoStart)
            {
                Debug.Log("Auto Starting Music...");
                Conductor.Instance.PlayMusic();
            }

            if (InputManager.Instance != null)
            {
                // Nếu chưa gán container trong inspector thì gán luôn bằng code cho chắc
                if (InputManager.Instance.laneContainer == null)
                    InputManager.Instance.laneContainer = trackRoot;

                //InputManager.Instance.Initialize();
            }
        }

        void SpawnNote(LaneController lane, double beat, NoteType type, double duration = 0f, int warningCount = 0)
        {
            var noteObj = Instantiate(notePrefab, lane.noteContainer);
            NoteData data = new NoteData { 
                beat = beat, 
                type = type,
                lane = lane.LaneID,
                duration = duration,
                warningCount = warningCount

            };
            
            noteObj.transform.localPosition = Vector3.zero;

            noteObj.Initialize(data, lane);
        }
    }
}