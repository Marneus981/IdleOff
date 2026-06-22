using UnityEngine;

namespace IdleOff.Visuals
{
    [DisallowMultipleComponent]
    public sealed class EntityVisualController : MonoBehaviour
    {
        private const string RendererChildName = "Entity Visual Renderer";

        [SerializeField] private string visualID;
        [SerializeField] private string fallbackSpritePath = VisualAssetResolver.PlayerPlaceholderPath;
        [SerializeField] private bool playDefaultAnimationOnApply = true;

        private SpriteRenderer spriteRenderer;
        private VisualDefinition currentVisual;
        private VisualAnimationDefinition currentAnimation;
        private Sprite[] currentFrames = System.Array.Empty<Sprite>();
        private string currentAnimationName;
        private float frameTimer;
        private int frameIndex;

        public string VisualID => visualID;
        public string CurrentAnimationName => currentAnimationName;
        public SpriteRenderer Renderer
        {
            get
            {
                EnsureRenderer();
                return spriteRenderer;
            }
        }

        private void Awake()
        {
            EnsureRenderer();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public bool ApplyVisual(string nextVisualID, string fallbackPath = null)
        {
            if (!string.IsNullOrWhiteSpace(fallbackPath))
            {
                fallbackSpritePath = fallbackPath;
            }

            if (string.IsNullOrWhiteSpace(nextVisualID))
            {
                return ApplyFallback();
            }

            if (visualID == nextVisualID && currentVisual != null)
            {
                return true;
            }

            visualID = nextVisualID;
            if (!VisualCatalog.TryGet(nextVisualID, out currentVisual))
            {
                return ApplyFallback();
            }

            ApplyStaticProperties(currentVisual);
            if (playDefaultAnimationOnApply && !string.IsNullOrWhiteSpace(currentVisual.defaultAnimation))
            {
                Play(currentVisual.defaultAnimation);
            }
            else
            {
                SetSprite(currentVisual.sprite != null
                    ? currentVisual.sprite
                    : VisualAssetResolver.GetSprite(currentVisual.spritePath, fallbackSpritePath));
            }

            return true;
        }

        public bool Play(string animationName)
        {
            if (currentVisual == null || currentVisual.animations == null || string.IsNullOrWhiteSpace(animationName))
            {
                return false;
            }

            if (!currentVisual.animations.TryGetValue(animationName, out currentAnimation) || currentAnimation == null)
            {
                return false;
            }

            currentFrames = VisualAssetResolver.GetAnimationFrames(currentAnimation, currentVisual.spritePath);
            if (currentFrames == null || currentFrames.Length == 0)
            {
                if (currentVisual.sprite == null)
                {
                    return false;
                }

                currentFrames = new[] { currentVisual.sprite };
            }

            currentAnimationName = animationName;
            frameIndex = 0;
            frameTimer = 0f;
            SetSprite(currentFrames[0]);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (currentAnimation == null || currentFrames == null || currentFrames.Length <= 1)
            {
                return;
            }

            var secondsPerFrame = 1f / Mathf.Max(0.01f, currentAnimation.fps);
            frameTimer += Mathf.Max(0f, deltaTime);
            while (frameTimer >= secondsPerFrame)
            {
                frameTimer -= secondsPerFrame;
                frameIndex++;
                if (frameIndex >= currentFrames.Length)
                {
                    if (!currentAnimation.loop)
                    {
                        frameIndex = currentFrames.Length - 1;
                        currentAnimation = null;
                        break;
                    }

                    frameIndex = 0;
                }

                SetSprite(currentFrames[frameIndex]);
            }
        }

        private void ApplyStaticProperties(VisualDefinition definition)
        {
            EnsureRenderer();
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingLayerName = string.IsNullOrWhiteSpace(definition.sortingLayer)
                ? spriteRenderer.sortingLayerName
                : definition.sortingLayer;
            spriteRenderer.sortingOrder = definition.sortingOrder;

            var scale = definition.scale == Vector2.zero ? Vector2.one : definition.scale;
            transform.localScale = Vector3.one;
            spriteRenderer.transform.localPosition = new Vector3(definition.offset.x, definition.offset.y, 0f);
            spriteRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        private bool ApplyFallback()
        {
            currentVisual = null;
            currentAnimation = null;
            currentFrames = System.Array.Empty<Sprite>();
            EnsureRenderer();
            spriteRenderer.color = Color.white;
            spriteRenderer.transform.localPosition = Vector3.zero;
            spriteRenderer.transform.localScale = Vector3.one;
            SetSprite(VisualAssetResolver.GetSprite(null, fallbackSpritePath));
            return false;
        }

        private void SetSprite(Sprite sprite)
        {
            EnsureRenderer();
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        public bool TryGetRenderedLocalBounds(out Bounds bounds)
        {
            EnsureRenderer();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                bounds = default;
                return false;
            }

            var spriteBounds = spriteRenderer.sprite.bounds;
            var min = spriteBounds.min;
            var max = spriteBounds.max;
            var corners = new[]
            {
                new Vector3(min.x, min.y, 0f),
                new Vector3(min.x, max.y, 0f),
                new Vector3(max.x, min.y, 0f),
                new Vector3(max.x, max.y, 0f)
            };

            bounds = new Bounds(transform.InverseTransformPoint(spriteRenderer.transform.TransformPoint(corners[0])), Vector3.zero);
            for (var i = 1; i < corners.Length; i++)
            {
                bounds.Encapsulate(transform.InverseTransformPoint(spriteRenderer.transform.TransformPoint(corners[i])));
            }

            return true;
        }

        public bool AlignRenderedBoundsToCenter()
        {
            if (!TryGetRenderedLocalBounds(out var bounds))
            {
                return false;
            }

            spriteRenderer.transform.localPosition -= new Vector3(bounds.center.x, bounds.center.y, 0f);
            return true;
        }

        private void EnsureRenderer()
        {
            if (spriteRenderer != null)
            {
                return;
            }

            var rendererTransform = transform.Find(RendererChildName);
            if (rendererTransform != null && rendererTransform.TryGetComponent(out spriteRenderer))
            {
                return;
            }

            var inheritedRenderer = GetComponent<SpriteRenderer>();
            var rendererObject = new GameObject(RendererChildName);
            rendererObject.transform.SetParent(transform, false);
            spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();

            if (inheritedRenderer != null)
            {
                spriteRenderer.sprite = inheritedRenderer.sprite;
                spriteRenderer.color = inheritedRenderer.color;
                spriteRenderer.sortingLayerID = inheritedRenderer.sortingLayerID;
                spriteRenderer.sortingOrder = inheritedRenderer.sortingOrder;
                inheritedRenderer.enabled = false;
            }
        }
    }
}
