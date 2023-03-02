using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class QuadtreeDebug : MonoBehaviour
{
    public Quadtree quadTree;

    private AABB quadTreeOrigin;

    public GrassPainter gp;


    private void Start()
    {
        quadTree = new Quadtree(new AABB(new Point(0, 0), 64), 10);

        gp = GameObject.FindGameObjectWithTag("GrassPainter").GetComponent<GrassPainter>();
        Mesh grassPositions = gp.positionsMesh;

        foreach (Vector3 position in grassPositions.vertices)
        {
            quadTree.Insert(new Point(position.x, position.z));
        }

        Queue<Quadtree> queue = new Queue<Quadtree>();

        queue.Clear();
        queue.Enqueue(quadTree);

        /*Debug.Log(quadTree.northWest);
        while (queue.Count > 0)
        {
            Quadtree currentQT = queue.Dequeue();

            foreach (Point p in currentQT.points)
            {
                Debug.Log("{ " + p.x + " ," + p.y + " }");
            }

            if (currentQT.subdivided)
            {
                queue.Enqueue(currentQT.northEast);
                queue.Enqueue(currentQT.northWest);
                queue.Enqueue(currentQT.southEast);
                queue.Enqueue(currentQT.southWest);
            }
        }*/

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Queue<Quadtree> queue = new Queue<Quadtree>();

        if (quadTree == null)
            return;

        queue.Clear();
        queue.Enqueue(quadTree);

        while (queue.Count > 0)
        {
            Quadtree currentQT = queue.Dequeue();

            Gizmos.DrawWireCube(new Vector3(currentQT.boundary.p.x, 0, currentQT.boundary.p.y),
                new Vector3(currentQT.boundary.halfDimension * 2, 0, currentQT.boundary.halfDimension * 2));

            if (currentQT.subdivided)
            {
                queue.Enqueue(currentQT.northEast);
                queue.Enqueue(currentQT.northWest);
                queue.Enqueue(currentQT.southEast);
                queue.Enqueue(currentQT.southWest);
            }
        }
    }
}
