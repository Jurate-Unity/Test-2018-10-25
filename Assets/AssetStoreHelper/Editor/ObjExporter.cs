using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Text;

//http://wiki.unity3d.com/index.php?title=ExportOBJ

public static class ObjExporter
{
    private static int index = 0;

    public static void DoExport(GameObject go, string savePath, bool makeSubmeshes = true)
    {
        string meshName = go.name;
        Transform t = go.transform;
        
        StringBuilder meshString = new StringBuilder();
        index = 0;

        Vector3 originalPosition = t.position;
        t.position = Vector3.zero;

        if (!makeSubmeshes)
            meshString.Append("g ").Append(t.name).Append("\n");

        meshString.Append(processTransform(t, makeSubmeshes));

        WriteToFile(meshString.ToString(), savePath);

        t.position = originalPosition;
        Debug.Log("Exported Mesh: " + savePath);
    }

    static string processTransform(Transform t, bool makeSubmeshes)
    {
        StringBuilder meshString = new StringBuilder();

        if (makeSubmeshes)
            meshString.Append("g ").Append(t.name).Append("\n");

        MeshFilter mf = t.GetComponent<MeshFilter>();
        if (mf)
            meshString.Append(MeshToString(mf, t));

        for (int i = 0; i < t.childCount; i++)
            meshString.Append(processTransform(t.GetChild(i), makeSubmeshes));

        return meshString.ToString();
    }

    static string MeshToString(MeshFilter mf, Transform t)
    {
        Vector3 s = t.localScale;
        Vector3 p = t.localPosition;
        Quaternion r = t.localRotation;

        int numVertices = 0;
        Mesh m = mf.sharedMesh;
        if (!m)
        {
            Debug.LogError("Failed to export mesh");
            return "####Error####";
        }
        Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;

        StringBuilder sb = new StringBuilder();

        foreach (Vector3 vv in m.vertices)
        {
            Vector3 v = t.TransformPoint(vv);
            numVertices++;
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 nn in m.normals)
        {
            Vector3 v = r * nn;
            sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        for (int material = 0; material < m.subMeshCount; material++)
        {
            sb.Append("\n");
            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            sb.Append("usemap ").Append(mats[material].name).Append("\n");

            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1 + index, triangles[i + 1] + 1 + index, triangles[i + 2] + 1 + index));
            }
        }

        index += numVertices;
        return sb.ToString();
    }

    static void WriteToFile(string s, string filename)
    {
        using (StreamWriter sw = new StreamWriter(filename))
            sw.Write(s);
    }
}
