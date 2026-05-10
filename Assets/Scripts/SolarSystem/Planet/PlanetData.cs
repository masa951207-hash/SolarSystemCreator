using System;
using UnityEngine;

public enum PlanetType { Rocky, Gas }

[Serializable]
public class PlanetData
{
    public string planetName  = "Planet";
    public float  radius      = 1f;
    public float  mass        = 1f;
    public Color  color       = Color.cyan;
    public PlanetType planetType  = PlanetType.Rocky;
    public float  orbitDistance  = 20f;
    public float  orbitSpeed     = 2.2f;
    public float  rotationSpeed  = 10f;
}
