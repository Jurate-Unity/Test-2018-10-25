using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetStoreHelper
{
    public enum DetectionType
    {
        ErrorOnDetect,
        WarningOnDetect,
        ErrorOnAbsence,
    }

    public enum CheckType
    {
        Demo,
        Prefab,
        PrefabTransform,
        PrefabCollider,
        Documentation,
        Orientation,
        Jpg,
        Prepackage,
        StandardAssets,
        MissingReference,
        JavaScript,
    };

    public enum PackageType
    {
        Art,
        Script,
        All
    }

    public static class AS_Data
    {
        internal static string AS_DIRECTORY = "AssetStoreHelper";
        internal static string AS_PATH = Path.Combine("Assets", AS_DIRECTORY);
        internal static string MANAGER_PATH = Path.Combine(AS_PATH, "AS_Checklist.asset");

        internal static readonly string[] MODEL_EXTENSIONS = { ".obj", ".fbx" };
        internal static readonly string[] JPG_EXTENSIONS = { ".jpg", ".jpeg" };
        internal static readonly string[] DEMO_EXTENSIONS = { ".unity" };
        internal static readonly string[] PACKAGE_EXTENSIONS = { ".unitypackage" };
        internal static readonly string[] DOC_EXTENSIONS = { ".txt", ".pdf", ".html", ".rtf" };
        internal static readonly string[] PREFAB_EXTENSIONS = { ".prefab" };
        internal static readonly string[] JS_EXTENSIONS = { ".js" };

        internal static readonly string[] EXCLUDED_DIRECTORIES = { "AssetStoreTools", "AssetStoreChecker", "AssetStoreHelper" };

        internal class CheckItemData
        {
            public CheckType Type;
            public string Title;
            public string Message;
            public DetectionType Detection;
            public PackageType Category;
        }

        internal static List<CheckItemData> ItemData = new List<CheckItemData> {
            new CheckItemData
            {
                Type = CheckType.Demo,
                Title = "Include demo",
                Message = "Please include a demo scene that properly showcases your assets. Please provide a practical demo with all of your assets set up in a lighted scene, and if your package has multiple assets, please have an additional demo scene that displays all of your assets in a grid or a continuous line. (https://unity3d.com/asset-store/sell-assets/submission-guidelines , Section 3.3.a)",
                Detection = DetectionType.ErrorOnAbsence,
                Category = PackageType.All
            },
            new CheckItemData
            {
                Type = CheckType.Prefab,
                Title = "Include prefabs",
                Message = "Each mesh should have a corresponding prefab set up with all variations of the texture/mesh/material that you are providing. Please create prefabs for all of your imported objects. (https://unity3d.com/asset-store/sell-assets/submission-guidelines , Section 4.1.e)",
                Detection = DetectionType.ErrorOnDetect,
                Category = PackageType.Art
            },
            new CheckItemData
            {
                Type = CheckType.PrefabTransform,
                Title = "Reset prefabs",
                Message = "Prefabs must have their position/rotation set to zero upon import, and should have their scale set to 1. Some of your prefabs are not set up this way.",
                Detection = DetectionType.WarningOnDetect,
                Category = PackageType.Art
            },
            new CheckItemData
            {
                Type = CheckType.PrefabCollider,
                Title = "Include colliders",
                Message = "It appears that your prefabs do not have any colliders applied to them. Please make sure you have appropriately sized colliders applied to your prefabs.",
                Detection = DetectionType.WarningOnDetect,
                Category = PackageType.Art
            },
            new CheckItemData
            {
                Type = CheckType.Documentation,
                Title = "Include documentation",
                Message = "We ask that you include documentation in the format of pdf, txt or rtf with your submission, as it is mandatory for all script packages and projects.Create a setup guide with a step-by step tutorial(video or pdf), as well as a script reference if users will need to do any coding. (https://unity3d.com/asset-store/sell-assets/submission-guidelines , Section 3.2.a)",
                Detection = DetectionType.ErrorOnAbsence,
                Category = PackageType.Script
            },
            new CheckItemData
            {
                Type = CheckType.Orientation,
                Title = "Fix orientation",
                Message = "Your meshes must have the correct orientation. The proper orientation is: Z - Vector is forward, Y - Vector is up, X - Vector is Right. (https://unity3d.com/asset-store/sell-assets/submission-guidelines , Section 4.1.j)",
                Detection = DetectionType.WarningOnDetect,
                Category = PackageType.Art
            },
            new CheckItemData
            {
                Type = CheckType.Jpg,
                Title = "Remove .jpg",
                Message = "You have some texture images within your package that are saved as jpgs. Please save all of your images as lossless format file types, such as PNG. (https://unity3d.com/asset-store/sell-assets/submission-guidelines , Section 4.1.3.a)",
                Detection = DetectionType.ErrorOnDetect,
                Category = PackageType.Art
            },
            new CheckItemData
            {
                Type = CheckType.Prepackage,
                Title = "Remove .package",
                Message = "We ask that you remove the .unitypackage from the submission as these files obscure the content of the package. If the content is essential to the submission then extract it into the file structure. (https://unity3d.com/asset-store/sell-assets/submission-guidelines , Section 3.4.a)",
                Detection = DetectionType.ErrorOnDetect,
                Category = PackageType.All
            },
            new CheckItemData
            {
                Type = CheckType.StandardAssets,
                Title = "Remove Standard Assets",
                Message = "Due to possible compatibility issues between Unity versions, we would ask that you remove all Standard Assets from your submission.If Standard Assets are required for demo scenes, or to achieve a certain effect, please list which packages the users should import in your README or other documentation. (https://unity3d.com/asset-store/sell-assets/submission-guidelines , Section 3.1.e)",
                Detection = DetectionType.ErrorOnDetect,
                Category = PackageType.All
            },
            new CheckItemData
            {
                Type = CheckType.MissingReference,
                Title = "Missing reference",
                Message = "There are missing or broken material/texture/prefab/script connections in your package. Before submitting your asset and creating your package, be sure to test it! Create a new project and import your package into it. Check that everything works properly—and take care that textures are linked to their respective materials. We often receive packages which do not work, throw exceptions, have unlinked textures and problems with surface normals. Please take extra care, this will save time in the long run.",
                Detection = DetectionType.ErrorOnDetect,
                Category = PackageType.All
            },
            new CheckItemData
            {
                Type = CheckType.JavaScript,
                Title = "Remove JavaScript",
                Message = "As of version 2017.2, Unity has deprecated UnityScript and we will no longer be accepting projects that include .js files. More information about UnityScript being deprecated and converting your code into C# can be found at https://blogs.unity3d.com/2017/08/11/unityscripts-long-ride-off-into-the-sunset/",
                Detection = DetectionType.ErrorOnDetect,
                Category = PackageType.Script
            }
        };

        // Search all files for the listed extensions, and returns them as (project relative) asset paths
        // Only returns paths within Asset directory  
        internal static List<string> GetPathsWithExtensions(string[] extensions, string[] exceptions = null)
        {
            exceptions = exceptions ?? new string[0];

            string path = Application.dataPath;
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            var allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

            var fileInfo = dirInfo.GetFiles("*", SearchOption.AllDirectories).
                Where(f => allowedExtensions.Contains(f.Extension));
            var paths = fileInfo.Select(f => ToProjectRelativePath(f.FullName)).
                Where(p => exceptions.All(e => !Path.GetDirectoryName(p).Contains(e)));

            return paths.ToList();
        }

        // Converts path to (project relative) asset paths (ex:Assets/Textures/image.png)
        internal static string ToProjectRelativePath(string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            //Strip data path
            string dataPath = Application.dataPath;
            dataPath = dataPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (path.StartsWith(dataPath))
                path = path.Substring(dataPath.Length + 1);

            //Add Assets directory
            if (!PathInAssetDir(path))
                path = Path.Combine("Assets", path);
            return path;
        }

        internal static bool PathInAssetDir(string path)
        {
            return (path.StartsWith("Assets" + Path.DirectorySeparatorChar) ||
                    path.StartsWith("Assets" + Path.AltDirectorySeparatorChar));
        }

        public static string GenerateUniquePath(string path, string extension)
        {
            var origExtension = Path.GetExtension(path).ToCharArray();
            path = path.TrimEnd(origExtension);

            var index = 0;
            string targetPath;
            do targetPath = path + '_' + ++index + extension;
            while (File.Exists(targetPath));

            return targetPath;
        }

        // Return the path of all model files
        // Attempts to exclude animations by filtering those without meshes, and with animation clips
        public static List<string> GetModelPaths()
        {
            var modelPaths = GetPathsWithExtensions(AS_Data.MODEL_EXTENSIONS);

            List<string> paths = new List<string>();

            foreach (var path in modelPaths)
            {
                var model = AS_Data.LoadAssetAtPath<GameObject>(path);
                List<Mesh> meshes = GetMeshes(model);

                if (meshes.Any()&&!HasAnimations(path))
                    paths.Add(path);
            }
            return paths;
        }

        private static bool HasAnimations(string path)
        {
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(path);
            var clips = modelImporter.clipAnimations.Count();
            return clips > 0;
        }

        //Returns all user-generated meshes by limiting search to the Assets directory; excludes primitives (Library/unity default resources)
        public static List<Mesh> GetMeshes(GameObject go)
        {
            List<Mesh> meshes = new List<Mesh>();

            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>(true);
            SkinnedMeshRenderer[] skinnedMeshes = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            meshes.AddRange(meshFilters.Select(m => m.sharedMesh));
            meshes.AddRange(skinnedMeshes.Select(m => m.sharedMesh));

            //Filter by Assets directory
            meshes = meshes.Where(m => AS_Data.PathInAssetDir(AssetDatabase.GetAssetPath(m))).ToList();

            return meshes;
        }

        public static Mesh GetMesh(Transform transform)
        {
            var meshFilter = transform.GetComponent<MeshFilter>();
            var skinnedMeshRenderer = transform.GetComponent<SkinnedMeshRenderer>();

            if (meshFilter)
                return meshFilter.sharedMesh;
            else if (skinnedMeshRenderer)
                return skinnedMeshRenderer.sharedMesh;

            return null;
        }

        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object
        {
        #if UNITY_5_1_OR_NEWER
			        return AssetDatabase.LoadAssetAtPath<T>(path);
        #else
                    return (T)AssetDatabase.LoadAssetAtPath(path, typeof(T));
        #endif
        }
    }

    // Comparer for paths with different directory separators 
    class CustomPathComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            x = x.Replace(Path.AltDirectorySeparatorChar,
                    Path.DirectorySeparatorChar);
            y = y.Replace(Path.AltDirectorySeparatorChar,
                Path.DirectorySeparatorChar);
            return x.Equals(y);
        }

        public int GetHashCode(string s)
        {
            return s.GetHashCode();
        }
    }
}