using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Methods related to rendering GeoJSON objects
/// </summary>
public class GeoJsonRenderer
{
    /// <summary>
    /// Render a node with given coordinates as a child of given feature object
    /// </summary>
    /// <param name="feature">The parent feature</param>
    /// <param name="coordinates">The node coordinates</param>
    /// <param name="properties">The layer rendering properties</param>
    public static void RenderNode(GameObject feature, Position coordinates, RenderingProperties properties)
    {
        // Setup the gameobject
        GameObject node = new GameObject("Node"); // Create Node gameobject
        node.transform.parent = feature.transform; // Set it as a child of the Feature gameobject

        // Setup the mesh components
        MeshRenderer meshRenderer = node.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = node.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        // Setup vertices
        double x = coordinates.GetWorldX(properties);
        double y = coordinates.GetWorldZ(properties); // GeoJSON uses z for height, while Unity uses y
        double z = coordinates.GetWorldY(properties); // GeoJSON uses z for height, while Unity uses y
        Vector3[] vertices = new Vector3[5] // TODO we're being greedy with vertices here and that means the normals will be messed up since we're sharing vertices between differently oriented faces
        {
            new Vector3((float)x, (float)y, (float)z),
            new Vector3((float)(x + properties.nodeRadius), (float)(y + properties.nodeHeight), (float)(z + properties.nodeRadius)),
            new Vector3((float)(x - properties.nodeRadius), (float)(y + properties.nodeHeight), (float)(z + properties.nodeRadius)),
            new Vector3((float)(x - properties.nodeRadius), (float)(y + properties.nodeHeight), (float)(z - properties.nodeRadius)),
            new Vector3((float)(x + properties.nodeRadius), (float)(y + properties.nodeHeight), (float)(z - properties.nodeRadius))
        };
        mesh.vertices = vertices;

        // Setup triangles
        int[] triangles = new int[18] // 6 * 3
        {
            0,1,2,
            0,2,3,
            0,3,4,
            0,4,1,
            1,4,3,
            1,3,2
        };
        mesh.triangles = triangles;

        // Assign mesh
        mesh.RecalculateNormals();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
        meshFilter.mesh = mesh;
    }

    /// <summary>
    /// Render an edge with given coordinates as a child of given feature object
    /// </summary>
    /// <param name="feature">The parent feature</param>
    /// <param name="coordinates">The edge coordinates</param>
    /// <param name="properties">The layer rendering properties</param>
    public static void RenderEdge(GameObject feature, Position[] coordinates, RenderingProperties properties)
    {
        // Setup the gameobject
        GameObject edge = new GameObject("Edge"); // Create Edge gameobject
        edge.transform.parent = feature.transform; // Set it as a child of the Feature gameobject

        // Setup the mesh components
        MeshRenderer meshRenderer = edge.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = edge.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        // Setup vertices
        int numSegments = coordinates.Length - 1; // Number of segments in the line
        Vector3[] vertices = new Vector3[numSegments * 4]; // Needs 4 vertices per line segment
        for (int segment = 0; segment < numSegments; segment++)
        {
            // Start point of segment AB
            double ax = coordinates[segment].GetWorldX(properties);
            double ay = coordinates[segment].GetWorldZ(properties); // GeoJSON uses z for height, while Unity uses y
            double az = coordinates[segment].GetWorldY(properties); // GeoJSON uses z for height, while Unity uses y

            // End point of segment AB
            double bx = coordinates[segment + 1].GetWorldX(properties);
            double by = coordinates[segment + 1].GetWorldZ(properties); // GeoJSON uses z for height, while Unity uses y
            double bz = coordinates[segment + 1].GetWorldY(properties); // GeoJSON uses z for height, while Unity uses y

            // Calculate AB
            double abX = bx - ax;
            double abZ = bz - az;
            double abMagnitude = Math.Sqrt(Math.Pow(abX, 2) + Math.Pow(abZ, 2));

            // AB⟂ with given width
            double abPerpX = (properties.edgeWidth * -abZ) / abMagnitude;
            double abPerpZ = (properties.edgeWidth * abX) / abMagnitude;

            // Add vertices
            vertices[(segment * 4) + 0] = new Vector3((float)(ax - abPerpX), (float)ay, (float)(az - abPerpZ));
            vertices[(segment * 4) + 1] = new Vector3((float)(ax + abPerpX), (float)ay, (float)(az + abPerpZ));
            vertices[(segment * 4) + 2] = new Vector3((float)(bx - abPerpX), (float)by, (float)(bz - abPerpZ));
            vertices[(segment * 4) + 3] = new Vector3((float)(bx + abPerpX), (float)by, (float)(bz + abPerpZ));
        }
        mesh.vertices = vertices;

        // Setup triangles
        int[] triangles = new int[numSegments * 6]; // numSegments * 2 * 3
        for (int segment = 0; segment < numSegments; segment++)
        {
            triangles[(segment * 6) + 0] = (segment * 4) + 0;
            triangles[(segment * 6) + 1] = (segment * 4) + 1;
            triangles[(segment * 6) + 2] = (segment * 4) + 3;

            triangles[(segment * 6) + 3] = (segment * 4) + 0;
            triangles[(segment * 6) + 4] = (segment * 4) + 3;
            triangles[(segment * 6) + 5] = (segment * 4) + 2;
        }
        mesh.triangles = triangles;

        // Assign mesh
        mesh.RecalculateNormals();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
        meshFilter.mesh = mesh;
    }

    /// <summary>
    /// Render an area with given coordinates as a child of given feature object
    /// </summary>
    /// <param name="feature">The parent feature</param>
    /// <param name="coordinates">The area coordinates</param>
    /// <param name="properties">The layer rendering properties</param>
    public static void RenderArea(GameObject feature, Position[][] coordinates, RenderingProperties properties)
    {
        // Setup the gameobject
        GameObject area = new GameObject("Area"); // Create Area gameobject
        area.transform.parent = feature.transform; // Set it as a child of the Feature gameobject

        // Check for empty coordinates array
        if (coordinates.Length == 0)
        {
            Logger.LogWarning($"{feature.name}: Tried to render an Area with no coordinates");
            return;
        }

        // Triangulate the polygon coordinates using Earcut
        EarcutLib.Data data = EarcutLib.Flatten(coordinates, properties);
        List<int> triangles = EarcutLib.Earcut(data.Vertices, data.Holes, data.Dimensions);
        /*double deviation = EarcutLib.Deviation(data.Vertices, data.Holes, data.Dimensions, triangles);
        Logger.Log(deviation == 0 ? "The triangulation is fully correct" : $"Triangulation deviation: {Math.Round(deviation, 6)}"); TODO clear this */

        // Setup the mesh components
        MeshRenderer meshRenderer = area.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = area.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        // Setup vertices
        Vector3[] vertices = new Vector3[data.Vertices.Count / data.Dimensions];
        for (int i = 0; i < data.Vertices.Count / data.Dimensions; i++)
        {
            double x = data.Vertices[i * data.Dimensions];
            double y = data.Vertices[(i * data.Dimensions) + 2];   // GeoJSON uses z for height, while Unity uses y
            double z = data.Vertices[(i * data.Dimensions) + 1];   // GeoJSON uses z for height, while Unity uses y
            vertices[i] = new Vector3((float)x, (float)y, (float)z);
        }
        mesh.vertices = vertices;

        // Setup triangles
        triangles.Reverse();
        mesh.triangles = triangles.ToArray();

        // Assign mesh
        mesh.RecalculateNormals();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
        meshFilter.mesh = mesh;

        // TODO we're getting an extra vertex because GeoJSON polygon's line rings loop around, should we cut it?
        // Logger.Log($"Mesh>Vertices:{meshFilter.mesh.vertexCount},Triangles:{meshFilter.mesh.triangles.Length / 3},Normals:{meshFilter.mesh.normals.Length}");
    }
}