using System.Collections.Generic;
using IdleOff.Combat;
using IdleOff.Maps;
using UnityEngine;

namespace IdleOff.Interactables
{
    [DisallowMultipleComponent]
    public sealed class InteractableObjectEntity : MonoBehaviour
    {
        private static readonly List<InteractableObjectEntity> Registered = new();

        [SerializeField] private int interactableID;
        [SerializeField] private string instanceID;
        [SerializeField, Min(0.01f)] private float interactRange = 1.5f;

        private SpriteRenderer spriteRenderer;
        private Sprite closedSprite;
        private Sprite openSprite;

        public InteractableObjectDefinition Definition { get; private set; }
        public string InstanceID => instanceID;
        public float InteractRange => interactRange;

        public static IReadOnlyList<InteractableObjectEntity> All => Registered;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (!Registered.Contains(this))
            {
                Registered.Add(this);
            }
        }

        private void OnDisable()
        {
            Registered.Remove(this);
        }

        private void Update()
        {
            RefreshVisual();
        }

        public void Initialize(string instanceID, InteractableObjectDefinition definition, Sprite closedSprite, Sprite openSprite)
        {
            this.instanceID = instanceID;
            Definition = definition?.Clone();
            interactableID = Definition?.interactableID ?? interactableID;
            this.closedSprite = closedSprite;
            this.openSprite = openSprite;
            spriteRenderer = GetComponent<SpriteRenderer>();
            RefreshVisual();
        }

        public bool CanInteract(PlayerCombatant player)
        {
            return Definition != null
                && player != null
                && Vector2.Distance(transform.position, player.transform.position) <= interactRange;
        }

        public bool TryInteract(PlayerCombatant player)
        {
            if (!CanInteract(player))
            {
                return false;
            }

            var mapManager = MapManager.Instance;
            if (mapManager == null || mapManager.CurrentRuntimeState == null)
            {
                return false;
            }

            var alreadyUnlocked = mapManager.CurrentRuntimeState.IsInteractableUnlocked(instanceID);
            if (!alreadyUnlocked && !InteractConditionResolver.IsMet(Definition.condition, player, mapManager.CurrentRuntimeState))
            {
                return false;
            }

            if (!alreadyUnlocked && !InteractConditionResolver.TryConsumeCost(Definition.condition, player))
            {
                return false;
            }

            if (!alreadyUnlocked)
            {
                mapManager.CurrentRuntimeState.MarkInteractableUnlocked(instanceID);
                mapManager.SaveCurrentMapState();
            }

            if (Definition.effect.type == InteractEffectType.TravelToMap)
            {
                mapManager.LoadMap(Definition.effect.targetMapID);
            }

            return true;
        }

        private void RefreshVisual()
        {
            if (spriteRenderer == null || Definition == null)
            {
                return;
            }

            var mapState = MapManager.Instance != null ? MapManager.Instance.CurrentRuntimeState : null;
            var isOpen = mapState != null
                && (mapState.IsInteractableUnlocked(instanceID) || InteractConditionResolver.IsMet(Definition.condition, MapManager.Instance.Player, mapState));
            spriteRenderer.sprite = isOpen && openSprite != null ? openSprite : closedSprite;
            spriteRenderer.color = isOpen ? Color.white : Color.white;
        }
    }
}
