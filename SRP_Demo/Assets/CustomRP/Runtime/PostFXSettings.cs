using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [SerializeField]
    Shader shader = default;

    [System.NonSerialized]
    Material material;

    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)]
        public int maxIterations;
        [Min(1f)]
        public int downscaleLimit;
        public bool bicubicUpsampling;
        [Min(0f)]
        public float threshold;
        [Range(0f, 1f)]
        public float thresholdKnee;
        [Min(0f)]
        public float intensity;
        public bool Fade_FireFlies;
        public enum Mode { Additive, Scattering };
        public Mode mode;
        [Range(0.05f, 0.95f)]
        public float scatter;
    };

    [System.Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode { 
            None = -1,
            ACES,
            Netural,
            Reinhard
        };
        public Mode mode;
    }

    [SerializeField]
    BloomSettings bloom = new BloomSettings {
        scatter = 0.7f
    };
    [SerializeField]
    ToneMappingSettings toneMapping = default;

    

    public BloomSettings Bloom => bloom;
    public ToneMappingSettings ToneMapping => toneMapping;


    public Material Material
    {
        get {
            if (material == null && shader != null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }
}
