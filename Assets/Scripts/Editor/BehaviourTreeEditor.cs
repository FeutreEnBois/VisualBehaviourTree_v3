using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;
using System;

// UnityEditor.EditorWindow : Derive from this class to create an editor window.
//Create your own custom editor window that can float free or be docked as a tab, just like the native windows in the Unity interface.
//Editor windows are typically opened using a menu item.
public class BehaviourTreeEditor : EditorWindow
{
    BehaviourTreeView treeView;
    InspectorView inspectorView;
    IMGUIContainer blackboardView;

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

    //VisualTreeAsset :
    // An instance of this class holds a tree of `VisualElementAsset`s, created from a UXML file. Each node in the file corresponds to a `VisualElementAsset`. You can clone a `VisualTreeAsset` to yield a tree of `VisualElement`s.
    public void CreateGUI()
    {
        // Each "EditorWindow" contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/BehaviourTreeEditor.uxml"); // Returns the first asset object of type type at given path assetPath.
        visualTree.CloneTree(root);
        

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/BehaviourTreeEditor.uss");
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
        OnSelectionChange();
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

        if (tree)
        {
            treeView.PopulateView(tree);
        }

        if (tree != null)
        {
            treeObject = new SerializedObject(tree);
            blackboardProperty = treeObject.FindProperty("blackboard");
        }
    }

    void OnNodeSelectionChanged(NodeView node)
    {
        inspectorView.UpdateSelection(node);
    }
}