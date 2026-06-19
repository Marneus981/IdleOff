using UnityEngine;

namespace IdleOff.Actions
{
    public sealed class ActionRuntimeState
    {
        public ActionRuntimeState(Action action)
        {
            Action = action;
        }

        public Action Action { get; }
        public float CooldownRemaining { get; private set; }
        public bool IsReady => CooldownRemaining <= 0f;

        public void Tick(float deltaTime)
        {
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - deltaTime);
        }

        public void StartCooldown()
        {
            CooldownRemaining = Mathf.Max(0f, Action?.cooldown ?? 0f);
        }
    }
}
