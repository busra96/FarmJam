using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FarmBoxMergeFeedbackController : MonoBehaviour
{
    private enum HapticStrength
    {
        Light,
        Medium,
        Strong
    }

    [Header("Features")]
    [SerializeField] private bool enableSound = true;
    [SerializeField] private bool enableMusic = true;
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private bool enableCameraFeedback = true;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip buttonClip;
    [SerializeField] private AudioClip mergeClip;
    [SerializeField] private AudioClip spawnClip;
    [SerializeField] private AudioClip itemLandClip;
    [SerializeField] private AudioClip trashClip;
    [SerializeField] private AudioClip boxClearClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip confettiClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("Audio Mix")]
    [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 0.72f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.075f;
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.055f;

    [Header("Visual Feel")]
    [SerializeField, Min(0.01f)] private float worldParticleSize = 0.16f;
    [SerializeField, Min(0.01f)] private float uiSparkleSize = 24f;
    [SerializeField] private Color creamSparkle = new Color(1f, 0.92f, 0.58f, 1f);
    [SerializeField] private Color leafSparkle = new Color(0.48f, 0.78f, 0.37f, 1f);

    private static FarmBoxMergeFeedbackController _instance;
    private readonly Dictionary<AudioClip, float> _lastPlayedAt = new Dictionary<AudioClip, float>();
    private readonly Stack<Image> _uiParticlePool = new Stack<Image>();

    private AudioSource _sfxSource;
    private AudioSource _musicSource;
    private ParticleSystem _worldParticles;
    private RectTransform _uiParticleLayer;
    private Canvas _canvas;
    private Sprite _particleSprite;
    private Material _particleMaterial;
    private Coroutine _cameraRoutine;
    private Transform _cameraTarget;
    private Vector3 _cameraBaseLocalPosition;
    private float _lastHapticAt = -10f;

    public static FarmBoxMergeFeedbackController Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        ResolveRuntimeObjects();
        InstallButtonFeedback();
    }

    private void Start()
    {
        StartMusic();
    }

    private void OnDestroy()
    {
        RestoreCameraPosition();

        if (_instance == this)
        {
            _instance = null;
        }

        if (_particleMaterial != null)
        {
            Destroy(_particleMaterial);
        }

        if (_particleSprite != null)
        {
            Texture2D texture = _particleSprite.texture;
            Destroy(_particleSprite);
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }

    public static Color ColorFor(ColorType colorType)
    {
        return colorType switch
        {
            ColorType.Green => new Color(0.43f, 0.78f, 0.45f),
            ColorType.Orange => new Color(1f, 0.57f, 0.25f),
            ColorType.Purple => new Color(0.73f, 0.45f, 0.88f),
            ColorType.Red => new Color(0.93f, 0.30f, 0.31f),
            ColorType.Yellow => new Color(1f, 0.80f, 0.20f),
            _ => Color.white
        };
    }

    public static void PlayButtonClick()
    {
        _instance?.PlaySfx(_instance.buttonClip, 0.42f, 0.035f, 0.035f);
    }

    public static void PlayCardPicked(RectTransform card)
    {
        if (_instance == null)
        {
            return;
        }

        _instance.PlaySfx(_instance.buttonClip, 0.22f, 0.06f, 0.055f);
    }

    public static void PlayCardSpawn(RectTransform card, Color color)
    {
        if (_instance == null || card == null)
        {
            return;
        }

        _instance.SpawnUiBurst(card, color, 5, 42f);
        _instance.PlaySfx(_instance.spawnClip, 0.34f, 0.09f, 0.08f);
    }

    public static void PlayCardMerge(RectTransform card, Color color)
    {
        if (_instance == null || card == null)
        {
            return;
        }

        _instance.SpawnUiBurst(card, color, 11, 76f);
        _instance.PlaySfx(_instance.mergeClip, 0.72f, 0.10f, 0.035f);
        _instance.PlayHaptic(HapticStrength.Light);
    }

    public static void PlayCardDiscard(RectTransform trashTarget)
    {
        if (_instance == null)
        {
            return;
        }

        if (trashTarget != null)
        {
            _instance.SpawnUiBurst(trashTarget, new Color(1f, 0.43f, 0.30f), 7, 54f);
        }

        _instance.PlaySfx(_instance.trashClip, 0.55f, 0.05f, 0.05f);
        _instance.PlayHaptic(HapticStrength.Medium);
    }

    public static void PlayItemSpawn(Transform item, ColorType colorType)
    {
        if (_instance == null || item == null)
        {
            return;
        }

        _instance.EmitWorldBurst(item.position + (Vector3.up * 0.12f), ColorFor(colorType), 4, 0.75f);
        _instance.StartCoroutine(_instance.AnimateWorldPop(item, 0.72f, 1.08f, 0.20f));
        _instance.PlaySfx(_instance.spawnClip, 0.24f, 0.12f, 0.09f);
    }

    public static void PlayItemLanded(Transform item, ColorType colorType)
    {
        if (_instance == null || item == null)
        {
            return;
        }

        _instance.EmitWorldBurst(item.position, ColorFor(colorType), 7, 1.05f);
        _instance.PlaySfx(_instance.itemLandClip, 0.46f, 0.08f, 0.055f);
        _instance.PlayHaptic(HapticStrength.Light);
    }

    public static void PlayBoxCreated(Transform boxGroup, ColorType colorType)
    {
        if (_instance == null || boxGroup == null)
        {
            return;
        }

        _instance.EmitWorldBurst(boxGroup.position + (Vector3.up * 0.16f), ColorFor(colorType), 10, 1.25f);
        _instance.StartCoroutine(_instance.AnimateWorldPop(boxGroup, 0.12f, 1.10f, 0.28f));
        _instance.PlaySfx(_instance.spawnClip, 0.52f, 0.08f, 0.08f);
        _instance.PlayHaptic(HapticStrength.Light);
    }

    public static void PlayBoxCleared(Vector3 position, ColorType colorType, int boxCount)
    {
        if (_instance == null)
        {
            return;
        }

        int particleCount = Mathf.Clamp(10 + (boxCount * 4), 14, 28);
        _instance.EmitWorldBurst(position + (Vector3.up * 0.22f), ColorFor(colorType), particleCount, 1.65f);
        _instance.PlaySfx(_instance.boxClearClip, 0.78f, 0.08f, 0.07f);
        _instance.PlayHaptic(boxCount >= 3 ? HapticStrength.Medium : HapticStrength.Light);
        _instance.PunchCamera(boxCount >= 3 ? 0.055f : 0.032f, 0.16f);
    }

    public static void PlayOutcome(GameObject panel, bool won)
    {
        if (_instance == null)
        {
            return;
        }

        if (panel != null)
        {
            _instance.StartCoroutine(_instance.AnimatePanelEntrance(panel));
        }

        if (won)
        {
            _instance.PlaySfx(_instance.winClip, 0.92f, 0f, 0f);
            _instance.PlaySfx(_instance.confettiClip, 0.52f, 0f, 0f);
            _instance.SpawnConfetti();
            _instance.PlayHaptic(HapticStrength.Strong);
            _instance.PunchCamera(0.045f, 0.22f);
            return;
        }

        _instance.PlaySfx(_instance.failClip, 0.72f, 0f, 0f);
        _instance.PlayHaptic(HapticStrength.Medium);
        _instance.PunchCamera(0.065f, 0.28f);
    }

    private void ResolveRuntimeObjects()
    {
        _canvas = FarmBoxMergeObjectUtility.FindSceneComponent<Canvas>();
        EnsureAudioSources();
        EnsureParticleSprite();
        EnsureWorldParticles();
        EnsureUiParticleLayer();
    }

    private void EnsureAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        _sfxSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        _musicSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        _sfxSource.playOnAwake = false;
        _sfxSource.loop = false;
        _sfxSource.spatialBlend = 0f;

        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        _musicSource.spatialBlend = 0f;
        _musicSource.volume = musicVolume;
    }

    private void StartMusic()
    {
        if (!enableSound || !enableMusic || gameplayMusic == null || _musicSource == null)
        {
            return;
        }

        _musicSource.clip = gameplayMusic;
        _musicSource.volume = musicVolume;
        _musicSource.Play();
    }

    private void PlaySfx(AudioClip clip, float relativeVolume, float pitchRange, float cooldown)
    {
        if (!enableSound || clip == null || _sfxSource == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (_lastPlayedAt.TryGetValue(clip, out float lastPlayed) && now - lastPlayed < cooldown)
        {
            return;
        }

        _lastPlayedAt[clip] = now;
        float effectivePitchRange = Mathf.Min(Mathf.Abs(pitchRange), pitchVariation);
        _sfxSource.pitch = 1f + Random.Range(-effectivePitchRange, effectivePitchRange);
        _sfxSource.PlayOneShot(clip, masterSfxVolume * relativeVolume);
    }

    private void EnsureParticleSprite()
    {
        if (_particleSprite != null)
        {
            return;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "FarmBoxMergeSoftParticle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * (3f - (2f * alpha));
                pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        _particleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
        _particleSprite.name = "FarmBoxMergeSoftParticleSprite";
        _particleSprite.hideFlags = HideFlags.HideAndDontSave;
    }

    private void EnsureWorldParticles()
    {
        if (_worldParticles != null)
        {
            return;
        }

        GameObject particleObject = new GameObject("WorldFeedbackParticles", typeof(ParticleSystem));
        particleObject.transform.SetParent(transform, false);
        _worldParticles = particleObject.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = _worldParticles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.68f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(worldParticleSize * 0.65f, worldParticleSize * 1.35f);
        main.maxParticles = 256;
        main.gravityModifier = 0.34f;

        ParticleSystem.EmissionModule emission = _worldParticles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = _worldParticles.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _worldParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient alphaGradient = new Gradient();
        alphaGradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = alphaGradient;

        ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingOrder = 25;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        shader ??= Shader.Find("Particles/Standard Unlit");
        if (shader != null)
        {
            _particleMaterial = new Material(shader)
            {
                name = "FarmBoxMergeRuntimeParticleMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };

            if (_particleMaterial.HasProperty("_BaseMap"))
            {
                _particleMaterial.SetTexture("_BaseMap", _particleSprite.texture);
            }
            else if (_particleMaterial.HasProperty("_MainTex"))
            {
                _particleMaterial.SetTexture("_MainTex", _particleSprite.texture);
            }

            particleRenderer.material = _particleMaterial;
        }
    }

    private void EmitWorldBurst(Vector3 position, Color color, int count, float speed)
    {
        if (!enableParticles || _worldParticles == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized;
            Vector3 direction = new Vector3(circle.x, Random.Range(0.45f, 1f), circle.y).normalized;
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = direction * Random.Range(speed * 0.62f, speed * 1.18f),
                startColor = Color.Lerp(color, creamSparkle, Random.Range(0f, 0.45f)),
                startSize = worldParticleSize * Random.Range(0.72f, 1.35f),
                startLifetime = Random.Range(0.38f, 0.68f)
            };
            _worldParticles.Emit(emitParams, 1);
        }
    }

    private void EnsureUiParticleLayer()
    {
        if (_uiParticleLayer != null || _canvas == null)
        {
            return;
        }

        Transform existing = _canvas.transform.Find("GameFeelParticleLayer");
        if (existing != null)
        {
            _uiParticleLayer = existing as RectTransform;
        }

        if (_uiParticleLayer == null)
        {
            GameObject layerObject = new GameObject("GameFeelParticleLayer", typeof(RectTransform), typeof(CanvasGroup));
            _uiParticleLayer = layerObject.GetComponent<RectTransform>();
            _uiParticleLayer.SetParent(_canvas.transform, false);
        }

        _uiParticleLayer.anchorMin = Vector2.zero;
        _uiParticleLayer.anchorMax = Vector2.one;
        _uiParticleLayer.pivot = Vector2.one * 0.5f;
        _uiParticleLayer.anchoredPosition = Vector2.zero;
        _uiParticleLayer.sizeDelta = Vector2.zero;
        _uiParticleLayer.SetAsLastSibling();

        CanvasGroup canvasGroup = _uiParticleLayer.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void SpawnUiBurst(RectTransform target, Color color, int count, float radius)
    {
        if (!enableParticles || target == null)
        {
            return;
        }

        EnsureUiParticleLayer();
        if (_uiParticleLayer == null)
        {
            return;
        }

        Camera eventCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, target.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiParticleLayer, screenPosition, eventCamera, out Vector2 localPosition))
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Image particle = GetUiParticle();
            Vector2 direction = Random.insideUnitCircle.normalized;
            Color particleColor = Color.Lerp(color, i % 3 == 0 ? creamSparkle : leafSparkle, Random.Range(0.08f, 0.38f));
            StartCoroutine(AnimateUiParticle(particle, localPosition, direction * Random.Range(radius * 0.55f, radius), particleColor));
        }
    }

    private Image GetUiParticle()
    {
        Image image;
        if (_uiParticlePool.Count > 0)
        {
            image = _uiParticlePool.Pop();
            image.gameObject.SetActive(true);
        }
        else
        {
            GameObject particleObject = new GameObject("UiSparkle", typeof(RectTransform), typeof(Image));
            image = particleObject.GetComponent<Image>();
            image.sprite = _particleSprite;
            image.raycastTarget = false;
        }

        RectTransform rect = image.rectTransform;
        rect.SetParent(_uiParticleLayer, false);
        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;
        rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = Vector2.one * uiSparkleSize * Random.Range(0.72f, 1.22f);
        rect.SetAsLastSibling();
        return image;
    }

    private IEnumerator AnimateUiParticle(Image image, Vector2 start, Vector2 offset, Color color)
    {
        RectTransform rect = image.rectTransform;
        rect.anchoredPosition = start;
        rect.localScale = Vector3.one * 0.35f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 180f));
        image.color = color;

        float duration = Random.Range(0.38f, 0.56f);
        float rotationSpeed = Random.Range(-260f, 260f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = FarmBoxMergeMath.EaseOutCubic(progress);
            rect.anchoredPosition = start + (offset * eased) + (Vector2.down * (22f * progress * progress));
            rect.localScale = Vector3.one * Mathf.Sin(progress * Mathf.PI);
            rect.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime);
            Color animatedColor = color;
            animatedColor.a = 1f - FarmBoxMergeMath.SmoothStep(progress);
            image.color = animatedColor;
            yield return null;
        }

        image.gameObject.SetActive(false);
        _uiParticlePool.Push(image);
    }

    private void SpawnConfetti()
    {
        if (!enableParticles)
        {
            return;
        }

        EnsureUiParticleLayer();
        if (_uiParticleLayer == null)
        {
            return;
        }

        Color[] palette =
        {
            new Color(0.98f, 0.40f, 0.33f),
            new Color(1f, 0.78f, 0.22f),
            new Color(0.43f, 0.78f, 0.45f),
            new Color(0.40f, 0.70f, 0.93f),
            new Color(0.72f, 0.46f, 0.88f)
        };

        float halfWidth = _uiParticleLayer.rect.width * 0.48f;
        float top = _uiParticleLayer.rect.height * 0.46f;
        for (int i = 0; i < 34; i++)
        {
            Image particle = GetUiParticle();
            particle.rectTransform.sizeDelta = new Vector2(uiSparkleSize * 0.45f, uiSparkleSize * 1.15f);
            Vector2 start = new Vector2(Random.Range(-halfWidth, halfWidth), top + Random.Range(-20f, 90f));
            Vector2 drift = new Vector2(Random.Range(-90f, 90f), -Random.Range(280f, 520f));
            StartCoroutine(AnimateConfettiParticle(particle, start, drift, palette[i % palette.Length], Random.Range(0f, 0.42f)));
        }
    }

    private IEnumerator AnimateConfettiParticle(Image image, Vector2 start, Vector2 drift, Color color, float delay)
    {
        image.color = color;
        image.rectTransform.anchoredPosition = start;
        image.gameObject.SetActive(false);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        image.gameObject.SetActive(true);
        float duration = Random.Range(0.85f, 1.25f);
        float rotationSpeed = Random.Range(-520f, 520f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            image.rectTransform.anchoredPosition = start + (drift * progress) + (Vector2.right * Mathf.Sin(progress * 12f) * 22f);
            image.rectTransform.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime);
            Color animatedColor = color;
            animatedColor.a = 1f - Mathf.Clamp01((progress - 0.72f) / 0.28f);
            image.color = animatedColor;
            yield return null;
        }

        image.gameObject.SetActive(false);
        _uiParticlePool.Push(image);
    }

    private IEnumerator AnimateWorldPop(Transform target, float startScaleMultiplier, float overshootMultiplier, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 baseScale = target.localScale;
        target.localScale = baseScale * startScaleMultiplier;
        float elapsed = 0f;
        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float multiplier;
            if (progress < 0.58f)
            {
                multiplier = Mathf.LerpUnclamped(startScaleMultiplier, overshootMultiplier, FarmBoxMergeMath.EaseOutCubic(progress / 0.58f));
            }
            else
            {
                multiplier = Mathf.LerpUnclamped(overshootMultiplier, 1f, FarmBoxMergeMath.SmoothStep((progress - 0.58f) / 0.42f));
            }

            target.localScale = baseScale * multiplier;
            yield return null;
        }

        if (target != null)
        {
            target.localScale = baseScale;
        }
    }

    private IEnumerator AnimatePanelEntrance(GameObject panel)
    {
        RectTransform rect = panel != null ? panel.transform as RectTransform : null;
        if (rect == null)
        {
            yield break;
        }

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        Vector3 baseScale = rect.localScale == Vector3.zero ? Vector3.one : rect.localScale;
        rect.localScale = baseScale * 0.72f;
        canvasGroup.alpha = 0f;

        const float duration = 0.34f;
        float elapsed = 0f;
        while (elapsed < duration && panel != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float overshoot = progress < 0.72f
                ? Mathf.LerpUnclamped(0.72f, 1.06f, FarmBoxMergeMath.EaseOutCubic(progress / 0.72f))
                : Mathf.LerpUnclamped(1.06f, 1f, FarmBoxMergeMath.SmoothStep((progress - 0.72f) / 0.28f));
            rect.localScale = baseScale * overshoot;
            canvasGroup.alpha = FarmBoxMergeMath.SmoothStep(Mathf.Clamp01(progress * 1.6f));
            yield return null;
        }

        if (rect != null)
        {
            rect.localScale = baseScale;
            canvasGroup.alpha = 1f;
        }
    }

    private void InstallButtonFeedback()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && !buttons[i].TryGetComponent(out FarmBoxMergeButtonFeedback _))
            {
                buttons[i].gameObject.AddComponent<FarmBoxMergeButtonFeedback>();
            }
        }
    }

    private void PunchCamera(float strength, float duration)
    {
        if (!enableCameraFeedback || Camera.main == null)
        {
            return;
        }

        if (_cameraRoutine != null)
        {
            StopCoroutine(_cameraRoutine);
            RestoreCameraPosition();
        }

        _cameraTarget = Camera.main.transform;
        _cameraBaseLocalPosition = _cameraTarget.localPosition;
        _cameraRoutine = StartCoroutine(CameraPunchRoutine(strength, duration));
    }

    private IEnumerator CameraPunchRoutine(float strength, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && _cameraTarget != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float fade = 1f - FarmBoxMergeMath.SmoothStep(progress);
            Vector2 offset = Random.insideUnitCircle * strength * fade;
            _cameraTarget.localPosition = _cameraBaseLocalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        RestoreCameraPosition();
        _cameraRoutine = null;
    }

    private void RestoreCameraPosition()
    {
        if (_cameraTarget != null)
        {
            _cameraTarget.localPosition = _cameraBaseLocalPosition;
        }

        _cameraTarget = null;
    }

    private void PlayHaptic(HapticStrength strength)
    {
        if (!enableHaptics || Time.unscaledTime - _lastHapticAt < 0.11f)
        {
            return;
        }

        _lastHapticAt = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR
        int duration = strength == HapticStrength.Light ? 18 : strength == HapticStrength.Medium ? 32 : 48;
        int amplitude = strength == HapticStrength.Light ? 45 : strength == HapticStrength.Medium ? 90 : 145;
        try
        {
            using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            using AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION");
            int sdk = version.GetStatic<int>("SDK_INT");
            if (sdk >= 26)
            {
                using AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)duration, amplitude);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", (long)duration);
            }
        }
        catch
        {
            Handheld.Vibrate();
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
