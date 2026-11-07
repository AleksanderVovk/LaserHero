using UnityEngine;

namespace LaserHero
{
    // Procedural voxel silhouettes; every structure stays a separate editable body.
    public static class LaserSpaceShapes
    {
        public static bool Cell(int style, int x, int y, int z, Vector3Int d, float cell, Vector3 origin, out byte colour)
        {
            colour = 0;
            float cx = (d.x - 1) * .5f, cz = (d.z - 1) * .5f;
            float r = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
            bool edge = x == 0 || x == d.x - 1 || z == 0 || z == d.z - 1;
            if (style == 30)
            {
                if (y < d.y - 1)
                {
                    colour = (byte)(y < 6 ? 0 : 5);
                    return true;
                }

                float wx = origin.x + (x + .5f) * cell, wz = origin.z + (z + .5f) * cell;
                float dx = Mathf.Min(Mathf.Abs(wx + 40), Mathf.Min(Mathf.Abs(wx), Mathf.Abs(wx - 40)));
                float dz = Mathf.Min(Mathf.Abs(wz - 62), Mathf.Min(Mathf.Abs(wz - 102), Mathf.Abs(wz - 142)));
                colour = (byte)(dx < 4 || dz < 4 ? 2 : dx < 5.5f || dz < 5.5f ? 3 : 1);
                bool pavedLot = wx < -6 && wz > 42 && wz < 57 || wx < -6 && wz > 69 && wz < 96 || wx > 6
                    && wx < 34 && wz > 70 && wz < 96 || wx > 46 && wz > 69 && wz < 136 || wx > -34 && wx < -6 && wz > 148
                    && wz < 164;
                if (colour == 1 && pavedLot)
                    colour = 3;
                bool lane = dx < .3f && dz > 6 && Mathf.FloorToInt(wz / 3) % 2 == 0 || dz < .3f && dx > 6
                    && Mathf.Abs(Mathf.FloorToInt(wx / 3)) % 2 == 0;
                bool crossing = dx < 3.6f && dz > 4.5f && dz < 6 && Mathf.FloorToInt(wx / .65f) % 2 == 0
                    || dz < 3.6f && dx > 4.5f && dx < 6 && Mathf.FloorToInt(wz / .65f) % 2 == 0;
                if (lane || crossing)
                    colour = 4;
                return true;
            }

            if (style == 31) // One connected rocket, including engines, fins and tapered nose.
            {
                float radius = d.x * .31f;
                int nose = d.y - 19;
                float outer = y >= nose ? Mathf.Max(.5f, radius * (d.y - y) / 19f) : radius;
                bool hull = r <= outer && (r >= outer - 1.7f || y % 14 == 0 || y >= d.y - 3);
                bool fins = y < 14 && (Mathf.Abs(x - cx) < 1.3f && Mathf.Abs(z - cz) < radius + 3
                    || Mathf.Abs(z - cz) < 1.3f && Mathf.Abs(x - cx) < radius + 3);
                bool engines = y < 5 && r < radius;
                colour = (byte)(y >= nose || y % 26 < 3 ? 1 : y < 5 ? 3 : y > nose - 8 && y < nose - 3
                    && z < cz && Mathf.Abs(x - cx) < 2.5f ? 2 : 0);
                return hull || fins || engines;
            }

            if (style == 32) // Continuous four-legged service tower, diagonal trusses and platforms.
            {
                bool corners = (x < 2 || x >= d.x - 2) && (z < 2 || z >= d.z - 2);
                bool side = x == 0 || x == d.x - 1 || z == 0 || z == d.z - 1;
                float u = (x == 0 || x == d.x - 1) ? z : x;
                float span = (x == 0 || x == d.x - 1) ? d.z - 1 : d.x - 1;
                bool brace = side && (Mathf.Abs(u - (y % 12) * span / 11f) < .85f
                    || Mathf.Abs(span - u - (y % 12) * span / 11f) < .85f);
                bool platform = y % 12 == 0;
                colour = (byte)(y >= d.y - 3 ? 2 : platform ? 1 : 0);
                return corners || brace || platform;
            }

            if (style == 33) // Vehicle assembly building: real open door, ribs and roof equipment.
            {
                bool door = z == 0 && x > 5 && x < d.x - 6 && y > 0 && y < d.y - 9;
                bool window = edge && y > 7 && y < d.y - 7 && ((z == 0 || z == d.z - 1) ? x % 10 < 5 : z % 10 < 5);
                bool ribs = edge && ((z == 0 || z == d.z - 1) ? x % 10 == 0 : z % 10 == 0);
                colour = (byte)(window ? 2 : ribs || y % 12 == 0 ? 1 : 0);
                if (y >= d.y - 4)
                {
                    colour = 1;
                    return y == d.y - 4 || edge && y < d.y - 2 || x > 4 && x < 10 && z > 5 && z < 12;
                }

                return y == 0 || edge && !door;
            }

            if (style == 34) // Operations centre with a connected raised glazed control room.
            {
                int roof = 14;
                if (y <= roof)
                {
                    bool window = edge && y >= 4 && y <= 10;
                    colour = (byte)(window ? 2 : y == roof ? 1 : 0);
                    return edge || y == 0 || y == roof;
                }

                bool core = x >= d.x / 2 - 5 && x <= d.x / 2 + 5 && z >= d.z / 2 - 5 && z <= d.z / 2 + 5;
                if (y < d.y - 12)
                {
                    colour = 0;
                    return core;
                }

                bool cab = x >= d.x / 2 - 9 && x <= d.x / 2 + 9 && z >= d.z / 2 - 8 && z <= d.z / 2 + 8;
                bool wall = x == d.x / 2 - 9 || x == d.x / 2 + 9 || z == d.z / 2 - 8 || z == d.z / 2 + 8;
                colour = (byte)(y > d.y - 11 && y < d.y - 3 ? 2 : 1);
                return cab && (wall || y == d.y - 12 || y >= d.y - 3);
            }

            if (style == 35) // Fuel silo, legs, ring bands and domed cap.
            {
                float radius = d.x * .43f;
                bool legs = y < 5 && (Mathf.Abs(x - cx) > radius - 2 && Mathf.Abs(z - cz) < 2
                    || Mathf.Abs(z - cz) > radius - 2 && Mathf.Abs(x - cx) < 2) && r < radius;
                float cap = y > d.y - 5 ? radius * (1 - (y - (d.y - 5)) * .15f) : radius;
                colour = (byte)(y < 5 ? 3 : y % 15 < 2 ? 1 : 0);
                return legs || y >= 5 && r < cap && (r > cap - 1.5f || y == 5 || y >= d.y - 5);
            }

            if (style == 36) // Satellite dish and mast, a single destructible connected object.
            {
                float radius = d.x * .46f;
                bool mast = Mathf.Abs(x - cx) < 1.5f && Mathf.Abs(z - cz) < 1.5f;
                int dishY = 8 + Mathf.FloorToInt(r * r / (radius * radius) * 8);
                colour = (byte)(y < 8 ? 1 : y > 17 ? 2 : 0);
                return mast && y < 22 || r < radius && Mathf.Abs(y - dishY) <= 1;
            }

            if (style == 37) // Ribbed shipping container.
            {
                colour = (byte)(y == 0 || y == d.y - 1 || x == 0 || x == d.x - 1 ? 1 : 0);
                return edge || y == 0 || y == d.y - 1 || x % 4 == 0;
            }

            if (style == 38) // Pipe bridge on two supports.
            {
                colour = (byte)(y >= d.y - 3 ? 1 : 0);
                return (x < 2 || x >= d.x - 2) && z >= d.z / 2 - 1 && z <= d.z / 2 + 1 || y >= d.y - 3
                    && z >= d.z / 2 - 1 && z <= d.z / 2 + 1;
            }

            if (style == 39) // Curved hangar shell.
            {
                float arch = 9 + Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((x - cx) / (cx + .5f), 2))) * (d.y - 10);
                // Bridge steep voxel steps and extend the side walls right up to the roof.
                float outerArch = 9 + Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((Mathf.Abs(x - cx) + 1) / (cx + .5f), 2))) * (d.y - 10);
                bool shell = y <= arch && y >= outerArch - 2;
                colour = (byte)(x % 8 < 2 ? 1 : 0);
                return y == 0 || (x < 2 || x >= d.x - 2) && y <= arch || shell || z == d.z - 1 && y <= arch;
            }

            if (style == 40) // Circular launch pad and hazard ring.
            {
                float radius = d.x * .49f;
                colour = (byte)(r > radius - 2 ? (x + z) % 6 < 3 ? 1 : 2 : 0);
                return r < radius;
            }

            return false;
        }
    }
}
