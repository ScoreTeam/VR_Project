using System.Collections.Generic;
using UnityEngine;
using System;
using System.Drawing;
using System.Linq;

public class MassSpringSystem : MonoBehaviour
{
    public bool isDeforming = false;
    // Mass point structure
    public struct MassPoint
    {
        public Vector3 restPosition;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public float mass;
        public float time;

        public MassPoint(Vector3 pos, float m)
        {
            restPosition = pos;
            position = pos;
            velocity = Vector3.zero;
            force = Vector3.zero;
            mass = m;
            time = 0;
        }
    }

    // Spring structure
    public struct Spring
    {
        public int pointA;
        public int pointB;
        public float restLength;
        public float stiffness;
        public float damping;

        public Spring(int a, int b, float restLen, float stiff, float damp)
        {
            pointA = a;
            pointB = b;
            restLength = restLen;
            stiffness = stiff;
            damping = damp;
        }
    }

    public float mass = 0.01f;
    public float stiffness = 0.01f;
    public float damping = -0.009f;
    public int k = 15; // Number of nearest neighbors to connect to 
    public int effectedVertexIndex = 0;
    public Vector3 externalForce = new Vector3(0.5f, 0, 0);
    public List<MassPoint> massPoints = new List<MassPoint>();
    private Vector3[] vertices;
    private List<Spring> springs = new List<Spring>();
    private int count = 0;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        vertices = mesh.vertices;

        InitializeMassPointsList();
        InitializeSprings();
    }

    void Update()
    {
         HandleInput();

        if (isDeforming && /*externalForce != new Vector3(0, 0, 0) &&*/ count % 10 == 0)
        {
            apply_from_doc(Time.deltaTime);
            UpdateMesh();
        }
        count++;
    }
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isDeforming = !isDeforming;
        }
    }

    void InitializeMassPointsList()
    {

        Transform transform = GetComponent<Renderer>().transform;

        foreach (var vertex in vertices)
        {
            Vector3 worldVertex = transform.TransformPoint(vertex);
            massPoints.Add(new MassPoint(worldVertex, mass));
        }
    }

    void InitializeSprings()
    {
        for (int i = 0; i < massPoints.Count; i++)
        {
            List<int> nearestNeighbors = FindKNearestNeighbors(i);
            foreach (int neighborIndex in nearestNeighbors)
            {
                float restLength = Vector3.Distance(massPoints[i].position, massPoints[neighborIndex].position);
                springs.Add(new Spring(i, neighborIndex, restLength, stiffness, damping));
            }
        }
    }

    List<int> FindKNearestNeighbors(int index)
    {
        List<int> nearestNeighbors = new List<int>();
        SortedList<float, int> distances = new SortedList<float, int>();

        Vector3 currentVertex = massPoints[index].position;
        for (int i = 0; i < massPoints.Count; i++)
        {
            if (i == index) continue; // Skip the same vertex

            float distance = Vector3.Distance(currentVertex, massPoints[i].position);
            distances.TryAdd(distance, i);
        }

        foreach (var kvp in distances)
        {
            if (nearestNeighbors.Count >= k) break;
            nearestNeighbors.Add(kvp.Value);
        }

        return nearestNeighbors;
    }

    void apply_from_doc(float deltaTime)
    {

        // apply eular
        MassPoint mass_point = massPoints[effectedVertexIndex];

        mass_point.position = mass_point.position + (deltaTime - mass_point.time) * mass_point.velocity;
        mass_point.velocity = mass_point.velocity + (deltaTime - mass_point.time) * (externalForce + damping * mass_point.velocity) / mass_point.mass;
        massPoints[effectedVertexIndex] = mass_point;

        foreach (var spring in springs)
        {

            MassPoint pointA = massPoints[spring.pointA];
            MassPoint pointB = massPoints[spring.pointB];

            Vector3 distance = pointA.position - pointB.position;


            if (distance.magnitude - spring.restLength != 0 && distance.magnitude < spring.restLength * 3 / 2)
            {
                // initial force
                pointA.force = -1 * spring.stiffness * (distance.magnitude - spring.restLength) * (pointA.position - pointB.position) / distance.magnitude;
                pointB.force = -1 * spring.stiffness * (distance.magnitude - spring.restLength) * (pointB.position - pointA.position) / distance.magnitude;


                // eular
                pointA.position = pointA.position + (deltaTime - pointA.time) * pointA.velocity;
                pointA.velocity = pointA.velocity + (deltaTime - pointA.time) * (pointA.force + spring.damping * mass_point.velocity) / mass_point.mass;

                pointB.position = pointB.position + (deltaTime - pointB.time) * pointB.velocity;
                pointB.velocity = pointB.velocity + (deltaTime - pointB.time) * (pointB.force + spring.damping * mass_point.velocity) / mass_point.mass;

                // // external force
                Vector3 externalForceA = pointA.force + spring.damping * pointA.velocity;
                Vector3 externalForceB = pointB.force + spring.damping * pointB.velocity;

                // // total force
                Vector3 totalForceA = pointA.force + externalForceA;
                Vector3 totalForceB = pointB.force + externalForceB;

                // // change
                float max_ability = spring.restLength * 2f;
                float min_ability = spring.restLength / 2f;

                if (distance.magnitude < max_ability && distance.magnitude > min_ability)
                {
                    pointA.force = totalForceA;
                    massPoints[spring.pointA] = pointA;

                    pointB.force = totalForceB;
                    massPoints[spring.pointB] = pointB;
                }
            }

        }
    }

    public Vector3[] UpdateMassPoints()
    {
        Vector3[] massPointsPosition = new Vector3[massPoints.Count];
        for (int i = 0; i < massPoints.Count; i++)
        {
            massPointsPosition[i] = massPoints[i].position;
        }
        return massPointsPosition;
    }

    void UpdateMesh()
    {
        // Create a new mesh
        Mesh newMesh = new Mesh();

        // Update vertices based on mass points
        Vector3[] updatedVertices = new Vector3[massPoints.Count];
        for (int i = 0; i < massPoints.Count; i++)
        {
            updatedVertices[i] = transform.InverseTransformPoint(massPoints[i].position);
        }

        // Set vertices to the new mesh
        newMesh.vertices = updatedVertices;

        // Copy other mesh data from the original mesh (if needed)
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh originalMesh = meshFilter.mesh;

        newMesh.triangles = originalMesh.triangles;
        newMesh.uv = originalMesh.uv;
        newMesh.normals = originalMesh.normals;
        newMesh.colors = originalMesh.colors;
        newMesh.tangents = originalMesh.tangents;
        newMesh.boneWeights = originalMesh.boneWeights;

        // Recalculate normals and bounds
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        // Replace the original mesh with the new mesh
        meshFilter.mesh = newMesh;
    }


    void OnDrawGizmos()
    {
        foreach (var massPoint in massPoints)
        {
            Gizmos.color = UnityEngine.Color.red;
            Gizmos.DrawSphere(massPoint.position, 0.01f);
        }

        if (springs == null || massPoints == null) return;

        Gizmos.color = UnityEngine.Color.black;

        foreach (var spring in springs)
        {
            Vector3 pointAPosition = massPoints[spring.pointA].position;
            Vector3 pointBPosition = massPoints[spring.pointB].position;
            Gizmos.DrawLine(pointAPosition, pointBPosition);
        }
    }
}
