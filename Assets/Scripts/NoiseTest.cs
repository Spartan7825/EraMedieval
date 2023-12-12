using System;
using System.Collections;
using System.Collections.Generic;
using QuantumCookie;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class NoiseTest : MonoBehaviour
{
    private NoiseGenerator _noiseGenerator;

    [SerializeField]
    private int seed;
    
    [SerializeField]
    private float width;

    [SerializeField]
    private float height;

    [SerializeField][Range(0, 100f)]
    private float scale;
    
    [SerializeField][Range(0.001f, 0.02f)]
    private float frequency;

    [SerializeField] private int debugResolution = 10;
    [SerializeField] private float debugSphereSize = 20f;
    private void OnEnable()
    {
        _noiseGenerator = new NoiseGenerator(seed, scale, frequency);
    }

    private void OnValidate()
    {
        _noiseGenerator = new NoiseGenerator(seed, scale, frequency);
    }
    private void OnDrawGizmos()
    {
        for (int i = 0; i < debugResolution; i++)
        {
            for (int j = 0; j < debugResolution; j++)
            {
                Vector3 offset = new Vector3(i * width / debugResolution, 0, j * height / debugResolution);
                Vector3 point = transform.position + offset;
                Gizmos.color = Color.Lerp(Color.black, Color.white, _noiseGenerator.SamplePerlinNoise(offset.x, offset.z));
                Gizmos.DrawSphere(point, debugSphereSize);
            }
        }
    }
}
