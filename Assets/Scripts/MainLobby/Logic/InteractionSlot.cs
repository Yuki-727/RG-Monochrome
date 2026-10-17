using UnityEngine;

namespace MainLobby.Logic
{
    [System.Serializable]
    public class InteractionSlot
    {
        [Header("Settings")]
        [Tooltip("Transform vị trí NPC sẽ đứng/ngồi")]
        public Transform anchorPoint;

        [Tooltip("Animation mặc định nếu không có override data")]
        public AnimationClip defaultAnim;

        [Header("Runtime State")]
        private bool _isOccupied;
        public bool IsOccupied => _isOccupied;

        [Tooltip("Thứ tự hiển thị của NPC khi ở Slot này. Cao hơn sẽ đè lên object khác.")]
        public int targetSortingOrder = 0;

        public void SetOccupied(bool status)
        {
            _isOccupied = status;
        }

        public bool IsValid() => anchorPoint != null;
    }
}