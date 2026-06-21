using System.Collections;
using System.Reflection;
using IdleOff.Combat;
using IdleOff.Game;
using IdleOff.Profiles;
using NUnit.Framework;
using UnityEngine;
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

    private void AssertButtonAnchors(string objectName, float minX, float maxX, float minY, float maxY)
    {
        var rect = FindRect(objectName);
        Assert.AreEqual(minX, rect.anchorMin.x, 0.001f);
        Assert.AreEqual(maxX, rect.anchorMax.x, 0.001f);
        Assert.AreEqual(minY, rect.anchorMin.y, 0.001f);
        Assert.AreEqual(maxY, rect.anchorMax.y, 0.001f);
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

    private static void DestroyExistingHud()
    {
        foreach (var existingHud in Object.FindObjectsByType<GameplayHud>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(existingHud.gameObject);
        }
    }
}
