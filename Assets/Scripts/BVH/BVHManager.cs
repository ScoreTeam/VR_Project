using System.Collections.Generic;
using UnityEngine;
using System;


public class BVHManager : MonoBehaviour
{
    private BVHTree bvhTree;
    private List<BVHObject> managedObjects;
    private List<BoxNode> boxes;

    [SerializeField] public int count = 5;

    void Start()
    {
        bvhTree = new BVHTree();

        managedObjects = new List<BVHObject>(FindObjectsOfType<BVHObject>());
        boxes = new List<BoxNode>();

        // Insert all managed objects into the BVH
        foreach (var obj in managedObjects)
        {
            obj.UpdateBoundingBoxes();
            foreach (var BoundingBox in obj.BoundingBoxes)
                bvhTree.Insert(obj.gameObject, BoundingBox);
        }

        if (bvhTree != null && bvhTree.Root != null)
        {
            DrawNodeGizmos(bvhTree.Root, 0);

            Debug.Log($"boxes number: {boxes.Count}");
        }

    }

    void Update()
    {
        // Rebuild the BVH every frame for simplicity (could be optimized)
        // boxes = new List<BoxNode>();
        // bvhTree = new BVHTree();

        // foreach (var obj in managedObjects)
        // {
        //     obj.UpdateBoundingBoxes();
        //     foreach (var BoundingBox in obj.BoundingBoxes)
        //         bvhTree.Insert(obj.gameObject, BoundingBox);
        // }
    }

    public List<GameObject> Query(AABB queryBox)
    {
        return bvhTree.Query(queryBox);
    }

    void OnDrawGizmos()
    {
        // if (bvhTree == null || bvhTree.Root == null)
        // {
        //     return;
        // }

        // DrawNodeGizmos(bvhTree.Root, 0);

        // Debug.Log($"boxes number: {boxes.Count}");

        // foreach (var obj in managedObjects)
        // {
        //     if (obj == null) continue;
        //     foreach (var box in obj.BoundingBoxes)
        //     {
        //         Gizmos.color = Color.green;
        //         Gizmos.DrawWireCube((box.Min + box.Max) / 2, box.Max - box.Min);
        //     }
        // }

        // if (boxes == null)
        // {
        //     return;
        // }

        // Gizmos.color = Color.black;
        // foreach (var b in boxes)
        // {
        //     Gizmos.DrawWireCube(b.Center, b.Size);
        // }
    }

    private void DrawNodeGizmos(BVHNode node, int step)
    {
        if (node == null) return;

        var Min = node.BoundingBox.Min;
        var Max = node.BoundingBox.Max;

        if (step >= count)
        {
            boxes.Add(new BoxNode((Min + Max) / 2, Max - Min));
        }

        if (!node.IsLeaf)
        {
            DrawNodeGizmos(node.Left, step + 1);
            DrawNodeGizmos(node.Right, step + 1);
        }
    }

    public List<BoxNode> GetBoxes()
    {
        return boxes;
    }

}