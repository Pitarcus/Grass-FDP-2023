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

// ------ AABB CLASS ------
// In the AABB class, we use the center point and half dimension to reference the area
public class AABB {
    
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
        return (p.x - halfDimension < point.x &&
                p.x + halfDimension > point.x &&
                p.y - halfDimension < point.y &&
                p.y + halfDimension > point.y);
                   
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
