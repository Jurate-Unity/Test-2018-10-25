using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityFBXExporter;

namespace AssetStoreHelper
{
	public class AS_Utility {

	    //Rotates all models (Z forward, Y up) at given paths, and exports (stationary) copies
	    public static List<string> FixRotationExport(List<string> modelPaths)
	    {
            List<string> rotatedPaths = new List<string>();

            //Limited to .fbx and .obj, and user-generated meshes within the Assets folder
	        //var modelPaths = AS_Data.GetModelPaths();
	        foreach (var modelPath in modelPaths)
	        {
	            GameObject model = (GameObject)AssetDatabase.LoadAssetAtPath(modelPath, typeof(GameObject));

	            var rotated = RotateAsset(model.transform, modelPath);
                if (rotated)
	            {
                    string rotatedPath = ExportModel(modelPath, model);
                    rotatedPaths.Add(rotatedPath);
	            }
	        }
	        AssetDatabase.Refresh();

            return rotatedPaths;
	    }

        //Exports a copy of a model based on its file types (.obj or .fbx)
        //Note: only works for stationary models. The mesh is converted to a MeshFilter, and no animations or rigs included.
        static string ExportModel(string modelPath, GameObject model)
        {
            //Convert to .fbx or .obj based on original file type
            string extension = Path.GetExtension(modelPath).ToLower();
            string path = AS_Data.GenerateUniquePath(modelPath, extension);
            Debug.LogFormat("Saving {0} to {1}", modelPath, path);

            if(extension.Equals(".obj"))
                ObjExporter.DoExport(model, path);
            else if(extension.Equals(".fbx"))
                FBXExporter.ExportGameObjToFBX(model, path);

            //Copy meta data/ import settings
            ModelImporter sourceModelImporter = ModelImporter.GetAtPath(modelPath) as ModelImporter;
            ModelImporter destinationTxImporter = ModelImporter.GetAtPath(path) as ModelImporter;
            EditorUtility.CopySerialized(sourceModelImporter, destinationTxImporter);

            //Reimport original model 
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);

            return path;
        }

        //Rotates a given mesh to Z forward and Y up
        static void RotateMesh(Mesh mesh, Matrix4x4 mat)
        {
            var vertices = mesh.vertices;
            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = mat.MultiplyPoint3x4(vertices[i]);
            }

            var normals = mesh.normals;
            var inverseTranspose = mat.inverse.transpose;
            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = inverseTranspose.MultiplyVector(normals[i]).normalized;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.RecalculateBounds();
        }

        //Rotate all submeshes with a model to Z forward and Y up
	    static bool RotateAsset(Transform transform, string path)
	    {
	        var mat = transform.localToWorldMatrix;
            Mesh mesh = AS_Data.GetMesh(transform);
            var rotated = false;

	        if (!mat.isIdentity && mesh)
	        {
                transform.localScale = Vector3.one;
                transform.localRotation = Quaternion.identity;
                transform.localPosition = Vector3.zero;

                RotateMesh(mesh, mat);
                rotated = true;
            }

	        foreach (Transform childTransform in transform)
	        {
	            rotated |= RotateAsset(childTransform, path);
	        }

	        return rotated;
	    }

	}
}
