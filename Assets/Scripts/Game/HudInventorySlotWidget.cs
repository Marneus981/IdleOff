using IdleOff.Profiles;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IdleOff.Game
{
    public sealed class HudInventorySlotWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private GameplayHud owner;
        private Bag bag;
        private int slotIndex;

        public Bag Bag => bag;
        public int SlotIndex => slotIndex;

        public void Initialize(GameplayHud hud, Bag sourceBag, int sourceSlotIndex)
        {
            owner = hud;
            bag = sourceBag;
            slotIndex = sourceSlotIndex;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.BeginInventorySlotDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.UpdateInventorySlotDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.EndInventorySlotDrag(eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {
            owner?.DropInventorySlotOn(this);
        }
    }
}
