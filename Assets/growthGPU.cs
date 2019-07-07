using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class growthGPU : MonoBehaviour
{

    [SerializeField, Range(4,30)]
    public float maxNeighbors = 12; 
    [SerializeField, Range(0f,10)]
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
    [SerializeField, Range(0,300)]
    public float threshold = 10;

    [SerializeField]
    private Material triangleMat;
    [SerializeField]
    private ComputeShader m_computeShader;

    private GSimGPU sim = new GSimGPU();

    [SerializeField]
    public Mesh m_mesh;

    private void OnEnable()
    {
        sim.m_computeShader = m_computeShader;
        sim.Init(m_mesh);
        sim.parameters = this;
    }

    private void Update()
    {
        sim.Tick();
    }

    private void OnRenderObject()
    {
        sim.Draw(triangleMat, transform);
    }

    private void OnDestroy()
    {
        sim.Release();
    }

    /*
    void OnDrawGizmos()
    {
        if (sim != null && sim.particleArray != null) 
        for (int i = 0; i < sim.particleArray.Length; i++)
        {
            for (int ii = 0; ii < sim.particleArray[i].numLinks; ii++)
            {
                int targetIndex = sim.GetLink(sim.particleArray[i], ii);
                Gizmos.DrawLine(sim.particleArray[i].position, sim.particleArray[targetIndex].position);

            }
        }
    }
    */

}
