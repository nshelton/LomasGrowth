
using System.Collections.Generic;
using UnityEngine;

public class GParticle
{
    public bool connectedTo(int i) { return links.Contains(i); }

    public void addLink(int i)
    {
        if (!connectedTo(i)) { links.Add(i); }
    }

    public void removeLink(int i)
    {
        links.Remove(i);
    }

    public List<int> links = new List<int>();

    public int age;
    public float food;
    public float curvature;

    public Vector3 position, delta, normal;

    public int collisions;

    public int index;

    public bool frozen;
};
