using System.Collections.Generic;
using IdleOff.Combat;
using IdleOff.Maps;
using IdleOff.Visuals;
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
        private EntityVisualController visualController;
        private Sprite closedSprite;
        private Sprite openSprite;
        private bool? lastOpenVisualState;

        public InteractableObjectDefinition Definition { get; private set; }
        public string InstanceID => instanceID;
        public float InteractRange => interactRange;

        public static IReadOnlyList<InteractableObjectEntity> All => Registered;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            visualController = GetComponent<EntityVisualController>();
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
            visualController = GetComponent<EntityVisualController>();
            if (visualController == null)
            {
                visualController = gameObject.AddComponent<EntityVisualController>();
            }

            lastOpenVisualState = null;
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
                Debug.Log($"[Interact] {player?.DisplayName ?? "Player"} tried to interact with {Definition?.name ?? gameObject.name}, but is out of range.");
                return false;
            }

            var mapManager = MapManager.Instance;
            if (mapManager == null || mapManager.CurrentRuntimeState == null)
            {
                Debug.Log($"[Interact] {Definition.name} cannot be used because no current map state is loaded.");
                return false;
            }

            var alreadyUnlocked = mapManager.CurrentRuntimeState.IsInteractableUnlocked(instanceID);
            if (!alreadyUnlocked && !InteractConditionResolver.IsMet(Definition.condition, player, mapManager.CurrentRuntimeState))
            {
                Debug.Log($"[Interact] {player.DisplayName} tried to use {Definition.name}, but its condition {Definition.condition.type} is not met.");
                return false;
            }

            if (!alreadyUnlocked && !InteractConditionResolver.TryConsumeCost(Definition.condition, player))
            {
                Debug.Log($"[Interact] {player.DisplayName} tried to use {Definition.name}, but its required cost could not be consumed.");
                return false;
            }

            if (!alreadyUnlocked)
            {
                mapManager.CurrentRuntimeState.MarkInteractableUnlocked(instanceID);
                mapManager.SaveCurrentMapState();
                Debug.Log($"[Interact] {player.DisplayName} unlocked {Definition.name} ({instanceID}).");
            }

            if (Definition.effect.type == InteractEffectType.TravelToMap)
            {
                Debug.Log($"[Interact] {player.DisplayName} used {Definition.name}; travelling to map {Definition.effect.targetMapID}.");
                mapManager.LoadMapFromPortal(Definition.effect.targetMapID);
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
            if (visualController != null && lastOpenVisualState != isOpen)
            {
                var visualID = isOpen ? Definition.openVisualID : Definition.closedVisualID;
                var fallbackPath = isOpen
                    ? VisualAssetResolver.PortalOpenPlaceholderPath
                    : VisualAssetResolver.PortalClosedPlaceholderPath;
                visualController.ApplyVisual(visualID, fallbackPath);
                lastOpenVisualState = isOpen;
            }
            else if (visualController == null)
            {
                spriteRenderer.sprite = isOpen && openSprite != null ? openSprite : closedSprite;
            }

            spriteRenderer.color = isOpen ? Color.white : Color.white;
        }
    }
}
