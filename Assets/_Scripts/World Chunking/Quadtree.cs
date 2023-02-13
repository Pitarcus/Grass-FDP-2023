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

public class AABB {
    
    public Point p;

    public float halfDimension;

    public AABB(Point XY, float halfDimension)
    {
        this.p = XY;
        this.halfDimension = halfDimension;
    }
}


public class Quadtree
{
    public AABB rootAABB;

    public Quadtree(AABB boundary)
    {
        rootAABB = boundary;
    }

    
}
