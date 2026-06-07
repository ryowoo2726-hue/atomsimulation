using System.Collections.Generic;
using UnityEngine;

public sealed class SimulationController : MonoBehaviour
{
    [SerializeField] private int particleCount = 24;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private Transform particleRoot;
    [SerializeField] private Material particleMaterial;

    private readonly List<Transform> particles = new();
    private NativeSimulation simulation;

    private void Start()
    {
        if (particlePrefab == null)
        {
            particlePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particlePrefab.transform.localScale = Vector3.one * 0.25f;
            ApplyParticleMaterial(particlePrefab);
            particlePrefab.SetActive(false);
        }

        if (particleRoot == null)
        {
            var root = new GameObject("Particles");
            particleRoot = root.transform;
        }

        simulation = new NativeSimulation(particleCount);
        CreateParticles(simulation.ParticleCount);
    }

    private void Update()
    {
        if (simulation == null)
        {
            return;
        }

        Vector3[] positions = simulation.Step(Time.deltaTime);
        for (int i = 0; i < positions.Length && i < particles.Count; i++)
        {
            particles[i].position = positions[i];
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            simulation.Reset();
        }
    }

    private void OnDestroy()
    {
        simulation?.Dispose();
        simulation = null;
    }

    private void CreateParticles(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(particlePrefab, particleRoot);
            instance.name = $"Particle_{i:00}";
            instance.SetActive(true);
            ApplyParticleMaterial(instance);
            particles.Add(instance.transform);
        }
    }

    private void ApplyParticleMaterial(GameObject target)
    {
        if (particleMaterial == null || !target.TryGetComponent(out Renderer targetRenderer))
        {
            return;
        }

        targetRenderer.sharedMaterial = particleMaterial;
    }
}
