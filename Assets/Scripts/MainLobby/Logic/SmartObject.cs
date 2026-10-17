using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MainLobby.Logic
{
    public enum ObjectState
    {
        Clean,
        InUse,
        Messy
    }

    public class SmartObject : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string objectTypeID; 

        [Header("Configuration")]
        [SerializeField] private List<InteractionSlot> slots;

        [Header("Debug/State")]
        [SerializeField] private ObjectState currentState = ObjectState.Clean;

        [SerializeField] private float baseDuration = 5f;
        [SerializeField] private float randomOffset = 2f;

        public string ObjectTypeID => objectTypeID;
        public List<InteractionSlot> Slots => slots;

        [Header("Compatibility Settings")]
        [Tooltip("Danh sách ID các NPC được phép dùng đồ vật này. Để trống = Ai dùng cũng được.")]
        [SerializeField] private List<string> allowedNPCIDs;

        public bool IsAllowed(string npcID)
        {
            // Nếu list trống -> Cho phép tất cả. 
            // Nếu có data -> Phải nằm trong list mới được dùng.
            if (allowedNPCIDs == null || allowedNPCIDs.Count == 0) return true;
            return allowedNPCIDs.Contains(npcID);
        }

        public float GetInteractionDuration()
        {
            return baseDuration + Random.Range(-randomOffset, randomOffset);
        }

        public InteractionSlot GetAvailableSlot()
        {
            if (currentState == ObjectState.Messy) return null; // Không dùng được nếu bừa bộn
            
            foreach (var slot in slots)
            {
                if (!slot.IsOccupied && slot.anchorPoint != null) // Check anchorPoint có tồn tại
                    return slot;
            }
            return null;
        }

        public void ClaimSlot(InteractionSlot slot)
        {
            if (slot != null && !slot.IsOccupied)
            {
                slot.SetOccupied(true);
                UpdateStateCheck();
            }
        }
        
        public void ReleaseSlot(InteractionSlot slot)
        {
            if (slot != null)
            {
                slot.SetOccupied(false);
                UpdateStateCheck();
            }
        }

        private void UpdateStateCheck()
        {
            bool anyInUse = slots.Any(s => s.IsOccupied);
            currentState = anyInUse ? ObjectState.InUse : ObjectState.Clean;

            // TODO: Thêm logic chuyển sang Messy sau thời gian dùng
        }
    }
}