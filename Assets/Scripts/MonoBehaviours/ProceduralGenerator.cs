using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MonoBehaviours
{
    // [CustomPropertyDrawer(typeof(ProceduralGenerator.Octave))]
    // public class OctaveSliderDrawer : PropertyDrawer
    // {
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //     {
    //         var frequencyProp = property.FindPropertyRelative("frequency");
    //         var amplitudeProp = property.FindPropertyRelative("amplitude");
    //         
    //         EditorGUI.BeginChangeCheck();
    //
    //         var scale = 1.0f;
    //         var frequencyScale = EditorGUI.Slider(new Rect(position.x, position.y, 150, 20), scale, 1, 100);
    //         var amplitudeScale = EditorGUI.Slider(new Rect(position.x, position.y + 30, 150, 20), scale, 1, 100);
    //
    //         if (EditorGUI.EndChangeCheck())
    //         {
    //             frequencyProp.floatValue = frequencyScale;
    //             amplitudeProp.floatValue = amplitudeScale;
    //         }
    //     }
    // }
    
    public class ProceduralGenerator : MonoBehaviour
    {
        [Serializable]
        public struct NoiseConfig
        {
            public int octaves;
            public float persistence;
            public float frequency;
            public float amplitude;
            public float frequencyMultiplier;
            public List<BlockThresholdStruct> BlockThresholds;

            public NoiseConfig(NoiseConfig other)
            {
                octaves = other.octaves;
                persistence = other.persistence;
                frequency = other.frequency;
                amplitude = other.amplitude;
                frequencyMultiplier = other.frequencyMultiplier;
                BlockThresholds = other.BlockThresholds;
            }
            
            public bool Equals(NoiseConfig other)
            {
                if (Math.Abs(persistence - other.persistence) > 0.01f
                    || octaves != other.octaves
                    || Math.Abs(frequency - other.frequency) > 0.01f
                    || Math.Abs(amplitude - other.amplitude) > 0.01f
                    || Math.Abs(frequencyMultiplier - other.frequencyMultiplier) > 0.01f)
                {
                    return false;
                }

                return true;
            }
        }

        public NoiseConfig noiseConfig;
        public static NoiseConfig StaticNoiseConfig;

        public static event EventHandler UpdateEvent;

        private void Awake()
        {
            StaticNoiseConfig = new NoiseConfig(noiseConfig);
        }

        private void Update()
        {
            if (StaticNoiseConfig.Equals(noiseConfig))
                return;
            StaticNoiseConfig = new NoiseConfig(noiseConfig);
            
            UpdateEvent?.Invoke(this, null);
        }
        
        public static float OctavePerlin(float x, float y) {
            var total = 0.0f;
            var frequency = StaticNoiseConfig.frequency;
            var amplitude = StaticNoiseConfig.amplitude;
            int octaves = StaticNoiseConfig.octaves;
            var maxValue = 0.0f;
            for(var i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
        
                maxValue += amplitude;
        
                amplitude *= StaticNoiseConfig.persistence;
                frequency *= StaticNoiseConfig.frequencyMultiplier;
            }
    
            return total / maxValue;
        }

        //public static float OctaveNoise(float x, float y)
        //{
        //    var octaveFrequencies = StaticNoiseConfig.octaves.Select(o => o.frequency).ToArray();
        //    var octaveAmplitudes = StaticNoiseConfig.octaves.Select(o => o.amplitude).ToArray();
        //    float noise = 0;
        //    for (var i = 0; i < octaveFrequencies.Length; ++i)
        //    {
        //        // noise = Mathf.PerlinNoise(x, y);
        //        // noise = amplitude * Mathf.PerlinNoise(freq * x, freq * y)
        //        noise += octaveAmplitudes[i] * Mathf.PerlinNoise(
        //            octaveFrequencies[i] * x, 
        //            octaveFrequencies[i] * y);
        //    }
        //    return noise;
        //}
    }

    [Serializable]
    public struct BlockThresholdStruct
    {
        public Constants.Blocks BlockTypeName;
        public float BlockThreshold;
    }
}