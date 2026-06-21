using IdleOff.Profiles;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IdleOff.Game
{
    public sealed class HudInventorySlotWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (TryGetItem(out var item))
            {
                ItemInfoTooltip.Show(item, eventData.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemInfoTooltip.Hide();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            ItemInfoTooltip.Move(eventData.position);
        }

        private bool TryGetItem(out Item item)
        {
            item = null;
            if (bag == null || !bag.TryGetSlot(slotIndex, out var slot) || slot.IsEmpty)
            {
                return false;
            }

            item = slot.item;
            return item != null;
        }
    }
}
