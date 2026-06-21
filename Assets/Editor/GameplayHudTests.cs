using System.Collections;
using System.Reflection;
using IdleOff.Combat;
using IdleOff.Drops;
using IdleOff.Game;
using IdleOff.Maps;
using IdleOff.Profiles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;

public sealed class GameplayHudTests
{
    private CharacterProfile profile;
    private CharacterData character;
    private GameObject playerObject;
    private PlayerCombatant player;
    private GameplayHud hud;

    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;
        GlobalModifierCatalog.LoadGlobalModifiers();
        GlobalItemCatalog.LoadItems();
        DestroyExistingHud();
        DestroyWorldDropsAndSpawners();

        profile = ScriptableObject.CreateInstance<CharacterProfile>();
        character = new CharacterData("Hud Tester", CharacterGender.Unspecified, 1);
        Assert.IsTrue(profile.TryAddCharacter(character));
        profile.SetActiveCharacterIndex(0);

        playerObject = new GameObject("HUD Test Player");
        player = playerObject.AddComponent<PlayerCombatant>();
        player.SetProfile(profile);

        hud = new GameObject("Gameplay HUD Test Instance").AddComponent<GameplayHud>();
        hud.SetCharacter(character);
        hud.SetPlayer(player);
        Assert.IsNotNull(hud.SectionTwo, "Expected the HUD to build Section Two during setup.");
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
        if (playerObject != null)
        {
            Object.DestroyImmediate(playerObject);
        }

        if (hud != null)
        {
            Object.DestroyImmediate(hud.gameObject);
        }

        if (profile != null)
        {
            Object.DestroyImmediate(profile);
        }

        DestroyExistingHud();
        DestroyWorldDropsAndSpawners();
        DestroyNamedObject("HUD Test EventSystem");
        DestroyNamedObject("HUD Test Camera");
        DestroyNamedObject("HUD Test MapManager");
    }

    [Test]
    public void SectionTwo_BuildsHpMpXpBarsWithExpectedLabels()
    {
        Assert.AreEqual("HP", FindText("HP Bar Label").text);
        Assert.AreEqual("MP", FindText("MP Bar Label").text);
        Assert.AreEqual("XP", FindText("XP Bar Label").text);

        AssertResourceText("HP Bar Value", player.CurrentHp, player.MaxHp);
        AssertResourceText("MP Bar Value", player.CurrentMp, player.MaxMp);
        AssertResourceText("XP Bar Value", character.CharacterClass.GetCurrentXP(), character.CharacterClass.GetMaxXP());
        Assert.AreEqual(1f, FindRect("HP Bar Fill").anchorMax.x, 0.001f);
        Assert.AreEqual(1f, FindRect("MP Bar Fill").anchorMax.x, 0.001f);
        Assert.AreEqual(0f, FindRect("XP Bar Fill").anchorMax.x, 0.001f);
    }

    [Test]
    public void HpBar_UpdatesWhenPlayerTakesDamage()
    {
        var damage = player.MaxHp * 0.25f;

        player.ReceiveDamage(new DamageResult(null, player, true, 1f, damage, damage, 0));

        AssertResourceText("HP Bar Value", player.CurrentHp, player.MaxHp);
        Assert.AreEqual(0.75f, FindRect("HP Bar Fill").anchorMax.x, 0.001f);
    }

    [Test]
    public void HpBar_UpdatesWhenPlayerRegeneratesHealth()
    {
        var damage = player.MaxHp * 0.25f;
        player.ReceiveDamage(new DamageResult(null, player, true, 1f, damage, damage, 0));

        player.TickHealthRegen(1f);

        AssertResourceText("HP Bar Value", player.CurrentHp, player.MaxHp);
        Assert.Greater(FindRect("HP Bar Fill").anchorMax.x, 0.75f);
    }

    [Test]
    public void MpBar_UpdatesWhenPlayerResourceEventFires()
    {
        SetPrivateCombatHealthCurrent(player, "mana", player.MaxMp * 0.4f);

        player.NotifyResourcesChanged();

        AssertResourceText("MP Bar Value", player.CurrentMp, player.MaxMp);
        Assert.AreEqual(0.4f, FindRect("MP Bar Fill").anchorMax.x, 0.001f);
    }

    [Test]
    public void MpBar_UpdatesWhenPlayerRegeneratesMana()
    {
        SetPrivateCombatHealthCurrent(player, "mana", player.MaxMp * 0.4f);
        player.NotifyResourcesChanged();

        player.TickMpRegen(1f);

        AssertResourceText("MP Bar Value", player.CurrentMp, player.MaxMp);
        Assert.Greater(FindRect("MP Bar Fill").anchorMax.x, 0.4f);
    }

    [Test]
    public void XpBar_UpdatesWhenCharacterGainsXp()
    {
        character.CharacterClass.SetCurrentXP(character.CharacterClass.GetMaxXP() * 0.25f);

        AssertResourceText("XP Bar Value", character.CharacterClass.GetCurrentXP(), character.CharacterClass.GetMaxXP());
        Assert.AreEqual(0.25f, FindRect("XP Bar Fill").anchorMax.x, 0.001f);
    }

    [Test]
    public void XpAndLevelWidgets_UpdateWhenCharacterLevelsUp()
    {
        character.CharacterClass.SetCurrentXP(character.CharacterClass.GetMaxXP());

        character.CharacterClass.LevelUp(character);

        Assert.AreEqual("LVL 02", FindText("Character Level").text);
        AssertResourceText("XP Bar Value", character.CharacterClass.GetCurrentXP(), character.CharacterClass.GetMaxXP());
        Assert.AreEqual(0f, FindRect("XP Bar Fill").anchorMax.x, 0.001f);
    }

    [Test]
    public void ClassLabel_UpdatesWhenCharacterClassChanges()
    {
        character.CharacterClass.ChangeClass("Wild Hunter");

        Assert.AreEqual("Wild Hunter", FindText("Character Class").text);
    }

    [Test]
    public void ResourceBars_UpdateMaxValuesWhenCharacterStatsRecalculate()
    {
        SetPrivateCombatHealthMaxAndCurrent(player, "health", 1f, 1f);
        SetPrivateCombatHealthMaxAndCurrent(player, "mana", 1f, 1f);
        player.NotifyResourcesChanged();

        character.UpdateStats();

        AssertResourceText("HP Bar Value", player.CurrentHp, player.MaxHp);
        AssertResourceText("MP Bar Value", player.CurrentMp, player.MaxMp);
        Assert.AreEqual(player.CurrentHp / player.MaxHp, FindRect("HP Bar Fill").anchorMax.x, 0.001f);
        Assert.AreEqual(player.CurrentMp / player.MaxMp, FindRect("MP Bar Fill").anchorMax.x, 0.001f);
    }

    [Test]
    public void SetCharacter_UnsubscribesPreviousCharacterUpdates()
    {
        var secondCharacter = new CharacterData("Second", CharacterGender.Unspecified, 1);
        secondCharacter.CharacterClass.SetCurrentXP(secondCharacter.CharacterClass.GetMaxXP() * 0.5f);

        hud.SetCharacter(secondCharacter);
        character.CharacterClass.SetCurrentXP(character.CharacterClass.GetMaxXP() * 0.9f);

        AssertResourceText("XP Bar Value", secondCharacter.CharacterClass.GetCurrentXP(), secondCharacter.CharacterClass.GetMaxXP());
        Assert.AreEqual(0.5f, FindRect("XP Bar Fill").anchorMax.x, 0.001f);
    }

    [Test]
    public void SectionFour_BuildsInventorySkillsAndMenuButtons()
    {
        Assert.AreEqual("Inventory", FindText("Inventory Button Text").text);
        Assert.AreEqual("Skills", FindText("Skills Button Text").text);
        Assert.AreEqual("Menu", FindText("Menu Button Text").text);

        AssertButtonAnchors("Inventory Button", 0f, 0.30f, 0f, 1f);
        AssertButtonAnchors("Skills Button", 0.35f, 0.65f, 0f, 1f);
        AssertButtonAnchors("Menu Button", 0.70f, 1f, 0f, 1f);
    }

    [Test]
    public void MenuPanel_BuildsHiddenAboveSectionFourWithExpectedButtons()
    {
        var panel = FindRect("HUD Menu Panel");

        Assert.IsFalse(panel.gameObject.activeSelf);
        Assert.AreEqual(0.705f, panel.anchorMin.x, 0.001f);
        Assert.AreEqual(0.99f, panel.anchorMax.x, 0.001f);
        Assert.AreEqual(1f, panel.anchorMin.y, 0.001f);
        Assert.AreEqual(1f, panel.anchorMax.y, 0.001f);
        Assert.AreEqual("Title Screen", FindText("Title Screen Button Text").text);
        Assert.AreEqual("Character Selection", FindText("Character Selection Button Text").text);
        AssertButtonAnchors("Title Screen Button", 0.05f, 0.95f, 0.05f, 0.475f);
        AssertButtonAnchors("Character Selection Button", 0.05f, 0.95f, 0.525f, 0.95f);
    }

    [UnityTest]
    public IEnumerator MenuButton_OpensExpandablePanelUpward()
    {
        var panel = FindRect("HUD Menu Panel");
        Assert.IsFalse(panel.gameObject.activeSelf);

        FindButton("Menu Button").onClick.Invoke();
        yield return null;
        yield return null;

        Assert.IsTrue(panel.gameObject.activeSelf);
        Assert.Greater(panel.offsetMax.y, 0f);
    }

    [UnityTest]
    public IEnumerator CloseMenuImmediate_HidesPanelAndResetsAnimatedHeight()
    {
        var panel = FindRect("HUD Menu Panel");
        FindButton("Menu Button").onClick.Invoke();
        yield return null;

        hud.CloseMenuImmediate();

        Assert.IsFalse(panel.gameObject.activeSelf);
        Assert.AreEqual(0f, panel.offsetMax.y, 0.001f);
    }

    [UnityTest]
    public IEnumerator RebindingHudAfterLogin_ClosesExpandedMenu()
    {
        var panel = FindRect("HUD Menu Panel");
        FindButton("Menu Button").onClick.Invoke();
        yield return null;

        hud.gameObject.SetActive(false);
        yield return null;

        hud.gameObject.SetActive(true);
        hud.SetCharacter(character);
        hud.SetPlayer(player);

        Assert.IsFalse(panel.gameObject.activeSelf);
        Assert.AreEqual(0f, panel.offsetMax.y, 0.001f);
    }

    [Test]
    public void TitleScreenButton_WarnsWhenBootFlowIsUnavailable()
    {
        LogAssert.Expect(LogType.Warning, "[HUD] Cannot return to title screen because no BootFlowUI instance exists.");

        FindButton("Title Screen Button").onClick.Invoke();
    }

    [Test]
    public void CharacterSelectionButton_WarnsWhenBootFlowIsUnavailable()
    {
        LogAssert.Expect(LogType.Warning, "[HUD] Cannot return to character selection because no BootFlowUI instance exists.");

        FindButton("Character Selection Button").onClick.Invoke();
    }

    [Test]
    public void CharacterClass_TryUpgradeBaseModifier_SpendsBasePointAndUpdatesStats()
    {
        var characterClass = character.CharacterClass;
        var modifier = characterClass.GetModifier(2001);
        var initialLevel = modifier.level;
        var initialBasePoints = characterClass.GetBaseTalentPoints();
        var initialClassPoints = characterClass.GetClassTalentPoints();
        var initialMaxHp = character.GetStatValueByID(1011);

        Assert.IsTrue(characterClass.TryUpgradeModifier(modifier));

        Assert.AreEqual(initialLevel + 1, modifier.level);
        Assert.AreEqual(initialBasePoints - 1, characterClass.GetBaseTalentPoints());
        Assert.AreEqual(initialClassPoints, characterClass.GetClassTalentPoints());
        Assert.AreEqual(initialMaxHp + 3f, character.GetStatValueByID(1011), 0.001f);
    }

    [Test]
    public void CharacterClass_TryUpgradeBaseModifier_CanSpendClassPointWhenBasePointsAreUnavailable()
    {
        var characterClass = character.CharacterClass;
        var modifier = characterClass.GetModifier(2001);
        characterClass.SetBaseTalentPoints(0);
        characterClass.SetClassTalentPoints(1);

        Assert.IsTrue(characterClass.TryUpgradeModifier(modifier));

        Assert.AreEqual(1, modifier.level);
        Assert.AreEqual(0, characterClass.GetBaseTalentPoints());
        Assert.AreEqual(0, characterClass.GetClassTalentPoints());
    }

    [Test]
    public void CharacterClass_TryUpgradeAction_SpendsClassPointAndRaisesSkillLevel()
    {
        var characterClass = character.CharacterClass;
        var action = characterClass.GetAction(2001);
        var initialLevel = action.level;
        var initialBasePoints = characterClass.GetBaseTalentPoints();
        var initialClassPoints = characterClass.GetClassTalentPoints();

        Assert.IsTrue(characterClass.TryUpgradeAction(action));

        Assert.AreEqual(initialLevel + 1, action.level);
        Assert.AreEqual(initialBasePoints, characterClass.GetBaseTalentPoints());
        Assert.AreEqual(initialClassPoints - 1, characterClass.GetClassTalentPoints());
    }

    [Test]
    public void CharacterClass_UpgradeFailsWhenTalentPointsAreInsufficient()
    {
        var characterClass = character.CharacterClass;
        var modifier = characterClass.GetModifier(2001);
        var action = characterClass.GetAction(2001);
        characterClass.SetBaseTalentPoints(0);
        characterClass.SetClassTalentPoints(0);

        Assert.IsFalse(characterClass.TryUpgradeModifier(modifier));
        Assert.IsFalse(characterClass.TryUpgradeAction(action));
        Assert.AreEqual(0, modifier.level);
        Assert.AreEqual(1, action.level);
    }

    [Test]
    public void SkillsPanel_ModifierUpgradeButton_UsesTalentPipelineAndRefreshesEntry()
    {
        var characterClass = character.CharacterClass;
        var modifier = characterClass.GetModifier(2001);

        FindButton("Modifier Skill Entry 2001 Upgrade Button").onClick.Invoke();

        Assert.AreEqual(1, modifier.level);
        Assert.AreEqual(2, characterClass.GetBaseTalentPoints());
        Assert.AreEqual(3, characterClass.GetClassTalentPoints());
        Assert.AreEqual("2", FindText("Base Skill Points Value").text);
        Assert.AreEqual("3", FindText("Class Skill Points Value").text);
        Assert.AreEqual("LVL\n1\n100", FindText("Modifier Skill Entry 2001 Level").text);
    }

    [Test]
    public void SkillsPanel_ActionUpgradeButton_UsesTalentPipelineAndRefreshesEntry()
    {
        var characterClass = character.CharacterClass;
        var action = characterClass.GetAction(2001);

        FindButton("Action Skill Entry 2001 Upgrade Button").onClick.Invoke();

        Assert.AreEqual(2, action.level);
        Assert.AreEqual(3, characterClass.GetBaseTalentPoints());
        Assert.AreEqual(2, characterClass.GetClassTalentPoints());
        Assert.AreEqual("3", FindText("Base Skill Points Value").text);
        Assert.AreEqual("2", FindText("Class Skill Points Value").text);
        Assert.AreEqual("LVL\n2\n100", FindText("Action Skill Entry 2001 Level").text);
    }

    [Test]
    public void SkillsPanel_UpgradeButtonsAreDisabledWhenTalentPointsAreInsufficient()
    {
        var characterClass = character.CharacterClass;
        characterClass.SetBaseTalentPoints(0);
        characterClass.SetClassTalentPoints(0);
        hud.SetCharacter(character);

        Assert.IsFalse(FindButton("Modifier Skill Entry 2001 Upgrade Button").interactable);
        Assert.IsFalse(FindButton("Action Skill Entry 2001 Upgrade Button").interactable);
        Assert.AreEqual("0", FindText("Base Skill Points Value").text);
        Assert.AreEqual("0", FindText("Class Skill Points Value").text);
    }

    [Test]
    public void InventoryPanel_BuildsExpectedColumnsAndSlotGrids()
    {
        var panel = FindRect("HUD Inventory Panel");
        var left = FindRect("HUD Inventory Left Canvas");
        var right = FindRect("HUD Inventory Scroll Field");
        var equipmentArea = FindRect("HUD Equipment Slots Area");
        var moneyArea = FindRect("HUD Inventory Money Area");

        Assert.IsFalse(panel.gameObject.activeSelf);
        Assert.AreEqual(0.21f, panel.anchorMin.x, 0.001f);
        Assert.AreEqual(0.99f, panel.anchorMax.x, 0.001f);
        Assert.AreEqual(0f, left.anchorMin.x, 0.001f);
        Assert.AreEqual(0.40f, left.anchorMax.x, 0.001f);
        Assert.AreEqual(0.45f, right.anchorMin.x, 0.001f);
        Assert.AreEqual(1f, right.anchorMax.x, 0.001f);
        Assert.AreEqual(0.12f, equipmentArea.anchorMin.y, 0.001f);
        Assert.AreEqual(1f, equipmentArea.anchorMax.y, 0.001f);
        Assert.AreEqual(0f, moneyArea.anchorMin.y, 0.001f);
        Assert.AreEqual(0.10f, moneyArea.anchorMax.y, 0.001f);
        Assert.IsNotNull(FindSlotWidget("HUD Equipment Slot 0"));
        Assert.IsNotNull(FindSlotWidget("HUD Inventory Slot 0"));
        Assert.IsNotNull(FindRect("HUD Equipment Slot 0 Item Slot").GetComponent<AspectRatioFitter>());
        Assert.IsNotNull(FindRect("HUD Inventory Slot 0 Item Slot").GetComponent<AspectRatioFitter>());
        AssertPaddedAnchors("HUD Equipment Slot 0", 0.02f);
        AssertPaddedAnchors("HUD Inventory Slot 0", 0.02f);
        AssertEquipmentItemSlotAnchors("HUD Equipment Slot 0 Item Slot");
        AssertPaddedAnchors("HUD Inventory Slot 0 Item Slot", 0.02f);
    }

    [Test]
    public void InventoryPanel_RendersItemIconsAndStackQuantity()
    {
        Assert.IsTrue(character.AddItem(5019, 3));
        hud.SetCharacter(character);

        var icon = FindImage("HUD Inventory Slot 0 Icon");
        Assert.IsNotNull(icon.sprite);
        Assert.AreEqual("3", FindText("HUD Inventory Slot 0 Quantity").text);
    }

    [Test]
    public void InventoryPanel_UpdatesWhenItemPickupCollected()
    {
        Assert.IsFalse(TryFindImage("HUD Inventory Slot 0 Icon", out _));
        var dropObject = new GameObject("HUD Item Pickup Drop");
        var drop = dropObject.AddComponent<WorldDrop>();
        drop.Initialize(WorldDropPayload.Item(5019, 3));

        Assert.IsTrue(drop.TryCollect(character));

        Assert.IsNotNull(FindImage("HUD Inventory Slot 0 Icon").sprite);
        Assert.AreEqual("3", FindText("HUD Inventory Slot 0 Quantity").text);
    }

    [Test]
    public void InventoryPanel_UpdatesWhenMoneyPickupCollected()
    {
        Assert.AreEqual("0g 0s 0c", FindText("HUD Inventory Money Text").text);
        var dropObject = new GameObject("HUD Money Pickup Drop");
        var drop = dropObject.AddComponent<WorldDrop>();
        drop.Initialize(WorldDropPayload.Money(new Money(0, 2, 5)));

        Assert.IsTrue(drop.TryCollect(character));

        Assert.AreEqual("0g 2s 5c", FindText("HUD Inventory Money Text").text);
    }

    [Test]
    public void InventoryPanel_DropOnInventorySlotSwapsItemsAndRefreshesUi()
    {
        Assert.IsTrue(character.AddItem(5019, 3));
        Assert.IsTrue(character.AddItem(5020, 2));
        hud.SetCharacter(character);
        var source = FindSlotWidget("HUD Inventory Slot 0");
        var destination = FindSlotWidget("HUD Inventory Slot 1");

        hud.BeginInventorySlotDrag(source);
        hud.DropInventorySlotOn(destination);
        hud.EndInventorySlotDrag(null);

        Assert.AreEqual(5020, character.Inventory.Slots[0].item.itemID);
        Assert.AreEqual(5019, character.Inventory.Slots[1].item.itemID);
        Assert.IsNotNull(FindImage("HUD Inventory Slot 0 Icon").sprite);
        Assert.IsNotNull(FindImage("HUD Inventory Slot 1 Icon").sprite);
    }

    [Test]
    public void InventoryPanel_DropOnMatchingInventorySlotStacksItemsAndRefreshesUi()
    {
        Assert.IsTrue(character.AddItem(5019, 103));
        Assert.IsTrue(character.Inventory.TryRemoveItem(5019, 95, out _));
        hud.SetCharacter(character);
        hud.ShowInventory();
        var source = FindSlotWidget("HUD Inventory Slot 0");
        var destination = FindSlotWidget("HUD Inventory Slot 1");

        hud.BeginInventorySlotDrag(source);
        hud.DropInventorySlotOn(destination);
        hud.EndInventorySlotDrag(null);

        Assert.IsTrue(character.Inventory.Slots[0].IsEmpty);
        Assert.AreEqual(8, character.Inventory.Slots[1].item.quantity);
        Assert.IsFalse(TryFindImage("HUD Inventory Slot 0 Icon", out _));
        Assert.AreEqual("8", FindText("HUD Inventory Slot 1 Quantity").text);
    }

    [Test]
    public void InventoryPanel_DropOnEquipmentSlotEquipsMatchingItemAndRefreshesUi()
    {
        Assert.IsTrue(character.AddItem(5002, 1));
        hud.SetCharacter(character);
        var source = FindSlotWidget("HUD Inventory Slot 0");
        var destination = FindSlotWidget("HUD Equipment Slot 0");

        hud.BeginInventorySlotDrag(source);
        hud.DropInventorySlotOn(destination);
        hud.EndInventorySlotDrag(null);

        Assert.AreEqual(5002, character.Equipment.Slots[0].item.itemID);
        Assert.AreEqual(5001, character.Inventory.Slots[0].item.itemID);
        Assert.IsTrue(character.EquipmentModifiers.ContainsKey(4002));
        Assert.IsFalse(character.EquipmentModifiers.ContainsKey(4001));
        Assert.IsNotNull(FindImage("HUD Equipment Slot 0 Icon").sprite);
    }

    [Test]
    public void InventoryPanel_DropOnEquipmentSlotRejectsInvalidItem()
    {
        Assert.IsTrue(character.AddItem(5019, 1));
        hud.SetCharacter(character);
        var source = FindSlotWidget("HUD Inventory Slot 0");
        var destination = FindSlotWidget("HUD Equipment Slot 0");

        hud.BeginInventorySlotDrag(source);
        hud.DropInventorySlotOn(destination);
        hud.EndInventorySlotDrag(null);

        Assert.AreEqual(5019, character.Inventory.Slots[0].item.itemID);
        Assert.AreEqual(5001, character.Equipment.Slots[0].item.itemID);
    }

    [Test]
    public void InventoryPanel_DragOutsideDropsItemIntoWorld()
    {
        Assert.IsTrue(character.AddItem(5019, 3));
        hud.SetCharacter(character);
        EnsureEventSystem();
        var source = FindSlotWidget("HUD Inventory Slot 0");
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(-5000f, -5000f)
        };

        hud.BeginInventorySlotDrag(source);
        hud.EndInventorySlotDrag(eventData);

        Assert.AreEqual(0, character.Inventory.GetItemQuantity(5019));
        var drops = Object.FindObjectsByType<WorldDrop>(FindObjectsSortMode.None);
        Assert.AreEqual(1, drops.Length);
        Assert.AreEqual(5019, drops[0].Payload.itemID);
        Assert.AreEqual(3, drops[0].Payload.quantity);
    }

    [Test]
    public void InventoryPanel_DragOutsideDropsItemAtPointerWorldPosition()
    {
        Assert.IsTrue(character.AddItem(5019, 1));
        hud.SetCharacter(character);
        EnsureEventSystem();
        var playerCollider = playerObject.AddComponent<BoxCollider2D>();
        var cameraObject = new GameObject("HUD Test Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(10f, 20f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        var screenPosition = new Vector2(25f, 25f);
        var expected = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
        expected.z = 0f;
        var source = FindSlotWidget("HUD Inventory Slot 0");
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        hud.BeginInventorySlotDrag(source);
        hud.EndInventorySlotDrag(eventData);

        var drops = Object.FindObjectsByType<WorldDrop>(FindObjectsSortMode.None);
        Assert.AreEqual(1, drops.Length);
        Assert.AreEqual(expected.x, drops[0].transform.position.x, 0.001f);
        Assert.AreEqual(expected.y, drops[0].transform.position.y, 0.001f);
        Assert.IsNotNull(drops[0].GetComponent<Rigidbody2D>());
        var solidCollider = GetDropCollider(drops[0], false);
        var pickupTrigger = GetDropCollider(drops[0], true);
        Assert.IsNotNull(solidCollider);
        Assert.IsNotNull(pickupTrigger);
        Assert.IsTrue(Physics2D.GetIgnoreCollision(solidCollider, playerCollider));
        Assert.IsFalse(Physics2D.GetIgnoreCollision(pickupTrigger, playerCollider));
    }

    [Test]
    public void InventoryPanel_DragOutsideCurrentMapBoundsKeepsItemInSlot()
    {
        Assert.IsTrue(character.AddItem(5019, 1));
        hud.SetCharacter(character);
        EnsureEventSystem();
        var mapManagerObject = new GameObject("HUD Test MapManager");
        var mapManager = mapManagerObject.AddComponent<MapManager>();
        InvokeUnityMessage(mapManager, "Awake");
        SetPrivateField(mapManager, "hasCurrentDropBounds", true);
        SetPrivateField(mapManager, "currentDropBounds", Rect.MinMaxRect(-1f, -1f, 1f, 1f));
        var cameraObject = new GameObject("HUD Test Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        var source = FindSlotWidget("HUD Inventory Slot 0");
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(100f, 100f)
        };

        hud.BeginInventorySlotDrag(source);
        hud.EndInventorySlotDrag(eventData);

        Assert.AreEqual(1, character.Inventory.GetItemQuantity(5019));
        Assert.AreEqual(0, Object.FindObjectsByType<WorldDrop>(FindObjectsSortMode.None).Length);
    }

    [Test]
    public void WorldDrop_ExpiresAfterConfiguredDespawnSeconds()
    {
        var dropObject = new GameObject("HUD Expiring Drop");
        var drop = dropObject.AddComponent<WorldDrop>();
        var expired = false;
        drop.Initialize(WorldDropPayload.Item(5019, 1));
        drop.SetDespawnSeconds(1f);
        drop.Expired += _ => expired = true;

        drop.TickDespawn(1.1f);

        Assert.IsTrue(expired);
        Assert.IsTrue(drop == null);
    }

    [Test]
    public void InventoryPanel_DragEquipmentOutsideClearsEquipmentIcon()
    {
        EnsureEventSystem();
        var source = FindSlotWidget("HUD Equipment Slot 0");
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(-5000f, -5000f)
        };

        hud.BeginInventorySlotDrag(source);
        hud.EndInventorySlotDrag(eventData);

        Assert.IsTrue(character.Equipment.Slots[0].IsEmpty);
        Assert.IsFalse(TryFindImage("HUD Equipment Slot 0 Icon", out _));
        var drops = Object.FindObjectsByType<WorldDrop>(FindObjectsSortMode.None);
        Assert.AreEqual(1, drops.Length);
        Assert.AreEqual(5001, drops[0].Payload.itemID);
    }

    private Text FindText(string objectName)
    {
        Assert.IsNotNull(hud, "Expected the test HUD to exist.");
        foreach (var text in hud.GetComponentsInChildren<Text>(true))
        {
            if (text.gameObject.name == objectName)
            {
                return text;
            }
        }

        Assert.Fail($"Expected to find HUD text object '{objectName}'.");
        return null;
    }

    private RectTransform FindRect(string objectName)
    {
        Assert.IsNotNull(hud, "Expected the test HUD to exist.");
        foreach (var rect in hud.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.gameObject.name == objectName)
            {
                return rect;
            }
        }

        Assert.Fail($"Expected to find HUD rect object '{objectName}'.");
        return null;
    }

    private Button FindButton(string objectName)
    {
        Assert.IsNotNull(hud, "Expected the test HUD to exist.");
        foreach (var button in hud.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name == objectName)
            {
                return button;
            }
        }

        Assert.Fail($"Expected to find HUD button object '{objectName}'.");
        return null;
    }

    private Image FindImage(string objectName)
    {
        Assert.IsNotNull(hud, "Expected the test HUD to exist.");
        if (TryFindImage(objectName, out var image))
        {
            return image;
        }
 
        Assert.Fail($"Expected to find HUD image object '{objectName}'.");
        return null;
    }

    private bool TryFindImage(string objectName, out Image foundImage)
    {
        foundImage = null;
        if (hud == null)
        {
            return false;
        }

        foreach (var image in hud.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject.name == objectName)
            {
                foundImage = image;
                return true;
            }
        }

        return false;
    }

    private HudInventorySlotWidget FindSlotWidget(string objectName)
    {
        Assert.IsNotNull(hud, "Expected the test HUD to exist.");
        foreach (var widget in hud.GetComponentsInChildren<HudInventorySlotWidget>(true))
        {
            if (widget.gameObject.name == objectName)
            {
                return widget;
            }
        }

        Assert.Fail($"Expected to find HUD inventory slot widget '{objectName}'.");
        return null;
    }

    private void AssertButtonAnchors(string objectName, float minX, float maxX, float minY, float maxY)
    {
        var rect = FindRect(objectName);
        Assert.AreEqual(minX, rect.anchorMin.x, 0.001f);
        Assert.AreEqual(maxX, rect.anchorMax.x, 0.001f);
        Assert.AreEqual(minY, rect.anchorMin.y, 0.001f);
        Assert.AreEqual(maxY, rect.anchorMax.y, 0.001f);
    }

    private void AssertPaddedAnchors(string objectName, float padding)
    {
        var rect = FindRect(objectName);
        Assert.AreEqual(padding, rect.anchorMin.x, 0.001f);
        Assert.AreEqual(padding, rect.anchorMin.y, 0.001f);
        Assert.AreEqual(1f - padding, rect.anchorMax.x, 0.001f);
    }

    private void AssertEquipmentItemSlotAnchors(string objectName)
    {
        var rect = FindRect(objectName);
        Assert.AreEqual(0f, rect.anchorMin.x, 0.001f);
        Assert.AreEqual(0.26f, rect.anchorMin.y, 0.001f);
        Assert.AreEqual(1f, rect.anchorMax.x, 0.001f);
        Assert.AreEqual(1f, rect.anchorMax.y, 0.001f);
    }

    private void AssertResourceText(string objectName, float current, float max)
    {
        Assert.AreEqual(FormatResourceText(current, max), FindText(objectName).text);
    }

    private static string FormatResourceText(float current, float max)
    {
        var safeMax = Mathf.Max(1f, max);
        var safeCurrent = Mathf.Clamp(current, 0f, safeMax);
        var ratio = Mathf.Clamp01(safeCurrent / safeMax);
        return $"{ratio * 100f:0.0}%({safeCurrent:0.0}/{safeMax:0.0})";
    }

    private static void SetPrivateCombatHealthCurrent(PlayerCombatant combatant, string fieldName, float current)
    {
        var health = GetPrivateCombatHealth(combatant, fieldName);
        health.SetCurrent(current);
    }

    private static void SetPrivateCombatHealthMaxAndCurrent(PlayerCombatant combatant, string fieldName, float max, float current)
    {
        var health = GetPrivateCombatHealth(combatant, fieldName);
        health.SetMax(max);
        health.SetCurrent(current);
    }

    private static CombatHealth GetPrivateCombatHealth(PlayerCombatant combatant, string fieldName)
    {
        var field = typeof(PlayerCombatant).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected PlayerCombatant to have private field '{fieldName}'.");
        return (CombatHealth)field.GetValue(combatant);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected {target.GetType().Name} to have private field '{fieldName}'.");
        field.SetValue(target, value);
    }

    private static void InvokeUnityMessage(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Expected {target.GetType().Name} to have Unity message '{methodName}'.");
        method.Invoke(target, null);
    }

    private static Collider2D GetDropCollider(WorldDrop drop, bool isTrigger)
    {
        foreach (var collider in drop.GetComponents<Collider2D>())
        {
            if (collider.isTrigger == isTrigger)
            {
                return collider;
            }
        }

        return null;
    }

    private static void DestroyExistingHud()
    {
        foreach (var existingHud in Object.FindObjectsByType<GameplayHud>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(existingHud.gameObject);
        }
    }

    private static void DestroyWorldDropsAndSpawners()
    {
        foreach (var drop in Object.FindObjectsByType<WorldDrop>(FindObjectsSortMode.None))
        {
            if (drop != null)
            {
                Object.DestroyImmediate(drop.gameObject);
            }
        }

        foreach (var spawner in Object.FindObjectsByType<WorldDropSpawner>(FindObjectsSortMode.None))
        {
            if (spawner != null)
            {
                Object.DestroyImmediate(spawner.gameObject);
            }
        }
    }

    private static void DestroyNamedObject(string objectName)
    {
        foreach (var gameObject in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (gameObject != null && gameObject.name == objectName)
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        new GameObject("HUD Test EventSystem").AddComponent<EventSystem>();
    }
}
