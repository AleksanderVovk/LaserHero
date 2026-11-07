using UnityEngine;

namespace LaserHero
{
    public static class LaserCityShapes
    {
        public static bool Cell(int style, int x, int y, int z, Vector3Int d, float cell, Vector3 origin, out byte colour)
        {
            colour = 0;
            bool edge = x == 0 || z == 0 || x == d.x - 1 || z == d.z - 1;
            if (style == 9)
            {
                if (y != d.y - 1)
                    return true;
                float wx = origin.x + (x + .5f) * cell, wz = origin.z + (z + .5f) * cell;
                float dx = Mathf.Min(Mathf.Abs(wx + 40), Mathf.Abs(wx), Mathf.Abs(wx - 40));
                float dz = Mathf.Min(Mathf.Abs(wz - 62), Mathf.Abs(wz - 102), Mathf.Abs(wz - 142));
                colour = (byte)(dx < 4 || dz < 4 ? 2 : dx < 5.5f || dz < 5.5f ? 3 : 1);
                bool lane = dx < .22f && dz > 6 && Mathf.FloorToInt(wz / 3) % 2 == 0 || dz < .22f && dx > 6
                    && Mathf.Abs(Mathf.FloorToInt(wx / 3)) % 2 == 0;
                bool crossing = dx < 3.6f && dz > 4.5f && dz < 6 && Mathf.FloorToInt(wx / .65f) % 2 == 0
                    || dz < 3.6f && dx > 4.5f && dx < 6 && Mathf.FloorToInt(wz / .65f) % 2 == 0;
                if (lane || crossing)
                    colour = 4;
                return true;
            }

            if (style == 10)
            {
                int roof = d.y - 8;
                if (y < roof)
                {
                    bool slab = y % 6 == 0;
                    bool window = edge && y % 6 >= 2 && y % 6 <= 4 && ((z == 0 || z == d.z - 1)
                        ? x % 5 >= 2 && x % 5 <= 3 : z % 5 >= 2 && z % 5 <= 3);
                    colour = (byte)(window ? 2 : slab ? 1 : 0);
                    if (z == 0 && x >= d.x / 2 - 1 && x <= d.x / 2 + 1 && y > 0 && y < 5)
                        return false;
                    return edge || slab;
                }

                colour = 1;
                if (y == roof)
                    return true;
                if (edge && y <= roof + 2)
                    return true;
                bool ac = x >= 3 && x <= 6 && z >= 3 && z <= 7 && y <= roof + 3;
                bool stair = x >= d.x - 9 && x <= d.x - 4 && z >= d.z - 9 && z <= d.z - 4 && y <= roof + 6;
                if (ac)
                    colour = (byte)(x % 2 == 0 ? 2 : 1);
                return ac || stair;
            }

            if (style == 11)
            {
                int boom = d.y - 16;
                bool mast = x >= 20 && x <= 25 && z >= 4 && z <= 7;
                bool frame = mast && ((x == 20 || x == 25) && (z == 4 || z == 7) || y % 8 == 0);
                bool brace = mast && (x == 20 || x == 25) && (Mathf.Abs((y % 8) * .5f - (z - 4)) <= .75f
                    || Mathf.Abs(3 - (y % 8) * .5f - (z - 4)) <= .75f);
                bool arm = x >= 1 && x < d.x - 1 && z >= 3 && z <= 8 && y >= boom && y <= boom + 6;
                bool truss = arm && ((y == boom || y == boom + 6) && (z == 3 || z == 8 || x % 6 == 0)
                    || x % 6 == 0 && (z == 3 || z == 8) || Mathf.Abs(y - boom - x % 6) <= 1 && (z == 3 || z == 8));
                bool cabin = x >= 14 && x <= 19 && z >= 2 && z <= 8 && y >= boom - 6 && y < boom;
                bool weight = x >= 1 && x <= 9 && z >= 3 && z <= 8 && y >= boom && y <= boom + 4;
                bool cable = x == d.x - 10 && z == 5 && y >= boom - 30 && y <= boom;
                bool hook = z == 5 && x >= d.x - 12 && x <= d.x - 8 && (y == boom - 31 || x == d.x - 8
                    && y >= boom - 31 && y <= boom - 28);
                colour = (byte)(weight || cable || hook ? 1 : cabin && y > boom - 5 && y < boom - 1 && (z == 2 || z == 8) ? 2 : 0);
                return frame || brace || truss || cabin || weight || cable || hook;
            }

            if (style == 12)
            {
                colour = (byte)(y >= d.y - 2 ? 1 : z == 0 && y == 2 ? 2 : 0);
                return y == 0 || y == d.y - 2 || edge && y < d.y - 2 && !(z == 0 && y == d.y - 3 && x > 0 && x < d.x - 1);
            }

            if (style == 13)
            {
                bool pot = y < 2 && x > 0 && x < d.x - 1 && z > 0 && z < d.z - 1;
                bool leaf = y >= 2 && Mathf.Abs(x - (d.x - 1) * .5f) + Mathf.Abs(z - (d.z - 1) * .5f) < (y
                    == d.y - 1 ? d.x * .42f : d.x * .68f);
                colour = (byte)(leaf ? 1 : 0);
                return pot || leaf;
            }

            if (style == 15)
            {
                bool post = (x < 2 || x >= d.x - 2) && (z < 2 || z >= d.z - 2);
                bool slab = y % 8 == 0;
                colour = (byte)(y % 8 == 0 ? 1 : 0);
                return post || slab;
            }

            if (style == 16)
            {
                bool wheel = (x == 0 || x == d.x - 1) && y < 2 && (z >= 2 && z <= 4 || z >= d.z - 5 && z <= d.z - 3);
                bool chassis = y == 2;
                bool body = y == 3 && x > 0 && x < d.x - 1 && z > 0 && z < d.z - 1;
                bool cabin = y >= 4 && y <= 5 && x > 0 && x < d.x - 1 && z >= 5 && z < d.z - 5;
                colour = (byte)(wheel ? 1 : cabin && y == 4 ? 2 : 0);
                return wheel || chassis || body || cabin;
            }

            if (style == 17)
            {
                colour = (byte)(x % 5 < 2 ? 1 : 0);
                return (x < 2 || x >= d.x - 2) || y == 3 || y == 6;
            }

            if (style == 18)
            {
                colour = (byte)(edge || y == 0 || y == d.y - 1 || x == y ? 1 : 0);
                return true;
            }

            if (style == 19)
            {
                colour = (byte)(y == 0 || y == 6 ? 1 : 0);
                return x >= 2 && x <= 3 && z >= 2 && z <= 3 || y == 0 && x > 0 && x < 5 && z > 0 && z < 5
                    || y == 4 && z >= 2 && z <= 3 || y == 6 && x > 0 && x < 5 && z > 0 && z < 5;
            }

            if (style == 20)
            {
                bool middle = x >= d.x / 2 - 1 && x <= d.x / 2 && z >= d.z / 2 - 1 && z <= d.z / 2;
                colour = (byte)(y == 1 && !edge && !middle ? 2 : 1);
                return y == 0 || y < 3 && edge || y == 1 || middle;
            }

            return false;
        }
    }
}
