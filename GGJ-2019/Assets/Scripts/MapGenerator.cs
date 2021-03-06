﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Random = System.Random;

public class MapGenerator : MonoBehaviour
{
    public enum TileType
    {
        Floor,
        Wall
    }

    public enum NeighborType
    {
        North,
        South,
        East,
        West
    }

    public int width;
    public int height;

    public string seed;
    public bool useRandomSeed;

    [Range(0, 100)]
    public int randomFillPercent;

    public NavMeshAgent[] mapAgents;

    public int smoothingIterations;
    public int wallThresholdSize;
    public int roomThresholdSize;
    public int borderSize;
    public int passagewayRadius = 1;
    public int specialZoneRadius = 12;

    public Vector3[] roomCenters;

    Coord exitCoord = new Coord(0, 0);

    TileType[,] map;

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        SetAgentStates(false);

        map = new TileType[width, height];
        
        // Maps starts by being filled with noise
        RandomFillMap();

        // Cellular Automaton Smoothing
        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
        }

        // draw the spawn
        DrawCircle(new Coord(width / 2, height / 2), specialZoneRadius);

        // determine map exit location
        Random rng = new Random(seed.GetHashCode());
        int exitPlace = rng.Next(0, 7);
        switch (exitPlace)
        {
            case 0:
                exitCoord.tileX = exitCoord.tileY = specialZoneRadius;
                break;
            case 1:
                exitCoord.tileX = width / 2;
                exitCoord.tileY = specialZoneRadius;
                break;
            case 2:
                exitCoord.tileX = width - specialZoneRadius;
                exitCoord.tileY = specialZoneRadius;
                break;
            case 3:
                exitCoord.tileX = width - specialZoneRadius;
                exitCoord.tileY = height / 2;
                break;
            case 4:
                exitCoord.tileX = width - specialZoneRadius;
                exitCoord.tileY = height - specialZoneRadius;
                break;
            case 5:
                exitCoord.tileX = width / 2;
                exitCoord.tileY = height - specialZoneRadius;
                break;
            case 6:
                exitCoord.tileX = specialZoneRadius;
                exitCoord.tileY = height - specialZoneRadius;
                break;
            case 7:
                exitCoord.tileX = specialZoneRadius;
                exitCoord.tileY = height / 2;
                break;
        }
        // draw the exit location
        DrawCircle(exitCoord, specialZoneRadius);

        // Ensure Connectivity Between Rooms (Longest Part of Map Generation)
        ProcessMap();

        // Add Border to Map to Make Sure the Player Cannot Escape
        TileType[,] borderedMap = new TileType[width + borderSize * 2, height + borderSize * 2];
        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                {
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x, y] = TileType.Wall;
                }
            }
        }

        // DebugMap(borderedMap);

        // Floor Mesh Generation, Marching Squares Smoothing, Wall Mesh Generation
        MeshGenerator meshGenerator = GetComponent<MeshGenerator>();
        meshGenerator.GenerateMesh(borderedMap, 1);

        SetAgentStates(true);
    }

    void SetAgentStates(bool agentEnabled)
    {
        if (mapAgents != null)
        {
            for (int i = 0; i < mapAgents.Length; i++)
            {
                if (mapAgents[i] != null)
                {
                    mapAgents[i].enabled = agentEnabled;
                }
            }
        }
    }

    void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(TileType.Wall);
        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = TileType.Floor;
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions(TileType.Floor);
        List<Room> survivingRooms = new List<Room>();
        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = TileType.Wall;
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));
            }
        }
        if (survivingRooms.Count > 0)
        {
            survivingRooms.Sort();

            survivingRooms[0].isMainRoom = true;
            survivingRooms[0].isAccessibleFromMainRoom = true;

            ConnectClosestRooms(survivingRooms);
            ConnectDeadEndsPass(survivingRooms);

            roomCenters = new Vector3[survivingRooms.Count];
            for(int i = 0; i < survivingRooms.Count; i++)
            {
                roomCenters[i] = CoordToWorldPoint(survivingRooms[i].GetCenterPoint());
            }
        }
    }

    class DistanceThing : IComparable
    {
        public int Distance;
        public Room roomA, roomB;
        public Coord tileA, tileB;

        public DistanceThing(int d, Room ra, Room rb, Coord ta, Coord tb)
        {
            Distance = d;
            roomA = ra;
            roomB = rb;
            tileA = ta;
            tileB = tb;
        }

        public int CompareTo(object obj)
        {
            if (obj is DistanceThing dt)
            {
                return Distance.CompareTo(dt.Distance);
            }
            throw new NotImplementedException();
        }
    }

    void ConnectDeadEndsPass(List<Room> allRooms)
    {
        List<Room> deadEnds = new List<Room>();
        foreach (Room room in allRooms)
        {
            if (room.connectedRooms.Count == 1)
            {
                deadEnds.Add(room);
            }
        }

        foreach (Room deadEnd in deadEnds)
        {
            List<DistanceThing> things = new List<DistanceThing>();
            foreach (Room room in allRooms)
            {
                if(deadEnd == room || deadEnd.IsConnected(room))
                    continue;
                
                Coord tileA = room.GetCenterPoint();
                Coord tileB = deadEnd.GetCenterPoint();

                int p1 = tileA.tileX - tileB.tileX;
                int p2 = tileA.tileY - tileB.tileY;
                p1 *= p1;
                p2 *= p2;

                int distanceSquaredBetweenRooms = p1 + p2;
                things.Add(new DistanceThing(distanceSquaredBetweenRooms, deadEnd, room, tileA, tileB));
            }

            things.Sort();

            int index = 3;
            if (things.Count > index)
            {
                CreatePassage(things[index].roomA, things[index].roomB, things[index].tileA, things[index].tileB);
            }
        }
    }

    void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
    {
        for(;;)
        {
            List<Room> roomListA;
            List<Room> roomListB;

            if (forceAccessibilityFromMainRoom)
            {
                roomListA = new List<Room>();
                roomListB = new List<Room>();

                foreach (Room room in allRooms)
                {
                    if (room.isAccessibleFromMainRoom)
                    {
                        roomListB.Add(room);
                    }
                    else
                    {
                        roomListA.Add(room);
                    }
                }
            }
            else
            {
                roomListA = allRooms;
                roomListB = allRooms;
            }

            int bestDistance = 0;
            Coord bestTileA = new Coord();
            Coord bestTileB = new Coord();
            Room bestRoomA = new Room();
            Room bestRoomB = new Room();
            bool possibleConnectionFound = false;

            foreach (Room roomA in roomListA)
            {
                if (!forceAccessibilityFromMainRoom)
                {
                    possibleConnectionFound = false;
                    if (roomA.connectedRooms.Count > 0)
                    {
                        continue;
                    }
                }

                foreach (Room roomB in roomListB)
                {
                    if (roomA == roomB || roomA.IsConnected(roomB))
                    {
                        continue;
                    }

                    Coord tileA = roomA.GetCenterPoint();
                    Coord tileB = roomB.GetCenterPoint();
                    int p1 = tileA.tileX - tileB.tileX;
                    int p2 = tileA.tileY - tileB.tileY;
                    p1 *= p1;
                    p2 *= p2;
                    
                    int distanceSquaredBetweenRooms = p1 + p2;
                    
                    if (distanceSquaredBetweenRooms < bestDistance || !possibleConnectionFound)
                    {
                        bestDistance = distanceSquaredBetweenRooms;
                        possibleConnectionFound = true;
                        bestTileA = tileA;
                        bestTileB = tileB;
                        bestRoomA = roomA;
                        bestRoomB = roomB;
                    }

                    //                    for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                    //                    {
                    //                        for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    //                        {
                    //                            Coord tileA = roomA.edgeTiles[tileIndexA];
                    //                            Coord tileB = roomB.edgeTiles[tileIndexB];
                    //
                    //                            int p1 = tileA.tileX - tileB.tileX;
                    //                            int p2 = tileA.tileY - tileB.tileY;
                    //                            p1 *= p1;
                    //                            p2 *= p2;
                    //
                    //                            int distanceSquaredBetweenRooms = p1 + p2;
                    //
                    //                            if (distanceSquaredBetweenRooms < bestDistance || !possibleConnectionFound)
                    //                            {
                    //                                bestDistance = distanceSquaredBetweenRooms;
                    //                                possibleConnectionFound = true;
                    //                                bestTileA = tileA;
                    //                                bestTileB = tileB;
                    //                                bestRoomA = roomA;
                    //                                bestRoomB = roomB;
                    //                            }
                    //                        }
                    //                    }
                }

                if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
                {
                    CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
                }
            }

            if (possibleConnectionFound && forceAccessibilityFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
                ConnectClosestRooms(allRooms, true);
            }

            if (!forceAccessibilityFromMainRoom)
            {
                forceAccessibilityFromMainRoom = true;
                continue;
            }

            break;
        }
    }

    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);

        List<Coord> line = GetLine(tileA, tileB);
        foreach (Coord c in line)
        {
            DrawCircle(c, passagewayRadius);
        }
    }

    void DrawCircle(Coord c, int r)
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r * r)
                {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if (IsInMapRange(drawX, drawY))
                    {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }

    List<Coord> GetLine(Coord from, Coord to)
    {
        List<Coord> line = new List<Coord>();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Math.Abs(dx);
        int shortest = Math.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Math.Abs(dy);
            shortest = Math.Abs(dx);
            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x, y));

            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }

                gradientAccumulation -= longest;
            }
        }

        return line;
    }

    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        TileType tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    List<List<Coord>> GetRegions(TileType tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);
                    foreach (Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = DateTime.UtcNow.ToString();
            seed += " " + DateTime.UtcNow.Millisecond + " ms";
        }

        Random rng = new Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = TileType.Wall;
                }
                else
                {
                    map[x, y] = rng.Next(0, 100) < randomFillPercent ? TileType.Wall : TileType.Floor;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallTiles = GetSurroundingWallCount(x, y);
                if (neighborWallTiles > 4)
                {
                    map[x, y] = TileType.Wall;
                }
                else if (neighborWallTiles < 4)
                {
                    map[x, y] = TileType.Floor;
                }
            }
        }
    }

    TileType GetNeighbor(int gridX, int gridY, NeighborType neighborType)
    {
        switch (neighborType)
        {
            case NeighborType.North:
                return IsInMapRange(gridX, gridY + 1) ? map[gridX, gridY + 1] : TileType.Wall;
            case NeighborType.South:
                return IsInMapRange(gridX, gridY - 1) ? map[gridX, gridY - 1] : TileType.Wall;
            case NeighborType.East:
                return IsInMapRange(gridX + 1, gridY) ? map[gridX + 1, gridY] : TileType.Wall;
            case NeighborType.West:
                return IsInMapRange(gridX - 1, gridY) ? map[gridX - 1, gridY] : TileType.Wall;
        }

        throw new NotImplementedException("Should never get here!");
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;

        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                if (IsInMapRange(neighborX, neighborY))
                {
                    if (neighborX != gridX || neighborY != gridY)
                    {
                        if (map[neighborX, neighborY] == TileType.Wall)
                        {
                            wallCount++;
                        }
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "MapGenerationPlayground")
        {
            if (Input.GetMouseButtonDown(0))
            {
                GameObject player = GameObject.Find("DummyPlayer");
                player.GetComponent<NavMeshAgent>().enabled = false;
                player.transform.position = new Vector3(0, 0, -2.5f);
                GenerateMap();
            }

            if (Input.GetMouseButtonDown(1))
            {
                NavMeshAgent nma = GameObject.Find("DummyPlayer").GetComponent<NavMeshAgent>();
                nma.SetDestination(CoordToWorldPoint(exitCoord));
            }
        }
    }

    public Vector3 GetExitLocation()
    {
        return CoordToWorldPoint(exitCoord);
    }

    class Room : IComparable
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        private Coord centerPoint = new Coord(Int32.MaxValue, Int32.MaxValue);

        public Coord GetCenterPoint()
        {
            if (tiles.Count == 0)
            {
                return new Coord(0, 0);
            }

            if (centerPoint.tileX == Int32.MaxValue)
            {
                int x = 0;
                int y = 0;
                foreach (Coord coord in tiles)
                {
                    x += coord.tileX;
                    y += coord.tileY;
                }

                x /= tiles.Count;
                y /= tiles.Count;
                centerPoint = new Coord(x, y);
            }

            return centerPoint;
        }

        public Room()
        {

        }

        public Room(List<Coord> roomTiles, TileType[,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            foreach (Coord tile in tiles)
            {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if (x == tile.tileX || y == tile.tileY)
                        {
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                if (map[x, y] == TileType.Wall)
                                {
                                    edgeTiles.Add(tile);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.isAccessibleFromMainRoom)
            {
                roomB.SetAccessableFromMainRoom();
            }
            else if (roomB.isAccessibleFromMainRoom)
            {
                roomA.SetAccessableFromMainRoom();
            }

            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }

        public void SetAccessableFromMainRoom()
        {
            if (!isAccessibleFromMainRoom)
            {
                isAccessibleFromMainRoom = true;
                foreach (Room connectedRoom in connectedRooms)
                {
                    connectedRoom.SetAccessableFromMainRoom();
                }
            }
        }

        public int CompareTo(object otherObj)
        {
            if (otherObj is Room otherRoom)
            {
                return otherRoom.roomSize.CompareTo(roomSize);
            }

            throw new NotImplementedException("Room can only be compared with Room!");
        }
    }

    void OnDrawGizmos()
    {
        if (roomCenters != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < roomCenters.Length; i++)
            {
                Gizmos.DrawSphere(roomCenters[i], 1);
            }
        }
    }

    void DebugMap(TileType[,] tileMap)
    {
        string contents = "";
        for (int x = 0; x < tileMap.GetLength(0); x++)
        {
            for (int y = 0; y < tileMap.GetLength(1); y++)
            {
                contents += tileMap[x, y] + " ";
            }

            contents += "\n";
        }
        System.IO.File.WriteAllText(@"..\debugMap.txt", contents);
    }

    Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-width / 2f + 0.5f + tile.tileX + transform.position.x, -height / 2f + 0.5f + tile.tileY + transform.position.y, -2);
    }
}
