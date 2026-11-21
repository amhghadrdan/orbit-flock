using UnityEngine;

namespace OrbitFlock
{
    internal sealed class BossFight
    {
        private enum Phase { Entry, Volley, Dive, Enraged }

        private readonly CombatWorld world;
        private readonly BossData data;
        private Phase phase;
        private float startedAt = Time.time;
        private float nextShot;
        private float nextSummon;
        private int volleys;
        private Vector2 diveStart, diveTarget;
        private const float AttackHeight = 2.45f;

        internal BossFight(CombatWorld world, BossData data)
        {
            this.world = world;
            this.data = data;
        }

        internal Vector2 Move(Vector2 position, float health, float step)
        {
            float now = Time.time;
            if (phase != Phase.Entry && phase != Phase.Enraged && health <= 0.30f)
                Enter(Phase.Enraged, now);

            switch (phase)
            {
                case Phase.Entry:
                    Vector2 arrival = Vector2.MoveTowards(position, new Vector2(0f, AttackHeight), step * 4f);
                    if (Mathf.Abs(arrival.y - AttackHeight) < 0.01f)
                        Enter(Phase.Volley, now);
                    return arrival;
                case Phase.Volley:
                    if (now >= nextShot)
                    {
                        world.FireBossSpread(position, data.VolleyShots, data.ShotSpeed, 13f);
                        nextShot = now + data.VolleyInterval;
                        if (++volleys == 3)
                        {
                            diveStart = position;
                            diveTarget = new Vector2(Mathf.Clamp(world.PlayerPosition.x, -6f, 6f), -2.8f);
                            Enter(Phase.Dive, now);
                        }
                    }
                    return Patrol(position, now, step, 1.6f, 3.8f);
                case Phase.Dive:
                    float travel = Mathf.Max(0.48f, Vector2.Distance(diveStart, diveTarget) / data.DiveSpeed);
                    float progress = (now - startedAt) / travel;
                    if (now >= nextShot && progress > 0.42f && progress < 1.45f)
                    {
                        world.FireBossSpread(position, 3, data.ShotSpeed + 1.5f, 23f, true);
                        nextShot = now + 0.30f;
                    }
                    if (progress >= 2f)
                        Enter(Phase.Volley, now);
                    return Vector2.Lerp(diveStart, diveTarget, Mathf.Clamp01(1f - Mathf.Abs(1f - progress)));
                default:
                    if (now >= nextShot)
                    {
                        world.FireBossSpread(position, data.VolleyShots, data.ShotSpeed + 0.8f, 10f);
                        nextShot = now + data.VolleyInterval * 0.52f;
                    }
                    if (now >= nextSummon)
                    {
                        world.SummonBossMinion(position);
                        nextSummon = now + 3.1f;
                    }
                    return Patrol(position, now, step, 2.4f, 5.2f);
            }
        }

        private static Vector2 Patrol(Vector2 position, float now, float step, float frequency, float speed)
        {
            var target = new Vector2(Mathf.Sin(now * frequency) * 4.0f, AttackHeight);
            return Vector2.MoveTowards(position, target, step * speed);
        }

        private void Enter(Phase next, float now)
        {
            phase = next;
            startedAt = now;
            nextShot = now + 0.35f;
            nextSummon = now + 1f;
            volleys = 0;
        }
    }
}
