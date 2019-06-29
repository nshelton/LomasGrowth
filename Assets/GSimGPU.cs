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
        public int index;
        public int link0;
        public int link1;
        public int link2;
        public int link3;
        public int link4;
        public int link5;
        public int link6;
        public int link7;
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
        "Tick",
        "Collision",
        "Normal",
        "Bulge",
        "Plane",
        "Spring",
        "Split",
        "Integrate"
    };

    private const int SIZE_PARTICLE = 11 * sizeof(int) + 11 * sizeof(float);
    private const int SIZE_TRIANGLE = 18 * sizeof(float);
    private const int WARP_SIZE = 64;

    public ComputeShader m_computeShader;
    public ComputeBuffer m_ParticleBuffer;

    public ComputeBuffer m_triangleBuffer;
    public ComputeBuffer m_argBuffer;

    public GParticleGPU[] particleArray;

    public growthGPU parameters;

    #region private variables

    private Dictionary<string, int> m_kernelIDs = new Dictionary<string, int>();

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
    }

    public void InitParticle(ref GParticleGPU p)
    {
        p.link0 = -1;
        p.link1 = -1;
        p.link2 = -1;
        p.link3 = -1;
        p.link4 = -1;
        p.link5 = -1;
        p.link6 = -1;
        p.link7 = -1;
    }

    public void SetLinks(ref GParticleGPU p, List<int> list)
    {
        if ( list.Count > 0)
        {
            p.link0 = list[0];
        }
        if (list.Count > 1)
        {
            p.link1 = list[1];
        }
        if (list.Count > 2)
        {
            p.link2 = list[2];
        }
        if (list.Count > 3)
        {
            p.link3 = list[3];
        }
        if (list.Count > 4)
        {
            p.link4 = list[4];
        }
        if (list.Count > 5)
        {
            p.link5 = list[5];
        }
        if (list.Count > 6)
        {
            p.link6 = list[6];
        }
        if (list.Count > 7)
        {
            p.link7 = list[7];
        }
    }

    public bool ContainsLink(GParticleGPU cell, int target)
    {
        return cell.link0 == target ||
             cell.link1 == target ||
             cell.link2 == target ||
             cell.link3 == target ||
             cell.link4 == target ||
             cell.link5 == target ||
             cell.link6 == target ||
             cell.link7 == target;
    }

    public void AddLink(ref GParticleGPU cell, int target)
    {
        switch(cell.numLinks)
        {
            case 0 :
                cell.link0 = target;
                cell.numLinks++;
                break;
            case 1:
                cell.link1 = target;
                cell.numLinks++;
                break;
            case 2:
                cell.link2 = target;
                cell.numLinks++;
                break;
            case 3:
                cell.link3 = target;
                cell.numLinks++;
                break;
            case 4:
                cell.link4 = target;
                cell.numLinks++;
                break;
            case 5:
                cell.link5 = target;
                cell.numLinks++;
                break;
            case 6:
                cell.link6 = target;
                cell.numLinks++;
                break;
            case 7:
                cell.link7 = target;
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
            result += ", " + cell.link0;
        }
        if (cell.numLinks > 1)
        {
            result += ", " + cell.link1;
        }
        if (cell.numLinks > 2)
        {
            result += ", " + cell.link2;
        }
        if (cell.numLinks > 3)
        {
            result += ", " + cell.link3;
        }
        if (cell.numLinks > 4)
        {
            result += ", " + cell.link4;
        }
        if (cell.numLinks > 5)
        {
            result += ", " + cell.link5;
        }
        if (cell.numLinks > 6)
        {
            result += ", " + cell.link6;
        }
        if (cell.numLinks > 7)
        {
            result += ", " + cell.link7;
        }
        return result;
    }


    public int GetLink( GParticleGPU cell, int i)
    {
        switch (i)
        {
            case 0: return cell.link0;
            case 1: return cell.link1;
            case 2: return cell.link2;
            case 3: return cell.link3;
            case 4: return cell.link4;
            case 5: return cell.link5;
            case 6: return cell.link6;
            case 7: return cell.link7;
            default:
                return -1;
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

        Debug.Log("wtff");
        return -1;
    }

    private void OrderNeighbors(ref GParticleGPU p)
    {
        Debug.Log($"actual: {p.index} : {PrintLinks(p)} : { p.numLinks} ");

        if (p.numLinks < 3)
        {
            return;
        }

        List<int> orderedLinks = new List<int>();

        orderedLinks.Add(GetLink(p,0));
        orderedLinks.Add(GetNext(p, GetLink(p, 0)));

        for (int i = 2; i < p.numLinks; i++)
        {
            Debug.Log($"links for {p.index} : {string.Join(",", orderedLinks)}");
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
            InitParticle(ref p);
            p.position = m.vertices[i]; 
            p.normal = m.normals[i];
            p.index = i;
            particleList.Add(p);
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

        //TODO this should be append later
        m_ParticleBuffer = new ComputeBuffer(particleArray.Length, SIZE_PARTICLE, ComputeBufferType.Append);
        m_ParticleBuffer.SetData(particleArray);

        m_triangleBuffer = new ComputeBuffer(1024, SIZE_TRIANGLE, ComputeBufferType.Append);
        m_argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

    }
    private void InitShadersAndBuffers()
    {
        m_kernelIDs["GenerateMesh"] = m_computeShader.FindKernel("GenerateMesh");
        m_computeShader.SetBuffer(m_kernelIDs["GenerateMesh"], "ParticleRWBuffer", m_ParticleBuffer);
        m_computeShader.SetBuffer(m_kernelIDs["GenerateMesh"], "TriangleAppendBufffer", m_triangleBuffer);

        foreach(var kernelName in m_kernelNames)
        {
            m_kernelIDs[kernelName] = m_computeShader.FindKernel(kernelName);
            m_computeShader.SetBuffer(m_kernelIDs[kernelName], "ParticleRWBuffer", m_ParticleBuffer);
        }
    }

    void RunSim()
    {
        var mWarpCount = Mathf.CeilToInt((float)m_ParticleBuffer.count / WARP_SIZE);
        m_computeShader.Dispatch(m_kernelIDs["Collision"], mWarpCount, 1, 1);
        m_computeShader.Dispatch(m_kernelIDs["Normal"], mWarpCount, 1, 1); // good
        m_computeShader.Dispatch(m_kernelIDs["Bulge"], mWarpCount, 1, 1); // good
        m_computeShader.Dispatch(m_kernelIDs["Spring"], mWarpCount, 1, 1); // maybe
        m_computeShader.Dispatch(m_kernelIDs["Plane"], mWarpCount, 1, 1);
        m_computeShader.Dispatch(m_kernelIDs["Split"], mWarpCount, 1, 1);
        m_computeShader.Dispatch(m_kernelIDs["Integrate"], mWarpCount, 1, 1); //good
    }

    void GenMesh()
    {
        m_triangleBuffer.SetCounterValue(0);
        var mWarpCount = Mathf.CeilToInt((float)m_ParticleBuffer.count / WARP_SIZE);
        m_computeShader.Dispatch(m_kernelIDs["GenerateMesh"], mWarpCount, 1, 1);

        int[] args = new int[] { 0, 1, 0, 0 };
        m_argBuffer.SetData(args);

        ComputeBuffer.CopyCount(m_triangleBuffer, m_argBuffer, 0);

        m_argBuffer.GetData(args);
        args[0] *= 3;
        m_argBuffer.SetData(args);

        Debug.Log("Vertex count:" + args[0]);
    }

    public void Tick()
    {
        if (!m_kernelIDs.ContainsKey("GenerateMesh") || m_kernelIDs["GenerateMesh"] == 0)
        {
            InitShadersAndBuffers();
        }

        SetParameters();
        RunSim();

        GenMesh();
    }

    public void Draw(Material mat, Transform transform)
    {
        mat.SetPass(0);
        mat.SetBuffer("triangles", m_triangleBuffer);
        mat.SetMatrix("model", transform.localToWorldMatrix);
        Graphics.DrawProceduralIndirect(MeshTopology.Triangles, m_argBuffer);
    }
}
