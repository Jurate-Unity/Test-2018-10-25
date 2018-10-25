using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetStoreHelper
{
    [InitializeOnLoad]
    public class AS_Window : EditorWindow
    {
        private static AS_Window window;
        private Vector2 scrollPos;
        
        private Texture2D errorIcon;
        private Texture2D warningIcon;
        private Texture2D infoIcon;
        private Texture2D checkIcon;

        private bool showErrorItems = true;
        private bool showWarningItems = true;
        private bool showPassItems = true;

        [MenuItem("Asset Store/Asset Store Helper")]
        public static void ShowWindow()
        {
            if (window == null)
            {
                window = GetWindow<AS_Window>();
                //window.titleContent = new GUIContent("AS Helper");
                //EditorWindow.GetWindow(typeof(ASWindow), false, "Asset Tool Helper Window", true);
            }
            window.Show();
        }

        public void OnEnable()
        {
            if(!checkIcon)
                checkIcon = EditorGUIUtility.Load("icons/vcs_check.png") as Texture2D;                
            if(!infoIcon)
                infoIcon = EditorGUIUtility.Load("icons/console.infoicon.png") as Texture2D;
            if (!errorIcon)
                errorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;
            if(!warningIcon)
                warningIcon = EditorGUIUtility.Load("icons/console.warnicon.png") as Texture2D;
        }

        private void Indent(int indentions = 1)
        {
            const int TAB_SIZE = 15;
            GUILayout.BeginHorizontal();
            GUILayout.Space(TAB_SIZE * indentions);
        }
        private void DrawIcon(Texture2D icon, int iconSize)
        {
            Rect position = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.MaxWidth(iconSize));
            position.x = 15f;
            position.y -= 2.5f;
            GUI.DrawTexture(position, icon, ScaleMode.ScaleToFit);
        }

        public void OnGUI()
        {
            AS_Checklist _checklist = AS_Checklist.GetCheckList();

            //Title
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Asset Store Helper", EditorStyles.boldLabel);
            GUILayout.Space(5);

            //Package selection dropdown
            PackageType prevPackageType = _checklist.PackageType;
            _checklist.PackageType = (PackageType)EditorGUILayout.EnumPopup("Package type:", _checklist.PackageType);

            if (prevPackageType != _checklist.PackageType)
                _checklist.ApplyPackageType();

            //Scan button
            bool scan = (GUILayout.Button("Scan"));
            if (scan)
            {
                _checklist.Scan();
            }

            EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);

            //Checklist menu bar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("Checklist", EditorStyles.boldLabel, GUILayout.MaxWidth(75));
            GUILayout.FlexibleSpace();
            
            showPassItems = GUILayout.Toggle(showPassItems, new GUIContent(infoIcon), EditorStyles.toolbarButton, GUILayout.Width(30));
            showWarningItems = GUILayout.Toggle(showWarningItems, new GUIContent(warningIcon), EditorStyles.toolbarButton, GUILayout.Width(30));
            showErrorItems = GUILayout.Toggle(showErrorItems, new GUIContent(errorIcon), EditorStyles.toolbarButton, GUILayout.Width(30));

            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            GUILayout.Space(7);

            //Foldout per check, sorted by status 
            if (showErrorItems)
            {
                foreach (var error in _checklist.Checks.Where(c => c.Status == CheckStatus.Error))
                    ChecklistItemGUI(error, errorIcon);
            }
            if (showWarningItems)
            {
                foreach (var warning in _checklist.Checks.Where(c => c.Status == CheckStatus.Warning))
                    ChecklistItemGUI(warning, warningIcon);
            }
            if (showPassItems)
            {
                foreach (var pass in _checklist.Checks.Where(c => c.Status == CheckStatus.Pass))
                    ChecklistItemGUI(pass, checkIcon);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ChecklistItemGUI(AS_ChecklistItem check, Texture2D icon = null)
        {
            if (!check.Active)
                return;

            GUILayout.BeginHorizontal();

            //Foldout
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            if (icon)
                foldoutStyle.contentOffset = new Vector2(20, 0);
            string title = AS_Data.ItemData[(int)check.Type].Title;
            check.Foldout = EditorGUILayout.Foldout(check.Foldout, title, foldoutStyle);

            //Draw icon to indicate warning/ error
            if (icon)
            {
                const int iconSize = 25;
                DrawIcon(icon, iconSize);
            }

            GUILayout.EndHorizontal();

            //Expanded
            if (check.Foldout)
            {
                string message = AS_Data.ItemData[(int)check.Type].Message;
                ChecklistMessageGUI(check, message);
                ChecklistAssetsGUI(check, check.AssetPaths);
            }
        }

        private void ChecklistMessageGUI(AS_ChecklistItem check, string message)
        {
            Indent();
            check.FoldoutMessage = EditorGUILayout.Foldout(check.FoldoutMessage, "Message:");

            //Foldout for Fogbugz message
            if (check.FoldoutMessage)
            {
                GUILayout.EndHorizontal();
                Indent(2);

                GUIStyle messageStyle = new GUIStyle(EditorStyles.textArea);
                messageStyle.wordWrap = true;
                EditorGUILayout.TextArea(message, messageStyle);
            }
            GUILayout.EndHorizontal();
        }

        private void SelectAndPing(List<string> paths)
        {
            var toSelect = paths.Select(f => AssetDatabase.LoadMainAssetAtPath(f));
            Selection.objects = toSelect.ToArray();
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private void OrientationAssetsGUI(AS_ChecklistItem check, List<string> paths)
        {
            Indent();
            check.FoldoutPaths = EditorGUILayout.Foldout(check.FoldoutPaths, "Assets:");

            //Button to select all assets
            if (GUILayout.Button("Select All", GUILayout.MaxWidth(80), GUILayout.MinWidth(80), GUILayout.MaxHeight(20), GUILayout.MinHeight(20)))
            {
                SelectAndPing(paths);
            }

            string tooltip = "Orientate and export assets. Does not work with animations and rigs.";
            if (GUILayout.Button(new GUIContent("Fix All", tooltip), GUILayout.MaxWidth(80), GUILayout.MinWidth(80),
                GUILayout.MaxHeight(20), GUILayout.MinHeight(20)))
            {
                List<string> rotatedPaths = AS_Utility.FixRotationExport(paths);
                SelectAndPing(rotatedPaths);
            }

            //Foldout for assets
            if (check.FoldoutPaths)
            {
                //Display object field per asset
                foreach (var a in paths)
                {
                    GUILayout.EndHorizontal();

                    Indent(2);
                    var loadedAsset = AssetDatabase.LoadMainAssetAtPath(a);
                    EditorGUILayout.ObjectField(loadedAsset, typeof(UnityEngine.Object), false);

                    if (GUILayout.Button(new GUIContent("Fix", tooltip), GUILayout.MaxWidth(80), GUILayout.MinWidth(80), GUILayout.MaxHeight(20), GUILayout.MinHeight(20)))
                    {
                        List<string> rotatedPaths = AS_Utility.FixRotationExport(new List<string>{a});
                        SelectAndPing(rotatedPaths);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ChecklistAssetsGUI(AS_ChecklistItem check, List<string> paths)
        {
            if(!paths.Any())
                return;

            //Special case for orientation check, which needs additional buttons to fix its assets
            if (check.Type == CheckType.Orientation)
            {
                OrientationAssetsGUI(check, paths);
                return;
            }

            Indent();
            check.FoldoutPaths = EditorGUILayout.Foldout(check.FoldoutPaths, "Assets:");

            //Button to select all assets
            if (GUILayout.Button("Select All", GUILayout.MaxWidth(80), GUILayout.MinWidth(80), GUILayout.MaxHeight(20), GUILayout.MinHeight(20)))
            {
                var toSelect = paths.Select(f => AssetDatabase.LoadMainAssetAtPath(f));
                Selection.objects = toSelect.ToArray();
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            //Foldout for assets
            if (check.FoldoutPaths)
            {
                //Display object field per asset
                foreach (var a in paths)
                {
                    GUILayout.EndHorizontal();

                    Indent(2);
                    var loadedAsset = AssetDatabase.LoadMainAssetAtPath(a);
                    EditorGUILayout.ObjectField(loadedAsset, typeof(UnityEngine.Object), false);
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}