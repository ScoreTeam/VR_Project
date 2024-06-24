using System;
using Unity.Mathematics;
using UnityEngine;

public class Spawner3D : MonoBehaviour
{
    public int numParticlesPerAxis;
    public int numPoints = 64000;
    public Vector3 centre;
    public float size;
    public float3 initialVel;
    public float jitterStrength;
    public bool showSpawnBounds;

    [Header("Info")]
    public int debug_numParticles;

    public SpawnData GetSpawnData()
    {
        float3[] points = new float3[numPoints];
        float3[] velocities = new float3[numPoints];

        OnValidate();

        // for (int x = 0; x < numParticlesPerAxis; x++)
        // {
        //     for (int y = 0; y < numParticlesPerAxis; y++)
        //     {
        //         for (int z = 0; z < numParticlesPerAxis; z++)
        //         {
        //             float tx = x / (numParticlesPerAxis - 1f);
        //             float ty = y / (numParticlesPerAxis - 1f);
        //             float tz = z / (numParticlesPerAxis - 1f);

        //             float px = (tx - 0.5f) * size + centre.x;
        //             float py = (ty - 0.5f) * size + centre.y;
        //             float pz = (tz - 0.5f) * size + centre.z;
        //             float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
        //             points[i] = new float3(px, py, pz) + jitter;
        //             velocities[i] = initialVel;
        //             i++;
        //         }
        //     }
        // }

        for (int j = 0; j < numPoints; j++)
        {
            points[j] = new float3(0, 100, 0);
            velocities[j] = new float3(0, 0, 0);
        }


        return new SpawnData() { points = points, velocities = velocities };
    }

    public SpawnData GeneratePoints()
    {
        int numPoints2 = numParticlesPerAxis * numParticlesPerAxis * numParticlesPerAxis;
        float3[] points = new float3[numPoints2];
        float3[] velocities = new float3[numPoints2];

        OnValidate();
        int i = 0;
        for (int x = 0; x < numParticlesPerAxis; x++)
        {
            for (int y = 0; y < numParticlesPerAxis; y++)
            {
                for (int z = 0; z < numParticlesPerAxis; z++)
                {
                    float tx = x / (numParticlesPerAxis - 1f);
                    float ty = y / (numParticlesPerAxis - 1f);
                    float tz = z / (numParticlesPerAxis - 1f);

                    float px = (tx - 0.5f) * size + centre.x;
                    float py = (ty - 0.5f) * size + centre.y;
                    float pz = (tz - 0.5f) * size + centre.z;
                    float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
                    points[i] = new float3(px, py, pz) + jitter;
                    velocities[i] = initialVel;
                    i++;
                }
            }
        }

        return new SpawnData() { points = points, velocities = velocities };
    }

    public struct SpawnData
    {
        public float3[] points;
        public float3[] velocities;
    }

    void OnValidate()
    {
        if (Math.Pow(numParticlesPerAxis, 3) >= numPoints)
        {
            numParticlesPerAxis = (int)Math.Pow(numPoints, 1.0 / 3.0);
        }
        debug_numParticles = numParticlesPerAxis * numParticlesPerAxis * numParticlesPerAxis;
    }

    void OnDrawGizmos()
    {
        if (showSpawnBounds && !Application.isPlaying)
        {
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireCube(centre, Vector3.one * size);
        }
    }
}
