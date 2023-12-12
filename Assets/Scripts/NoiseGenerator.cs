using UnityEngine;
using Random = System.Random;

namespace QuantumCookie
{
    public class NoiseGenerator
    {
        private readonly float _scale;
        private readonly float _frequency;

        private readonly float _offsetX;
        private readonly float _offsetY;

        public NoiseGenerator(int seed, float scale, float frequency)
        {
            _scale = scale;
            _frequency = frequency;

            var rng = new Random(seed);
            _offsetX = (float)rng.NextDouble() * 1000.0f;
            _offsetY = (float)rng.NextDouble() * 1000.0f;
        }

        public float SamplePerlinNoise(float x, float y)
        {
            return _scale * Mathf.PerlinNoise(_frequency * (x + _offsetX), _frequency * (y + _offsetY));
        }
    }
}