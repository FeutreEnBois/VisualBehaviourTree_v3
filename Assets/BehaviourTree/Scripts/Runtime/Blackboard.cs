using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is the blackboard container shared between all nodes.
// Use this to store temporary data that multiple nodes need read and write access to.
[System.Serializable]
public class Blackboard
{
    public Vector3 moveToPosition;
}
