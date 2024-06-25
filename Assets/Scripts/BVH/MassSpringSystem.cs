using System.Collections.Generic;
using UnityEngine;
using System;
using System.Drawing;
using System.Linq;

public class MassSpringSystem : MonoBehaviour
{
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
    public float damping = -0.007f;
    public float springLength = 10f;
    public int k = 8; // Number of nearest neighbors to connect to 
    public int effectedVertexIndex = 0;
    public Vector3 externalForce = new Vector3(0, 0, 0.5f);
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    private List<MassPoint> massPoints = new List<MassPoint>();
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
        // bool isSetToDefault = false;
        if (externalForce != new Vector3(0, 0, 0) && count % 5 == 0)
           { apply_from_doc(Time.deltaTime);
            // UpdateMesh();
            }
        // applyEuler();
        // if (count % 5 == 0)

        // if (externalForce != new Vector3(0, 0, 0))
        // {
        //     while (isSetToDefault == false)
        //     {
        //         for (int i = 0; i < massPoints.Count; i++)
        //         {
        //             MassPoint mass_point = massPoints[i];
        //             mass_point.velocity = mass_point.velocity + (Time.deltaTime - mass_point.time) * (damping * mass_point.velocity) / mass_point.mass;
        //             mass_point.position = mass_point.position + (Time.deltaTime - mass_point.time) * mass_point.velocity;

        //             Vector3 distance = massPoints[i].restPosition - mass_point.position;

        //             if (distance.magnitude > 0)
        //                 massPoints[i] = mass_point;

        //             int c = 0;
        //             for (int j = 0; j < massPoints.Count; j++)
        //             {
        //                 if (massPoints[j].position == massPoints[j].restPosition)
        //                 {
        //                     c++;
        //                 }
        //                 if (c == massPoints.Count - 1) isSetToDefault = true;
        //             }
        //         }
        //     }

        // }
        // apply_from_doc(Time.deltaTime);

        // }
        count++;
        // UpdateMesh();
        // ApplyForces();
        // UpdateMassPoints(Time.deltaTime);
        
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

    void applyEuler()
    {
        MassPoint mass_point = massPoints[effectedVertexIndex];

        mass_point.position = mass_point.position + (Time.deltaTime - mass_point.time) * mass_point.velocity;
        mass_point.velocity = mass_point.velocity + (Time.deltaTime - mass_point.time) * ((externalForce + mass_point.force) - damping * mass_point.velocity) / mass_point.mass;

        massPoints[effectedVertexIndex] = mass_point;
    }

    void apply_from_doc(float deltaTime)
    {

        // apply eular
        MassPoint mass_point = massPoints[effectedVertexIndex];

        mass_point.position = mass_point.position + (deltaTime - mass_point.time) * mass_point.velocity;
        mass_point.velocity = mass_point.velocity + (deltaTime - mass_point.time) * (externalForce + mass_point.force + damping * mass_point.velocity) / mass_point.mass;

        // Vector3 difference = mass_point.position - massPoints[effectedVertexIndex].position;
        // Debug.Log(difference.magnitude + " , " + springLength);

        // if (difference.magnitude < springLength * 2 && difference.magnitude > springLength / 2)
        // {
        massPoints[effectedVertexIndex] = mass_point;
        // }

        // Debug.Log("position : " + mass_point.position + " velocity: " + mass_point.velocity);

        foreach (var spring in springs)
        {

            MassPoint pointA = massPoints[spring.pointA];
            MassPoint pointB = massPoints[spring.pointB];

            Vector3 distance = pointA.position - pointB.position;


            if (distance.magnitude - spring.restLength != 0 && distance.magnitude < spring.restLength)
            {// initial force
                // Debug.Log(distance.magnitude);


                // Debug.Log(-1 * spring.stiffness * (distance.magnitude - spring.restLength) * (pointA.position - pointB.position) / distance.magnitude);

                pointA.force = -1 * spring.stiffness * (distance.magnitude - spring.restLength) * (pointA.position - pointB.position) / distance.magnitude;
                pointB.force = -1 * spring.stiffness * (distance.magnitude - spring.restLength) * (pointB.position - pointA.position) / distance.magnitude;

                // Debug.Log("point force x: " + pointA.force.x);
                // Debug.Log("point force: " + pointA.force + " , " + pointB.force);

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

        // MassPoint mass_point = massPoints[effectedVertexIndex];


        // mass_point.time = deltaTime - mass_point.time;

        // damping += -1 * mass_point.force.magnitude / 2f; // ??
        // Vector3 current_velocity = mass_point.velocity + mass_point.time * ((mass_point.force + externalForce + damping * mass_point.velocity) / mass_point.mass);

        // Vector3 equilibrium_position = mass_point.position;
        // Vector3 current_position = equilibrium_position + mass_point.time * current_velocity;
        // mass_point.position = current_position;
        // Vector3 displacement = current_position - equilibrium_position;

        // mass_point.force = mass_point.mass * (current_velocity - mass_point.velocity) / mass_point.time + damping * mass_point.velocity;

        // Debug.Log(mass_point.position);
        // massPoints[effectedVertexIndex] = mass_point;

        // foreach (var spring in springs)
        // {
        //     Vector3 springForce = -1 * spring.stiffness * displacement;


        //     // // // point A
        //     MassPoint mass_point_A = massPoints[spring.pointA];
        //     Vector3 acceleration_A = (mass_point_A.force + externalForce) / mass_point_A.mass;

        //     // change force
        //     Vector3 total_force_A = springForce + mass_point_A.mass * acceleration_A + spring.damping * mass_point_A.velocity + spring.stiffness * mass_point_A.position;
        //     mass_point_A.force = total_force_A;

        //     // change velocity
        //     mass_point_A.velocity = -1 * (acceleration_A * mass_point_A.mass - total_force_A) / spring.damping;

        //     massPoints[spring.pointA] = mass_point_A;


        //     // // // point B
        //     MassPoint mass_point_B = massPoints[spring.pointB];
        //     Vector3 acceleration_B = (mass_point_B.force + externalForce) / mass_point_B.mass;

        //     // change force
        //     Vector3 total_force_B = springForce + mass_point_B.mass * acceleration_B + spring.damping * mass_point_B.velocity + spring.stiffness * mass_point_B.position;
        //     mass_point_B.force = total_force_B;

        //     // change velocity
        //     mass_point_B.velocity = -1 * (acceleration_B * mass_point_B.mass - total_force_B) / spring.damping;

        //     massPoints[spring.pointB] = mass_point_B;
        // }

    }

    void ApplyForces()
    {

        for (int i = 0; i < massPoints.Count; i++)
        {
            MassPoint point = massPoints[i];
            point.force = externalForce * massPoints[i].mass;
            massPoints[i] = point;
        }

        // Compute spring forces
        foreach (var spring in springs)
        {
            MassPoint pointA = massPoints[spring.pointA];
            MassPoint pointB = massPoints[spring.pointB];

            Vector3 delta = pointB.position - pointA.position;
            float currentLength = delta.magnitude;
            Vector3 direction = delta.normalized;

            Vector3 springForce = -spring.stiffness * (currentLength - spring.restLength) * direction;

            Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
            Vector3 dampingForce = -spring.damping * relativeVelocity;

            pointA.force += springForce + dampingForce;
            pointB.force -= springForce + dampingForce;
            massPoints[spring.pointA] = pointA;
            massPoints[spring.pointB] = pointB;
        }
    }

    void UpdateMassPoints(float deltaTime)
    {
        for (int i = 0; i < massPoints.Count; i++)
        {
            MassPoint point = massPoints[i];

            Vector3 acceleration = point.force / point.mass;
            point.velocity += acceleration * deltaTime;
            point.position += point.velocity * deltaTime;

            massPoints[i] = point; // Update mass point in the list
        }
    }

    void UpdateMesh()
    {
        // Create and update mesh based on mass points
        Mesh mesh = new Mesh();

        for (int i = 0; i < massPoints.Count; i++)
        {
            vertices[i] = transform.InverseTransformPoint(massPoints[i].position);
        }

        mesh.vertices = vertices;

        GetComponent<MeshFilter>().mesh = mesh;

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
