using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MainLobby.Logic
{
    public class RoomController : MonoBehaviour
    {
        [Header("Hierarchy References")]
        [SerializeField] private Transform interactablesRoot;
        [SerializeField] private Transform charactersRoot;

        [Header("Runtime Management")]
        [SerializeField] private List<SmartObject> roomSmartObjects;
        [SerializeField] private List<Visuals.NPCController> roomNPCs;

        [Header("Settings")]
        [SerializeField] private float taskUpdateInterval = 3f;

        private void Start()
        {
            InitializeRoom();
            InvokeRepeating(nameof(AssignTasksLoop), 1f, taskUpdateInterval);
        }

        public void InitializeRoom()
        {
            // Tự tìm References nếu chưa kéo tay
            if (interactablesRoot == null) interactablesRoot = transform.Find("Interactables");
            if (charactersRoot == null) charactersRoot = transform.Find("Characters");

            if (interactablesRoot != null)
                roomSmartObjects = interactablesRoot.GetComponentsInChildren<SmartObject>(true).ToList();

            if (charactersRoot != null)
                roomNPCs = charactersRoot.GetComponentsInChildren<Visuals.NPCController>(true).ToList();

            // DEBUG CHECK
            Debug.Log($"[Room Init] NPC Found: {roomNPCs?.Count ?? 0} | Objects Found: {roomSmartObjects?.Count ?? 0}");
        }

        private void AssignTasksLoop()
        {
            if (roomNPCs == null) return;

            foreach (var npc in roomNPCs)
            {
                if (npc.IsBusy) continue; // NPC đang bận thì kệ nó
                TryAssignRandomTask(npc);
            }
        }

        private void TryAssignRandomTask(Visuals.NPCController npc)
        {
            // --- ĐOẠN DEBUG (Chỉ dùng để tìm lỗi) ---
            // Kiểm tra từng món đồ xem tại sao nó bị loại
            foreach (var obj in roomSmartObjects)
            {
                string reason = "";
                if (!obj.gameObject.activeInHierarchy)
                    reason = "Object đang bị tắt (Inactive)";
                else if (!obj.IsAllowed(npc.NPCID))
                    reason = "NPC không nằm trong danh sách cho phép (Allowed IDs)";
                else if (obj.GetAvailableSlot() == null)
                {
                    // Soi kỹ hơn tại sao không có Slot
                    if (obj.Slots == null || obj.Slots.Count == 0)
                        reason = "List Slots đang rỗng (Size = 0)";
                    else
                        reason = "Có Slot nhưng quên kéo AnchorPoint hoặc đã đầy";
                }

                if (reason != "")
                {
                    Debug.Log($"[BỊ LOẠI] {obj.name} - Lý do: {reason}");
                }
            }
            // ----------------------------------------

            // Logic giao việc thật
            var availableObjects = roomSmartObjects
                .Where(obj =>
                    obj.gameObject.activeInHierarchy &&
                    obj.IsAllowed(npc.NPCID) &&
                    obj.GetAvailableSlot() != null
                )
                .ToList();

            if (availableObjects.Count > 0)
            {
                var randomObj = availableObjects[Random.Range(0, availableObjects.Count)];
                var slot = randomObj.GetAvailableSlot();

                if (slot != null)
                {
                    Debug.Log($"[THÀNH CÔNG] Giao {npc.NPCID} tới {randomObj.name}");
                    npc.InteractWith(randomObj, slot);
                }
            }
            else
            {
                Debug.LogWarning($"[THẤT BẠI] {npc.NPCID} thất nghiệp! Không tìm thấy việc nào phù hợp.");
            }
        }
    }
}