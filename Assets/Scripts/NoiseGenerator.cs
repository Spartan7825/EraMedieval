using UnityEngine;
using Random = System.Random;

namespace QuantumCookie
{
    public class NoiseGenerator
    {
        private int seed;
        private float width, height;
        private float scale, frequency;

        private Random rng;
        
        public NoiseGenerator(int _seed, float _width, float _height, float _scale, float _frequency)
        {
            seed = _seed;
            width = _width;
            height = _height;
            scale = _scale;
            frequency = _frequency;

            rng = new Random(seed);
        }

        public float SampleNoise(float x, float y)
        {
            return Mathf.PerlinNoise(scale * x / frequency, scale * y / frequency);
        }
    }
}