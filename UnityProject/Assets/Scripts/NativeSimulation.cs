using System;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class NativeSimulation : IDisposable
{
    private const string LibraryName = "SimulationCore";

    private readonly float[] positions;
    private readonly ManagedParticle[] managedParticles;
    private readonly bool usingNative;
    private IntPtr world;

    public NativeSimulation(int particleCount)
    {
        try
        {
            world = sim_create(particleCount);
            if (world != IntPtr.Zero)
            {
                usingNative = true;
                ParticleCount = sim_get_particle_count(world);
                positions = new float[ParticleCount * 3];
                managedParticles = Array.Empty<ManagedParticle>();
                Debug.Log("SimulationCore native plugin loaded.");
                return;
            }
        }
        catch (DllNotFoundException)
        {
            Debug.LogWarning("SimulationCore native plugin was not found. Using managed fallback simulation.");
        }
        catch (EntryPointNotFoundException exception)
        {
            Debug.LogWarning($"SimulationCore API mismatch. Using managed fallback simulation. {exception.Message}");
        }

        ParticleCount = Mathf.Max(1, particleCount);
        positions = new float[ParticleCount * 3];
        managedParticles = new ManagedParticle[ParticleCount];
        ResetManaged();
    }

    public int ParticleCount { get; }

    public void Reset()
    {
        if (usingNative)
        {
            EnsureAlive();
            sim_reset(world);
            return;
        }

        ResetManaged();
    }

    public Vector3[] Step(float deltaTime)
    {
        if (!usingNative)
        {
            return StepManaged(deltaTime);
        }

        EnsureAlive();
        sim_step(world, deltaTime);
        int count = sim_get_positions(world, positions, positions.Length);
        var result = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = new Vector3(
                positions[i * 3 + 0],
                positions[i * 3 + 1],
                positions[i * 3 + 2]);
        }

        return result;
    }

    public void Dispose()
    {
        if (!usingNative || world == IntPtr.Zero)
        {
            return;
        }

        sim_destroy(world);
        world = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }

    private void EnsureAlive()
    {
        if (usingNative && world == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(NativeSimulation));
        }
    }

    private void ResetManaged()
    {
        for (int i = 0; i < managedParticles.Length; i++)
        {
            managedParticles[i] = new ManagedParticle
            {
                Position = new Vector3(InitialX(i, managedParticles.Length), 2.0f + (i % 5) * 0.25f, 0.0f),
                Velocity = new Vector3(0.25f * Mathf.Sin(i), 0.0f, 0.25f * Mathf.Cos(i))
            };
        }
    }

    private Vector3[] StepManaged(float deltaTime)
    {
        const float gravity = -9.81f;
        const float floorY = 0.0f;
        const float restitution = 0.72f;

        var result = new Vector3[managedParticles.Length];

        for (int i = 0; i < managedParticles.Length; i++)
        {
            ManagedParticle particle = managedParticles[i];
            particle.Velocity.y += gravity * deltaTime;
            particle.Position += particle.Velocity * deltaTime;

            if (particle.Position.y < floorY)
            {
                particle.Position.y = floorY;
                particle.Velocity.y = -particle.Velocity.y * restitution;
            }

            managedParticles[i] = particle;
            result[i] = particle.Position;
        }

        return result;
    }

    private static float InitialX(int index, int count)
    {
        if (count <= 1)
        {
            return 0.0f;
        }

        return ((float)index / (count - 1) - 0.5f) * 8.0f;
    }

    ~NativeSimulation()
    {
        Dispose();
    }

    private struct ManagedParticle
    {
        public Vector3 Position;
        public Vector3 Velocity;
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sim_create(int particleCount);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void sim_destroy(IntPtr world);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void sim_reset(IntPtr world);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void sim_step(IntPtr world, float deltaTime);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sim_get_particle_count(IntPtr world);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sim_get_positions(IntPtr world, [Out] float[] outXyz, int maxFloats);
}
