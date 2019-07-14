using KdTree;
using KdTree.Math;
using Supercluster.KDTree;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class GSim
{
    private int simFrame = 0;
    public List<GParticle> cells;
    public growth parameters;

    public void initialize(growth param, Mesh meshTemplate)
    {
        parameters = param;
        simFrame = 0;
        GenerateCells(meshTemplate);
    }

    private void ConnectCell(int i, int j)
    {
        if (!cells[i].connectedTo(j))
        {
            cells[i].addLink(j);
        }
        if (!cells[j].connectedTo(i))
        {
            cells[j].addLink(i);
        }
    }

    private void GenerateCells( Mesh m)
    {
        cells = new List<GParticle>();

        for (int i = 0; i < m.vertexCount; ++i)
        {
            GParticle p = new GParticle();
            p.position = m.vertices[i];
            p.normal = m.normals[i].normalized;
            p.index = i;
            cells.Add(p);
        }

        for (int i = 0; i<m.GetIndexCount(0); i += 3)
        {
            ConnectCell(m.GetIndices(0)[i], m.GetIndices(0)[i + 1]);
            ConnectCell(m.GetIndices(0)[i], m.GetIndices(0)[i + 2]);
            ConnectCell(m.GetIndices(0)[i + 1], m.GetIndices(0)[i + 2]);
        }
    }

    private bool doSim = true;

    public void Tick()
    {
        Profiler.BeginSample("AddCollisionForce");
        //AddCollisionForce();
        AddCollisionBruteForce();
        Profiler.EndSample();

        Profiler.BeginSample("Iterate");
        for (int i = 0; i < cells.Count; i++)
        {
            var p = cells[i];
            CalculateNormal(p);

            AddBulgeForce(p);

            AddSpringForce(p);

            AddPlanarForce(p);

            p.food += Mathf.Pow(Mathf.Max(p.curvature / 20.0f, 0.00001f),
                               parameters.foodExponent);
        }
        Profiler.EndSample();

        Profiler.BeginSample("All Splits");
        for ( int i = 0; i < cells.Count; i ++)
        {
            var p = cells[i];
            if (p.food > parameters.threshold)
            {
                Split(p);
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("IntegrateParticles");
        IntegrateParticles();
        Profiler.EndSample();

        Debug.Log($"fRAME {simFrame++} : {cells.Count}");
    }

    private void IntegrateParticles()
    {
        foreach (var p in cells)
        {
            p.position += p.delta * parameters.dampening;
            p.delta = Vector3.zero;
        }
    }

    private void Split(GParticle p)
    {
        int index = (int)cells.Count;
        cells.Add(new GParticle());
        GParticle baby = cells[index];

        baby.normal = p.normal;
        baby.position = p.position;
        baby.index = index;
        baby.food = 1;
        SetLinks(p, baby);

        SetPositions(p, baby);

        CalculateNormal(p);
        CalculateNormal(baby);

        baby.food = 0;
        p.food = 0;
    }

    private Vector3 tmpNormal;

    int orientation(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float val = (p2.y - p1.y) * (p3.x - p2.x) -
                  (p2.x - p1.x) * (p3.y - p2.y);

        if (val == 0)
            return 0; // colinear 

        return (val > 0) ? -1 : 1; // clock or counterclock wise 
    }


    private void CalculateNormal(GParticle p)
    {
        OrderNeighbors(p);

        tmpNormal = Vector3.zero ;

        for (int i = 0; i < p.links.Count; i++)
        {
            Vector3 c = cells[p.links[i]].position;
            Vector3 d = cells[p.links[(i + 1) % p.links.Count]].position;
            tmpNormal += Vector3.Cross(d, c);
        }

        // area = newNormal.squaredNorm();
        tmpNormal = tmpNormal.normalized;

        if (orientation(
            p.position, 
            cells[p.links[0]].position, 
            cells[p.links[1]].position) < 0 )
        {
            p.normal = tmpNormal;
        }
        else
        {
            p.normal = -tmpNormal;
            p.links.Reverse();
        }
    }

    private int FindShortestAxis(GParticle p)
    {
        List<int> links = p.links;

        float minLength = 0;
        int shortestIndex = -1;
        for (int i = 0; i<links.Count; i++)
        {
            float distance = (p.position - cells[links[i]].position).magnitude;
            int opposite = (i + (links.Count / 2)) % links.Count;
            distance += (p.position - cells[links[opposite]].position).magnitude;
            if (distance<minLength || i == 0)
            {
                minLength = distance;
                shortestIndex = (int) i;
    }
        }
        return shortestIndex;
    }

    private int FindLongestAxis(GParticle p)
    {
        List<int> links = p.links;

        float maxLength = float.MaxValue;
        int longestIndex = -1;
        for (int i = 0; i < links.Count; i++)
        {
            float distance = (p.position - cells[links[i]].position).magnitude;
            int opposite = (i + (links.Count / 2)) % links.Count;
            distance += (p.position - cells[links[opposite]].position).magnitude;
            if (distance > maxLength || i == 0)
            {
                maxLength = distance;
                longestIndex = (int)i;
            }
        }
        return longestIndex;
    }

    private int GetNext(GParticle p, int i) { return GetNext(p, i, i); }

    private int GetNext(GParticle p, int previousIndex, int currentIndex)
    {
        GParticle current = cells[currentIndex];

        for (int i = 0; i < p.links.Count; i++)
        {
            for (int j = 0; j < current.links.Count; j++)
            {
                if ((p.links[i] == current.links[j]) &&
                    (p.links[i] != previousIndex) && 
                    (p.links[i] != currentIndex))
                {
                    return p.links[i];
                }
            }
        }

        return -1;
    }

    private void OrderNeighbors(GParticle p)
    {
        if (p.links.Count < 3)
        {
            return;
        }

        List<int> orderedLinks = new List<int>();

        orderedLinks.Add(p.links[0]);
        orderedLinks.Add(GetNext(p, p.links[0]));

        for (int i = 2; i < p.links.Count; i++)
        {
            orderedLinks.Add(GetNext(p, orderedLinks[i - 2], orderedLinks[i - 1]));
        }

        p.links = orderedLinks;
    }

    public List<int> Rotate(List<int> items, int places)
    {
        List<int> result = new List<int>();

        result.AddRange(items.GetRange(places, items.Count - places));
        result.AddRange(items.GetRange(0, places));

        return result;

    }

    private void SetLinks(GParticle parent, GParticle baby)
    {
        OrderNeighbors(parent);

        int firstIndex;
        if (parameters.longestAxis)
        {
            firstIndex = FindLongestAxis(parent);
        }
        else
        {
            firstIndex = FindShortestAxis(parent);
        }

        List<int> originalLinks = Rotate(parent.links, firstIndex);

        for (int i = 0; i <= originalLinks.Count/2;  i++)
        {
            baby.addLink(originalLinks[i]);
            cells[originalLinks[i]].addLink(baby.index);

            if ( i != 0 && i != originalLinks.Count / 2)
            {
                parent.removeLink(originalLinks[i]);
                cells[originalLinks[i]].removeLink(parent.index);
            }
        }

        parent.addLink(baby.index);
        baby.addLink(parent.index);
    }

    private void SetPositions(GParticle parent, GParticle baby)
    {
        Vector3 babyAverage = parent.position;
        for (int i = 0; i < baby.links.Count; ++i)
        {
            babyAverage += cells[baby.links[i]].position;
        }

        babyAverage /= baby.links.Count + 1;

        Vector3 parentAverage = parent.position;
        for (int i = 0; i < parent.links.Count; ++i)
        {
            parentAverage += cells[parent.links[i]].position;
        }
        parentAverage /= parent.links.Count + 1;

        // set positions
        baby.position = babyAverage;
        parent.position = parentAverage;
    }

    public double L2Norm(double[] x, double[] y)
    {
        double dist = 0f;
        for (int i = 0; i < x.Length; i++)
        {
            dist += (x[i] - y[i]) * (x[i] - y[i]);
        }

        return dist;
    }

    private List<double[]> _points = new List<double[]>();
    private List<int> _nodes = new List<int>();

    private void AddCollisionBruteForce()
    {
        var tree = new KdTree<float, int>(3, new FloatMath());

        for (int i = 0; i < cells.Count; i++)
        {
            tree.Add(new[] { cells[i].position.x, cells[i].position.y, cells[i].position.z }, i);
        }


        double radiusSquared = parameters.radius * parameters.radius;

        for (int i = 0; i < cells.Count; i++)
        {
            GParticle p = cells[i];
            p.collisions = 0;

            var rsquared = parameters.radius * parameters.radius;

            var neighbors = tree.GetNearestNeighbours(new[] { cells[i].position.x, cells[i].position.y, cells[i].position.z },
                (int)parameters.maxNeighbors);

            for (int j = 0; j < neighbors.Length; j++)
            {
                GParticle q = cells[neighbors[j].Value];
                Vector3 displacement = p.position - q.position;

                if (displacement.magnitude < parameters.radius)
                {
                    float distanceSquared = displacement.x * displacement.x +
                          displacement.y * displacement.y +
                          displacement.z * displacement.z;

                    if ((i != j) && (!p.connectedTo(j)))
                    {
                        displacement.Normalize();
                        displacement *= (rsquared - distanceSquared) / rsquared; // TODO sqrt here? NGS what is this even ????

                        p.delta += displacement * parameters.collisionFactor;
                        p.collisions++;
                    }
                }
            }
        }
    }

    private void AddCollisionForce()
    {
        _points.Clear();
        _nodes.Clear();

        for (int i =0; i < cells.Count; i++)
        {
            var p = cells[i];
            _points.Add(new double[] { p.position.x, p.position.y, p.position.z });
            _nodes.Add(i);
        }

        KDTree<double, int> tree = new KDTree<double, int>(3, _points.ToArray(), _nodes.ToArray(), L2Norm);

        double radiusSquared = parameters.radius * parameters.radius;
            
        for(int i = 0; i < cells.Count; i++)
        {
            GParticle p = cells[i];
            p.collisions = 0;

            var rsquared = parameters.radius * parameters.radius;

            Tuple<double[], int>[] neighbors = tree.NearestNeighbors(new double[] { p.position.x, p.position.y, p.position.z }, (int)parameters.maxNeighbors);
            
            foreach (var neighbor in neighbors)
            {
                GParticle q = cells[neighbor.Item2];
                Vector3 displacement = p.position - q.position;

                float distanceSquared = displacement.x * displacement.x +
                                        displacement.y * displacement.y +
                                        displacement.z * displacement.z;

                if ((i != neighbor.Item2) && (!p.connectedTo(neighbor.Item2)))
                {
                    displacement.Normalize();
                    displacement *= (rsquared - distanceSquared) / rsquared; // TODO sqrt here? NGS what is this even ????

                    p.delta += displacement * parameters.collisionFactor;
                    p.collisions++;
                }
            }
        }
    }

    private void AddBulgeForce(GParticle p)
    {

        float bulgeDistance = 0;
        float thetaL, thetaD, thetaC, radicand;
        for (int i = 0; i < p.links.Count; i++)
        {
            Vector3 d = cells[p.links[i]].position - p.position;
            thetaL = Mathf.Acos(Vector3.Dot(d, p.normal) / d.magnitude);
            thetaD = Mathf.Asin(d.magnitude * Mathf.Sin(thetaL) / parameters.springLength);
            thetaC = Mathf.PI - thetaD - thetaL;

            if (float.IsNaN(thetaC) || float.IsInfinity(thetaC))
            {
                continue;
            }

            radicand = Mathf.Pow(parameters.springLength, 2f) + d.sqrMagnitude -
                        2.0f * d.magnitude * parameters.springLength * Mathf.Cos(thetaC);

            if (radicand < 0.0)
            {
                radicand = 0;
            }

            bulgeDistance += Mathf.Sqrt(radicand);
        }

        bulgeDistance /= p.links.Count;

        p.delta += p.normal * bulgeDistance * parameters.bulgeFactor;
    }

    private void AddSpringForce(GParticle  p)
    {
        Vector3 target = Vector3.zero;
        foreach(var l in p.links)
        {
            Vector3 d = cells[l].position - p.position;
            d.Normalize();
            d *= parameters.springLength;
            target += d;
        }

        target /= p.links.Count;
        target *= parameters.springLength;
        p.delta += target * parameters.springFactor;
    }

    private void AddPlanarForce(GParticle p)
    {
        Vector3 planarTarget = Vector3.zero;

        for (int i = 0; i < p.links.Count; ++i)
        {
            planarTarget += cells[p.links[i]].position;
        }
        planarTarget /= p.links.Count;
        planarTarget = planarTarget - p.position;

        p.curvature = -1.0f * planarTarget.magnitude *
                        Vector3.Dot(p.normal, planarTarget.normalized);

        p.delta += planarTarget * parameters.planarFactor;
    }


    public Mesh GetMesh()
    {
        //Profiler.BeginSample("GetMesh");

        Mesh m = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> ints = new List<int>();

        for (int i = 0; i < cells.Count; ++i)
        {
            verts.Add(cells[i].position);
            colors.Add(Color.white * (cells[i].curvature + 5.0f) * 20.0f);
            normals.Add(-cells[i].normal);
        }

        m.SetVertices(verts);
        m.SetColors(colors);
        m.SetNormals(normals);

        for (int i = 0; i < cells.Count; ++i)
        {
            GParticle p = cells[i];

            int numLinks = p.links.Count;
            for (int ii = 0; ii < numLinks; ++ii)
            {
                ints.Add(p.index);
                ints.Add(p.links[ii]);
                ints.Add(p.links[(ii + 1) % numLinks]);
            }
        }

        m.SetIndices(ints.ToArray(), MeshTopology.Triangles,0);
        //Profiler.EndSample();

        return m;
    }
}