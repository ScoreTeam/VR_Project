using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class Simulation3D : MonoBehaviour
{
    public event System.Action SimulationStepCompleted;

    [Header("Settings")]
    public float timeScale = 0.5f;
    public bool fixedTimeStep = true;
    public int iterationsPerFrame = 3;
    public float mass = -0.0098f;
    [Range(0, 1)] public float collisionDamping = 0.95f;
    public float smoothingRadius = 0.1f;
    public float targetDensity = 400;
    public float pressureMultiplier = 268f;
    public float nearPressureMultiplier = 10;
    public float viscosityStrength = 0.01f;

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
    public ComputeBuffer prepositionBuffer { get; private set; }
    public ComputeBuffer crrpositionBuffer { get; private set; }
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

    ComputeBuffer pointsBool;

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


    Spawner3D.SpawnData genData;
    private static BVHManager bvhManager;
    private float PreTime, PreTime2 = 0;

    private int layerCount = 0;

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
    }
    void Start()
    {
        Debug.Log("Controls: Space = Play/Pause, R = Reset,Right Arrow = Next frame, Esc = Quit");
        Debug.Log("Use transform tool in scene to scale/rotate simulation bounding box.");
        // Application.targetFrameRate = 60;
        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;

        spawnData = spawner.GetSpawnData();

        // Create buffers
        int numParticles = spawnData.points.Length;
        positionBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        initPositionBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        prepositionBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        crrpositionBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        predictedPositionsBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        velocityBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        initVelocityBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        densityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        spatialIndices = ComputeHelper.CreateStructuredBuffer<uint3>(numParticles);
        spatialOffsets = ComputeHelper.CreateStructuredBuffer<uint>(numParticles);

        ObBoxesCenters = ComputeHelper.CreateStructuredBuffer<float3>(1000);
        ObBoxesSizes = ComputeHelper.CreateStructuredBuffer<float3>(1000);

        pointsBool = ComputeHelper.CreateStructuredBuffer<uint>(numParticles);
        // Set buffer data
        SetInitialBufferData(spawnData);

        // Init compute
        ComputeHelper.SetBuffer(compute, positionBuffer, "Positions", externalForcesKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, prepositionBuffer, "PrePositions", externalForcesKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, crrpositionBuffer, "CrrPositions", externalForcesKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, initPositionBuffer, "InitPositions", externalForcesKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, predictedPositionsBuffer, "PredictedPositions", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, spatialIndices, "SpatialIndices", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, spatialOffsets, "SpatialOffsets", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, densityBuffer, "Densities", densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, velocityBuffer, "Velocities", externalForcesKernel, pressureKernel, viscosityKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, initVelocityBuffer, "InitVelocities", externalForcesKernel, pressureKernel, viscosityKernel, updatePositionsKernel);

        ComputeHelper.SetBuffer(compute, ObBoxesCenters, "ObCenters", viscosityKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, ObBoxesSizes, "ObSizes", viscosityKernel, updatePositionsKernel);

        ComputeHelper.SetBuffer(compute, pointsBool, "PointsBool", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, updatePositionsKernel);
        compute.SetInt("numParticles", positionBuffer.count);

        gpuSort = new();
        gpuSort.SetBuffers(spatialIndices, spatialOffsets);

        // Init display
        display.Init(this);
        bvhManager = FindObjectOfType<BVHManager>();

        if (bvhManager != null)
        {
            // Access the list of boxes
            bvhManager.Initialize();
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
            float t = Time.realtimeSinceStartup;

            if (t - PreTime >= 0.1f)
            {
                PreTime = t;
                SetPreviousPositions();
            }

            if (t - PreTime2 >= 1.0f)
            {
                PreTime2 = t;
                SetNewLayer();
            }

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
        compute.SetFloat("mass", mass);
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

        int[] pb = new int[spawnData.points.Length];
        for (int i = 0; i < spawnData.points.Length; i++)
        {
            pb[i] = 0;
        }
        pointsBool.SetData(pb);

        positionBuffer.SetData(allPoints);
        initPositionBuffer.SetData(allPoints);
        prepositionBuffer.SetData(allPoints);
        crrpositionBuffer.SetData(allPoints);
        predictedPositionsBuffer.SetData(allPoints);
        velocityBuffer.SetData(spawnData.velocities);
        initVelocityBuffer.SetData(spawnData.velocities);
    }

    void SetNewLayer()
    {
        genData = spawner.GeneratePoints();

        float3[] partPoints = new float3[genData.points.Length];
        System.Array.Copy(genData.points, partPoints, genData.points.Length);


        float3[] partVelocities = new float3[genData.velocities.Length];
        System.Array.Copy(genData.velocities, partVelocities, genData.velocities.Length);

        int[] pb = new int[genData.points.Length];
        for (int i = 0; i < genData.points.Length; i++)
        {
            pb[i] = 1;
        }

        float3[] P = new float3[positionBuffer.count];
        float3[] V = new float3[positionBuffer.count];
        float3[] INIT_V = new float3[positionBuffer.count];
        float3[] PRE = new float3[positionBuffer.count];
        float3[] CRR = new float3[positionBuffer.count];
        int[] B = new int[pointsBool.count];

        positionBuffer.GetData(P);
        velocityBuffer.GetData(V);
        initVelocityBuffer.GetData(INIT_V);
        prepositionBuffer.GetData(PRE);
        crrpositionBuffer.GetData(CRR);
        pointsBool.GetData(B);
        if (layerCount + genData.points.Length >= positionBuffer.count)
        {
            layerCount = 0;
        }
        for (int i = 0; i < genData.points.Length; i++)
        {
            P[layerCount + i] = partPoints[i];
            V[layerCount + i] = partVelocities[i];
            INIT_V[layerCount + i] = partVelocities[i];
            B[layerCount + i] = pb[i];
            PRE[layerCount + i] = partPoints[i];
            CRR[layerCount + i] = partPoints[i];
        }
        layerCount += genData.points.Length;

        pointsBool.SetData(B);
        positionBuffer.SetData(P);
        predictedPositionsBuffer.SetData(P);
        prepositionBuffer.SetData(PRE);
        crrpositionBuffer.SetData(CRR);
        velocityBuffer.SetData(V);
        initVelocityBuffer.SetData(INIT_V);
    }

    void SetPreviousPositions()
    {
        float3[] Points1 = new float3[spawnData.points.Length];
        float3[] Points2 = new float3[spawnData.points.Length];

        positionBuffer.GetData(Points1);
        crrpositionBuffer.GetData(Points2);
        crrpositionBuffer.SetData(Points1);
        prepositionBuffer.SetData(Points2);
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            isPaused = true;
            SetInitialBufferData(spawnData);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    void OnDestroy()
    {
        ComputeHelper.Release(positionBuffer, predictedPositionsBuffer, velocityBuffer, densityBuffer, spatialIndices, spatialOffsets,
         ObBoxesCenters, ObBoxesSizes, initVelocityBuffer, initPositionBuffer, crrpositionBuffer, prepositionBuffer, pointsBool);
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
                // Gizmos.color = Color.black;
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

}