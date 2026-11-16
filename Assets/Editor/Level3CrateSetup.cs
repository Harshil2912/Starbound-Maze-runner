using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Level3CrateSetup
{
    [MenuItem("Tools/Setup Level3 Crate and Colliders")]
    public static void ExecuteSetup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "PuzzleSolver")
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/PuzzleSolver.unity");
        }

        var anchor = GameObject.Find("ActiveLevelAnchor");
        if (anchor == null)
        {
            var root = GameObject.Find("ARContentRoot");
            if (root != null)
            {
                var t = root.transform.Find("ActiveLevelAnchor");
                if (t != null) anchor = t.gameObject;
            }
        }

        if (anchor == null)
        {
            Debug.LogError("[Level3CrateSetup] ActiveLevelAnchor not found in scene!");
            return;
        }

        var lvl3 = anchor.transform.Find("Level3");
        if (lvl3 == null)
        {
            Debug.LogError("[Level3CrateSetup] Level3 not found under ActiveLevelAnchor!");
            return;
        }

        // 1. Configure Crate in Level3
        var crateObj = lvl3.Find("Puzzle/Crate");
        if (crateObj != null)
        {
            Undo.RecordObject(crateObj.transform, "Configure Crate Transform");
            Vector3 targetLocalPos = new Vector3(14.00f, 0.76f, 2.00f);
            crateObj.localPosition = targetLocalPos;
            crateObj.localRotation = Quaternion.identity;

            var crateComp = crateObj.GetComponent<Crate>();
            if (crateComp != null)
            {
                Undo.RecordObject(crateComp, "Configure Crate Component");
                crateComp.defaultSpawnLocalPos = targetLocalPos;
                crateComp.floorLocalY = 0.76f;
                crateComp.fallY = -6f;
                crateComp.pushResponsiveness = 4.5f;
                crateComp.decelerationRate = 8.0f;
            }

            var rb = crateObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Undo.RecordObject(rb, "Configure Crate Rigidbody");
                rb.useGravity = false;
                rb.freezeRotation = true;
                rb.mass = 2.2f;
                rb.linearDamping = 3f;
                rb.angularDamping = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            var col = crateObj.GetComponent<BoxCollider>();
            if (col != null)
            {
                Undo.RecordObject(col, "Configure Crate Collider");
                col.center = Vector3.zero;
                col.size = Vector3.one;
            }

            // Material: Golden Yellow with emissive accent
            var rend = crateObj.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                Undo.RecordObject(rend.sharedMaterial, "Configure Crate Material");
                Color goldenYellow = new Color(0.98f, 0.82f, 0.15f, 1f); // #FAD126
                if (rend.sharedMaterial.HasProperty("_Color")) rend.sharedMaterial.SetColor("_Color", goldenYellow);
                if (rend.sharedMaterial.HasProperty("_BaseColor")) rend.sharedMaterial.SetColor("_BaseColor", goldenYellow);
                if (rend.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    rend.sharedMaterial.EnableKeyword("_EMISSION");
                    rend.sharedMaterial.SetColor("_EmissionColor", goldenYellow * 0.40f);
                }
            }

            Debug.Log($"[Level3CrateSetup] Crate successfully positioned at {targetLocalPos} inside puzzle room.");
        }
        else
        {
            Debug.LogError("[Level3CrateSetup] Crate not found under Level3/Puzzle!");
        }

        // 2. Add BoxColliders to all 76 floor tiles under Level3/Floor
        var floor3 = lvl3.Find("Floor");
        int addedCols = 0;
        if (floor3 != null)
        {
            for (int i = 0; i < floor3.childCount; i++)
            {
                var tile = floor3.GetChild(i);
                var bc = tile.GetComponent<BoxCollider>();
                if (bc == null)
                {
                    bc = Undo.AddComponent<BoxCollider>(tile.gameObject);
                    bc.center = Vector3.zero;
                    bc.size = Vector3.one;
                    addedCols++;
                }
            }
            Debug.Log($"[Level3CrateSetup] Added {addedCols} BoxColliders to Level3 Floor tiles. (Total: {floor3.childCount})");
        }

        // 3. Add a dedicated BoxCollider for the puzzle room floor under FloorCollider
        var floorColliderObj = lvl3.Find("FloorCollider");
        if (floorColliderObj != null)
        {
            var roomBc = floorColliderObj.GetComponent<BoxCollider>();
            if (roomBc == null)
            {
                roomBc = Undo.AddComponent<BoxCollider>(floorColliderObj.gameObject);
            }
            Undo.RecordObject(roomBc, "Configure Puzzle Room Floor BoxCollider");
            // Puzzle room spans x: 6..22, z: -6..10. Center=(14, -0.5, 2), Size=(16.5, 1, 16.5)
            roomBc.center = new Vector3(14.0f, -0.5f, 2.0f);
            roomBc.size = new Vector3(16.5f, 1.0f, 16.5f);
            Debug.Log("[Level3CrateSetup] Puzzle room solid floor BoxCollider configured.");
        }

        // 4. Add MeshColliders to Level4 FloorA and FloorB
        var lvl4 = anchor.transform.Find("Level4");
        if (lvl4 != null)
        {
            var floor4 = lvl4.Find("Floor");
            if (floor4 != null)
            {
                for (int i = 0; i < floor4.childCount; i++)
                {
                    var ch = floor4.GetChild(i);
                    var mf = ch.GetComponent<MeshFilter>();
                    var mc = ch.GetComponent<MeshCollider>();
                    if (mc == null && mf != null && mf.sharedMesh != null)
                    {
                        mc = Undo.AddComponent<MeshCollider>(ch.gameObject);
                        mc.sharedMesh = mf.sharedMesh;
                        Debug.Log($"[Level3CrateSetup] Added MeshCollider to Level4 {ch.name}");
                    }
                }
            }
        }

        // Save scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Level3CrateSetup] Scene '{scene.name}' saved with all Level 3 Crate & Floor collision fixes applied!");
    }
}
