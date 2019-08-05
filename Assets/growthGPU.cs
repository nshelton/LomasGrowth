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
    [SerializeField]
    private Mesh m_renderMesh;
    [SerializeField]
    private Material instanceMaterial;

    private GSimGPU sim = new GSimGPU();

    [SerializeField]
    public Mesh m_mesh;


    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds bigBounds = new Bounds(Vector3.zero, Vector3.one * 1e5f);

    private void OnEnable()
    {
        sim.m_computeShader = m_computeShader;
        sim.Init(m_mesh);
        sim.parameters = this;
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
    }

    private void Update()
    {
        sim.Tick();
        Render();
    }


    private void Render()
    {

        uint instanceCount = (uint)sim.NumParticles;

        Debug.Log(instanceCount);
        args[0] = (uint)m_renderMesh.GetIndexCount(0);
        args[1] = (uint)instanceCount;
        args[2] = 0;
        args[3] = 0;
        args[4] = 0;

        argsBuffer.SetData(args);

        var matrices = new Matrix4x4[sim.NumParticles];

        instanceMaterial.SetBuffer("particles", sim.m_particleBuffer);

        Graphics.DrawMeshInstancedIndirect(
            m_renderMesh,
            0,
            instanceMaterial,
            bigBounds,
            argsBuffer,
            0, 
            null, 
            UnityEngine.Rendering.ShadowCastingMode.On, 
            true);

        //   sim.Draw(triangleMat, transform);
    }

    private void OnDestroy()
    {
        sim.Release();
        argsBuffer.Release();
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
