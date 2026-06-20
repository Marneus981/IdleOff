using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class CharacterData
    {
        [SerializeField] private string characterID;
        [SerializeField] private string characterName = "New Character";
        [SerializeField] private CharacterGender gender = CharacterGender.Unspecified;
        [SerializeField] private CharacterClass characterClass = CharacterClass.CreateWanderingSoul();
        [SerializeField] private MainStats mainStats = new();
        [SerializeField] private SecondaryStats secondaryStats = new();
        [SerializeField] private int starSignModifierID = 3002;
        [SerializeField] private StarSignModifier starSignModifier;
        [SerializeField] private FamilyBonusModifier familyBonusModifier;
        [SerializeField] private Equipment equipment = new();
        [SerializeField] private Inventory inventory = new();
        [SerializeField] private Storage storage = new();
        [SerializeField] private Dictionary<int, ItemModifier> equipmentModifiers = new();
        private Dictionary<int, StarSignModifier> inactiveStarSignModifiers = new();
        private Dictionary<int, ItemModifier> inactiveItemModifiers = new();
        [NonSerialized] private CharacterProfile parentProfile;

        public string CharacterID
        {
            get
            {
                EnsureCharacterID();
                return characterID;
            }
        }

        public string CharacterName => characterName;
        public CharacterGender Gender => gender;
        public CharacterClass CharacterClass => GetCharacterClass();
        public CharacterProfile ParentProfile => parentProfile;
        public int Level => GetCharacterClass().GetLevelNumber();
        public StarSignModifier StarSignModifier => GetStarSignModifier();
        public FamilyBonusModifier FamilyBonusModifier => GetFamilyModifier();
        public Equipment Equipment => equipment;
        public Inventory Inventory => inventory;
        public Storage Storage => storage;
        public IReadOnlyDictionary<int, ItemModifier> EquipmentModifiers => equipmentModifiers;
        public MainStats MainStats
        {
            get
            {
                LoadStats();
                return mainStats;
            }
        }

        public SecondaryStats SecondaryStats
        {
            get
            {
                LoadStats();
                return secondaryStats;
            }
        }

        public Speed Speed
        {
            get
            {
                LoadStats();
                return secondaryStats.Speed;
            }
        }

        public CharacterData()
        {
            EnsureCharacterID();
            LoadStats();
        }

        public CharacterData(string characterName, CharacterGender gender, int level)
            : this(characterName, gender, level, 3002, 5001)
        {
        }

        public CharacterData(string characterName, CharacterGender gender, int level, int starSignModifierID, int startingHatItemID = 5001)
        {
            this.characterName = characterName;
            EnsureCharacterID();
            this.gender = gender;
            this.starSignModifierID = starSignModifierID;
            GetCharacterClass().SetLevelNumber(level);
            LoadStats();
            EquipStartingItem(startingHatItemID);
        }

        public CharacterData(string characterName, CharacterGender gender, CharacterClass characterClass)
            : this(characterName, gender, characterClass, 3002, 5001)
        {
        }

        public CharacterData(string characterName, CharacterGender gender, CharacterClass characterClass, int starSignModifierID, int startingHatItemID = 5001)
        {
            this.characterName = characterName;
            EnsureCharacterID();
            this.gender = gender;
            this.characterClass = characterClass ?? CharacterClass.CreateWanderingSoul();
            this.starSignModifierID = starSignModifierID;
            LoadStats();
            EquipStartingItem(startingHatItemID);
        }

        public void UpdateStats()
        {
            LoadStats(); //may reset saved values for the character
            mainStats.Update();
            secondaryStats.Update();
        }

        internal void SetParentProfile(CharacterProfile profile)
        {
            // Character-owned modifiers can query account-wide character data through this profile reference.
            parentProfile = profile;
            familyBonusModifier?.SetOwner(this);
        }

        private CharacterClass GetCharacterClass()
        {
            characterClass ??= CharacterClass.CreateWanderingSoul();
            characterClass.SetOwner(this);
            return characterClass;
        }

        public Modifier GetModifier(int modifierID)
        {
            if (modifierID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(modifierID), modifierID, "Modifier ID must be positive.");
            }

            // Stat updates still ask CharacterData for modifier IDs; this range split routes those IDs to the right source.
            if (modifierID == 3001)
            {
                return GetFamilyModifier();
            }

            if (modifierID >= 3002 && modifierID < 4000)
            {
                var activeStarSign = GetStarSignModifier();
                return activeStarSign.modifierID == modifierID
                    ? activeStarSign
                    : GetInactiveStarSignModifier(modifierID);
            }

            if (modifierID >= 4001)
            {
                EnsureBagsLoaded();
                if (equipmentModifiers.TryGetValue(modifierID, out var itemModifier))
                {
                    return itemModifier;
                }

                return GetInactiveItemModifier(modifierID);
            }

            return GetCharacterClass().GetModifier(modifierID);
        }        

        public StarSignModifier GetStarSignModifier()
        {
            EnsureCharacterModifiersLoaded();
            return starSignModifier;
        }

        private void EnsureCharacterID()
        {
            if (string.IsNullOrWhiteSpace(characterID))
            {
                characterID = Guid.NewGuid().ToString("N");
            }
        }

        public FamilyBonusModifier GetFamilyModifier()
        {
            EnsureCharacterModifiersLoaded();
            return familyBonusModifier;
        }

        public void UpdateByStatID(int statID)
        {
            if (statID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(statID), statID, "Stat ID must be positive.");
            }

            LoadStats();
            if (mainStats.UpdateByStatID(statID) || secondaryStats.UpdateByStatID(statID))
            {
                return;
            }

            throw new KeyNotFoundException($"Stat ID {statID} was not found in main or secondary stats.");
        }

        public bool AddItem(int itemID, int quantity)
        {
            EnsureBagsLoaded();
            return inventory.AddItem(itemID, quantity);
        }

        public bool TryAddItem(int itemID, int quantity, out int leftoverQuantity)
        {
            EnsureBagsLoaded();
            return inventory.TryAddItem(itemID, quantity, out leftoverQuantity);
        }

        public bool MoveItem(int itemID, Bag departureBag, Bag destinationBag)
        {
            return MoveItem(itemID, departureBag, destinationBag, out _);
        }

        public bool MoveItem(int itemID, Bag departureBag, Bag destinationBag, out int leftoverQuantity)
        {
            if (departureBag == null || destinationBag == null || ReferenceEquals(departureBag, destinationBag))
            {
                leftoverQuantity = 0;
                return false;
            }

            var quantity = departureBag.GetItemQuantity(itemID);
            if (quantity <= 0)
            {
                leftoverQuantity = 0;
                return false;
            }

            GlobalItemCatalog.EnsureLoaded();
            if (!GlobalItemCatalog.Items.TryGetValue(itemID, out var template))
            {
                throw new KeyNotFoundException($"Item ID {itemID} was not found.");
            }

            destinationBag.TryAddItem(template, quantity, out leftoverQuantity);
            var movedQuantity = quantity - leftoverQuantity;
            if (movedQuantity <= 0)
            {
                return false;
            }

            if (!departureBag.TryRemoveItem(itemID, movedQuantity, out _))
            {
                throw new InvalidOperationException($"Failed to remove moved quantity for item ID {itemID} from departure bag.");
            }

            if (destinationBag == equipment || departureBag == equipment)
            {
                RebuildEquipmentModifiers();
                UpdateStats();
            }

            return leftoverQuantity == 0;
        }

        public bool AddMoney(Money moneyObj)
        {
            EnsureBagsLoaded();
            return inventory.AddMoney(moneyObj);
        }

        public bool MoveMoney(Money moneyObj, Bag departureBag, Bag destinationBag)
        {
            if (departureBag == null || destinationBag == null || !departureBag.TryRemoveMoney(moneyObj))
            {
                return false;
            }

            if (destinationBag.AddMoney(moneyObj))
            {
                return true;
            }

            departureBag.AddMoney(moneyObj);
            return false;
        }

        public bool EquipItem(int itemID)
        {
            EnsureBagsLoaded();
            if (!inventory.TryRemoveItem(itemID, 1, out var itemToEquip) || itemToEquip == null || !itemToEquip.IsEquipment)
            {
                return false;
            }

            var equipmentSlot = equipment.FindSlotFor(itemToEquip);
            if (equipmentSlot == null)
            {
                inventory.AddItem(itemToEquip, itemToEquip.quantity);
                return false;
            }

            var previouslyEquipped = equipmentSlot.item;
            if (previouslyEquipped != null && !inventory.AddItem(previouslyEquipped, previouslyEquipped.quantity))
            {
                inventory.AddItem(itemToEquip, itemToEquip.quantity);
                return false;
            }

            equipmentSlot.item = itemToEquip;
            RebuildEquipmentModifiers();
            UpdateStats();
            return true;
        }

        public bool UnequipItem(int itemID)
        {
            EnsureBagsLoaded();
            foreach (var slot in equipment.Slots)
            {
                if (slot.IsEmpty || slot.item.itemID != itemID)
                {
                    continue;
                }

                var item = slot.item;
                if (!inventory.AddItem(item, item.quantity))
                {
                    return false;
                }

                slot.item = null;
                RebuildEquipmentModifiers();
                UpdateStats();
                return true;
            }

            return false;
        }

        private void LoadStats()
        {
            mainStats ??= new MainStats();
            secondaryStats ??= new SecondaryStats();
            EnsureBagsLoaded();
            GetCharacterClass().SetOwner(this);
            EnsureCharacterModifiersLoaded();
            mainStats.SetOwner(this);
            secondaryStats.SetOwner(this);

            if (!mainStats.IsLoaded())
            {
                mainStats.LoadMainStats(this);
            }

            if (!secondaryStats.IsLoaded())
            {
                secondaryStats.LoadSecondaryStats(this);
            }
        }

        private void EnsureCharacterModifiersLoaded()
        {
            GlobalModifierCatalog.EnsureLoaded();

            if (familyBonusModifier == null)
            {
                familyBonusModifier = new FamilyBonusModifier();
            }

            familyBonusModifier.SetOwner(this);

            if (starSignModifier == null || starSignModifier.modifierID != starSignModifierID)
            {
                starSignModifier = CreateCharacterStarSignModifier(starSignModifierID, 1);
            }

            starSignModifier.SetOwner(this);
        }

        private StarSignModifier GetInactiveStarSignModifier(int modifierID)
        {
            inactiveStarSignModifiers ??= new Dictionary<int, StarSignModifier>();
            if (!inactiveStarSignModifiers.TryGetValue(modifierID, out var modifier))
            {
                modifier = CreateCharacterStarSignModifier(modifierID, 0);
                inactiveStarSignModifiers.Add(modifierID, modifier);
            }

            return modifier;
        }

        private static StarSignModifier CreateCharacterStarSignModifier(int modifierID, int modifierLevel)
        {
            if (!GlobalModifierCatalog.StarSignBonuses.TryGetValue(modifierID, out var source))
            {
                throw new KeyNotFoundException($"Star sign modifier ID {modifierID} was not found.");
            }

            // Characters get their own modifier instance so owner-bound updates do not mutate the global catalog object.
            var modifier = new StarSignModifier
            {
                modifierID = source.modifierID,
                name = source.name,
                description = source.description,
                level = modifierLevel,
                maxLevel = source.maxLevel,
                indexIncreaseByStatID = new Dictionary<int, Modifier.IndexIncrease>(source.indexIncreaseByStatID)
            };
            modifier.SetTags(new List<string>(source.GetTags()));
            return modifier;
        }

        private void EnsureBagsLoaded()
        {
            equipment ??= new Equipment();
            inventory ??= new Inventory();
            storage ??= new Storage();
            equipmentModifiers ??= new Dictionary<int, ItemModifier>();
            inactiveItemModifiers ??= new Dictionary<int, ItemModifier>();
            RebuildEquipmentModifiers();
        }

        private void EquipStartingItem(int startingItemID)
        {
            if (startingItemID < 5001 || startingItemID > 5004)
            {
                throw new ArgumentOutOfRangeException(nameof(startingItemID), startingItemID, "Starting item must be one of the first four hat item IDs.");
            }

            EnsureBagsLoaded();
            if (equipment.GetItemQuantity(startingItemID) > 0)
            {
                return;
            }

            AddItem(startingItemID, 1);
            EquipItem(startingItemID);
        }

        private void RebuildEquipmentModifiers()
        {
            equipmentModifiers ??= new Dictionary<int, ItemModifier>();
            equipmentModifiers.Clear();
            if (equipment == null)
            {
                return;
            }

            foreach (var slot in equipment.Slots)
            {
                if (slot.IsEmpty || slot.item.modifier <= 0)
                {
                    continue;
                }

                equipmentModifiers[slot.item.modifier] = CreateCharacterItemModifier(slot.item.modifier, 1);
            }
        }

        private ItemModifier GetInactiveItemModifier(int modifierID)
        {
            inactiveItemModifiers ??= new Dictionary<int, ItemModifier>();
            if (!inactiveItemModifiers.TryGetValue(modifierID, out var modifier))
            {
                modifier = CreateCharacterItemModifier(modifierID, 0);
                inactiveItemModifiers.Add(modifierID, modifier);
            }

            return modifier;
        }

        private ItemModifier CreateCharacterItemModifier(int modifierID, int modifierLevel)
        {
            GlobalModifierCatalog.EnsureLoaded();
            if (!GlobalModifierCatalog.ItemBonuses.TryGetValue(modifierID, out var source))
            {
                throw new KeyNotFoundException($"Item modifier ID {modifierID} was not found.");
            }

            var modifier = new ItemModifier
            {
                modifierID = source.modifierID,
                name = source.name,
                description = source.description,
                level = modifierLevel,
                maxLevel = source.maxLevel,
                indexIncreaseByStatID = new Dictionary<int, Modifier.IndexIncrease>(source.indexIncreaseByStatID)
            };
            modifier.SetTags(new List<string>(source.GetTags()));
            modifier.SetOwner(this);
            return modifier;
        }
        public float GetStatValueByID(int statID)
        {
            if (statID < 1005)
            {
                return MainStats.GetStatValueByID(statID);
            }
            return SecondaryStats.GetStatValueByID(statID);
        }
    }
}
