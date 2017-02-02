using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class NewBehaviourScript : MonoBehaviour {

    public Mesh mesh1;
    public Mesh mesh2;

    SkinnedMeshRenderer smr;

    // Use this for initialization
    void Start ()
    {




        smr = GetComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = CreateMesh();



    }

    //---------------------------------------------------------------------------------------------------------

    private Mesh CreateMesh()
    {
        Vector3[] vertices1 = new Vector3[3] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0) };

        //Vector3[] normals = new Vector3[3] { Vector3.back, Vector3.back, Vector3.back };
        //Vector2[] uv = new Vector2[3] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0) };
        int[] tirangles = new int[3] { 0, 1, 2 };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices1;
        //mesh.normals = normals;
        //mesh.uv = uv;
        mesh.triangles = tirangles;


        Vector3[] diff1 = new Vector3[3] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 0) };
        Vector3[] diff2 = new Vector3[3] { new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, -1, 0) };

        mesh.AddBlendShapeFrame("a", 1.0f, diff1, null, null);
        mesh.AddBlendShapeFrame("b", 1.0f, diff2, null, null);


        mesh.RecalculateBounds();

#if UNITY_EDITOR
        AssetDatabase.CreateAsset(mesh, "Assets/test.asset");
        AssetDatabase.Refresh();
#endif

        Debug.Log(mesh.blendShapeCount);
        return mesh;
    }

    //---------------------------------------------------------------------------------------------------------

    // Update is called once per frame
    void Update () {


        float v = Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup));

        smr.SetBlendShapeWeight(0, v);
        //smr.SetBlendShapeWeight(1, v);

        //  smr.SetBlendShapeWeight(1, 1.0f - v);

    }
}
