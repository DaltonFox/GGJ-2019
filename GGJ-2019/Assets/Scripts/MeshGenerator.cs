using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeshGenerator : MonoBehaviour
{
    // Corner Switches = Control Nodes
    // Middle Points = Nodes

    public SquareGrid squareGrid;
    public MeshFilter walls;
    public bool is2D;

    List<Vector3> vertices;
    List<int> triangles;
    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();

    public void GenerateMesh(MapGenerator.TileType[,] map, float squareSize)
    {
        squareGrid = new SquareGrid(map, squareSize);

        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        //HashSet<int> seen = new HashSet<int>();
        //System.Random r = new System.Random(DateTime.UtcNow.Millisecond);
        //for(int i = 0; i < triangles.Count / 3; i++)
        //{
        //    float amt = r.Next(0, 1000) / 2000.0f;
        //    int a = triangles[i * 3 + 0];
        //    int b = triangles[i * 3 + 1];
        //    int c = triangles[i * 3 + 2];

        //    if (seen.Contains(a) || seen.Contains(b) || seen.Contains(c))
        //    {
        //        continue;
        //    }

        //    seen.Add(a);
        //    seen.Add(b);
        //    seen.Add(c);

        //    float zAmt = amt * (r.Next(0, 1) == 1 ? 1 : -1);
            
        //    Vector3 y = vertices[a];
        //    //y.z += zAmt;
        //    vertices[a] = y;

        //    y = vertices[b];
        //    //y.z += zAmt;
        //    vertices[b] = y;

        //    y = vertices[c];
        //    //y.z += zAmt;
        //    vertices[c] = y;
        //}

//        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\miner\Desktop\GGJ-2019\debugModel.obj"))
//        {
//            foreach (Vector3 vertex in vertices)
//            {
//                file.WriteLine("v " + vertex.x + " " + vertex.y + " " + vertex.z);
//            }
//
//            for (int i = 0; i < triangles.Count / 3; i++)
//            {
//                int a = triangles[i * 3 + 0] + 1;
//                int b = triangles[i * 3 + 1] + 1;
//                int c = triangles[i * 3 + 2] + 1;
//                file.WriteLine("f " + a + " " + b + " " + c);
//            }
//        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        if (is2D)
        {
            Generate2DColliders();
        }
        else
        {
            CreateWallMesh();
        }
    }

    void Generate2DColliders()
    {
        CalculateMeshOutlines();

        // 2D Collision Style
        EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();
        for (int i = 0; i < currentColliders.Length; i++)
        {
            Destroy(currentColliders[i]);
        }

        foreach (List<int> outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];
            for (int i = 0; i < outline.Count; i++)
            {
                edgePoints[i] = vertices[outline[i]];
            }
            edgeCollider.points = edgePoints;
        }
    }

    void CreateWallMesh()
    {
        CalculateMeshOutlines();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = -5;

        foreach (List<int> outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]); // left vertex
                wallVertices.Add(vertices[outline[i + 1]]); // right vertex
                wallVertices.Add(vertices[outline[i]] + Vector3.forward * wallHeight); // bottom left vertex
                wallVertices.Add(vertices[outline[i + 1]] + Vector3.forward * wallHeight); // bottom right vertex

                // triangle 1
                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                // triangle 2
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;
        wallMesh.RecalculateNormals();
        

        // 3D Collision Style
        MeshCollider[] colliders = walls.gameObject.GetComponents<MeshCollider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;
        wallCollider.material = GameObject.Find("Collision Point").GetComponent<SphereCollider>().material;
    }

    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            // 0 points
            case 0:
                break;

            // 1 points
            case 1:
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;

            // 2 points
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5:
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;
            
            // 3 points
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 4 points
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);
        for (int i = 3; i < 7; i++)
        {
            if (points.Length >= i)
            {
                CreateTriangle(points[0], points[i - 2], points[i - 1]);
            }
        }
    }

    void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if (triangleDictionary.ContainsKey(vertexIndexKey))
        {
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            triangleDictionary.Add(vertexIndexKey, new List<Triangle> { triangle });
        }
    }

    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertexIndex = triangleDictionary[vertexIndex];
        for (int i = 0; i < trianglesContainingVertexIndex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertexIndex[i];
            for (int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    void CalculateMeshOutlines()
    {
        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);
                    outlines.Add(new List<int> { vertexIndex });
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        for(;;)
        {
            outlines[outlineIndex].Add(vertexIndex);
            checkedVertices.Add(vertexIndex);
            int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);
            if (nextVertexIndex != -1)
            {
                vertexIndex = nextVertexIndex;
                continue;
            }

            break;
        }
    }

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;
        for (int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount++;
            }

            if (sharedTriangleCount > 1)
            {
                break;
            }
        }

        return sharedTriangleCount == 1;
    }

    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        private int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;
            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i]
        {
            get { return vertices[i]; }
        }

        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(MapGenerator.TileType[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];
            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    Vector3 position = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, -mapHeight / 2 + y * squareSize + squareSize / 2, 0);
                    controlNodes[x, y] = new ControlNode(position, map[x, y] == MapGenerator.TileType.Floor, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x, y], controlNodes[x + 1, y]);
                }
            }
        }
    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomLeft, bottomRight;
        public Node centerTop, centerRight, centerBottom, centerLeft;
        public int configuration;

        public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomLeft, ControlNode bottomRight)
        {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomLeft = bottomLeft;
            this.bottomRight = bottomRight;

            centerTop = this.topLeft.right;
            centerRight = this.bottomRight.above;
            centerBottom = this.bottomLeft.right;
            centerLeft = this.bottomLeft.above;

            if (this.topLeft.active)
            {
                configuration += 8;
            }
            if (this.topRight.active)
            {
                configuration += 4;
            }
            if (this.bottomRight.active)
            {
                configuration += 2;
            }
            if (this.bottomLeft.active)
            {
                configuration += 1;
            }
        }
    }

    public class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 position)
        {
            this.position = position;
        }
    }

    public class ControlNode : Node
    {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 position, bool active, float squareSize) : base(position)
        {
            this.active = active;

            above = new Node(position + Vector3.up * squareSize / 2f);
            right = new Node(position + Vector3.right * squareSize / 2f);
        }

    }
}
