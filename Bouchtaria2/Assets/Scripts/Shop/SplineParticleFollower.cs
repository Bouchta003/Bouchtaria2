using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(ParticleSystem))]
public class SplineParticleFollower : MonoBehaviour
{
    public SplineContainer splineContainer;

    ParticleSystem ps;
    ParticleSystem.Particle[] particles;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[256];
    }

    void LateUpdate()
    {
        if (splineContainer == null)
            return;

        int count = ps.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            float lifeProgress =
                1f - (particles[i].remainingLifetime / particles[i].startLifetime);

            // Small per-particle offset so the bridge is filled
            float seedOffset = (particles[i].randomSeed % 1000) / 1000f * 0.2f;

            float t = Mathf.Clamp01(seedOffset + lifeProgress);

            particles[i].position = splineContainer.EvaluatePosition(t);
        }

        ps.SetParticles(particles, count);
    }
}
