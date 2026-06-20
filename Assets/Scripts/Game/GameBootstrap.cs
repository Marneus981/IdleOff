using UnityEngine;

namespace IdleOff.Game
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBootstrapBeforeScene()
        {
            if (FindFirstObjectByType<GameBootstrap>() == null)
            {
                var bootstrap = new GameObject("Game Bootstrap");
                DontDestroyOnLoad(bootstrap);
                bootstrap.AddComponent<GameBootstrap>();
            }
        }

        private void Awake()
        {
            if (GameStateManager.Instance == null)
            {
                new GameObject("Game State Manager").AddComponent<GameStateManager>();
            }

            if (FindFirstObjectByType<BootFlowUI>() == null)
            {
                new GameObject("Boot Flow UI").AddComponent<BootFlowUI>();
            }
        }
    }
}
