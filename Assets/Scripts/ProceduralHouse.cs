using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace QuantumCookie
{
    [ExecuteInEditMode]

    public class ProceduralHouse : MonoBehaviour
    {
        private const int BLANK = 0;
        private const int FLOOR = 1;
        private const int ROOF = 5;

        private int seed;
        private int length;
        private int breadth;
        private float tileSize = 3f;

        private float growthProbability = 0.5f;

        [HideInInspector] public GameObject wallPrefab;
        [HideInInspector] public GameObject floorPrefab;
        [HideInInspector] public GameObject pillarPrefab;
        [HideInInspector] public GameObject doorPrefab;
        [HideInInspector] public GameObject windowPrefab;
        [HideInInspector] public GameObject singleRoofPrefab;
        [HideInInspector] public GameObject doubleRoofPrefab;

        private int[,] floorGrid, roofGrid;
        private int floorCount;

        public int Length => length;
        public int Breadth => breadth;
        
        public Vector3 HouseFacing { get; private set; }

        public void Generate(int _seed, int _length, int _breadth, float _tileSize, float _growthProbability)
        {
            seed = _seed;
            length = _length;
            breadth = _breadth;
            tileSize = _tileSize;
            growthProbability = _growthProbability;
            
            Random.InitState(seed);

            floorGrid = new int[length, breadth];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < breadth; j++)
                {
                    floorGrid[i, j] = BLANK;
                }
            }
            
            //Clear previous children
            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++) children.Add(transform.GetChild(i).gameObject);
            for (int i = 0; i < children.Count; i++) DestroyImmediate(children[i]);


            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < breadth; j++) floorGrid[i, j] = BLANK;
            }

            GenerateFloors();
            GenerateWalls();
            GenerateRoof();

            gameObject.layer = LayerMask.NameToLayer("House");
        }

        private void GenerateRoof()
        {
            roofGrid = new int[length, breadth];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < breadth; j++)
                {
                    roofGrid[i, j] = floorGrid[i, j];
                }
            }

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < breadth; j++)
                {
                    if (roofGrid[i, j] == ROOF || roofGrid[i, j] == BLANK) continue;

                    if (i + 1 < length && roofGrid[i + 1, j] == FLOOR)
                    {
                        //Instantiate double roof vertical
                        GameObject doubleRoof = Instantiate(doubleRoofPrefab,
                            transform.position + new Vector3(i, 1f, j) * tileSize, Quaternion.identity, transform);

                        roofGrid[i, j] = roofGrid[i + 1, j] = ROOF;
                    }
                    else if (j + 1 < breadth && roofGrid[i, j + 1] == FLOOR)
                    {
                        //Instantiate double roof horizontal
                        GameObject doubleRoof = Instantiate(doubleRoofPrefab,
                            transform.position + new Vector3(i + 1, 1f, j) * tileSize, Quaternion.Euler(0, -90, 0),
                            transform);

                        roofGrid[i, j] = roofGrid[i, j + 1] = ROOF;
                    }
                    else
                    {
                        //Instantiate single roof
                        GameObject singleRoof = Instantiate(singleRoofPrefab,
                            transform.position + new Vector3(i, 1f, j) * tileSize, Quaternion.Euler(0, -90, 0),
                            transform);

                        roofGrid[i, j] = ROOF;
                    }
                }
            }
        }

        private void GenerateFloors()
        {
            floorCount = 0;
            floorGrid[0, 0] = FLOOR;

            int iterations = Mathf.Max(length, breadth);

            while (iterations-- > 0)
            {
                for (int i = 0; i < length; i++)
                {
                    for (int j = 0; j < breadth; j++)
                    {
                        if (floorGrid[i, j] != BLANK) continue;
                        if (Random.value > growthProbability) continue;

                        if (i > 0 && floorGrid[i - 1, j] == FLOOR) floorGrid[i, j] = FLOOR;
                        else if (j > 0 && floorGrid[i, j - 1] == FLOOR) floorGrid[i, j] = FLOOR;

                    }
                }
            }

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < breadth; j++)
                {
                    if (floorGrid[i, j] == BLANK) continue;

                    if (floorGrid[i, j] == FLOOR)
                    {
                        GameObject floor = Instantiate(floorPrefab,
                            transform.position + new Vector3(i * tileSize, 0, j * tileSize), Quaternion.identity);
                        floor.transform.SetParent(this.gameObject.transform);
                        floorCount++;
                    }

                }
            }
        }

        private void GenerateWalls()
        {
            int windowCount = 2 * floorCount - 1;
            float windowProbability = 0.4f;

            int doorCount = 1;

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < breadth; j++)
                {
                    var up = GetCell(i - 1, j);
                    var left = GetCell(i, j - 1);
                    var down = GetCell(i + 1, j);
                    var right = GetCell(i, j + 1);
                    var current = floorGrid[i, j];

                    if (current == BLANK)
                    {
                        //Generate pillars at inner corners

                        //TOP-LEFT
                        if (up == FLOOR && left == FLOOR)
                        {
                            GameObject pillar = Instantiate(pillarPrefab,
                                transform.position + new Vector3(i, 0, j) * tileSize, Quaternion.identity, transform);
                        }

                        //TOP-RIGHT
                        if (up == FLOOR && right == FLOOR)
                        {
                            GameObject pillar = Instantiate(pillarPrefab,
                                transform.position + new Vector3(i, 0, j + 1) * tileSize, Quaternion.identity,
                                transform);
                        }

                        //BOTTOM-LEFT
                        if (down == FLOOR && left == FLOOR)
                        {
                            GameObject pillar = Instantiate(pillarPrefab,
                                transform.position + new Vector3(i + 1, 0, j) * tileSize, Quaternion.identity,
                                transform);
                        }

                        //BOTTOM-RIGHT
                        if (down == FLOOR && right == FLOOR)
                        {
                            GameObject pillar = Instantiate(pillarPrefab,
                                transform.position + new Vector3(i + 1, 0, j + 1) * tileSize, Quaternion.identity,
                                transform);
                        }
                    }
                    else if (current == FLOOR)
                    {
                        GameObject wall;
                        Vector3 wallPos;
                        Quaternion wallRot;

                        //UP
                        if (IsBlankOrOutOfBounds(up))
                        {
                            wallPos = transform.position + new Vector3(i * tileSize, 0, j * tileSize);
                            wallRot = Quaternion.identity;

                            if (doorCount > 0)
                            {
                                wall = Instantiate(doorPrefab, wallPos, wallRot, transform);
                                HouseFacing = doorPrefab.transform.forward;
                                doorCount--;
                            }
                            else if (windowCount > 0 && Random.value < windowProbability)
                            {
                                wall = Instantiate(windowPrefab, wallPos, wallRot, transform);
                            }
                            else
                            {
                                wall = Instantiate(wallPrefab, wallPos, wallRot, transform);
                            }

                        }

                        //LEFT
                        if (IsBlankOrOutOfBounds(left))
                        {
                            wallPos = transform.position + new Vector3((i + 1) * tileSize, 0, j * tileSize);
                            wallRot = Quaternion.Euler(0, -90, 0);

                            if (windowCount > 0 && Random.value < windowProbability)
                            {
                                wall = Instantiate(windowPrefab, wallPos, wallRot, transform);
                            }
                            else
                            {
                                wall = Instantiate(wallPrefab, wallPos, wallRot, transform);
                            }

                        }

                        //DOWN
                        if (IsBlankOrOutOfBounds(down))
                        {
                            wallPos = transform.position + new Vector3((i + 1) * tileSize, 0, (j + 1) * tileSize);
                            wallRot = Quaternion.Euler(0, 180, 0);

                            if (windowCount > 0 && Random.value < windowProbability)
                            {
                                wall = Instantiate(windowPrefab, wallPos, wallRot, transform);
                            }
                            else
                            {
                                wall = Instantiate(wallPrefab, wallPos, wallRot, transform);
                            }
                        }

                        //RIGHT
                        if (IsBlankOrOutOfBounds(right))
                        {
                            wallPos = transform.position + new Vector3(i * tileSize, 0, (j + 1) * tileSize);
                            wallRot = Quaternion.Euler(0, 90, 0);

                            if (windowCount > 0 && Random.value < windowProbability)
                            {
                                wall = Instantiate(windowPrefab, wallPos, wallRot, transform);
                            }
                            else
                            {
                                wall = Instantiate(wallPrefab, wallPos, wallRot, transform);
                            }
                        }

                        //Generate pillars at outer corners

                        //TOP-LEFT
                        if (IsBlankOrOutOfBounds(up) && IsBlankOrOutOfBounds(left))
                        {
                            GameObject pillar = Instantiate(pillarPrefab,
                                transform.position + new Vector3(i, 0, j) * tileSize, Quaternion.identity, transform);
                        }

                        //TOP-RIGHT
                        if (IsBlankOrOutOfBounds(up) && IsBlankOrOutOfBounds(right))
                        {
                            GameObject pillar = Instantiate(pillarPrefab,
                                transform.position + new Vector3(i, 0, j + 1) * tileSize, Quaternion.identity,
                                transform);
                        }

                        //BOTTOM-LEFT
                        if (IsBlankOrOutOfBounds(down) && IsBlankOrOutOfBounds(left))
                        {
                            GameObject pillar = Instantiate(pillarPrefab,
                                transform.position + new Vector3(i + 1, 0, j) * tileSize, Quaternion.identity,
                                transform);
                        }

                        //BOTTOM-RIGHT
                        if (IsBlankOrOutOfBounds(down) && IsBlankOrOutOfBounds(right))
                        {
                            GameObject pillar = Instantiate(pillarPrefab,
                                transform.position + new Vector3(i + 1, 0, j + 1) * tileSize, Quaternion.identity,
                                transform);
                        }
                    }
                }
            }
        }

        public List<Vector3> GetTrackingPoints()
        {
            List<Vector3> points = new List<Vector3>();
            
            for (int i = 0; i <= length; i++)
            {
                for (int j = 0; j <= breadth; j++)
                {
                    Vector3 pos = transform.position
                                  + Vector3.right * length * tileSize * i
                                  + Vector3.forward * breadth * tileSize * j;

                    points.Add(pos);
                }
            }

            return points;
        }
        
        private int GetCell(int i, int j) => (i < 0 || i >= length || j < 0 || j >= breadth) ? -1 : floorGrid[i, j];
        private bool IsBlankOrOutOfBounds(int cell) => cell == -1 || cell == BLANK;

        private void OnDrawGizmos()
        {
            Vector3 center = transform.position 
                             + (transform.right * length * tileSize * 0.5f)
                             + (transform.forward * breadth * tileSize * 0.5f) +
                             (transform.up * tileSize * 0.5f);
            
            Gizmos.DrawWireCube(center, new Vector3(breadth, 1, length) * tileSize);
        }
    }
}
