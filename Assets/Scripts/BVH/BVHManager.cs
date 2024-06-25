using System.Collections.Generic;
using UnityEngine;

public class BVHManager : MonoBehaviour
{
    private BVHTree bvhTree = new BVHTree();
    private List<BVHObject> managedObjects;
    private List<BoxNode> boxes = new List<BoxNode>();
    // int t1, t2 = 0;
    [SerializeField] public int count = 5;

    public void Initialize()
    {
        managedObjects = new List<BVHObject>(FindObjectsOfType<BVHObject>());

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

            // Debug.Log($"Initialize boxes number: {boxes.Count}");
        }
    }

    void Update()
    {
        //         t1= Time.realtimeSinceStartup();
        // if
        //         // Rebuild the BVH every frame for simplicity (could be optimized)
        //         boxes = new List<BoxNode>();
        //         bvhTree = new BVHTree();

        //         foreach (var obj in managedObjects)
        //         {
        //             obj.UpdateBoundingBoxes();
        //             foreach (var BoundingBox in obj.BoundingBoxes)
        //                 bvhTree.Insert(obj.gameObject, BoundingBox);
        //         }
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

        // Debug.Log($"OnDrawGizmos boxes number: {boxes.Count}");

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
        Debug.Log($"GetBoxes boxes number: {boxes.Count}");
        return boxes;
    }

}
