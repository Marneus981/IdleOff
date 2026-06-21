using IdleOff.Combat;
using IdleOff.Drops;
using IdleOff.Player;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Game
{
    public static class FloatingFeedbackService
    {
        private const float DefaultLifetime = 1.5f;
        private const float DefaultFontHeightRatio = 0.25f;
        private const float DamageFontHeightRatio = DefaultFontHeightRatio * 1.5f;
        private const float LevelUpLifetime = DefaultLifetime * 2f;
        private const float LevelUpFontHeightRatio = 0.5f;

        public static void ShowCombatResult(DamageResult result, Vector3 impactPoint)
        {
            var defenderTransform = GetTransform(result.Defender);
            var type = result.Defender != null && result.Defender.IsPlayerControlled
                ? FloatingFeedbackType.DamageTaken
                : FloatingFeedbackType.DamageDealt;

            if (!result.Hit)
            {
                Show(new FloatingFeedbackRequest(
                    "MISS",
                    impactPoint,
                    FloatingFeedbackType.Miss,
                    DefaultLifetime,
                    DefaultFontHeightRatio,
                    defenderTransform));
                return;
            }

            if (result.FinalDamage <= 0f)
            {
                return;
            }

            var text = Mathf.RoundToInt(result.FinalDamage).ToString();
            if (result.CritCount > 0)
            {
                text += "!!";
            }

            Show(new FloatingFeedbackRequest(
                text,
                impactPoint,
                type,
                DefaultLifetime,
                DamageFontHeightRatio,
                defenderTransform));
        }

        public static void ShowXpGain(PlayerCombatant player, int xp)
        {
            if (player == null || xp <= 0)
            {
                return;
            }

            ShowOnFacingSide(player, $"+{xp} XP", FloatingFeedbackType.XpGain);
        }

        public static void ShowDropName(PlayerCombatant player, WorldDropPayload payload)
        {
            if (player == null || payload == null || payload.IsEmpty)
            {
                return;
            }

            ShowOnFacingSide(player, GetDropName(payload), FloatingFeedbackType.DropName);
        }

        public static void ShowLevelUp(PlayerCombatant player)
        {
            if (player == null)
            {
                return;
            }

            var position = GetHeadPosition(player.transform, 0.35f);
            Show(new FloatingFeedbackRequest(
                "LEVEL UP",
                position,
                FloatingFeedbackType.LevelUp,
                LevelUpLifetime,
                LevelUpFontHeightRatio,
                player.transform,
                player.transform,
                position - player.transform.position));
        }

        public static void ShowBaseTalentPoints(PlayerCombatant player, int points)
        {
            if (player == null || points <= 0)
            {
                return;
            }

            ShowOnFacingSide(player, $"+{points} Base Points", FloatingFeedbackType.BaseTalentPoints);
        }

        public static void ShowClassTalentPoints(PlayerCombatant player, int points)
        {
            if (player == null || points <= 0)
            {
                return;
            }

            ShowOnFacingSide(player, $"+{points} Class Points", FloatingFeedbackType.ClassTalentPoints);
        }

        public static void ShowHealing(Transform target, float amount)
        {
            if (target == null || amount <= 0f)
            {
                return;
            }

            Show(new FloatingFeedbackRequest($"+{amount:0.#} HP", target.position, FloatingFeedbackType.Healing, DefaultLifetime, DefaultFontHeightRatio, target));
        }

        public static void ShowManaGain(Transform target, float amount)
        {
            if (target == null || amount <= 0f)
            {
                return;
            }

            Show(new FloatingFeedbackRequest($"+{amount:0.#} MP", target.position, FloatingFeedbackType.ManaGain, DefaultLifetime, DefaultFontHeightRatio, target));
        }

        public static void Show(FloatingFeedbackRequest request)
        {
            FloatingFeedbackController.EnsureExists().Show(request);
        }

        private static void ShowOnFacingSide(PlayerCombatant player, string text, FloatingFeedbackType type)
        {
            var position = GetFacingSidePosition(player);
            Show(new FloatingFeedbackRequest(text, position, type, DefaultLifetime, DefaultFontHeightRatio, player.transform));
        }

        private static Vector3 GetFacingSidePosition(PlayerCombatant player)
        {
            var direction = Vector2.right;
            var movement = player.GetComponent<PlayerMovement2D>();
            if (movement != null && movement.FacingDirection.sqrMagnitude > 0f)
            {
                direction = movement.FacingDirection.normalized;
            }

            var collider = player.GetComponentInChildren<Collider2D>();
            var height = collider != null ? collider.bounds.size.y : 1f;
            var width = collider != null ? collider.bounds.size.x : 0.5f;
            var center = collider != null ? collider.bounds.center : player.transform.position;
            return center + new Vector3(direction.x * (width * 0.75f + 0.25f), height * 0.2f, 0f);
        }

        private static Vector3 GetHeadPosition(Transform target, float extraHeight)
        {
            var collider = target.GetComponentInChildren<Collider2D>();
            if (collider == null)
            {
                return target.position + Vector3.up * (1f + extraHeight);
            }

            return new Vector3(collider.bounds.center.x, collider.bounds.max.y + extraHeight, collider.bounds.center.z);
        }

        private static Transform GetTransform(ICombatant combatant)
        {
            return combatant is MonoBehaviour behaviour ? behaviour.transform : null;
        }

        private static string GetDropName(WorldDropPayload payload)
        {
            if (payload.isMoney)
            {
                return $"Money {payload.money.goldP}g {payload.money.silverP}s {payload.money.copperP}c";
            }

            GlobalItemCatalog.EnsureLoaded();
            if (GlobalItemCatalog.Items.TryGetValue(payload.itemID, out var item) && !string.IsNullOrWhiteSpace(item.name))
            {
                return item.quantity > 1 ? $"{item.name} x{payload.quantity}" : item.name;
            }

            return $"Item {payload.itemID} x{payload.quantity}";
        }
    }
}
