#include "simulation_core.h"

#include <math.h>
#include <stdlib.h>

typedef struct Particle {
    float x;
    float y;
    float z;
    float vx;
    float vy;
    float vz;
} Particle;

struct SimWorld {
    int particle_count;
    float elapsed;
    Particle* particles;
};

static float initial_x(int index, int count)
{
    if (count <= 1) {
        return 0.0f;
    }

    return ((float)index / (float)(count - 1) - 0.5f) * 8.0f;
}

SimWorld* sim_create(int particle_count)
{
    if (particle_count <= 0) {
        particle_count = 16;
    }

    SimWorld* world = (SimWorld*)calloc(1, sizeof(SimWorld));
    if (world == NULL) {
        return NULL;
    }

    world->particle_count = particle_count;
    world->particles = (Particle*)calloc((size_t)particle_count, sizeof(Particle));
    if (world->particles == NULL) {
        free(world);
        return NULL;
    }

    sim_reset(world);
    return world;
}

void sim_destroy(SimWorld* world)
{
    if (world == NULL) {
        return;
    }

    free(world->particles);
    free(world);
}

void sim_reset(SimWorld* world)
{
    if (world == NULL || world->particles == NULL) {
        return;
    }

    world->elapsed = 0.0f;

    for (int i = 0; i < world->particle_count; i++) {
        Particle* particle = &world->particles[i];
        particle->x = initial_x(i, world->particle_count);
        particle->y = 2.0f + (float)(i % 5) * 0.25f;
        particle->z = 0.0f;
        particle->vx = 0.25f * sinf((float)i);
        particle->vy = 0.0f;
        particle->vz = 0.25f * cosf((float)i);
    }
}

void sim_step(SimWorld* world, float delta_time)
{
    if (world == NULL || world->particles == NULL || delta_time <= 0.0f) {
        return;
    }

    const float gravity = -9.81f;
    const float floor_y = 0.0f;
    const float restitution = 0.72f;

    world->elapsed += delta_time;

    for (int i = 0; i < world->particle_count; i++) {
        Particle* particle = &world->particles[i];

        particle->vy += gravity * delta_time;
        particle->x += particle->vx * delta_time;
        particle->y += particle->vy * delta_time;
        particle->z += particle->vz * delta_time;

        if (particle->y < floor_y) {
            particle->y = floor_y;
            particle->vy = -particle->vy * restitution;
        }
    }
}

int sim_get_particle_count(const SimWorld* world)
{
    if (world == NULL) {
        return 0;
    }

    return world->particle_count;
}

int sim_get_positions(const SimWorld* world, float* out_xyz, int max_floats)
{
    if (world == NULL || world->particles == NULL || out_xyz == NULL || max_floats <= 0) {
        return 0;
    }

    const int writable_particles = max_floats / 3;
    const int count = writable_particles < world->particle_count ? writable_particles : world->particle_count;

    for (int i = 0; i < count; i++) {
        const Particle* particle = &world->particles[i];
        out_xyz[i * 3 + 0] = particle->x;
        out_xyz[i * 3 + 1] = particle->y;
        out_xyz[i * 3 + 2] = particle->z;
    }

    return count;
}
