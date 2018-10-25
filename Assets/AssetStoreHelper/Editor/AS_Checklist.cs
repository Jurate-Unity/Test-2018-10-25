using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetStoreHelper
{
    class ASPostprocessor : AssetPostprocessor
    {
        //Asset paths
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            AS_Checklist checklist = AS_Data.LoadAssetAtPath<AS_Checklist>(AS_Data.MANAGER_PATH);

            if (checklist == null)
                return;

            foreach (var check in checklist.Checks)
            {
                check.CheckAssetsForDeletion(deletedAssets);
                check.CheckAssetsForMove(movedFromAssetPaths, movedAssets);
            }
        }
    }

    [Serializable]
    public class AS_Checklist : ScriptableObject
    {
        private static AS_Checklist _checklist;
        [SerializeField]
        internal List<AS_ChecklistItem> Checks = new List<AS_ChecklistItem>();
        internal PackageType PackageType = PackageType.All;

        private static void CreateChecklist()
        {
            _checklist = CreateInstance<AS_Checklist>();
            AssetDatabase.CreateAsset(_checklist, AS_Data.MANAGER_PATH);
            foreach (AS_Data.CheckItemData d in AS_Data.ItemData)
                _checklist.AddCheck(d);
            EditorUtility.SetDirty(_checklist);
            AssetDatabase.ImportAsset(AS_Data.MANAGER_PATH);
            AssetDatabase.SaveAssets();
        }

        private void AddCheck(AS_Data.CheckItemData data)
        {
            AS_ChecklistItem checkItem = ScriptableObject.CreateInstance<AS_ChecklistItem>();
            checkItem.Init(data);
            AssetDatabase.AddObjectToAsset(checkItem, AS_Data.MANAGER_PATH);
            Checks.Add(checkItem);
        }

        internal static AS_Checklist GetCheckList()
        {
            _checklist = AS_Data.LoadAssetAtPath<AS_Checklist>(AS_Data.MANAGER_PATH);
            if (_checklist == null)
                CreateChecklist();
            return _checklist;
        }

        internal static AS_ChecklistItem GetCheck(CheckType check)
        {
            return _checklist.Checks[(int)check];
        }

        //Toggles whether listed items are active (visible in the GUI) based on which checks are relevant for the package type
        internal void ApplyPackageType()
        {
            if (PackageType == PackageType.All)
            {
                foreach (var c in Checks)
                    c.Active = true;
            }
            else
            {
                foreach (var check in Checks)
                {
                    var category = AS_Data.ItemData[(int)check.Type].Category;
                    check.Active = (category == PackageType.All || category == PackageType);
                }
            }
        }

        //Performs all scans and populates relevant listed items with the results and corresponding states
        public void Scan()
        {
            foreach (var c in Checks)
                c.Clear();

            ScanForExtensions(GetCheck(CheckType.Demo), AS_Data.DEMO_EXTENSIONS);
            ScanForExtensions(GetCheck(CheckType.Jpg), AS_Data.JPG_EXTENSIONS);
            ScanForExtensions(GetCheck(CheckType.Prepackage), AS_Data.PACKAGE_EXTENSIONS);
            ScanForExtensions(GetCheck(CheckType.Documentation), AS_Data.DOC_EXTENSIONS, AS_Data.EXCLUDED_DIRECTORIES);
            ScanForExtensions(GetCheck(CheckType.JavaScript), AS_Data.JS_EXTENSIONS);

            ScanPrefabs(GetCheck(CheckType.PrefabCollider), GetCheck(CheckType.PrefabTransform));
            ScanStandardAssets(GetCheck(CheckType.StandardAssets));
            ScanReferences(GetCheck(CheckType.MissingReference));
            ScanOrientation(GetCheck(CheckType.Orientation));
            ScanModelsForPrefabs(GetCheck(CheckType.Prefab));

            foreach (var c in Checks)
                c.UpdateState();
        }

        //Checks whether an asset has missing references in any of its components 
        private static bool IsMissingReference(GameObject asset)
        {
            var components = asset.GetComponents<Component>();
            foreach (var c in components)
            {
                if (!c)
                    return true;
            }
            return false;
        }

        //Checks whether a model (and its children) have a rotation of (0,0,0)
        //Skips over empty objects with no mesh components 
        private static bool IsUpright(GameObject model)
        {
            Transform[] transforms = model.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t.localRotation!=Quaternion.identity && AS_Data.GetMesh(t))
                    return false;
            }
            return true;
        }

        // Returns whether prefab (containing a user-generated mesh) needs a collider
        private static bool NeedsCollider(GameObject go)
        {
            //Check if prefab contains user-generated meshes
            List<Mesh> meshes = AS_Data.GetMeshes(go);
            if(!meshes.Any())
                return false;

            //If so, check if prefab has any colliders
            var colliders = go.GetComponentsInChildren<Collider>(true);
            return (!colliders.Any());
        }

        // Returns whether a prefab (containing a user-generated mesh) needs to reset its top-level transform 
        private static bool NeedsTransformReset(GameObject go)
        {
            List<Mesh> meshes = AS_Data.GetMeshes(go);
            if (!meshes.Any())
                return false;

            var mat = go.transform.localToWorldMatrix;
            return (!mat.isIdentity);
        }

        //Returns all paths with given extensions, exluding given directories 
        private static void ScanForExtensions(AS_ChecklistItem item, string[] extensions, string[] exclusions = null)
        {
            List<string> paths = AS_Data.GetPathsWithExtensions(extensions, exclusions);
            if (paths.Any())
                item.AddPaths(paths);
        }

        //Checks all assets for missing references 
        private static void ScanReferences(AS_ChecklistItem item)
        {
            var assetPaths = AssetDatabase.GetAllAssetPaths().Where(p => AS_Data.PathInAssetDir(p));

            foreach (var path in assetPaths)
            {
                var asset = AS_Data.LoadAssetAtPath<GameObject>(path);
                if (asset != null && IsMissingReference(asset))
                    item.AddPath(path);
            }
        }

        //Checks all model files (.obj and .fbx) for rotations of (0,0,0) 
        //Note: animation files without meshes are excluded
        private static void ScanOrientation(AS_ChecklistItem item)
        {
            var modelPaths = AS_Data.GetModelPaths();

            foreach (var path in modelPaths)
            {
                var model = AS_Data.LoadAssetAtPath<GameObject>(path);
                if (!IsUpright(model))
                    item.AddPath(AssetDatabase.GetAssetPath(model));
            }
        }

        //Checks that all user-generated meshes have corresponding prefabs
        private static void ScanModelsForPrefabs(AS_ChecklistItem item)
        {
            var prefabPaths = AS_Data.GetPathsWithExtensions(AS_Data.PREFAB_EXTENSIONS);
            HashSet<string> usedModelPaths = new HashSet<string>();

            foreach (var path in prefabPaths)
            {
                var prefab = AS_Data.LoadAssetAtPath<GameObject>(path);

                List<Mesh> meshes = AS_Data.GetMeshes(prefab);
                foreach (var mesh in meshes)
                {
                    string meshPath = AssetDatabase.GetAssetPath(mesh);
                    usedModelPaths.Add(meshPath);
                }
            }
            List<string> allModelPaths = AS_Data.GetModelPaths();
            List<string> unusedModels = allModelPaths.Except(usedModelPaths, new CustomPathComparer()).ToList();

            item.AddPaths(unusedModels);
        }

        //Checks for Standard Assets by finding the Standard Assets.meta file 
        private static void ScanStandardAssets(AS_ChecklistItem item)
        {
            var directories = Directory.GetDirectories(Application.dataPath).Where(s => s.Contains("Standard Assets")).ToList();

            //Additional check
            /*
            bool inNamespace = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                  from type in assembly.GetTypes()
                                  where type.Namespace!=null && type.Namespace.StartsWith("UnityStandardAssets")
                                  select type).Any();
            */

            item.AddPaths(directories);
        }

        //Checks all prefabs (containing user-generated meshes) for colliders and top level transforms of (0,0,0) for position and rotation, and (1,1,1) for scale
        private static void ScanPrefabs(AS_ChecklistItem colliderCheck, AS_ChecklistItem transformCheck)
        {
            var prefabPaths = AS_Data.GetPathsWithExtensions(AS_Data.PREFAB_EXTENSIONS);

            foreach (var path in prefabPaths)
            {
                var prefab = AS_Data.LoadAssetAtPath<GameObject>(path);
                
                if (NeedsCollider(prefab))
                    colliderCheck.AddPath(path);
                if (NeedsTransformReset(prefab))
                    transformCheck.AddPath(path);
            }
        }
    }
}