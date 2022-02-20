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
    public BehaviourTreeView()
    {
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/BehaviourTreeEditor.uss");
        styleSheets.Add(styleSheet);
    }

    NodeView FindNodeView(Node node)
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
            var children = tree.GetChildren(n);
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
            return graphViewChange;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
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
                evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a) => CreateNode(type));
            }
        }
        //Add a new action for every CompositeNode
        {
            var types = TypeCache.GetTypesDerivedFrom<CompositeNode>(); // get all nodes derived from an composite nodes <3
            foreach (var type in types)
            {
                evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a) => CreateNode(type));
            }
        }
        //Add a new action for every DecoratorNode
        {
            var types = TypeCache.GetTypesDerivedFrom<DecoratorNode>(); // get all nodes derived from an decorator nodes <3
            foreach (var type in types)
            {
                evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a) => CreateNode(type));
            }
        }
    }

    private void CreateNode(System.Type type)
    {
        Node node = tree.CreateNode(type);
        CreateNodeView(node);
    }

    void CreateNodeView(Node node)
    {
        NodeView nodeView = new NodeView(node);
        nodeView.OnNodeSelected = OnNodeSelected;
        AddElement(nodeView);
    }


}
