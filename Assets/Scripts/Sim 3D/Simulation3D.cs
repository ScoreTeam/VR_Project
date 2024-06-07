using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class Simulation3D : MonoBehaviour
{
    public event System.Action SimulationStepCompleted;

    [Header("Settings")]
    public float timeScale = 1;
    public bool fixedTimeStep;
    public int iterationsPerFrame;
    public float gravity = -10;
    [Range(0, 1)] public float collisionDamping = 0.05f;
    public float smoothingRadius = 0.2f;
    public float targetDensity;
    public float pressureMultiplier;
    public float nearPressureMultiplier;
    public float viscosityStrength;

    private static Vector3[] obstacleCentres;
    private static Vector3[] obstacleSizes;


    [Header("References")]
    public ComputeShader compute;
    public Spawner3D spawner;
    public ParticleDisplay3D display;
    public Transform floorDisplay;

    // Buffers
    public ComputeBuffer positionBuffer { get; private set; }
    public ComputeBuffer initPositionBuffer { get; private set; }
    public ComputeBuffer velocityBuffer { get; private set; }
    public ComputeBuffer initVelocityBuffer { get; private set; }
    public ComputeBuffer densityBuffer { get; private set; }
    public ComputeBuffer predictedPositionsBuffer;
    ComputeBuffer spatialIndices;
    ComputeBuffer spatialOffsets;

    public Vector3 BoundsSize = new Vector3(10, 10, 10);
    public Vector3 BoundsCentre = new Vector3(0, 0, 0);

    ComputeBuffer ObBoxesCenters;
    ComputeBuffer ObBoxesSizes;

    // Kernel IDs
    const int externalForcesKernel = 0;
    const int spatialHashKernel = 1;
    const int densityKernel = 2;
    const int pressureKernel = 3;
    const int viscosityKernel = 4;
    const int updatePositionsKernel = 5;
    const int collisionDetection = 6;

    GPUSort gpuSort;

    // State
    bool isPaused;
    bool pauseNextFrame;
    Spawner3D.SpawnData spawnData;
    private static BVHManager bvhManager;

    void Awake()
    {

        floorDisplay.transform.localScale = new Vector3(BoundsSize.x, 1 / BoundsSize.y * 0.1f, BoundsSize.z);
        floorDisplay.transform.position = new Vector3(BoundsCentre.x, BoundsCentre.y - BoundsSize.y * 0.5f, BoundsCentre.z);

    }
    static Simulation3D()
    {
        // Initialize obstacle centers and sizes
        // obstacleCentres = new Vector3[4];
        // obstacleSizes = new Vector3[4];
        // for (int i = 0; i < 4; i++)
        // {
        //     obstacleCentres[i] = new Vector3(i * 3 - 4.5f, i - 1.5f, 0);
        //     obstacleSizes[i] = new Vector3(1, 1, 5);
        // }



        // else
        // {
        //     Debug.LogError("BVHManager not found in the scene.");
        // }
    }
    void Start()
    {
        Debug.Log("Controls: Space = Play/Pause, R = Reset");
        Debug.Log("Use transform tool in scene to scale/rotate simulation bounding box.");

        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;

        spawnData = spawner.GetSpawnData();

        // Create buffers
        int numParticles = spawnData.points.Length;
        positionBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        initPositionBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        predictedPositionsBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        velocityBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        initVelocityBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        densityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        spatialIndices = ComputeHelper.CreateStructuredBuffer<uint3>(numParticles);
        spatialOffsets = ComputeHelper.CreateStructuredBuffer<uint>(numParticles);

        ObBoxesCenters = ComputeHelper.CreateStructuredBuffer<float3>(1000);
        ObBoxesSizes = ComputeHelper.CreateStructuredBuffer<float3>(1000);

        // Set buffer data
        SetInitialBufferData(spawnData);

        // Init compute
        ComputeHelper.SetBuffer(compute, positionBuffer, "Positions", externalForcesKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, initPositionBuffer, "InitPositions", externalForcesKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, predictedPositionsBuffer, "PredictedPositions", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, spatialIndices, "SpatialIndices", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, spatialOffsets, "SpatialOffsets", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, densityBuffer, "Densities", densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, velocityBuffer, "Velocities", externalForcesKernel, pressureKernel, viscosityKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, initVelocityBuffer, "InitVelocities", externalForcesKernel, pressureKernel, viscosityKernel, updatePositionsKernel);

        ComputeHelper.SetBuffer(compute, ObBoxesCenters, "ObCenters", collisionDetection, viscosityKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, ObBoxesSizes, "ObSizes", collisionDetection, viscosityKernel, updatePositionsKernel);

        compute.SetInt("numParticles", positionBuffer.count);

        gpuSort = new();
        gpuSort.SetBuffers(spatialIndices, spatialOffsets);

        // Init display
        display.Init(this);
        bvhManager = FindObjectOfType<BVHManager>();

        if (bvhManager != null)
        {
            // Access the list of boxes
            List<BoxNode> boxes = bvhManager.GetBoxes();
            BoxNode[] boxesArray = boxes.ToArray();
            // // Process the boxes

            obstacleCentres = new Vector3[boxesArray.Length];
            obstacleSizes = new Vector3[boxesArray.Length];
            for (int i = 0; i < boxesArray.Length; i++)
            {
                obstacleCentres[i] = boxesArray[i].Center;
                obstacleSizes[i] = boxesArray[i].Size;
            }
            // foreach (var box in boxes)
            // {
            //     Debug.Log($"Box Center: {box.Center}, Size: {box.Size}");
            // }
        }
    }

    void FixedUpdate()
    {
        // Run simulation if in fixed timestep mode
        if (fixedTimeStep)
        {
            RunSimulationFrame(Time.fixedDeltaTime);
        }
    }

    void Update()
    {
        // Run simulation if not in fixed timestep mode
        // (skip running for first few frames as timestep can be a lot higher than usual)
        if (!fixedTimeStep && Time.frameCount > 10)
        {
            RunSimulationFrame(Time.deltaTime);
        }

        if (pauseNextFrame)
        {
            isPaused = true;
            pauseNextFrame = false;
        }
        // floorDisplay.transform.localScale = new Vector3(BoundsSize.x, 1 / BoundsSize.y * 0.1f, BoundsSize.z);
        // floorDisplay.transform.position = new Vector3(BoundsCentre.x,BoundsCentre.y - BoundsSize.y*0.5f,BoundsCentre.z);
        HandleInput();
    }

    void RunSimulationFrame(float frameTime)
    {
        if (!isPaused)
        {
            float timeStep = frameTime / iterationsPerFrame * timeScale;

            UpdateSettings(timeStep);

            for (int i = 0; i < iterationsPerFrame; i++)
            {
                RunSimulationStep();
                SimulationStepCompleted?.Invoke();
            }
        }
    }

    void RunSimulationStep()
    {
        ComputeHelper.Dispatch(compute, positionBuffer.count, kernelIndex: externalForcesKernel);
        ComputeHelper.Dispatch(compute, positionBuffer.count, kernelIndex: spatialHashKernel);
        gpuSort.SortAndCalculateOffsets();
        ComputeHelper.Dispatch(compute, positionBuffer.count, kernelIndex: densityKernel);
        ComputeHelper.Dispatch(compute, positionBuffer.count, kernelIndex: pressureKernel);
        ComputeHelper.Dispatch(compute, positionBuffer.count, kernelIndex: viscosityKernel);
        ComputeHelper.Dispatch(compute, positionBuffer.count, kernelIndex: updatePositionsKernel);
        // ComputeHelper.Dispatch(compute, positionBuffer.count, kernelIndex: collisionDetection);

    }

    void UpdateSettings(float deltaTime)
    {
        // Vector3 simBoundsSize = transform.localScale;
        // Vector3 simBoundsCentre = transform.position;

        compute.SetFloat("deltaTime", deltaTime);
        compute.SetFloat("gravity", gravity);
        compute.SetFloat("collisionDamping", collisionDamping);
        compute.SetFloat("smoothingRadius", smoothingRadius);
        compute.SetFloat("targetDensity", targetDensity);
        compute.SetFloat("pressureMultiplier", pressureMultiplier);
        compute.SetFloat("nearPressureMultiplier", nearPressureMultiplier);
        compute.SetFloat("viscosityStrength", viscosityStrength);
        compute.SetVector("boundsSize", BoundsSize);
        compute.SetVector("centre", BoundsCentre);
        if (obstacleCentres != null)
        {
            compute.SetInt("numObstacles", obstacleCentres.Length);
            compute.SetVectorArray("obstacleSizes", ConvertToVector4Array(obstacleSizes));
            compute.SetVectorArray("obstacleCentres", ConvertToVector4Array(obstacleCentres));
        }
        compute.SetMatrix("localToWorld", transform.localToWorldMatrix);
        compute.SetMatrix("worldToLocal", transform.worldToLocalMatrix);
    }

    void SetInitialBufferData(Spawner3D.SpawnData spawnData)
    {
        float3[] allPoints = new float3[spawnData.points.Length];
        System.Array.Copy(spawnData.points, allPoints, spawnData.points.Length);

        positionBuffer.SetData(allPoints);
        initPositionBuffer.SetData(allPoints);
        predictedPositionsBuffer.SetData(allPoints);
        velocityBuffer.SetData(spawnData.velocities);
        initVelocityBuffer.SetData(spawnData.velocities);
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            isPaused = false;
            pauseNextFrame = true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            isPaused = true;
            SetInitialBufferData(spawnData);
        }
    }

    void OnDestroy()
    {
        ComputeHelper.Release(positionBuffer, predictedPositionsBuffer, velocityBuffer, densityBuffer, spatialIndices, spatialOffsets, ObBoxesCenters, ObBoxesSizes);
    }

    void OnDrawGizmos()
    {
        // Draw Bounds
        var m = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawWireCube(BoundsCentre, BoundsSize);
        if (obstacleCentres != null)
        {
            for (int i = 0; i < obstacleCentres.Length; i++)
            {
                Gizmos.DrawWireCube(obstacleCentres[i], obstacleSizes[i]);
            }
        }
        Gizmos.matrix = m;

    }

    Vector4[] ConvertToVector4Array(Vector3[] input)
    {
        Vector4[] output = new Vector4[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = input[i];
        }
        return output;
    }
    // public static void ReGenerateParticles(ComputeBuffer PositionsBuffer, ComputeBuffer VelocitiesBuffer, int ParticlesLayer, Vector3 centre, float size, float3 initialVel, float jitterStrength)
    //     {
    //         Vector3[] positionsData = new Vector3[PositionsBuffer.count];
    //         Vector3[] velocitiesData = new Vector3[VelocitiesBuffer.count];
    //         PositionsBuffer.GetData(positionsData);
    //         VelocitiesBuffer.GetData(velocitiesData);
    //         int n = Convert.ToInt32(Math.Pow(PositionsBuffer.count, 1f / 3f));

    //         int particleIndex = n * (ParticlesLayer - 1);

    //         // Vector3 posLocal = WorldToLocal.MultiplyPoint(positionsData[particleIndex]);
    //         // Vector3 velocityLocal = WorldToLocal.MultiplyVector(velocitiesData[particleIndex]);
    //         for (int y = 0; y < n; y++)
    //         {
    //             for (int z = 0; z < n; z++)
    //             {
    //                 float tx = 0 / (n - 1f);
    //                 float ty = y / (n - 1f);
    //                 float tz = z / (n - 1f);

    //                 float px = (tx - 0.5f) * size + centre.x;
    //                 float py = (ty - 0.5f) * size + centre.y;
    //                 float pz = (tz - 0.5f) * size + centre.z;
    //                 float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
    //                 positionsData[particleIndex] = new float3(px, py, pz) + jitter;
    //                 velocitiesData[particleIndex] = initialVel;
    //                 particleIndex++;
    //             }
    //             // positionsData[particleIndex] = LocalToWorld.MultiplyPoint(posLocal);
    //             // velocitiesData[particleIndex] = LocalToWorld.MultiplyPoint(velocityLocal);
    //         }
    //         // Set the modified data back to the buffer
    //         PositionsBuffer.SetData(positionsData);
    //         VelocitiesBuffer.SetData(velocitiesData);
    //     }



}