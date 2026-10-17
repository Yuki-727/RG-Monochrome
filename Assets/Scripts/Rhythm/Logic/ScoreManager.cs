using MainLobby.Data;
using RhythmCore.Gameplay;
using RhythmCore.UI;
using System;
using UnityEngine;
using static RhythmCore.Logic.Judgement;

namespace RhythmCore.Logic
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public int CurrentScore { get; private set; }
        public int CurrentCombo { get; private set; }

        // --- THÊM MỚI ---
        public int MaxCombo { get; private set; }
        public int PerfectCount { get; private set; }
        public int GreatCount { get; private set; }
        public int MissCount { get; private set; }
        private int totalNotesProcessed = 0;
        // ----------------

        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnComboChanged;

        public JudgementDisplayUI judgementDisplay;

        void Awake() { if (Instance == null) Instance = this; }

        public void ResetSession()
        {
            CurrentScore = 0;
            CurrentCombo = 0;
            MaxCombo = 0;
            PerfectCount = 0;
            GreatCount = 0;
            MissCount = 0;
            totalNotesProcessed = 0;

            OnScoreChanged?.Invoke(0);
            OnComboChanged?.Invoke(0);
        }

        public void ProcessHit(HitResult result, NoteController note, int scoreGain)
        {
            totalNotesProcessed++;

            // 1. Xử lý Combo & Count
            if (result == HitResult.Miss)
            {
                if (note.breaksComboOnMiss) CurrentCombo = 0;
                MissCount++;
            }
            else
            {
                CurrentCombo++;
                if (CurrentCombo > MaxCombo) MaxCombo = CurrentCombo;

                if (result == HitResult.Perfect) PerfectCount++;
                else if (result == HitResult.Great) GreatCount++;
            }

            // 2. Cộng điểm
            CurrentScore += scoreGain;

            // 3. Update UI/Event
            OnComboChanged?.Invoke(CurrentCombo);
            OnScoreChanged?.Invoke(CurrentScore);

            if (judgementDisplay != null) judgementDisplay.ShowJudgement(result);
        }

        // --- HÀM TÍNH TOÁN KẾT QUẢ CUỐI CÙNG ---
        public GlobalDataManager.PlayResultData GetFinalResult()
        {
            // Tính Accuracy (Ví dụ: Perfect 100%, Good 50%, Miss 0%)
            float acc = 0f;
            if (totalNotesProcessed > 0)
            {
                float totalPoints = (PerfectCount * 1f) + (GreatCount * 0.5f);
                acc = (totalPoints / totalNotesProcessed) * 100f;
            }

            // Tính Rank
            string rank = "F";
            if (acc >= 95f) rank = "S";
            else if (acc >= 90f) rank = "A";
            else if (acc >= 80f) rank = "B";
            else if (acc >= 70f) rank = "C";

            return new GlobalDataManager.PlayResultData
            {
                score = CurrentScore,
                maxCombo = MaxCombo,
                accuracy = acc,
                rank = rank,
                perfectCount = PerfectCount,
                goodCount = GreatCount,
                missCount = MissCount,
                isFullCombo = (MissCount == 0 && totalNotesProcessed > 0)
            };
        }
    }
}