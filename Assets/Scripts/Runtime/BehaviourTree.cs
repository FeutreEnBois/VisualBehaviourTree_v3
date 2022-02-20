using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu()]
public class BehaviourTree : ScriptableObject
{
    public Node rootNode;
    public Node.State treeState = Node.State.Running;
    public List<Node> nodes = new List<Node>();
    public Node.State Update()
    {
        if(rootNode.state == Node.State.Running)
        {
            treeState = rootNode.Update();
        }
        return treeState;
    }

    public Node CreateNode(System.Type type)
    {
        Node node = ScriptableObject.CreateInstance(type) as Node;
        node.name = type.Name;
        node.guid = GUID.Generate().ToString();
        nodes.Add(node);
        
        AssetDatabase.AddObjectToAsset(node,this);
        AssetDatabase.SaveAssets();
        return node;
    }

    public void DeleteNode(Node node)
    {
        nodes.Remove(node);
        AssetDatabase.RemoveObjectFromAsset(node);
        AssetDatabase.SaveAssets();
    }

    public void AddChild(Node parent, Node child)
    {
        
        if(parent is DecoratorNode decorator)
        {
            decorator.child = child;
        }
        //
        if(parent is RootNode root){
            root.child = child;
        }

        if(parent is CompositeNode compositeNode)
        {
            compositeNode.children.Add(child);
        }
    }

    public void RemoveChild(Node parent, Node child)
    {
        if (parent is DecoratorNode decorator)
        {
            decorator.child = null;
        }
        if (parent is RootNode root)
        {
            root.child = null;
        }
        if (parent is CompositeNode compositeNode)
        {
            compositeNode.children.Remove(child);
        }
    }

    public List<Node> GetChildren(Node parent)
    {
        List<Node> children = new List<Node>();

        if (parent is DecoratorNode decorator && decorator.child != null)
        {
            children.Add(decorator.child);
        }
        if (parent is RootNode root && root.child != null)
        {
            children.Add(root.child);
        }

        if (parent is CompositeNode compositeNode)
        {
            return compositeNode.children;
        }
        return children;
    }

    public BehaviourTree Clone()
    {
        BehaviourTree tree = Instantiate(this);
        tree.rootNode = tree.rootNode.Clone();
        return tree;
    }

}
