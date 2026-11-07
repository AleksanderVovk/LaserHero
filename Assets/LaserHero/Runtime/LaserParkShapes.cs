using UnityEngine;

namespace LaserHero
{
    public static class LaserParkShapes
    {
        public static float CoasterHeight(float a) => 6 + 23 * Mathf.Pow((1 + Mathf.Sin(a + .6f)) * .5f, 2) + 3 * Mathf.Sin(a * 3);
        static bool Beam(Vector3 p, Vector3 a, Vector3 b, float radius)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(.001f, ab.sqrMagnitude));
            return (p - a - ab * t).sqrMagnitude <= radius * radius;
        }

        public static bool Cell(int style, int x, int y, int z, Vector3Int d, float cell, Vector3 origin, out byte colour)
        {
            colour = 0;
            float cx = (d.x - 1) * .5f, cz = (d.z - 1) * .5f;
            Vector3 p = new Vector3(x, y, z);
            float dx = x - cx, dz = z - cz, r = Mathf.Sqrt(dx * dx + dz * dz), angle = Mathf.Atan2(dz, dx);
            if (style == 70)
            {
                if (y < d.y - 1)
                {
                    colour = (byte)(y < 6 ? 0 : 1);
                    return true;
                }

                float wx = origin.x + (x + .5f) * cell, wz = origin.z + (z + .5f) * cell;
                bool path = Mathf.Abs(wx) < 5 || Mathf.Abs(wz - 82) < 3.5f || Mathf.Abs(wz - 100) < 3.5f
                    || Mathf.Abs(wz - 143) < 3.5f;
                colour = (byte)(path ? ((Mathf.FloorToInt(wx / 2) + Mathf.FloorToInt(wz / 2)) & 1) == 0 ? 4 : 5 : 2);
                if (!path && wx < -5 && wz > 100)
                    colour = 3;
                if (!path && wx > 13 && wz > 122)
                    colour = 6;
                if (!path && wx > 14 && wx < 52 && wz > 69 && wz < 95)
                    colour = 7;
                return true;
            }

            if (style >= 71 && style <= 76)
            {
                float wx = origin.x + (x + .5f) * cell, wz = origin.z + (z + .5f) * cell, wy = origin.y + (y + .5f) * cell;
                float nx = (wx + 32) / 25, nz = (wz - 130) / 29, a = Mathf.Atan2(nz, nx);
                if (a < 0)
                    a += Mathf.PI * 2;
                float sector = a / (Mathf.PI / 3);
                if (sector < style - 71 - .002f || sector > style - 70 + .002f)
                    return false;
                float radial = Mathf.Sqrt(nx * nx + nz * nz), height = CoasterHeight(a);
                bool rail = (Mathf.Abs(radial - .975f) < .016f || Mathf.Abs(radial - 1.025f) < .016f)
                    && Mathf.Abs(wy - height) < .55f;
                bool tie = Mathf.Abs(radial - 1) < .065f && Mathf.Abs(wy - (height - .5f)) < .45f
                    && Mathf.Abs(Mathf.Sin(a * 48)) < .65f;
                bool node = Mathf.Abs(Mathf.Sin(a * 12)) < .18f;
                bool post = node && (Mathf.Abs(radial - .95f) < .021f || Mathf.Abs(radial - 1.05f) < .021f) && wy < height;
                bool cross = node && Mathf.Abs(radial - 1) < .07f && (Mathf.Abs(wy - (height - .8f)) < .5f || wy < .6f);
                bool train = a > 4.15f && a < 4.52f && Mathf.Abs(radial - 1) < .055f && wy > height + .15f && wy < height + 1.65f;
                bool carShell = wy < height + .7f || Mathf.Abs(radial - 1) > .038f || Mathf.Sin((a - 4.15f) * 90) > .7f;
                colour = (byte)(train ? wy > height + 1.2f ? 4 : 3 : rail ? 0 : tie ? 1 : 2);
                return rail || tie || post || cross || train && carShell;
            }

            if (style == 77) // Ferris wheel with spokes, coloured cabins and two A frames.
            {
                float cy = d.y * .56f, radius = d.x * .39f;
                float yy = y - cy, dist = Mathf.Sqrt(dx * dx + yy * yy), a = Mathf.Atan2(yy, dx);
                bool plane = Mathf.Abs(Mathf.Abs(dz) - 2) < 1.2f;
                bool ring = plane && Mathf.Abs(dist - radius) < 1.65f;
                // The angular frequency scales the distance: compensate so diagonal
                // spokes stay a connected, full-width voxel beam.
                bool spoke = plane && dist < radius && Mathf.Abs(Mathf.Sin(a * 6)) * dist < 6.9f;
                bool axle = Mathf.Abs(dx) < 2.2f && Mathf.Abs(yy) < 2.2f;
                bool support = false;
                foreach (float zz in new[]
                {
                    1f,
                    d.z - 2f
                }

                )
                    support |= Beam(p, new Vector3(cx - d.x * .34f, 0, zz), new Vector3(cx, cy, zz),
                        1.6f) || Beam(p, new Vector3(cx + d.x * .34f, 0, zz), new Vector3(cx, cy, zz), 1.6f);
                colour = (byte)(support ? 2 : 0);
                for (int k = 0; k < 12; k++)
                {
                    float ca = k * Mathf.PI / 6, bx = cx + Mathf.Cos(ca) * radius, by = cy + Mathf.Sin(ca) * radius - 3;
                    bool box = Mathf.Abs(x - bx) < 3.5f && Mathf.Abs(y - by) < 3.5f && Mathf.Abs(dz) < 4.6f;
                    bool shell = Mathf.Abs(x - bx) > 2.2f || Mathf.Abs(y - by) > 2.1f || Mathf.Abs(dz) > 3.2f;
                    if (box && shell)
                    {
                        colour = (byte)(3 + k % 4);
                        return true;
                    }
                }

                return ring || spoke || axle || support;
            }

            if (style == 78) // Canopied carousel with rods, seats and a striped conical roof.
            {
                float radius = d.x * .46f;
                bool floor = y < 3 && r < radius;
                bool mast = r < 1.7f;
                float roof = d.y * .67f + (1 - r / radius) * d.y * .28f;
                bool canopy = r < radius && Mathf.Abs(y - roof) < 1.4f;
                colour = (byte)(canopy ? 3 + ((int)((angle + Mathf.PI) * 6 / Mathf.PI) % 4) : floor ? 0 : 1);
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI / 4, px = cx + Mathf.Cos(a) * radius * .68f, pz = cz + Mathf.Sin(a) * radius * .68f;
                    bool rod = Mathf.Abs(x - px) < .9f && Mathf.Abs(z - pz) < .9f && y < roof;
                    bool seat = Mathf.Abs(x - px) < 2.2f && Mathf.Abs(z - pz) < 2.8f && y >= 5 && y <= 8;
                    bool back = Mathf.Abs(x - px) < 2.2f && z > pz + 1 && z < pz + 3 && y > 7 && y < 12;
                    if (rod || seat || back)
                    {
                        colour = (byte)(rod ? 1 : 3 + k % 4);
                        return true;
                    }
                }

                return floor || mast || canopy;
            }

            if (style == 79) // Two-seat swings, connected chains and splayed four-legged frame.
            {
                bool frame = false;
                foreach (float xx in new[]
                {
                    2f,
                    d.x - 3f
                }

                )
                    foreach (float zz in new[]
                    {
                        1f,
                        d.z - 2f
                    }

                    )
                        frame |= Beam(p, new Vector3(xx, 0, zz), new Vector3(xx, d.y - 3, cz), 1.25f);
                bool top = y >= d.y - 4 && Mathf.Abs(dz) < 1.3f;
                colour = 0;
                for (int k = 0; k < 2; k++)
                {
                    float xx = d.x * (k == 0 ? .32f : .68f);
                    bool chain = Mathf.Abs(Mathf.Abs(x - xx) - 2.1f) < .75f && Mathf.Abs(dz) < .9f && y >= 5 && y < d.y - 2;
                    bool seat = Mathf.Abs(x - xx) < 3 && Mathf.Abs(dz) < 2.2f && y >= 4 && y <= 6;
                    if (chain || seat)
                    {
                        colour = (byte)(chain ? 1 : 2);
                        return true;
                    }
                }

                return frame || top;
            }

            if (style == 80) // Drop tower with broad foot, guide shaft and passenger gondola.
            {
                bool basePart = y < 3 && r < d.x * .48f;
                bool shaft = Mathf.Abs(dx) < 2.5f && Mathf.Abs(dz) < 2.5f;
                bool cab = y > d.y * .67f && y < d.y * .77f && Mathf.Abs(dx) < cx * .94f && Mathf.Abs(dz) < cz * .94f;
                bool cap = y > d.y - 7 && r < (d.y - y) * 1.4f;
                colour = (byte)(basePart ? 0 : cab ? 2 : cap ? 1 : y % 12 < 3 ? 1 : 0);
                return basePart || shaft || cab || cap;
            }

            if (style == 81) // Open-front stalls with striped awnings.
            {
                bool legs = (x < 2 || x >= d.x - 2) && (z < 2 || z >= d.z - 2);
                bool counter = y >= 6 && y <= 8;
                bool rear = z >= d.z - 2 && y < d.y - 6;
                float roof = d.y - 4 + (1 - Mathf.Abs(dx) / (cx + .5f)) * 3;
                bool canopy = y >= roof - 1.6f && y <= roof;
                colour = (byte)(canopy ? x % 6 < 3 ? 0 : 1 : counter ? 1 : 2);
                return legs || counter || rear || canopy;
            }

            if (style == 82) // Bumper cars with grounded wheels and open seats.
            {
                bool wheel = y < 3 && (Mathf.Abs(dx) > cx - 2) && (z < 4 || z > d.z - 5);
                bool body = y >= 2 && y < 6 && Mathf.Abs(dx) < cx - .3f && Mathf.Abs(dz) < cz - .3f;
                bool back = y >= 5 && y < d.y - 1 && z > d.z * .58f && z < d.z * .76f && Mathf.Abs(dx) < cx * .65f;
                colour = (byte)(wheel ? 3 : back ? 1 : y == 5 ? 2 : 0);
                return wheel || body || back;
            }

            if (style == 83) // Open bumper-car pavilion.
            {
                bool columns = (x < 2 || x >= d.x - 2) && (z < 2 || z >= d.z - 2 || z == d.z / 2);
                colour = (byte)(y == 0 ? 2 : y > d.y - 4 ? (x / 5) % 2 : 0);
                return y == 0 || columns || y >= d.y - 3;
            }

            if (style == 84) // A balloon bouquet held on connected stems above a weighted base.
            {
                if (y < 3 && r < 3)
                {
                    colour = 1;
                    return true;
                }

                for (int k = 0; k < 3; k++)
                {
                    float a = k * Mathf.PI * 2 / 3, xx = cx + Mathf.Cos(a) * cx * .42f, zz =
                        cz + Mathf.Sin(a) * cz * .42f, yy = d.y * (.68f + k * .07f);
                    bool stem = Beam(p, new Vector3(cx, 0, cz), new Vector3(xx, yy, zz), .8f);
                    bool balloon = Mathf.Pow((x - xx) / (d.x * .22f),
                        2) + Mathf.Pow((y - yy) / (d.y * .18f), 2) + Mathf.Pow((z - zz) / (d.z * .22f), 2) < 1;
                    if (stem || balloon)
                    {
                        colour = (byte)(balloon ? 3 + k : 1);
                        return true;
                    }
                }

                return false;
            }

            if (style == 85) // Giant ice cream sculpture, cone and two scoops.
            {
                float cone = d.x * .36f * Mathf.Clamp01(y / (d.y * .58f)) + 1;
                bool basePart = y < d.y * .59f && r < cone;
                float scoop = (dx * dx + dz * dz) / Mathf.Pow(d.x * .41f, 2) + Mathf.Pow((y - d.y * .65f) / (d.y * .22f), 2);
                colour = (byte)(basePart ? 0 : y > d.y * .72f ? 2 : 1);
                return basePart || scoop < 1;
            }

            if (style == 86) // Grounded pennant pole.
            {
                colour = (byte)(x > cx + 1 ? 1 : 0);
                return Mathf.Abs(dx) < 1.2f && Mathf.Abs(dz) < 1.2f || x >= cx && Mathf.Abs(dz) < 1
                    && y > d.y - 10 && y < d.y - 2 && x - cx < (d.y - y) * 1.2f;
            }

            if (style == 87)
            {
                bool result = LaserOceanShapes.Cell(56, x, y, z, d, cell, origin, out colour);
                colour = (byte)((y / 3) % 6);
                return result;
            }

            if (style == 89) // Pirate-ship swing, four legs, suspension rods and curved hull.
            {
                bool frame = false;
                foreach (float xx in new[]
                {
                    2f,
                    d.x - 3f
                }

                )
                    foreach (float zz in new[]
                    {
                        1f,
                        d.z - 2f
                    }

                    )
                        frame |= Beam(p, new Vector3(xx, 0, zz), new Vector3(xx, d.y - 3, cz), 1.4f);
                bool beam = y >= d.y - 4 && Mathf.Abs(dz) < 1.5f;
                bool rope = (Mathf.Abs(x - d.x * .3f) < 1 || Mathf.Abs(x - d.x * .7f) < 1) && Mathf.Abs(dz) < 1 && y > d.y * .23f;
                float bottom = d.y * .22f + Mathf.Pow(dx / (cx + .5f), 2) * d.y * .2f;
                bool boat = Mathf.Abs(dx) < cx - 3 && Mathf.Abs(dz) < cz * .65f && y > bottom
                    && y < bottom + 7 && (y < bottom + 2 || Mathf.Abs(dz) > cz * .65f - 2 || Mathf.Abs(dx) > cx - 5);
                colour = (byte)(boat ? y > bottom + 5 ? 1 : 2 : 0);
                return frame || beam || rope || boat;
            }

            return false;
        }
    }
}
