using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class GSimGPU
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GParticleGPU
    {
        public int link00;
        public int link01;
        public int link02;
        public int link03;
        public int link04;
        public int link05;
        public int link06;
        public int link07;
        public int link08;
        public int link09;
        public int link10;
        public int link11;
        public int link12;
        public int link13;
        public int link14;
        public int link15;
        public int numLinks;
        public int age;
        public float food;
        public float curvature;
        public Vector3 position;
        public Vector3 delta;
        public Vector3 normal;
    }

    string[] m_kernelNames = new string[]
    {
        "GenerateMesh",
        "Collision",
        "Normal",
        "Bulge",
        "Plane",
        "Spring",
        "Split",
        "Integrate",
        "RotateTest",
    };

    private const int SIZE_PARTICLE = 18 * sizeof(int) + 11 * sizeof(float);
    private const int SIZE_TRIANGLE = 18 * sizeof(float);
    private const int WARP_SIZE = 64;
    private const int MAX_PARTICLES = 1024;
        

    public ComputeShader m_computeShader;

    public ComputeBuffer m_particleBuffer;
    public ComputeBuffer m_particleCounterBuffer;

    public ComputeBuffer m_triangleBuffer;
    public ComputeBuffer m_triangleArgBuffer;
    public ComputeBuffer m_particleArgBuffer;

    public GParticleGPU[] particleArray;

    public growthGPU parameters;

    #region private variables

    private Dictionary<string, int> m_kernelIDs = new Dictionary<string, int>();

    private int m_currentNumParticles;
    private int m_frameNum;

    #endregion

    void SetParameters()
    {
        m_computeShader.SetFloat("_maxNeighbors", parameters.maxNeighbors);
        m_computeShader.SetFloat("_radius", parameters.radius);
        m_computeShader.SetFloat("_collisionFactor", parameters.collisionFactor);
        m_computeShader.SetFloat("_bulgeFactor", parameters.bulgeFactor);
        m_computeShader.SetFloat("_springFactor", parameters.springFactor);
        m_computeShader.SetFloat("_springLength", parameters.springLength);
        m_computeShader.SetFloat("_planarFactor", parameters.planarFactor);
        m_computeShader.SetFloat("_dampening", parameters.dampening);
        m_computeShader.SetFloat("_foodExponent", parameters.foodExponent);
        m_computeShader.SetFloat("_threshold", parameters.threshold);
        m_computeShader.SetInt("_numParticles", m_currentNumParticles);
        m_computeShader.SetInt("_frameNum", m_frameNum);
    }

    public void InitLinks(ref GParticleGPU p)
    {
        p.link00 = -1;
        p.link01 = -1;
        p.link02 = -1;
        p.link03 = -1;
        p.link04 = -1;
        p.link05 = -1;
        p.link06 = -1;
        p.link07 = -1;
        p.link08 = -1;
        p.link09 = -1;
        p.link10 = -1;
        p.link11 = -1;
        p.link12 = -1;
        p.link13 = -1;
        p.link14 = -1;
        p.link15 = -1;
    }

    public void SetLinks(ref GParticleGPU p, List<int> list)
    {
        if (list.Count > 0)
        {
            p.link00 = list[0];
        }
        if (list.Count > 1)
        {
            p.link01 = list[1];
        }
        if (list.Count > 2)
        {
            p.link02 = list[2];
        }
        if (list.Count > 3)
        {
            p.link03 = list[3];
        }
        if (list.Count > 4)
        {
            p.link04 = list[4];
        }
        if (list.Count > 5)
        {
            p.link05 = list[5];
        }
        if (list.Count > 6)
        {
            p.link06 = list[6];
        }
        if (list.Count > 7)
        {
            p.link07 = list[7];
        }
    }

    public bool ContainsLink(GParticleGPU cell, int target)
    {
        return  cell.link00 == target ||
                cell.link01 == target ||
                cell.link02 == target ||
                cell.link03 == target ||
                cell.link04 == target ||
                cell.link05 == target ||
                cell.link06 == target ||
                cell.link07 == target ||
                cell.link08 == target ||
                cell.link09 == target ||
                cell.link10 == target ||
                cell.link11 == target ||
                cell.link12 == target ||
                cell.link13 == target ||
                cell.link14 == target ||
                cell.link15 == target;
    }

    public void AddLink(ref GParticleGPU cell, int target)
    {
        switch (cell.numLinks)
        {
            case 0:
                cell.link00 = target;
                cell.numLinks++;
                break;
            case 1:
                cell.link01 = target;
                cell.numLinks++;
                break;
            case 2:
                cell.link02 = target;
                cell.numLinks++;
                break;
            case 3:
                cell.link03 = target;
                cell.numLinks++;
                break;
            case 4:
                cell.link04 = target;
                cell.numLinks++;
                break;
            case 5:
                cell.link05 = target;
                cell.numLinks++;
                break;
            case 6:
                cell.link06 = target;
                cell.numLinks++;
                break;
            case 7:
                cell.link07 = target;
                cell.numLinks++;
                break;
            default:
                Debug.LogError("too many links");
                break;
        }
    }

    public string PrintLinks(GParticleGPU cell)
    {
        string result = string.Empty;
        if (cell.numLinks > 0)
        {
            result += cell.link00;
        }
        if (cell.numLinks > 1)
        {
            result += ", " + cell.link01;
        }
        if (cell.numLinks > 2)
        {
            result += ", " + cell.link02;
        }
        if (cell.numLinks > 3)
        {
            result += ", " + cell.link03;
        }
        if (cell.numLinks > 4)
        {
            result += ", " + cell.link04;
        }
        if (cell.numLinks > 5)
        {
            result += ", " + cell.link05;
        }
        if (cell.numLinks > 6)
        {
            result += ", " + cell.link06;
        }
        if (cell.numLinks > 7)
        {
            result += ", " + cell.link07;
        }
        if (cell.numLinks > 8)
        {
            result += ", " + cell.link08;
        }
        if (cell.numLinks > 9)
        {
            result += ", " + cell.link09;
        }
        if (cell.numLinks >10)
        {
            result += ", " + cell.link10;
        }
        if (cell.numLinks > 11)
        {
            result += ", " + cell.link11;
        }
        if (cell.numLinks > 12)
        {
            result += ", " + cell.link12;
        }
        if (cell.numLinks > 13)
        {
            result += ", " + cell.link13;
        }
        if (cell.numLinks > 14)
        {
            result += ", " + cell.link14;
        }
        if (cell.numLinks > 15)
        {
            result += ", " + cell.link15;
        }

        return result;
    }

    public int GetLink(GParticleGPU cell, int i)
    {
        switch (i)
        {
            case 0: return cell.link00;
            case 1: return cell.link01;
            case 2: return cell.link02;
            case 3: return cell.link03;
            case 4: return cell.link04;
            case 5: return cell.link05;
            case 6: return cell.link06;
            case 7: return cell.link07;
            case 8: return cell.link08;
            case 9: return cell.link09;
            case 10: return cell.link10;
            case 11: return cell.link11;
            case 12: return cell.link12;
            case 13: return cell.link13;
            case 14: return cell.link14;
            case 15: return cell.link15;
            default:
                return -99;
        }
    }

    private void ConnectCell(ref GParticleGPU left, ref GParticleGPU right, int i, int j)
    {
        if (!ContainsLink(right, i))
        {
            AddLink(ref right, i);
        }
        if (!ContainsLink(left, j))
        {
            AddLink(ref left, j);
        }
    }

    private int GetNext(GParticleGPU p, int i) { return GetNext(p, i, i); }

    private int GetNext(GParticleGPU p, int previousIndex, int currentIndex)
    {
        Debug.Log($"{previousIndex} - {currentIndex}");
        GParticleGPU current = particleArray[currentIndex];

        for (int i = 0; i < p.numLinks; i++)
        {
            for (int j = 0; j < current.numLinks; j++)
            {
                if ((GetLink(p, i) == GetLink(current, j)) &&
                    (GetLink(p, i) != previousIndex) && (GetLink(p, i) != currentIndex))
                {
                    return GetLink(p, i);
                }
            }
        }

        return -1;
    }

    private void OrderNeighbors(ref GParticleGPU p)
    {
        if (p.numLinks < 3)
        {
            return;
        }

        List<int> orderedLinks = new List<int>();

        orderedLinks.Add(GetLink(p, 0));
        orderedLinks.Add(GetNext(p, GetLink(p, 0)));

        for (int i = 2; i < p.numLinks; i++)
        {
            orderedLinks.Add(
                GetNext(p, orderedLinks[i - 2], orderedLinks[i - 1]));
        }

        SetLinks(ref p, orderedLinks);
    }

    public void Init(Mesh m)
    {
        List<GParticleGPU> particleList = new List<GParticleGPU>();
        for (int i = 0; i < m.vertexCount; i++)
        {
            GParticleGPU p = new GParticleGPU();
            InitLinks(ref p);
            p.position = m.vertices[i];
            p.normal = m.normals[i];
            particleList.Add(p);

            if (i  == 0)
                p.food = 10;
        }

        particleArray = particleList.ToArray();

        for (int i = 0; i < m.GetIndexCount(0); i += 3)
        {
            var ii = m.GetIndices(0)[i];
            var jj = m.GetIndices(0)[i + 1];
            ConnectCell(ref particleArray[ii], ref particleArray[jj], ii, jj);

            ii = m.GetIndices(0)[i];
            jj = m.GetIndices(0)[i + 2];
            ConnectCell(ref particleArray[ii], ref particleArray[jj], ii, jj);

            ii = m.GetIndices(0)[i + 1];
            jj = m.GetIndices(0)[i + 2];
            ConnectCell(ref particleArray[ii], ref particleArray[jj], ii, jj);
        }

        m_particleBuffer = new ComputeBuffer(MAX_PARTICLES, SIZE_PARTICLE, ComputeBufferType.Counter);
        m_particleBuffer.SetData(particleArray);
        m_particleBuffer.SetCounterValue((uint)particleArray.Length);

        m_triangleBuffer = new ComputeBuffer(1024, SIZE_TRIANGLE, ComputeBufferType.Append);
        m_currentNumParticles = particleArray.Length;

        m_triangleArgBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        m_particleArgBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
    }

    private void InitShadersAndBuffers()
    {
        foreach (var kernelName in m_kernelNames)
        {
            m_kernelIDs[kernelName] = m_computeShader.FindKernel(kernelName);
            m_computeShader.SetBuffer(m_kernelIDs[kernelName], "ParticleRWBuffer", m_particleBuffer);
        }

        m_computeShader.SetBuffer(m_kernelIDs["GenerateMesh"], "TriangleAppendBufffer", m_triangleBuffer);

    }

    private int GetParticleCount()
    {
        int[] args = new int[] { 0, 1, 0, 0 };
        m_particleArgBuffer.SetData(args);
        ComputeBuffer.CopyCount(m_particleBuffer, m_particleArgBuffer, 0);
        m_particleArgBuffer.GetData(args);
        return args[0];
    }

    GParticleGPU[] lastgpuData = new GParticleGPU[MAX_PARTICLES];
    GParticleGPU[] thisgpuData = new GParticleGPU[MAX_PARTICLES];
    private List<int> m_particleCounts = new List<int>();

    void RunSim()
    {
        var mWarpCount = Mathf.CeilToInt((float)m_currentNumParticles / WARP_SIZE);
        m_computeShader.Dispatch(m_kernelIDs["Collision"], mWarpCount, 1, 1);
        m_computeShader.Dispatch(m_kernelIDs["Normal"], mWarpCount, 1, 1); // good
        m_computeShader.Dispatch(m_kernelIDs["Bulge"], mWarpCount, 1, 1); // good
        m_computeShader.Dispatch(m_kernelIDs["Spring"], mWarpCount, 1, 1); // maybe
        m_computeShader.Dispatch(m_kernelIDs["Plane"], mWarpCount, 1, 1);

        m_computeShader.Dispatch(m_kernelIDs["Split"], mWarpCount, 1, 1);


        Debug.Log("Particle count:" + m_currentNumParticles);
        m_computeShader.Dispatch(m_kernelIDs["Integrate"], mWarpCount, 1, 1); //good

        lastgpuData = (GParticleGPU[])(thisgpuData.Clone());
        m_particleCounts.Add(m_currentNumParticles);

        m_currentNumParticles = GetParticleCount();
        m_particleBuffer.GetData(thisgpuData);

        CheckGraphConsistency();
        CheckCorruption();

    }

    private void CheckCorruption()
    {
        for (int i = 0; i < m_currentNumParticles; i++)
        {

            if (PrintLinks(lastgpuData[i]) != PrintLinks(thisgpuData[i]))
            {
                Debug.LogWarning($"================linkChange= {i}=================");

                Debug.LogWarning($"<<<<{i} was ({lastgpuData[i].numLinks}) {PrintLinks(lastgpuData[i])}");
                Debug.LogWarning($">>>>{i} now ({thisgpuData[i].numLinks}) {PrintLinks(thisgpuData[i])}");
            }

            for (int j = 0; j < thisgpuData[i].numLinks; j++)
            {
                if (GetLink(thisgpuData[i], j) < 0)
                {
                    Debug.LogError($"{i} : {j} {GetLink(thisgpuData[i], j)}");
                }
            }
        }
    }

    private void CheckGraphConsistency()
    {

        for ( int i = 0; i < thisgpuData.Length; i ++)
        {
            var cell = thisgpuData[i];
            for(int j = 0; j < cell.numLinks; j ++)
            {
                var to = GetLink(cell, j);
                var from = i;

                if ( to < 0  || to >= thisgpuData.Length)
                {
                    Debug.LogError($"{ i} has link-> {to}");
                }
                else if  (!ContainsLink(thisgpuData[to], from))
                {
                    Debug.LogError("Inconsistent");
                    Debug.LogError($"<<<<{from} : {PrintLinks(thisgpuData[from])}");
                    Debug.LogError($">>>>{to} : {PrintLinks(thisgpuData[to])}");
                }
            }
        }

    }

    void CreateMesh()
    {
        m_triangleBuffer.SetCounterValue(0);
        var mWarpCount = Mathf.CeilToInt((float)m_currentNumParticles / WARP_SIZE);
        m_computeShader.Dispatch(m_kernelIDs["GenerateMesh"], mWarpCount, 1, 1);

        int[] args = new int[] { 0, 1, 0, 0 };
        m_triangleArgBuffer.SetData(args);

        ComputeBuffer.CopyCount(m_triangleBuffer, m_triangleArgBuffer, 0);

        m_triangleArgBuffer.GetData(args);
        args[0] *= 3;
        m_triangleArgBuffer.SetData(args);

        Debug.Log("Vertex count:" + args[0]);
    }

    public void Tick()
    {
        if ( m_currentNumParticles >= MAX_PARTICLES)
        {
            Debug.Log("reached MAX Partickes");
            return;
        }

        if (!m_kernelIDs.ContainsKey("GenerateMesh") || m_kernelIDs["GenerateMesh"] == 0)
        {
            InitShadersAndBuffers();
        }
      
              SetParameters();
              RunSim();
              CreateMesh();
   
        m_frameNum++;


        /*    GParticleGPU[] gpuData = new GParticleGPU[MAX_PARTICLES];

          m_computeShader.Dispatch(m_kernelIDs["RotateTest"], 1, 1, 1);
          m_currentNumParticles = GetParticleCount();

          m_particleBuffer.GetData(gpuData);

          Debug.Log(PrintLinks(gpuData[0]));
          m_computeShader.SetInt("_frameNum", m_frameNum);
            */
    }

    public void Draw(Material mat, Transform transform)
    {
        mat.SetPass(0);
        mat.SetBuffer("triangles", m_triangleBuffer);
        mat.SetMatrix("model", transform.localToWorldMatrix);
        
        Graphics.DrawProceduralIndirect(MeshTopology.Triangles, m_triangleArgBuffer);
    }

    public void Release()
    {
        m_particleBuffer.Release();
        m_particleArgBuffer.Release();

        m_triangleBuffer.Release();
        m_triangleArgBuffer.Release();
    }
}
