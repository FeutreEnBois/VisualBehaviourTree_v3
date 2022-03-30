using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Callbacks;

// UnityEditor.EditorWindow : Derive from this class to create an editor window.
//Create your own custom editor window that can float free or be docked as a tab, just like the native windows in the Unity interface.
//Editor windows are typically opened using a menu item.
public class BehaviourTreeEditor : EditorWindow
{
    BehaviourTreeView treeView;
    BehaviourTree tree;

    InspectorView inspectorView;
    IMGUIContainer blackboardView;

    ToolbarMenu toolbarMenu;
    TextField treeNameField;
    TextField locationPathField;
    Button createNewTreeButton;
    VisualElement overlay;
    BehaviourTreeSettings settings;

    SerializedObject treeObject;
    SerializedProperty blackboardProperty;

//    The MenuItem attribute allows you to add menu items to the main menu and inspector context menus.
//The MenuItem attribute turns any static function into a menu command.
//Only static functions can use the MenuItem attribute.

   [MenuItem("BehaviourTreeEditor/Scripts/Editor ...")]
    public static void OpenWindow()
    {
        BehaviourTreeEditor wnd = GetWindow<BehaviourTreeEditor>();
        // GetWindow<>() : 
        #region
        //t	-> The type of the window. Must derive from EditorWindow.
        //utility = false -> Set this to true, to create a floating utility window, false to create a normal window.
        //title = null -> If GetWindow creates a new window, it will get this title.If this value is null, use the class name as title.
        //focus = true -> Whether to give the window focus, if it already exists. (If GetWindow creates a new window, it will always get focus).
        #endregion
        wnd.titleContent = new GUIContent("BehaviourTreeEditor"); // The GUIContent used for drawing the title of EditorWindows.
        wnd.minSize = new Vector2(800, 600);
    }

    //Adding this attribute to a static method will make the method be called when Unity is about to open an asset.
    //Return true if you handled the opening of the asset or false if an external tool should open it.
    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceId, int line)
    {
        if(Selection.activeObject is BehaviourTree)
        {
            OpenWindow();
            return true;
        }
        return false; 
    }

    List<T> LoadAssets<T>() where T : UnityEngine.Object
    {
        string[] assetIds = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        List<T> assets = new List<T>();
        foreach (var assetId in assetIds)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetId);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            assets.Add(asset);
        }
        return assets;
    }

    //VisualTreeAsset :
    // An instance of this class holds a tree of `VisualElementAsset`s, created from a UXML file. Each node in the file corresponds to a `VisualElementAsset`. You can clone a `VisualTreeAsset` to yield a tree of `VisualElement`s.
    public void CreateGUI()
    {
        settings = BehaviourTreeSettings.GetOrCreateSettings();

        // Each "EditorWindow" contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree = settings.behaviourTreeXml;
        visualTree.CloneTree(root);

        /*var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/BehaviourTreeEditor.uxml"); // Returns the first asset object of type type at given path assetPath.
        visualTree.CloneTree(root);*/


        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = settings.behaviourTreeStyle;
        root.styleSheets.Add(styleSheet);

        treeView = root.Q<BehaviourTreeView>(); // UQuery is a set of extension methods allowing you to select individual or collection of visualElements inside a complex hierarchy.
        treeView.OnNodeSelected = OnNodeSelectionChanged; // not a called method, but a reference to a method. OnNodeSelected type is Action<NodeView>
        inspectorView = root.Q<InspectorView>();
        blackboardView = root.Q<IMGUIContainer>();
        blackboardView.onGUIHandler = () => {
            if (treeObject != null && treeObject.targetObject != null)
            {
                treeObject.Update();
                EditorGUILayout.PropertyField(blackboardProperty);
                treeObject.ApplyModifiedProperties();
            }
        };

        // Toolbar assets menu
        toolbarMenu = root.Q<ToolbarMenu>();
        var behaviourTrees = LoadAssets<BehaviourTree>();
        behaviourTrees.ForEach(tree => {
            toolbarMenu.menu.AppendAction($"{tree.name}", (a) => {
                Selection.activeObject = tree;
            });
        });
        toolbarMenu.menu.AppendSeparator();
        toolbarMenu.menu.AppendAction("New Tree...", (a) => CreateNewTree("NewBehaviourTree"));

        // New Tree Dialog
        treeNameField = root.Q<TextField>("TreeName");
        locationPathField = root.Q<TextField>("LocationPath");
        overlay = root.Q<VisualElement>("Overlay");
        createNewTreeButton = root.Q<Button>("CreateButton");
        createNewTreeButton.clicked += () => CreateNewTree(treeNameField.value);

        if (tree == null)
        {
            OnSelectionChange();
        }
        else
        {
            SelectTree(tree);
        }

    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange obj)
    {
        switch (obj)
        {
            case PlayModeStateChange.EnteredEditMode:
                OnSelectionChange();
                break;
            case PlayModeStateChange.ExitingEditMode:
                break;
            case PlayModeStateChange.EnteredPlayMode:
                OnSelectionChange();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                break;
        }
    }

    // Called whenever the selection has changed.
    // called as soon as a scriptableObject BehaviourTree is selected in the hierarchy
    private void OnSelectionChange()
    {
        EditorApplication.delayCall += () => {
            BehaviourTree tree = Selection.activeObject as BehaviourTree;
            if (!tree)
            {
                if (Selection.activeGameObject)
                {
                    BehaviourTreeRunner runner = Selection.activeGameObject.GetComponent<BehaviourTreeRunner>();
                    if (runner)
                    {
                        tree = runner.tree;
                    }
                }
            }

            SelectTree(tree);
        };
    }

    void SelectTree(BehaviourTree newTree)
    {

        if (treeView == null)
        {
            return;
        }

        if (!newTree)
        {
            return;
        }

        this.tree = newTree;

        overlay.style.visibility = Visibility.Hidden;

        if (Application.isPlaying)
        {
            treeView.PopulateView(tree);
        }
        else
        {
            treeView.PopulateView(tree);
        }


        treeObject = new SerializedObject(tree);
        blackboardProperty = treeObject.FindProperty("blackboard");

        EditorApplication.delayCall += () => {
            treeView.FrameAll();
        };
    }

    void OnNodeSelectionChanged(NodeView node)
    {
        inspectorView.UpdateSelection(node);
    }


    private void OnInspectorUpdate()
    {
        treeView?.UpdateNodeStates();
    }

    void CreateNewTree(string assetName)
    {
        string path = System.IO.Path.Combine(locationPathField.value, $"{assetName}.asset");
        BehaviourTree tree = ScriptableObject.CreateInstance<BehaviourTree>();
        tree.name = treeNameField.ToString();
        AssetDatabase.CreateAsset(tree, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = tree;
        EditorGUIUtility.PingObject(tree);
    }
}