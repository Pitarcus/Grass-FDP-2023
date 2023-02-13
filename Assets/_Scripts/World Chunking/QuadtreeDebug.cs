using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class QuadtreeDebug : MonoBehaviour
{
    public Quadtree quadTree;

    private AABB quadTreeOrigin;

    private void OnEnable()
    {
        quadTree = new Quadtree(new AABB(new Point(0, 0), 64));
        quadTreeOrigin = quadTree.rootAABB;

        Debug.Log(quadTreeOrigin);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(quadTreeOrigin.p.x, 0, quadTreeOrigin.p.y),
            new Vector3(quadTreeOrigin.halfDimension * 2, 0, quadTreeOrigin.halfDimension * 2));
    }
}
