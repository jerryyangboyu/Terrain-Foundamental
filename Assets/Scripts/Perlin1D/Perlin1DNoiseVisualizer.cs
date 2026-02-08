using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class Perlin1DNoiseVisualizer : MonoBehaviour
{
    [Header("Noise")]
    [SerializeField, Min(2)] private int sampleCount = 256;
    [SerializeField, Min(0.01f)] private float domainLength = 50f;
    [SerializeField, Min(0.0001f)] private float frequency = 1f;
    [SerializeField, Min(1)] private int octaves = 1;
    [SerializeField, Range(0f, 1f)] private float persistence = 0.5f;
    [SerializeField, Min(1f)] private float lacunarity = 2f;
    [SerializeField] private int seed = 0;
    [SerializeField] private float xOffset = 0f;

    [Header("Display")]
    [SerializeField] private float amplitude = 10f;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float zOffset = 0f;
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] private Color lineColor = new Color(0.15f, 0.95f, 0.3f, 1f);
    [SerializeField] private bool autoRefresh = true;

    private LineRenderer lineRenderer;

    private void Reset()
    {
        EnsureLineRenderer();
        ConfigureLineRenderer();
        Rebuild();
    }

    private void OnEnable()
    {
        EnsureLineRenderer();
        ConfigureLineRenderer();
        Rebuild();
    }

    private void OnValidate()
    {
        if (!autoRefresh)
        {
            return;
        }

        EnsureLineRenderer();
        ConfigureLineRenderer();
        Rebuild();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        EnsureLineRenderer();

        int safeSampleCount = Mathf.Max(2, sampleCount);
        float safeDomainLength = Mathf.Max(0.01f, domainLength);

        lineRenderer.positionCount = safeSampleCount;

        for (int i = 0; i < safeSampleCount; i++)
        {
            float t = i / (float)(safeSampleCount - 1);
            float x = t * safeDomainLength;
            float noise = SampleNoise1D(x + xOffset);
            float y = ((noise * 2f) - 1f) * amplitude + yOffset;
            lineRenderer.SetPosition(i, new Vector3(x, y, zOffset));
        }
    }

    private float SampleNoise1D(float x)
    {
        float total = 0f;
        float totalAmplitude = 0f;

        float currentFrequency = Mathf.Max(0.0001f, frequency);
        float currentAmplitude = 1f;
        float seedOffset = seed * 0.12345f;

        int safeOctaves = Mathf.Max(1, octaves);

        for (int octave = 0; octave < safeOctaves; octave++)
        {
            float sampleX = (x + seedOffset) * currentFrequency;
            float sampleY = seedOffset + octave * 11.731f;

            float value = Mathf.PerlinNoise(sampleX, sampleY);
            total += value * currentAmplitude;
            totalAmplitude += currentAmplitude;

            currentAmplitude *= persistence;
            currentFrequency *= lacunarity;
        }

        if (totalAmplitude <= 0f)
        {
            return 0.5f;
        }

        return total / totalAmplitude;
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.widthMultiplier = Mathf.Max(0.001f, lineWidth);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(lineColor, 0f),
                new GradientColorKey(lineColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(lineColor.a, 0f),
                new GradientAlphaKey(lineColor.a, 1f)
            }
        );
        lineRenderer.colorGradient = gradient;
    }
}
