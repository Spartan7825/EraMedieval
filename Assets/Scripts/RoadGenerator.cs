using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

namespace QuantumCookie
{
    public class Vertex
    {
        public Vector3 position;
        public int branchID;
        public int vertexID;
        public int branchDepth;
        public float branchProbability;
        public Vector3 direction;
        public Vector3 normal;

        public Vector3 Tangent => Vector3.Cross(normal, direction).normalized;
        public Vector3 ReverseTangent => Vector3.Cross(direction, normal).normalized;

        public Vertex(Vector3 _position)
        {
            position = _position;
            direction = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1)).normalized;
            if (direction == Vector3.zero) direction = new Vector3(1, 0, 1).normalized;
        }

        public Vertex(Vector3 _position, Vector3 _direction)
        {
            position = _position;
            direction = _direction;
        }
    }

    public class Edge
    {
        public Vertex start, end;

        private float _length;
        public Vector3 Direction => (end.position - start.position).normalized;
        public Vector3 Tangent => (start.Tangent + end.Tangent) * 0.5f;
        public Vector3 Normal => (start.normal + end.normal) * 0.5f;
        public Vector3 ReverseTangent => (start.ReverseTangent + end.ReverseTangent) * 0.5f;

        public float Length => _length;

        public Edge(Vertex _start, Vertex _end)
        {
            start = _start;
            end = _end;
            _length = Vector3.Distance(start.position, end.position);
        }
    }

    public class RoadGenerator : MonoBehaviour
    {
        private Vector2 mapSize;
        private int seed;
        private float stepSize;
        private float inertia;
        private float curveAngleMax;
        private float branchProbability;
        private float branchAngleMax;
        private int branchDepthMax;
        private float branchProbabilityDecay;
        private float weldThreshold;

        private float roadWidth;


        private bool showDebug = true;
        private bool showIndices = true;
        private float debugSphereSize = 5f;

        private List<Vector3> roadOrigins;
        private Vector2 mapBoundsMin, mapBoundsMax;

        private List<List<Vertex>> vertices;
        private List<List<Edge>> edges;

        public List<List<Edge>> Edges => edges;

        private Mesh roadMesh;
        private MeshFilter meshFilter;

        private VillageGenerator villageGen;

        public void RegenerateRoad(VillageGenerator _villageGen, int _seed, Vector2 _mapSize, float _stepSize, float _roadWidth, float _inertia,
            float _curveAngleMax, float _branchProbability, float _branchAngleMax, int _branchDepthMax,
            float _branchProbabilityDecay, float _weldThreshold, bool _showDebug)
        {
            villageGen = _villageGen;
            seed = _seed;
            mapSize = _mapSize;
            stepSize = _stepSize;
            roadWidth = _roadWidth;
            inertia = _inertia;
            curveAngleMax = _curveAngleMax;
            branchProbability = _branchProbability;
            branchAngleMax = _branchAngleMax;
            branchDepthMax = _branchDepthMax;
            branchProbabilityDecay = _branchProbabilityDecay;
            weldThreshold = _weldThreshold;
            showDebug = _showDebug;

            Random.InitState(seed);

            mapBoundsMin = new Vector2(transform.position.x, transform.position.z);
            mapBoundsMax = mapBoundsMin + mapSize;

            vertices = new List<List<Vertex>>();
            edges = new List<List<Edge>>();

            List<Vertex> queue = new List<Vertex>();
            roadOrigins = new List<Vector3>();

            for (int i = 0; i < transform.childCount; i++)
            {
                roadOrigins.Add(transform.GetChild(i).position);

                Vector3 roadDirection = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1)).normalized;
                if (roadDirection == Vector3.zero) roadDirection = new Vector3(1, 0, 1).normalized;

                Vertex origin = new Vertex(roadOrigins[i], roadDirection);
                origin.branchID = i;
                origin.vertexID = 0;
                origin.branchDepth = 0;
                origin.branchProbability = branchProbability;

                queue.Add(origin);

                vertices.Add(new List<Vertex>() { origin });
                edges.Add(new List<Edge>());
            }

            while (queue.Count > 0)
            {
                List<Vertex> newVerts = new List<Vertex>();

                for (int j = 0; j < queue.Count; j++)
                {
                    Vertex current = queue[j];

                    //Generate next point in current road

                    Vertex nextVert = new Vertex(Vector3.zero, Vector3.zero);
                    
                    bool canPropagate = GetNewPoint(current, out nextVert);
                    canPropagate = canPropagate && Vector3.Dot(villageGen.GetTerrainNormalAt(nextVert.position), Vector3.up) > 0.7f;

                    nextVert.vertexID = current.vertexID + 1;
                    nextVert.branchDepth = current.branchDepth;
                    nextVert.branchID = current.branchID;
                    nextVert.branchProbability = current.branchProbability;

                    if (canPropagate)
                    {
                        vertices[nextVert.branchID].Add(nextVert);
                        edges[nextVert.branchID].Add(new Edge(current, nextVert));

                        newVerts.Add(nextVert);
                    }
                    else
                    {
                        continue;
                    }

                    //Attempt to generate branch

                    if (Random.value > current.branchProbability || current.branchDepth >= branchDepthMax) continue;

                    Vertex nextOrigin = new Vertex(current.position, current.direction);
                    nextOrigin.vertexID = 0;
                    nextOrigin.branchDepth = current.branchDepth + 1;
                    nextOrigin.branchID = vertices.Count;
                    nextOrigin.branchProbability = current.branchProbability * branchProbabilityDecay;

                    canPropagate = GetNewBranchPoint(nextOrigin, out nextVert);

                    nextVert.vertexID = 1;
                    nextVert.branchDepth = current.branchDepth + 1;
                    nextVert.branchID = vertices.Count;
                    nextVert.branchProbability = current.branchProbability * branchProbabilityDecay;

                    if (canPropagate)
                    {
                        nextOrigin.direction = nextVert.direction;

                        vertices.Add(new List<Vertex>());
                        vertices[nextOrigin.branchID].Add(nextOrigin);
                        vertices[nextVert.branchID].Add(nextVert);

                        edges.Add(new List<Edge>());
                        edges[nextVert.branchID].Add(new Edge(nextOrigin, nextVert));

                        //newVerts.Add(nextOrigin);
                        newVerts.Add(nextVert);
                    }
                    else
                    {
                        continue;
                    }
                }

                //vertices.AddRange(newVerts);
                queue = new List<Vertex>(newVerts);
            }

            ApplyTerrainCorrection();
            GenerateMesh();
            SetupColliders();
            AssignLayers();
        }

        private void AssignLayers()
        {
            gameObject.layer = LayerMask.NameToLayer("Road");
        }

        private void SetupColliders()
        {
            MeshCollider roadCollider = GetComponent<MeshCollider>();
            roadCollider.sharedMesh = roadMesh;
            roadCollider.isTrigger = false;
        }

        private void ApplyTerrainCorrection()
        {
            for (int branch = 0; branch < vertices.Count; branch++)
            {
                vertices[branch][0].normal = villageGen.GetTerrainNormalAt(vertices[branch][0].position);
                vertices[branch][0].position = villageGen.ProjectPointToTerrain(vertices[branch][0].position);

                for (int i = 1; i < vertices[branch].Count; i++)
                {
                    vertices[branch][i].normal = villageGen.GetTerrainNormalAt(vertices[branch][i].position);
                    vertices[branch][i].position = villageGen.ProjectPointToTerrain(vertices[branch][i].position);
                    vertices[branch][i].direction =
                        (vertices[branch][i].position - vertices[branch][i - 1].position).normalized;
                }
            }
        }

        private bool GetNewPoint(Vertex currentPoint, out Vertex newPoint)
        {
            Vector3 newDir = Quaternion.Euler(0, Random.Range(-curveAngleMax, curveAngleMax), 0) *
                             currentPoint.direction;
            newDir = currentPoint.direction * inertia + newDir * (1 - inertia);

            newPoint = new Vertex(currentPoint.position + newDir * stepSize, newDir);

            if (!isWithinBounds(newPoint)) return false;
            
            for (int b = 0; b < vertices.Count; b++)
            {
                if (b == currentPoint.branchID) continue;

                for (int i = 0; i < vertices[b].Count; i++)
                {
                    if ((vertices[b][i].position - newPoint.position).sqrMagnitude <
                        (stepSize * stepSize * weldThreshold * weldThreshold))
                    {
                        newPoint = new Vertex(vertices[b][i].position, vertices[b][i].direction);
                        return false;
                    }
                }
            }

            return true;
        }

        private bool GetNewBranchPoint(Vertex currentPoint, out Vertex newPoint)
        {
            Vector3 newDir = Quaternion.Euler(0, Mathf.Sign(Random.value * 2 - 1) * branchAngleMax, 0) *
                             currentPoint.direction;
            //newDir = currentPoint.direction * inertia + newDir * (1 - inertia);

            newPoint = new Vertex(currentPoint.position + newDir * stepSize, newDir);

            if (!isWithinBounds(newPoint)) return false;
            
            for (int b = 0; b < vertices.Count; b++)
            {
                if (b == currentPoint.branchID) continue;

                for (int i = 0; i < vertices[b].Count; i++)
                {
                    if ((vertices[b][i].position - newPoint.position).sqrMagnitude <
                        (stepSize * stepSize * weldThreshold * weldThreshold))
                    {
                        newPoint = new Vertex(vertices[b][i].position, vertices[b][i].direction);
                        return false;
                    }
                }
            }

            return true;
        }

        private bool isWithinBounds(Vertex point)
        {
            if (point.position.x > mapBoundsMax.x || point.position.x < mapBoundsMin.x ||
                point.position.z > mapBoundsMax.y || point.position.z < mapBoundsMin.y) return false;

            return true;
        }

        private void GenerateMesh()
        {
            List<Vector3> meshVertices = new List<Vector3>();
            List<Vector3> meshNormals = new List<Vector3>();
            List<int> meshTris = new List<int>();
            List<Vector2> meshUvs = new List<Vector2>();

            //The index of the next free slot in the mesh vertex data, used while storing triangulation data
            int meshVertIndex = 0;

            for (int branch = 0; branch < vertices.Count; branch++)
            {
                List<Vertex> currentBranchVerts = vertices[branch];
                List<Edge> currentBranchEdges = edges[branch];

                if (currentBranchEdges.Count == 0) break;

                //Generate each branch as a separate strip of quads
                //Add the first 2 generated vertices separately

                Vertex p1 = currentBranchEdges[0].start;
                Vector3 v1 = p1.position + p1.Tangent * (roadWidth * 0.5f);
                Vector3 v2 = p1.position + p1.ReverseTangent * (roadWidth * 0.5f);

                v1 = villageGen.ProjectPointToTerrain(v1);
                v2 = villageGen.ProjectPointToTerrain(v2);

                meshVertices.AddRange(new Vector3[] { v1, v2 });
                meshNormals.AddRange(new Vector3[] { villageGen.GetTerrainNormalAt(v1), villageGen.GetTerrainNormalAt(v2) });
                meshUvs.AddRange(new Vector2[] { Vector2.up, Vector2.zero });

                //Debug.Log(p1.normal);

                for (int j = 1; j < currentBranchEdges.Count; j++)
                {
                    Edge currentEdge = currentBranchEdges[j];
                    Vertex p2 = currentEdge.end;

                    Vector3 v3 = p2.position + p2.Tangent * (roadWidth * 0.5f);
                    Vector3 v4 = p2.position + p2.ReverseTangent * (roadWidth * 0.5f);

                    v3 = villageGen.ProjectPointToTerrain(v3);
                    v4 = villageGen.ProjectPointToTerrain(v4);

                    meshVertices.AddRange(new Vector3[] { v3, v4 });
                    meshTris.AddRange(new int[]
                    {
                        meshVertIndex, meshVertIndex + 1, meshVertIndex + 2, meshVertIndex + 1, meshVertIndex + 3,
                        meshVertIndex + 2
                    });
                    meshNormals.AddRange(new Vector3[] { villageGen.GetTerrainNormalAt(v3), villageGen.GetTerrainNormalAt(v4) });
                    meshUvs.AddRange(new Vector2[]
                        { new Vector2(j * stepSize / roadWidth, 1), new Vector2(j * stepSize / roadWidth, 0) });

                    meshVertIndex += 2;
                }

                meshVertIndex += 2;
            }

            roadMesh = new Mesh();
            roadMesh.name = "RoadMesh";
            meshFilter = GetComponent<MeshFilter>();

            roadMesh.SetVertices(meshVertices);
            roadMesh.SetTriangles(meshTris, 0);
            roadMesh.SetNormals(meshNormals);
            roadMesh.SetUVs(0, meshUvs);

            meshFilter.mesh = roadMesh;
        }

        private void OnDrawGizmos()
        {
            if (vertices == null) return;

            if (!showDebug) return;

            foreach (List<Vertex> list in vertices)
            {
                if (list == null) continue;

                foreach (Vertex vertex in list)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(vertex.position, 5);
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(vertex.position, vertex.normal * 5f);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(vertex.position, vertex.direction * 5f);
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(vertex.position, vertex.Tangent * 5f);
                    if (showIndices) Handles.Label(vertex.position, vertex.vertexID.ToString());
                }
            }

            if (edges == null) return;
            Gizmos.color = Color.green;

            foreach (List<Edge> list in edges)
            {
                foreach (Edge ed in list)
                {
                    Gizmos.DrawLine(ed.start.position, ed.end.position);
                }
            }
        }
    }
}
