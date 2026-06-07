using System;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class NativeSimulation : IDisposable
{
    private const string LibraryName = "SimulationCore";

    private readonly float[] positions;
    private IntPtr world;

    public NativeSimulation(int particleCount)
    {
        world = sim_create(particleCount);
        if (world == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create native simulation world.");
        }

        ParticleCount = sim_get_particle_count(world);
        positions = new float[ParticleCount * 3];
    }

    public int ParticleCount { get; }

    public void Reset()
    {
        EnsureAlive();
        sim_reset(world);
    }

    public Vector3[] Step(float deltaTime)
    {
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
        if (world == IntPtr.Zero)
        {
            return;
        }

        sim_destroy(world);
        world = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }

    private void EnsureAlive()
    {
        if (world == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(NativeSimulation));
        }
    }

    ~NativeSimulation()
    {
        Dispose();
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
