using IdleOff.Visuals;
using NUnit.Framework;
using UnityEngine;

public sealed class VisualSystemTests
{
    [SetUp]
    public void SetUp()
    {
        VisualCatalog.LoadVisuals();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var controller in Object.FindObjectsByType<EntityVisualController>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(controller.gameObject);
        }
    }

    [Test]
    public void VisualCatalog_LoadsPlaceholderAndWildHunterDefinitions()
    {
        Assert.IsTrue(VisualCatalog.TryGet("mob_training_basic", out var mobVisual));
        Assert.AreEqual(VisualAssetResolver.MobPlaceholderPath, mobVisual.spritePath);

        Assert.IsTrue(VisualCatalog.TryGet("player_wildhunter", out var wildHunterVisual));
        Assert.IsTrue(wildHunterVisual.animations.ContainsKey("idle"));
        Assert.AreEqual("Assets/Art/Sprites/Character/WildHunter/Idle.png", wildHunterVisual.animations["idle"].spriteSheetPath);
    }

    [Test]
    public void VisualAssetResolver_LoadsFramesFromWildHunterSpriteSheetPath()
    {
        Assert.IsTrue(VisualCatalog.TryGet("player_wildhunter", out var wildHunterVisual));

        var frames = VisualAssetResolver.GetAnimationFrames(
            wildHunterVisual.animations["idle"],
            wildHunterVisual.spritePath);

        Assert.IsNotNull(frames);
        Assert.GreaterOrEqual(frames.Length, 1);
        Assert.IsNotNull(frames[0]);
    }

    [Test]
    public void EntityVisualController_AppliesVisualAndCanPlayAnimation()
    {
        var visualObject = new GameObject("Visual Test Entity");
        var controller = visualObject.AddComponent<EntityVisualController>();

        Assert.IsTrue(controller.ApplyVisual("player_wildhunter", VisualAssetResolver.PlayerPlaceholderPath));
        Assert.AreEqual("player_wildhunter", controller.VisualID);
        Assert.IsTrue(controller.Play("idle"));
        Assert.AreEqual("idle", controller.CurrentAnimationName);
        var renderer = visualObject.GetComponentInChildren<SpriteRenderer>();
        Assert.IsNotNull(renderer);
        Assert.IsNotNull(renderer.sprite);
        Assert.AreEqual(Vector3.one, visualObject.transform.localScale);
        Assert.AreNotEqual(Vector3.one, renderer.transform.localScale);
        Assert.IsTrue(controller.AlignRenderedBoundsToCenter());
        Assert.IsTrue(controller.TryGetRenderedLocalBounds(out var bounds));
        Assert.AreEqual(0f, bounds.center.x, 0.001f);
        Assert.AreEqual(0f, bounds.center.y, 0.001f);
        Assert.Greater(bounds.size.y, 0f);
    }

    [Test]
    public void EntityVisualController_OffsetMovesRendererWithoutMovingRoot()
    {
        var visualObject = new GameObject("Visual Offset Test Entity");
        var controller = visualObject.AddComponent<EntityVisualController>();
        var originalPosition = visualObject.transform.position;

        Assert.IsTrue(controller.ApplyVisual("player_wildhunter", VisualAssetResolver.PlayerPlaceholderPath));
        var renderer = visualObject.GetComponentInChildren<SpriteRenderer>();
        Assert.IsNotNull(renderer);

        renderer.transform.localPosition = new Vector3(0.25f, 0.5f, 0f);

        Assert.AreEqual(originalPosition, visualObject.transform.position);
        Assert.AreEqual(0.25f, renderer.transform.localPosition.x);
        Assert.AreEqual(0.5f, renderer.transform.localPosition.y);
        Assert.IsTrue(controller.TryGetRenderedLocalBounds(out var bounds));
        Assert.AreNotEqual(0f, bounds.center.x);
        Assert.AreNotEqual(0f, bounds.center.y);

        Assert.IsTrue(controller.AlignRenderedBoundsToCenter());
        Assert.AreEqual(originalPosition, visualObject.transform.position);
        Assert.IsTrue(controller.TryGetRenderedLocalBounds(out bounds));
        Assert.AreEqual(0f, bounds.center.x, 0.001f);
        Assert.AreEqual(0f, bounds.center.y, 0.001f);
    }
}
