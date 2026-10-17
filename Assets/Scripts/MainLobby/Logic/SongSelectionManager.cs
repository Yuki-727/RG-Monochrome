using UnityEngine;
using System;
using MainLobby.Data;

namespace MainLobby.Logic
{
    public class SongSelectionManager : MonoBehaviour
    {
        public static SongSelectionManager Instance;

        // --- CŨ: Difficulty ---
        public DifficultyType CurrentGlobalDifficulty { get; private set; } = DifficultyType.Hard;
        public event Action<DifficultyType> OnGlobalDifficultyChanged;

        // --- MỚI: Index bài hát ---
        public int CurrentSongIndex { get; private set; } = 0;
        // Event này để SongSelectController lắng nghe -> Tự scroll tới bài vừa chọn
        public event Action<int> OnSongIndexChanged;

        private void Awake()
        {
            Instance = this;
        }

        public void ChangeGlobalDifficulty(DifficultyType newDiff)
        {
            if (CurrentGlobalDifficulty == newDiff) return;
            CurrentGlobalDifficulty = newDiff;
            Debug.Log($"Đổi độ khó toàn cục sang: {newDiff}");
            OnGlobalDifficultyChanged?.Invoke(newDiff);
        }

        // --- MỚI: Hàm để TOC gọi ---
        public void SetCurrentIndex(int index)
        {
            if (CurrentSongIndex == index) return;

            CurrentSongIndex = index;
            Debug.Log($"[Manager] Chọn bài index: {index}");

            // Bắn event để UI bên kia update theo
            OnSongIndexChanged?.Invoke(index);
        }
    }
}