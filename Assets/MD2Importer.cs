using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class MD2Importer : MonoBehaviour
{
    //---------------------------------------------------------------------------------------------------------

    public TextAsset MD2File;

    //---------------------------------------------------------------------------------------------------------

    public class MinMaxFrame
    {
        public int Min;
        public int Max;

        public MinMaxFrame(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    //---------------------------------------------------------------------------------------------------------

    private void Start()
    {
        MD2File md2File = new MD2File(MD2File.bytes);
        MD2Frame[] frames = md2File.GetAllFrames();


        Mesh[] meshes = new Mesh[frames.Length];

        for (int i = 0; i < meshes.Length; i++)
        {
            meshes[i] = ConvertFrameToMesh(frames[i]);
        }

        


        Dictionary<string, MinMaxFrame> animations = GetAnimationDescriptions(frames);
        Mesh baseMesh = meshes[0];

        foreach (var group in animations)
        {
            AddBlendShapesToMesh(baseMesh, meshes, group.Value.Min, group.Value.Max, group.Key);

            string key = group.Key;
            MinMaxFrame minMaxFrame = group.Value;
            Debug.Log(key + " " + minMaxFrame.Min + " " + minMaxFrame.Max);
        }


        #if UNITY_EDITOR
        AssetDatabase.CreateAsset(baseMesh, "Assets/baseMesh.asset");
        AssetDatabase.Refresh();
        #endif

        GetComponent<MeshFilter>().mesh = baseMesh;

    }

    //---------------------------------------------------------------------------------------------------------

    private Dictionary<string, MinMaxFrame> GetAnimationDescriptions(MD2Frame[] frames)
    {
        Dictionary<string, MinMaxFrame> groups = new Dictionary<string, MinMaxFrame>();

        for (int i = 0; i < frames.Length; i++)
        {
            Regex regex = new Regex(@"\d+");
            Match math = regex.Match(frames[i].Name);
            string key = frames[i].Name.Substring(0, math.Index);

            if (groups.ContainsKey(key) == false)
            {
                groups[key] = new MinMaxFrame(i, i);
            }
            else
            {
                MinMaxFrame minMaxFrame = groups[key];

                if (i >= minMaxFrame.Max)
                    minMaxFrame.Max = i;
                else
                {
                    if (i < minMaxFrame.Min)
                        minMaxFrame.Min = i;
                }
            }
        }

        return groups;
    }

    //---------------------------------------------------------------------------------------------------------

    private void AddBlendShapesToMesh(Mesh baseMesh, Mesh[] meshes, int start, int end, string name)
    {
        for (int i = start; i <= end; i++)
        {
            int i1 = i - 1;
            int i2 = i;

            Vector3[] vDiff = null;
            Vector3[] nDiff = null;

            if (i == start)
            {
                vDiff = GetDiff(baseMesh.vertices, meshes[i2].vertices);
                nDiff = GetDiff(baseMesh.normals, meshes[i2].normals);
            }
            else
            {
                vDiff = GetDiff(meshes[i1].vertices, meshes[i2].vertices);
                nDiff = GetDiff(meshes[i1].normals, meshes[i2].normals);
            }

            baseMesh.AddBlendShapeFrame(name + (i - start + 1), 1.0f, vDiff, nDiff, null);
        }
    }

    //---------------------------------------------------------------------------------------------------------

    private Vector3[] GetDiff(Vector3[] v1, Vector3[] v2)
    {
        Vector3[] diff = new Vector3[v1.Length];

        for(int i = 0; i < diff.Length; i++)
        {
            diff[i] = v2[i] - v1[i];
        }

        return diff;
    }

    //---------------------------------------------------------------------------------------------------------

    private unsafe Mesh ConvertFrameToMesh(MD2Frame frame)
    {
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();
        List<int> newTriangles = new List<int>();

        for (int t = 0; t < frame.Triagles.Length; t++)
        {
            MD2Triangle triagle = frame.Triagles[t];

            for (int i = 0; i < 3; i++)
            {
                Vector3 vertex = frame.Vertices[triagle.index_xyz[i]];
                Vector3 normal = frame.Normals[triagle.index_xyz[i]];
                Vector2 uv = frame.UVs[triagle.index_st[i]];

                bool doesExistsSuchVertex = false;
                int index = 0;

                for (index = 0; index < newVertices.Count; index++)
                {
                    if (newVertices[index] == vertex && newUVs[index] == uv && newNormals[index] == normal)
                    {
                        doesExistsSuchVertex = true;
                        break;
                    }
                }

                newTriangles.Add(index);

                if (doesExistsSuchVertex == false)
                {
                    newVertices.Add(vertex);
                    newNormals.Add(normal);
                    newUVs.Add(uv);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = newVertices.ToArray();
        mesh.normals = newNormals.ToArray();
        mesh.uv = newUVs.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.RecalculateBounds();
        mesh.name = frame.Name;

        return mesh;
    }

    //---------------------------------------------------------------------------------------------------------
}
