using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetStoreHelper
{
    public enum CheckStatus
    {
        Pass,
        Warning,
        Error
    }

    [Serializable]
    public class AS_ChecklistItem : ScriptableObject
    {
        public CheckType Type;

        [SerializeField]
        public List<string> AssetPaths = new List<string>();
        public CheckStatus Status = CheckStatus.Pass;
        public bool Active = true;

        //GUI
        public bool Foldout = false;
        public bool FoldoutMessage = true;
        public bool FoldoutPaths = true;

        internal void Init(AS_Data.CheckItemData data)
        {
            Type = data.Type;
            EditorUtility.SetDirty(this);
        }

        //Add a (project relative) asset path
        internal void AddPath(string path)
        {
            if (!AssetPaths.Contains(path))
                AssetPaths.Add(path);
        }

        internal void AddPaths(List<string> paths)
        {
            foreach (string p in paths)
                AddPath(p);
        }

        internal void Clear()
        {
            AssetPaths.Clear();
            Status = CheckStatus.Pass;
        }

        internal void UpdateState()
        {
            var Detection = AS_Data.ItemData[(int)Type].Detection;
            bool hasIssue = (Detection == DetectionType.ErrorOnAbsence) ?
                !AssetPaths.Any() : AssetPaths.Any();
            Status = hasIssue ?
                (Detection == DetectionType.WarningOnDetect ? CheckStatus.Warning : CheckStatus.Error)
                : CheckStatus.Pass;
        }

        //Remove any paths that have been deleted 
        internal void CheckAssetsForDeletion(string[] deletedAssets)
        {
            int pathNum = AssetPaths.Count;

            deletedAssets = deletedAssets.Select(d => Path.GetFullPath(d)).ToArray();
            AssetPaths.RemoveAll(asset => deletedAssets.Contains(Path.GetFullPath(asset)));

            if (AssetPaths.Count != pathNum)
                UpdateState();
        }

        //Replace any paths that have been moved with their new paths 
        internal void CheckAssetsForMove(string[] movedFromAssetPaths, string[] movedAssets)
        {
            bool requiresUpdate = false;

            for (int j = 0; j < movedAssets.Length; j++)
            {
                string m = movedFromAssetPaths[j];
                var i = AssetPaths.FindIndex(x => Path.GetFullPath(x).Equals(Path.GetFullPath(m)));
                if (i > -1)
                {
                    AssetPaths[i] = movedAssets[j];
                    requiresUpdate = true;
                }
            }

            if (requiresUpdate)
                UpdateState();
        }
    }
}
    

