using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------ POINT CLASS ------
public class Point
{
    public float x;
    public float y;

    public Point(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
}


public enum FustrumPlane
{
    Left, Right, Down, Up, Near, Far
}


// ------ AABB CLASS ------
// In the AABB class, we use the center point and half dimension to reference the area
public class AABB 
{
    public Point p;

    public float halfDimension;

    public AABB(Point XY, float halfDimension)
    {
        this.p = XY;
        this.halfDimension = halfDimension;
    }

    public AABB(float x, float y, float halfDimension)
    {
        this.p = new Point(x, y);
        this.halfDimension = halfDimension;
    }

    public bool Contains(Point point)
    {
        return (p.x - halfDimension <= point.x && // The LEFT HALF has preference
                p.x + halfDimension > point.x &&
                p.y - halfDimension < point.y &&
                p.y + halfDimension >= point.y); // The TOP HALF has preference        
    }



    // Frustum stuff 
    public bool IsOnFrustum(Plane[] frustum, Texture2D heightmap, float displacementStrength)
    {
        float heightValue = heightmap.GetPixel(heightmap.width / 2, heightmap.height / 2).r * displacementStrength;
        return this.IsOnOrForwardPlane(frustum[(int)FustrumPlane.Left], heightValue) &&
            this.IsOnOrForwardPlane(frustum[(int)FustrumPlane.Right], heightValue) &&
            this.IsOnOrForwardPlane(frustum[(int)FustrumPlane.Down], heightValue) &&
            this.IsOnOrForwardPlane(frustum[(int)FustrumPlane.Up], heightValue) &&
            this.IsOnOrForwardPlane(frustum[(int)FustrumPlane.Near], heightValue) &&
            this.IsOnOrForwardPlane(frustum[(int)FustrumPlane.Far], heightValue);
    }

    public bool IsOnOrForwardPlane(Plane plane, float heightValue)
    {
        float r = halfDimension * Mathf.Abs(plane.normal.x) + halfDimension * 2 * Mathf.Abs(plane.normal.y) + halfDimension * Mathf.Abs(plane.normal.z);

        return -r <= plane.GetDistanceToPoint(new Vector3(p.x, heightValue, p.y));
    }
}


// ------ QUADTREE CLASS ------
public class Quadtree
{
    public AABB boundary;
    public int capacity;
    public List<Point> points;

    public Quadtree northWest;
    public Quadtree northEast;
    public Quadtree southWest;
    public Quadtree southEast;

    public bool subdivided = false;

    public Quadtree(AABB boundary, int capacity)
    {
        this.boundary = boundary;

        this.capacity = capacity;

        points = new List<Point>();
    }

    public void Subdivide()
    {
        float x = boundary.p.x;
        float y = boundary.p.y;
        float w = boundary.halfDimension / 2;   // Half the width of the parent


        AABB nw = new AABB(x - w, y + w, w);
        northWest = new Quadtree(nw, capacity);

        AABB ne = new AABB(x + w, y + w, w);
        northEast= new Quadtree(ne, capacity);

        AABB sw = new AABB(x - w, y - w, w);
        southWest = new Quadtree(sw, capacity);

        AABB se = new AABB(x + w, y - w, w);
        southEast = new Quadtree(se, capacity);

        subdivided = true;
    }

    public void Insert(Point point)
    {
        if (!boundary.Contains(point))
        {
            return;
        }

        // The point is inside the boundaries of the QT
        if (points.Count < capacity)   // If the point is still able to go inside the node
        {
            points.Add(point);
        }
        // The point does not fit the node
        else if (!subdivided)
        {
            this.Subdivide();

            northEast.Insert(point);
            northWest.Insert(point);
            southEast.Insert(point);
            southWest.Insert(point);
        }
        else
        {
            northEast.Insert(point);
            northWest.Insert(point);
            southEast.Insert(point);
            southWest.Insert(point);
        }
    }
    
}

// ----------- GRASS QUADTREE ----------------
public class GrassQuadtree : IEquatable<GrassQuadtree>
{
    public AABB boundary;
    public int maxDepth;
    public int currentDepth;
    public bool containsGrass;
    public bool hasBeenSet;

    public int textureOffsetX;
    public int textureOffsetY;

    public Texture2D grassMask;
    public Texture2D heightMap;
    public Texture2D rootHeightmap;
    public float heightDisplacementStrength;
    public Material material;
    public Material materialLOD;
    public ComputeShader grassCompute;
    public uint numberOfGrassBlades; // Max number of visible blades in the node
    public uint numberOfInstances;  // Actual number of instances

    public ComputeBuffer grassDataBuffer;   // Fist data buffer with all the positions
    public ComputeBuffer culledGrassDataBuffer;
    public ComputeBuffer culledGrassDataBufferLOD;
    public ComputeBuffer argsBuffer;
    public ComputeBuffer argsLODBuffer;

    public GrassQuadtree northWest = null;
    public GrassQuadtree northEast = null;
    public GrassQuadtree southWest = null;
    public GrassQuadtree southEast = null;

    public bool subdivided = false;

    public GrassQuadtree(AABB boundary, int currentDepth, int maxDepth, Texture2D grassMask, Texture2D heightMap, float heightDisplacementStrength, Vector2 offsets)
    {
        this.boundary = boundary;

        this.maxDepth = maxDepth;

        this.currentDepth = currentDepth;

        this.grassMask = grassMask;

        this.heightMap = heightMap;

        this.heightDisplacementStrength = heightDisplacementStrength;

        this.textureOffsetX = (int)offsets.x;
        this.textureOffsetY = (int)offsets.y;
    }

    public void SetRootHeightmap(Texture2D texture) 
    {
        rootHeightmap = texture;
    }

    public void Subdivide()
    {
        float x = boundary.p.x;
        float y = boundary.p.y;
        float w = boundary.halfDimension / 2;   // Half the width of the parent


        AABB nw = new AABB(x - w, y + w, w);
        northWest = new GrassQuadtree(nw, currentDepth + 1, maxDepth, SubdivideTexture(grassMask, false, true, true), 
            SubdivideTexture(heightMap, false, true, false), heightDisplacementStrength, CalculateChildTextureOffsets(heightMap, false, true));
        northWest.SetRootHeightmap(rootHeightmap);

        AABB ne = new AABB(x + w, y + w, w);
        northEast = new GrassQuadtree(ne, currentDepth + 1, maxDepth, SubdivideTexture(grassMask, true, true, true), 
            SubdivideTexture(heightMap, true, true, false), heightDisplacementStrength, CalculateChildTextureOffsets(heightMap, true, true));
        northEast.SetRootHeightmap(rootHeightmap);

        AABB sw = new AABB(x - w, y - w, w);
        southWest = new GrassQuadtree(sw, currentDepth + 1, maxDepth, SubdivideTexture(grassMask, false, false, true), 
            SubdivideTexture(heightMap, false, false, false), heightDisplacementStrength, CalculateChildTextureOffsets(heightMap, false, false));
        southWest.SetRootHeightmap(rootHeightmap);

        AABB se = new AABB(x + w, y - w, w);
        southEast = new GrassQuadtree(se, currentDepth + 1, maxDepth, SubdivideTexture(grassMask, true, false, true), 
            SubdivideTexture(heightMap, true, false, false), heightDisplacementStrength, CalculateChildTextureOffsets(heightMap, true, false));
        southEast.SetRootHeightmap(rootHeightmap);

        subdivided = true;
    }

    private Texture2D SubdivideTexture(Texture2D texture, bool positiveX, bool positiveY, bool isPosition)
    {
        Texture2D resultTexture;
        if (isPosition)
        {
            resultTexture = new Texture2D(texture.width / 2, texture.height / 2, TextureFormat.RGBA32, false);
            resultTexture.wrapMode = TextureWrapMode.Clamp;
            resultTexture.filterMode = FilterMode.Bilinear;
        }
        else 
        {
            resultTexture = new Texture2D(texture.width / 2, texture.height / 2, TextureFormat.R16, false);
            resultTexture.wrapMode = TextureWrapMode.Clamp;
            resultTexture.filterMode = FilterMode.Bilinear;
        }

        int startX, startY;

        if (positiveX)
        {
            startX = texture.width / 2;
        }
        else
        {
            startX = 0;
        }

        if (positiveY) 
        {
            startY = texture.height / 2;
        }
        else 
        {
            startY = 0;
        }


        for (int y = startY; y < startY + texture.height / 2; y++)
        {
            for (int x = startX; x < startX + texture.width / 2; x++)
            {
                resultTexture.SetPixel(x % (texture.width / 2), y % (texture.height / 2), texture.GetPixel(x, y));
            }
        }

        resultTexture.Apply();

        return resultTexture;
    }

    private Vector2 CalculateChildTextureOffsets(Texture2D subdividedTex, bool positiveX, bool positiveY)
    {
        int childOffsetX, childOffsetY;

        if (positiveX)
        {
            childOffsetX = textureOffsetX + subdividedTex.width / 2;
        }
        else
        {
            childOffsetX = textureOffsetX;
        }

        if (positiveY)
        {
            childOffsetY = textureOffsetY + subdividedTex.height / 2;
        }
        else
        {
            childOffsetY = textureOffsetY;
        }

        return new Vector2(childOffsetX, childOffsetY);
    }
    private bool GrassTextureContainsAlpha()
    {
        for (int y = 0; y < grassMask.height; y++) // Loop through the size of the mask
        {
            for (int x = 0; x < grassMask.width; x++)
            {
                Color currentPixel = grassMask.GetPixel(x, y);
                if (currentPixel.a > 0.1f)
                {
                    containsGrass = true;
                    return true;
                }
            }
        }
        containsGrass = false;
        return false;
    }

    // Subdivide the whole quadtree at the same time taking into account the max depth
    public void Build()
    {
        // Only keep subdividing if there is alpha (grass) in the texture - The last nodes should also test to get the correct bool OMFG!!!!!!!!!
        if (!GrassTextureContainsAlpha())
        {
            return;
        }
        if (currentDepth < maxDepth - 1)
        {
            this.Subdivide();

            // Clean unused textures
            //Texture2D.DestroyImmediate(grassMask);

            northEast.Build();
            northWest.Build();
            southEast.Build();
            southWest.Build();
        }
    }

    // Test the frustum against a quadtree, only visible quadtrees with grass will appear
    public bool TestFrustum(Vector3 cameraPosition, float leafCutoffDistance, float quadtreeCutoffDistance, Plane[] frustum, ref List<GrassQuadtree> validQuadtrees)
    {
        if(!boundary.IsOnFrustum(frustum, heightMap, heightDisplacementStrength))
        {
            return false;
        }

        if (!containsGrass)
        {
            return false;
        }
        if (Vector3.Distance(cameraPosition, new Vector3(boundary.p.x, 10, boundary.p.y)) > quadtreeCutoffDistance)
        {
            return false;
        }


        // Quadtree is in frustum, in distance && contains grass
        if (subdivided)
        {
            if (northWest.TestFrustum(cameraPosition, leafCutoffDistance, quadtreeCutoffDistance, frustum, ref validQuadtrees) |
                northEast.TestFrustum(cameraPosition, leafCutoffDistance, quadtreeCutoffDistance, frustum, ref validQuadtrees) |
                southEast.TestFrustum(cameraPosition, leafCutoffDistance, quadtreeCutoffDistance, frustum, ref validQuadtrees) |
                southWest.TestFrustum(cameraPosition, leafCutoffDistance, quadtreeCutoffDistance, frustum, ref validQuadtrees))
            {
                return false;
            }
            else
            {
                if (Vector3.Distance(cameraPosition, new Vector3(boundary.p.x, 10, boundary.p.y)) > leafCutoffDistance)
                {
                    return false;
                }
                validQuadtrees.Add(this);
                return true;
            }
        }
        else
        {
            if (Vector3.Distance(cameraPosition, new Vector3(boundary.p.x, 10, boundary.p.y)) > leafCutoffDistance)
            {
                return false;
            }
            validQuadtrees.Add(this);
            return true;
        }
    }

    public bool Equals(GrassQuadtree other)
    {
        return this.boundary.p.x == other.boundary.p.x && this.boundary.p.y == other.boundary.p.y;
    }
}
