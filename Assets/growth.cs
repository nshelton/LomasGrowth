using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class growth : MonoBehaviour
{

    [SerializeField, Range(4,30)]
    public float maxNeighbors = 12; 
    [SerializeField, Range(0.1f,10)]
    public float radius = 3;
    [SerializeField, Range(0,1)]
    public float collisionFactor = 1;
    [SerializeField, Range(0,1)]
    public float bulgeFactor = 0.1f;
    [SerializeField, Range(0,1)]
    public float springFactor = 0.1f; 
    [SerializeField, Range(0,10)]
    public float springLength = 0; 
    [SerializeField, Range(0,1)]
    public float planarFactor = 1;
    [SerializeField, Range(0,1)]
    public float dampening = 0.1f;
    [SerializeField, Range(0,10)]
    public float foodExponent = 1; 
    [SerializeField]
    public bool longestAxis = true;
    [SerializeField, Range(0,300)]
    public float threshold = 10;

    
    [SerializeField]
    private Material pointMat;
    private GSim sim = new GSim();


    [SerializeField]
    public Mesh m_mesh;

    public Mesh currentMesh;

    private void OnEnable()
    {
        sim.initialize(this, m_mesh);
    }

    private void Update()
    {
        sim.Tick();
        currentMesh = sim.GetMesh();
        Graphics.DrawMesh(currentMesh, Vector3.zero, Quaternion.identity, pointMat, 0);
    }

}
