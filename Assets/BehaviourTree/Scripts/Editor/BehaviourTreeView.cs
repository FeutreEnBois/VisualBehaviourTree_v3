using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;

public class BehaviourTreeView : GraphView
{
    public Action<NodeView> OnNodeSelected; // Action type can store reference to Methods
    public new class UxmlFactory : UxmlFactory<BehaviourTreeView, GraphView.UxmlTraits> { } // Instantiates and clones a TemplateContainer using the data read from a UXML file.

    BehaviourTree tree;
    BehaviourTreeSettings settings;

    public struct ScriptTemplate
    {
        public TextAsset templateFile;
        public string defaultFileName;
        public string subFolder;
    }

    public ScriptTemplate[] scriptFileAssets = {

            new ScriptTemplate{ templateFile=BehaviourTreeSettings.GetOrCreateSettings().scriptTemplateActionNode, defaultFileName="NewActionNode.cs", subFolder="Actions" },
            new ScriptTemplate{ templateFile=BehaviourTreeSettings.GetOrCreateSettings().scriptTemplateCompositeNode, defaultFileName="NewCompositeNode.cs", subFolder="Composites" },
            new ScriptTemplate{ templateFile=BehaviourTreeSettings.GetOrCreateSettings().scriptTemplateDecoratorNode, defaultFileName="NewDecoratorNode.cs", subFolder="Decorators" },
    };

    public BehaviourTreeView()
    {
        settings = BehaviourTreeSettings.GetOrCreateSettings();
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        this.AddManipulator(new DoubleClickSelection());

        var styleSheet = settings.behaviourTreeStyle;
        styleSheets.Add(styleSheet);

        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        PopulateView(tree);
        AssetDatabase.SaveAssets();
    }

    public NodeView FindNodeView(Node node)
    {
        return GetNodeByGuid(node.guid) as NodeView; // See node.guid = GUID.Generate() :)
    }

    internal void PopulateView(BehaviourTree tree)
    {
        this.tree = tree;
        // GraphViewToolWindow.OnGraphViewChanged : Callback invoked when the GraphView has changed.
        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements.ToList());
        graphViewChanged += OnGraphViewChanged;

        // If no rootNode in the tree, create one
        if(tree.rootNode == null)
        {
            tree.rootNode = tree.CreateNode(typeof(RootNode)) as RootNode;
            EditorUtility.SetDirty(tree); // You can use SetDirty when you want to modify an object without creating an undo entry, but still ensure the change is registered and not lost. -> we have modified the Editor, need to apply the changes
            AssetDatabase.SaveAssets(); // we have created a new Node in the BT.asset, need to apply the changes
        }
        // Creates nodes view
        tree.nodes.ForEach(n => CreateNodeView(n)); // System.Linq syntax

        // Creates edges view
        
        tree.nodes.ForEach(n =>
        {
            var children = BehaviourTree.GetChildren(n);
            children.ForEach(c =>
            {
                NodeView parentView = FindNodeView(n);
                NodeView childView = FindNodeView(c);

                Edge edge = parentView.output.ConnectTo(childView.input); // connect parent to child
                AddElement(edge);

            });
        });

    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endPort =>
        endPort.direction != startPort.direction &&
        endPort.node != startPort.node).ToList();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        if(graphViewChange.elementsToRemove != null)
        {
            graphViewChange.elementsToRemove.ForEach(e =>
            {
                NodeView nodeView = e as NodeView;
                if(nodeView != null)
                {
                    tree.DeleteNode(nodeView.node);
                }

                Edge edge = e as Edge;
                if (edge != null)
                {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;
                    tree.RemoveChild(parentView.node, childView.node);
                }
            });
        }

        if (graphViewChange.edgesToCreate != null)
        {
            graphViewChange.edgesToCreate.ForEach(edge =>
            {
                NodeView parentView = edge.output.node as NodeView;
                NodeView childView = edge.input.node as NodeView;
                tree.AddChild(parentView.node, childView.node);
            });
        }
        nodes.ForEach((n) => {
            NodeView view = n as NodeView;
            view.SortChildren();
        });

        return graphViewChange;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        // New script functions
        evt.menu.AppendAction($"Create Script.../New Action Node", (a) => CreateNewScript(scriptFileAssets[0]));
        evt.menu.AppendAction($"Create Script.../New Composite Node", (a) => CreateNewScript(scriptFileAssets[1]));
        evt.menu.AppendAction($"Create Script.../New Decorator Node", (a) => CreateNewScript(scriptFileAssets[2]));
        evt.menu.AppendSeparator();

        Vector2 nodePosition = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);

        //Comment region
        #region
        //base.BuildContextualMenu(evt);
        // AppendAction : Add an item that will execute an action in the drop-down menu. The item is added at the end of the current item list.
        //Add a new action for every ActionNode
        //You can use an open and close set of curly braces to define a self containing block, which has its own scope.
        #endregion
        {
            var types = TypeCache.GetTypesDerivedFrom<ActionNode>(); // get all nodes derived from an action nodes <3
            foreach (var type in types)
            {
                evt.menu.AppendAction($"[Action]/{type.Name}", (a) => CreateNode(type, nodePosition));
            }
        }
        //Add a new action for every CompositeNode
        {
            var types = TypeCache.GetTypesDerivedFrom<CompositeNode>(); // get all nodes derived from an composite nodes <3
            foreach (var type in types)
            {
                evt.menu.AppendAction($"[Composite]/{type.Name}", (a) => CreateNode(type, nodePosition));
            }
        }
        //Add a new action for every DecoratorNode
        {
            var types = TypeCache.GetTypesDerivedFrom<DecoratorNode>(); // get all nodes derived from an decorator nodes <3
            foreach (var type in types)
            {
                evt.menu.AppendAction($"[Decorator]/{type.Name}", (a) => CreateNode(type, nodePosition));
            }
        }
    }

    void SelectFolder(string path)
    {
        // https://forum.unity.com/threads/selecting-a-folder-in-the-project-via-button-in-editor-window.355357/
        // Check the path has no '/' at the end, if it does remove it,
        // Obviously in this example it doesn't but it might
        // if your getting the path some other way.

        if (path[path.Length - 1] == '/')
            path = path.Substring(0, path.Length - 1);

        // Load object
        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

        // Select the object in the project folder
        Selection.activeObject = obj;

        // Also flash the folder yellow to highlight it
        EditorGUIUtility.PingObject(obj);
    }

    void CreateNewScript(ScriptTemplate template)
    {
        SelectFolder($"{settings.newNodeBasePath}/{template.subFolder}");
        var templatePath = AssetDatabase.GetAssetPath(template.templateFile);
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, template.defaultFileName);
    }

    private void CreateNode(System.Type type, Vector2 position)
    {
        Node node = tree.CreateNode(type);
        node.position = position;
        CreateNodeView(node);
    }

    void CreateNodeView(Node node)
    {
        NodeView nodeView = new NodeView(node);
        nodeView.OnNodeSelected = OnNodeSelected;
        AddElement(nodeView);
    }

    public void UpdateNodeStates()
    {
        nodes.ForEach(n => {
            NodeView view = n as NodeView;
            view.UpdateState();
        });
    }


}
