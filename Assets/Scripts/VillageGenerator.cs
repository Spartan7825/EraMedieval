using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace QuantumCookie
{
    [System.Serializable]
    public class GeneratorData
    {
        [Header("Generator")] public int seed = 293744;
        public float stepSize = 20f;
        [Range(0, 1)] public float inertia = 0.8f;
        [Range(0, 20)] public float curveAngleMax = 10f;
        [Range(0, 0.5f)] public float branchProbability = 0.05f;
        [Range(60, 120)] public float branchAngleMax = 90f;
        [Range(1, 6)] public int branchDepthMax = 3;
        [Range(0, 1)] public float branchProbabilityDecay = 0.5f;
        [Range(0, 1)] public float weldThreshold = 0.5f;

        [Header("Mesh Attributes")] public float width = 6f;
        [Header("Debug")] public bool showDebug = true;
    }
    
    [ExecuteInEditMode]
    public class VillageGenerator : MonoBehaviour
    {
        public RiverGenerator riverGenerator;
        public RoadGenerator roadGenerator;
     
        [Header("Map")] public Vector2 mapSize;
        
        [Header("House Settings")] 
        public int seed = 2343255;
        public float houseProbability = 0.3f;
        [Range(1, 10)] public int length;
        [Range(1, 10)] public int breadth;
        public float tileSize = 3f;

        [Range(0, 1)] public float growthProbability = 0.5f;
        
        [Space]
        
        public GameObject wallPrefab;
        public GameObject floorPrefab;
        public GameObject pillarPrefab;
        public GameObject doorPrefab;
        public GameObject windowPrefab;
        public GameObject doubleRoofPrefab;
        public GameObject singleRoofPrefab;

        [Space] public GeneratorData riverGeneratorData;
        [FormerlySerializedAs("roadGeneratorDataa")] [Space] public GeneratorData roadGeneratorData;
        
        [Header("Debug")]
        public bool showDebug = false;
        public bool regenerate = false;
        public float debugSphereSize = 20f;

        private List<ProceduralHouse> houses;
        private void Update()
        {
            //noiseGen = new NoiseGenerator(seed, mapWidth, mapHeight, noiseScale, noiseFrequency);
            
            if (regenerate)
            {
                regenerate = false;
                riverGenerator.RegenerateRiver(this,
                    riverGeneratorData.seed, mapSize, riverGeneratorData.stepSize, riverGeneratorData.width, riverGeneratorData.inertia,
                    riverGeneratorData.curveAngleMax, riverGeneratorData.branchProbability, riverGeneratorData.branchAngleMax,
                    riverGeneratorData.branchDepthMax, riverGeneratorData.branchProbabilityDecay, riverGeneratorData.weldThreshold, riverGeneratorData.showDebug
                    );
                roadGenerator.RegenerateRoad(this,
                    roadGeneratorData.seed, mapSize, roadGeneratorData.stepSize, roadGeneratorData.width, roadGeneratorData.inertia,
                    roadGeneratorData.curveAngleMax, roadGeneratorData.branchProbability, roadGeneratorData.branchAngleMax,
                    roadGeneratorData.branchDepthMax, roadGeneratorData.branchProbabilityDecay, roadGeneratorData.weldThreshold, roadGeneratorData.showDebug
                );
                RegenerateHouses();
            }
        }

        public bool RoadIntersectsRiver(Edge edge, float roadWidth)
        {
            return riverGenerator.IntersectsRiver(edge, roadWidth);
        }
        
        public void RegenerateHouses()
        {
            Random.InitState(seed);
            
            ProceduralHouse[] previousHouses = GetComponentsInChildren<ProceduralHouse>();
            for(int i = 0; i < previousHouses.Length; i++) DestroyImmediate(previousHouses[i].gameObject);

            houses = new List<ProceduralHouse>();
            
            List<List<Edge>> riverEdges = riverGenerator.Edges;
            List<List<Edge>> roadEdges = roadGenerator.Edges;

            for (int branch = 0; branch < riverEdges.Count; branch++)
            {
                GenerateAlongBranch(riverEdges[branch], branch, riverGeneratorData.width);
            }
            
            for (int branch = 0; branch < roadEdges.Count; branch++)
            {
                GenerateAlongBranch(roadEdges[branch], branch, roadGeneratorData.width);
            }

            int iterations = 10;
            while (iterations-- > 0)
            {
                Vector3 point = new Vector3(Random.Range(transform.position.x, transform.position.x + mapSize.x), 100f,
                    Random.Range(transform.position.z, transform.position.z + mapSize.y));
                GenerateAtFoci(point, 50f, 100);
            }
        }

        private void GenerateAtFoci(Vector3 origin, float radius, int iterations)
        {
            while (iterations-- > 0)
            {
                if(Random.value > houseProbability) continue;

                Vector2 rndCircle = Random.insideUnitCircle * radius;
                Vector3 point = origin + new Vector3(rndCircle.x, 0f, rndCircle.y);

                if (IsPointOutOfBounds(point)) continue;
                
                point = ProjectPointToTerrain(point);

                Vector3 upVector = GetTerrainNormalAt(point);
                Vector3 awayFrom = (point - ProjectPointToTerrain(origin)).normalized;
                
                CreateHouseAt(point, awayFrom, houses.Count, upVector, true);
            }
        }
        
        private void GenerateAlongBranch(List<Edge> edges, int branch, float width)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if(Random.value > houseProbability) continue;
                    
                Edge edge = edges[i];

                Vector3 midPoint = (edge.start.position + edge.end.position) * 0.5f;
                Vector3 pointA = midPoint + edge.Tangent * (width * 0.6f);
                Vector3 pointB = midPoint + edge.ReverseTangent * (width * 0.6f);
                
                CreateHouseAt(pointA, edge.Tangent, (branch + 1) * i, edge.Normal, true);
                CreateHouseAt(pointB, edge.ReverseTangent, (branch + 1) * i, edge.Normal, true);
            }
        }

        private void CreateHouseAt(Vector3 point, Vector3 awayFromPath, int id, Vector3 upVector, bool followRoads)
        {
            //Too steep
            if(Vector3.Dot(upVector, Vector3.up) < 0.7f) return;
            
            int houseLength = Random.Range(1, length);
            int houseBreadth = Random.Range(1, breadth);
            
            GameObject newHouse = new GameObject("House" + id);
            newHouse.transform.SetParent(this.transform);
            newHouse.transform.SetPositionAndRotation(point, Quaternion.identity);
                    
            ProceduralHouse houseComp = newHouse.AddComponent<ProceduralHouse>();
            houseComp.wallPrefab = wallPrefab;
            houseComp.floorPrefab = floorPrefab;
            houseComp.pillarPrefab = pillarPrefab;
            houseComp.doorPrefab = doorPrefab;
            houseComp.windowPrefab = windowPrefab;
            houseComp.doubleRoofPrefab = doubleRoofPrefab;
            houseComp.singleRoofPrefab = singleRoofPrefab;

            houseComp.Generate(Random.Range(1000000, 9999999), length, breadth, tileSize, growthProbability);
            
            if(followRoads)
            {
                newHouse.transform.SetPositionAndRotation(point,
                    Quaternion.AngleAxis(-90, upVector) * Quaternion.LookRotation(awayFromPath, upVector));
            }

            if (DoesHouseOverlapOtherHouse(newHouse, houseComp))
            {
                DestroyImmediate(newHouse);
                return;
            }
            
            houses.Add(houseComp);
        }

        private bool DoesHouseOverlapOtherHouse(GameObject house, ProceduralHouse houseComp)
        {
            Vector3 oldPos = house.transform.position;
            Vector3 lengthwise = house.transform.right;
            Vector3 breadthwise = house.transform.forward;
            Vector3 upVector = house.transform.up;

            int roadLayer = LayerMask.NameToLayer("Road");
            int riverLayer = LayerMask.NameToLayer("River");
            int houseLayer = LayerMask.NameToLayer("House");
            int layers = roadLayer | riverLayer | houseLayer;
            
            for (int i = 0; i <= houseComp.Length; i++)
            {
                for (int j = 0; j <= houseComp.Breadth; j++)
                {
                    Vector3 pos = oldPos + lengthwise * (i * tileSize) + breadthwise * (j * tileSize) + upVector * (tileSize * 0.5f);

                    //Check house overlaps
                    
                    Collider[] col = Physics.OverlapSphere(pos, tileSize * 2, 1 << houseLayer);
                    
                    for(int k = 0; k < col.Length; k++)
                    {
                        ProceduralHouse other = col[0].GetComponentInParent<ProceduralHouse>();
                        if (other.Equals(houseComp)) continue;
                        
                        //Debug.LogFormat("{0} at {1} overlaps with {2} at {3}", house.name, house.transform.position, other.gameObject.name, other.transform.position);
                        return true;
                    }

                    RaycastHit hit;
                    if (Physics.Raycast(pos + upVector * tileSize, -house.transform.up, out hit, 50f, (1 << roadLayer) | (1 << riverLayer)))
                    {
                        //Debug.LogFormat("{0} hit a {1}", house.name, hit.transform.name);
                        return true;
                    }
                }
            }
            
            return false;
        }

        public Vector3 ProjectPointToTerrain(Vector3 worldPoint)
        {
            worldPoint.y = Terrain.activeTerrain.SampleHeight(worldPoint) + 0.09f;
            return worldPoint;
        }

        public Vector3 GetTerrainNormalAt(Vector3 worldPoint)
        {
            Vector3 localPoint = Terrain.activeTerrain.transform.InverseTransformPoint(worldPoint);
            Vector3 terrainSize = Terrain.activeTerrain.terrainData.size;

            return Terrain.activeTerrain.terrainData.GetInterpolatedNormal(localPoint.x / terrainSize.x,
                localPoint.z / terrainSize.z);
        }

        private bool IsPointOutOfBounds(Vector3 point)
        {
            return point.x < transform.position.x || point.x > transform.position.x + mapSize.x ||
                   point.z < transform.position.z || point.z > transform.position.z + mapSize.y;
        }
        
        private void OnDrawGizmos()
        {
            
        }
    }
}
