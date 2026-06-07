#ifndef SIMULATION_CORE_H
#define SIMULATION_CORE_H

#ifdef _WIN32
#ifdef SIMULATIONCORE_EXPORTS
#define SIM_API __declspec(dllexport)
#else
#define SIM_API __declspec(dllimport)
#endif
#else
#define SIM_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct SimWorld SimWorld;

SIM_API SimWorld* sim_create(int particle_count);
SIM_API void sim_destroy(SimWorld* world);
SIM_API void sim_reset(SimWorld* world);
SIM_API void sim_step(SimWorld* world, float delta_time);
SIM_API int sim_get_particle_count(const SimWorld* world);
SIM_API int sim_get_positions(const SimWorld* world, float* out_xyz, int max_floats);

#ifdef __cplusplus
}
#endif

#endif
