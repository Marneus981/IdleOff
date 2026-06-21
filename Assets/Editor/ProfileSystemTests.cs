using System;
using System.Linq;
using IdleOff.Game;
using IdleOff.Profiles;
using NUnit.Framework;
using UnityEngine;

public sealed class ProfileSystemTests
{
    [SetUp]
    public void SetUp()
    {
        GlobalModifierCatalog.LoadGlobalModifiers();
        GlobalItemCatalog.LoadItems();
    }

    [Test]
    public void Money_NormalizesAndSupportsArithmetic()
    {
        var money = new Money(1, 99, 150);

        Assert.AreEqual(2, money.goldP);
        Assert.AreEqual(0, money.silverP);
        Assert.AreEqual(50, money.copperP);
        Assert.AreEqual(20050, money.TotalCopper);

        money.Add(new Money(0, 0, 75));
        Assert.AreEqual(2, money.goldP);
        Assert.AreEqual(1, money.silverP);
        Assert.AreEqual(25, money.copperP);

        Assert.IsTrue(money.TrySubtract(new Money(1, 50, 25)));
        Assert.AreEqual(new Money(0, 51, 0).TotalCopper, money.TotalCopper);
        Assert.IsFalse(money.TrySubtract(new Money(1, 0, 0)));
        Assert.Throws<InvalidOperationException>(() => _ = new Money(0, 0, 1) - new Money(0, 0, 2));
    }

    [Test]
    public void GlobalCatalogs_LoadExpectedItemAndModifierDefinitions()
    {
        Assert.AreEqual(21, GlobalItemCatalog.Items.Count);
        Assert.AreEqual(18, GlobalModifierCatalog.ItemBonuses.Count);
        Assert.AreEqual(4, GlobalModifierCatalog.StarSignBonuses.Count);

        var circlet = GlobalItemCatalog.Items[5001];
        Assert.AreEqual("4 Leaf Circlet", circlet.name);
        Assert.AreEqual(ItemIconResolver.DefaultItemIconPath, circlet.iconPath);
        Assert.Contains("equipment", circlet.tags);
        Assert.Contains("hat", circlet.tags);
        Assert.AreEqual(1, circlet.maxStack);
        Assert.AreEqual(4001, circlet.modifier);

        var potion = GlobalItemCatalog.Items[5019];
        Assert.AreEqual("Health Potion", potion.name);
        Assert.AreEqual(99, potion.maxStack);
        Assert.AreEqual(0, potion.modifier);
        Assert.IsNotNull(ItemIconResolver.GetIcon(potion));
    }

    [Test]
    public void Bag_AddsStacksAndRemovesItems()
    {
        var inventory = new Inventory();

        Assert.IsTrue(inventory.AddItem(5019, 120));
        Assert.AreEqual(120, inventory.GetItemQuantity(5019));

        Assert.IsTrue(inventory.TryRemoveItem(5019, 30, out var removed));
        Assert.AreEqual(30, removed.quantity);
        Assert.AreEqual(90, inventory.GetItemQuantity(5019));

        Assert.IsFalse(inventory.AddItem(5019, 0));
        Assert.IsFalse(inventory.TryRemoveItem(5019, 999, out _));
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => inventory.AddItem(999999, 1));
    }

    [Test]
    public void Bag_TryAddItemReportsLeftoverWhenPartiallyFilled()
    {
        var inventory = new Inventory();

        Assert.IsFalse(inventory.TryAddItem(5019, 2000, out var leftover));
        Assert.AreEqual(20, leftover);
        Assert.AreEqual(1980, inventory.GetItemQuantity(5019));
    }

    [Test]
    public void Equipment_OnlyAcceptsMatchingTaggedEquipment()
    {
        var equipment = new Equipment();

        Assert.IsTrue(equipment.AddItem(5001, 1));
        Assert.AreEqual(1, equipment.GetItemQuantity(5001));

        Assert.IsFalse(equipment.AddItem(5019, 1));
        Assert.AreEqual(0, equipment.GetItemQuantity(5019));

        var potion = GlobalItemCatalog.Items[5019];
        Assert.IsNull(equipment.FindSlotFor(potion));
    }

    [Test]
    public void CharacterCreation_EquipsDefaultHatAndActivatesItsModifier()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);

        Assert.AreEqual(1, character.Equipment.GetItemQuantity(5001));
        Assert.IsTrue(character.EquipmentModifiers.ContainsKey(4001));
        Assert.AreEqual(5f, character.GetModifier(4001).AppliedIncrease(1004).Item2);

        Assert.AreEqual(0f, character.GetModifier(4002).AppliedIncrease(1001).Item2);
    }

    [Test]
    public void CharacterCreation_CanChooseStartingHat()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1, 3002, 5004);

        Assert.AreEqual(1, character.Equipment.GetItemQuantity(5004));
        Assert.IsTrue(character.EquipmentModifiers.ContainsKey(4004));
        Assert.AreEqual(5f, character.GetModifier(4004).AppliedIncrease(1003).Item2);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterData("Bad Hat", CharacterGender.Unspecified, 1, 3002, 5005));
    }

    [Test]
    public void EquipItem_SwapsHatAndUpdatesEquipmentModifiers()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);

        Assert.IsTrue(character.AddItem(5002, 1));
        Assert.IsTrue(character.EquipItem(5002));

        Assert.AreEqual(1, character.Equipment.GetItemQuantity(5002));
        Assert.AreEqual(1, character.Inventory.GetItemQuantity(5001));
        Assert.IsFalse(character.EquipmentModifiers.ContainsKey(4001));
        Assert.IsTrue(character.EquipmentModifiers.ContainsKey(4002));
        Assert.AreEqual(5f, character.GetModifier(4002).AppliedIncrease(1001).Item2);
        Assert.AreEqual(0f, character.GetModifier(4001).AppliedIncrease(1004).Item2);
    }

    [Test]
    public void UnequipItem_MovesEquipmentBackToInventoryAndDisablesModifier()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);

        Assert.IsTrue(character.UnequipItem(5001));
        Assert.AreEqual(0, character.Equipment.GetItemQuantity(5001));
        Assert.AreEqual(1, character.Inventory.GetItemQuantity(5001));
        Assert.IsFalse(character.EquipmentModifiers.ContainsKey(4001));
        Assert.AreEqual(0f, character.GetModifier(4001).AppliedIncrease(1004).Item2);
    }

    [Test]
    public void SlotMove_SwapsInventoryItemsBySlot()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);
        Assert.IsTrue(character.AddItem(5019, 3));
        Assert.IsTrue(character.AddItem(5020, 2));

        Assert.IsTrue(character.TryMoveSlotItem(character.Inventory, 0, character.Inventory, 1));

        Assert.AreEqual(5020, character.Inventory.Slots[0].item.itemID);
        Assert.AreEqual(5019, character.Inventory.Slots[1].item.itemID);
    }

    [Test]
    public void SlotMove_EquipsMatchingItemAndRebuildsEquipmentModifiers()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);
        Assert.IsTrue(character.AddItem(5002, 1));

        Assert.IsTrue(character.TryMoveSlotItem(character.Inventory, 0, character.Equipment, 0));

        Assert.AreEqual(5002, character.Equipment.Slots[0].item.itemID);
        Assert.AreEqual(5001, character.Inventory.Slots[0].item.itemID);
        Assert.IsTrue(character.EquipmentModifiers.ContainsKey(4002));
        Assert.IsFalse(character.EquipmentModifiers.ContainsKey(4001));
    }

    [Test]
    public void SlotMove_RejectsItemThatDoesNotMatchEquipmentSlot()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);
        Assert.IsTrue(character.AddItem(5019, 1));

        Assert.IsFalse(character.TryMoveSlotItem(character.Inventory, 0, character.Equipment, 0));

        Assert.AreEqual(5019, character.Inventory.Slots[0].item.itemID);
        Assert.AreEqual(5001, character.Equipment.Slots[0].item.itemID);
    }

    [Test]
    public void SlotDrop_RemovesItemFromSourceAndRebuildsEquipmentModifiers()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);

        Assert.IsTrue(character.TryDropSlotItem(character.Equipment, 0, out var dropped));

        Assert.AreEqual(5001, dropped.itemID);
        Assert.IsTrue(character.Equipment.Slots[0].IsEmpty);
        Assert.IsFalse(character.EquipmentModifiers.ContainsKey(4001));
    }

    [Test]
    public void MoneyMovement_OnlyWorksBetweenBagsThatCanHoldMoney()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);
        var amount = new Money(0, 2, 50);

        Assert.IsTrue(character.AddMoney(amount));
        Assert.AreEqual(amount.TotalCopper, character.Inventory.Money.TotalCopper);

        Assert.IsTrue(character.MoveMoney(new Money(0, 1, 25), character.Inventory, character.Storage));
        Assert.AreEqual(new Money(0, 1, 25).TotalCopper, character.Inventory.Money.TotalCopper);
        Assert.AreEqual(new Money(0, 1, 25).TotalCopper, character.Storage.Money.TotalCopper);

        Assert.IsFalse(character.MoveMoney(new Money(0, 1, 0), character.Inventory, character.Equipment));
        Assert.AreEqual(new Money(0, 1, 25).TotalCopper, character.Inventory.Money.TotalCopper);
    }

    [Test]
    public void MoveItem_MovesWholeStackBetweenBags()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);

        Assert.IsTrue(character.AddItem(5019, 12));
        Assert.IsTrue(character.MoveItem(5019, character.Inventory, character.Storage));

        Assert.AreEqual(0, character.Inventory.GetItemQuantity(5019));
        Assert.AreEqual(12, character.Storage.GetItemQuantity(5019));
        Assert.IsFalse(character.MoveItem(5019, character.Inventory, character.Storage));
    }

    [Test]
    public void MoveItem_PartialDestinationKeepsLeftoverInSource()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);

        Assert.IsTrue(character.Storage.AddItem(5019, 4940));
        Assert.IsTrue(character.AddItem(5019, 120));

        Assert.IsFalse(character.MoveItem(5019, character.Inventory, character.Storage, out var leftover));
        Assert.AreEqual(110, leftover);
        Assert.AreEqual(110, character.Inventory.GetItemQuantity(5019));
        Assert.AreEqual(4950, character.Storage.GetItemQuantity(5019));
    }

    [Test]
    public void MoveItem_RejectsSameSourceAndDestination()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);

        Assert.IsTrue(character.AddItem(5019, 5));
        Assert.IsFalse(character.MoveItem(5019, character.Inventory, character.Inventory, out var leftover));
        Assert.AreEqual(0, leftover);
        Assert.AreEqual(5, character.Inventory.GetItemQuantity(5019));
    }

    [Test]
    public void StarSignModifier_OnlySelectedSignContributes()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1, 3003);

        Assert.AreEqual(3f, character.GetModifier(3003).AppliedIncrease(1002).Item2);
        Assert.AreEqual(0f, character.GetModifier(3002).AppliedIncrease(1001).Item2);
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => character.GetModifier(3999));
    }

    [Test]
    public void StatFormula_DispatchesThroughBaseReference()
    {
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1);
        var statsTable = Stat.LoadStatsTable("Assets/Tables/Stats.json");
        Stat accuracy = Stat.CreateFromTable<Accuracy>(1005, statsTable, character);
        var formulaValues = Enumerable.Repeat(0f, 9).ToList();

        Assert.AreEqual(4.5f, accuracy.Formula(formulaValues));
    }

    [Test]
    public void FamilyBonus_UsesHighestMatchingCharacterLevelsInProfile()
    {
        var profile = ScriptableObject.CreateInstance<CharacterProfile>();
        try
        {
            var owner = new CharacterData("Owner", CharacterGender.Unspecified, 1);
            var knight = new CharacterData("Knight", CharacterGender.Unspecified, CharacterClass.CreateErrantKnight(), 3002, 5001);
            var hunter = new CharacterData("Hunter", CharacterGender.Unspecified, CharacterClass.CreateWildHunter(), 3002, 5001);
            var whisperer = new CharacterData("Whisperer", CharacterGender.Unspecified, CharacterClass.CreateVoidWhisperer(), 3002, 5001);
            var wanderer = new CharacterData("Wanderer", CharacterGender.Unspecified, CharacterClass.CreateWanderingSoul(), 3002, 5001);

            knight.CharacterClass.SetLevelNumber(7);
            hunter.CharacterClass.SetLevelNumber(5);
            whisperer.CharacterClass.SetLevelNumber(4);
            wanderer.CharacterClass.SetLevelNumber(9);

            Assert.IsTrue(profile.TryAddCharacter(owner));
            Assert.IsTrue(profile.TryAddCharacter(knight));
            Assert.IsTrue(profile.TryAddCharacter(hunter));
            Assert.IsTrue(profile.TryAddCharacter(whisperer));
            Assert.IsTrue(profile.TryAddCharacter(wanderer));

            Assert.AreEqual(4f, owner.GetModifier(3001).AppliedIncrease(1001).Item2);
            Assert.AreEqual(3f, owner.GetModifier(3001).AppliedIncrease(1002).Item2);
            Assert.AreEqual(0f, owner.GetModifier(3001).AppliedIncrease(1003).Item2);
            Assert.AreEqual(5f, owner.GetModifier(3001).AppliedIncrease(1004).Item2);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    [Test]
    public void CharacterProfile_RejectsNullAndRespectsMaxCharacters()
    {
        var profile = ScriptableObject.CreateInstance<CharacterProfile>();
        try
        {
            Assert.IsFalse(profile.TryAddCharacter(null));

            for (var i = 0; i < CharacterProfile.MaxCharacters; i++)
            {
                Assert.IsTrue(profile.TryAddCharacter(new CharacterData($"Character {i}", CharacterGender.Unspecified, 1)));
            }

            Assert.IsFalse(profile.HasRoom);
            Assert.IsFalse(profile.TryAddCharacter(new CharacterData("Extra", CharacterGender.Unspecified, 1)));
            Assert.AreEqual(CharacterProfile.MaxCharacters, profile.Characters.Count);
            Assert.IsTrue(profile.Characters.All(character => character.ParentProfile == profile));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    [Test]
    public void ProfileManager_SaveLoad_PreservesFullCharacterProgressionAndBags()
    {
        var manager = new ProfileManager();
        var record = manager.CreateProfile("Full Save Test " + Guid.NewGuid().ToString("N"));
        try
        {
            var character = new CharacterData("Saved Hero", CharacterGender.Unspecified, CharacterClass.CreateWildHunter(), 3004, 5001);
            Assert.IsTrue(record.Profile.TryAddCharacter(character));
            record.Profile.SetActiveCharacterIndex(0);

            character.CharacterClass.SetLevelNumber(7);
            character.CharacterClass.SetCurrentXP(42f);
            character.CharacterClass.SetMaxXP(900f);
            character.CharacterClass.SetBaseTalentPoints(11);
            character.CharacterClass.SetClassTalentPoints(13);
            character.CharacterClass.GetModifier(2001).level = 3;
            character.CharacterClass.GetClassActions()[0].level = 4;
            character.AddItem(5019, 12);
            character.AddMoney(new Money(1, 2, 3));
            Assert.IsTrue(character.MoveMoney(new Money(0, 1, 0), character.Inventory, character.Storage));
            Assert.IsTrue(character.AddItem(5002, 1));
            Assert.IsTrue(character.EquipItem(5002));
            Assert.IsTrue(character.MoveItem(5019, character.Inventory, character.Storage));

            manager.SaveProfile(record);

            var loadedManager = new ProfileManager();
            loadedManager.LoadProfiles();
            var loadedRecord = loadedManager.Profiles.First(profile => profile.ProfileID == record.ProfileID);
            var loaded = loadedRecord.Profile.ActiveCharacter;

            Assert.AreEqual(character.CharacterID, loaded.CharacterID);
            Assert.AreEqual("Saved Hero", loaded.CharacterName);
            Assert.AreEqual("Wild Hunter", loaded.CharacterClass.GetClassName());
            Assert.AreEqual(7, loaded.Level);
            Assert.AreEqual(42f, loaded.CharacterClass.GetCurrentXP(), 0.001f);
            Assert.AreEqual(900f, loaded.CharacterClass.GetMaxXP(), 0.001f);
            Assert.AreEqual(11, loaded.CharacterClass.GetBaseTalentPoints());
            Assert.AreEqual(13, loaded.CharacterClass.GetClassTalentPoints());
            Assert.AreEqual(3, loaded.CharacterClass.GetModifier(2001).level);
            Assert.AreEqual(4, loaded.CharacterClass.GetClassActions()[0].level);
            Assert.AreEqual(1, loaded.Equipment.GetItemQuantity(5002));
            Assert.AreEqual(12, loaded.Storage.GetItemQuantity(5019));
            Assert.AreEqual(new Money(1, 1, 3).TotalCopper, loaded.Inventory.Money.TotalCopper);
            Assert.AreEqual(new Money(0, 1, 0).TotalCopper, loaded.Storage.Money.TotalCopper);
        }
        finally
        {
            manager.DeleteProfile(record);
        }
    }
}
