using System;
using System.Globalization;
using System.Numerics;

namespace LaserHero
{
    // Integer millionths: exact addition, spending and serialization, with no float ceiling.
    public readonly struct LaserAmount : IComparable<LaserAmount>, IEquatable<LaserAmount>
    {
        public static readonly BigInteger Scale = 1000000;
        public readonly BigInteger Units;
        LaserAmount(BigInteger units)
        {
            Units = units;
        }

        public static LaserAmount FromUnits(BigInteger units) => new LaserAmount(units);
        public static LaserAmount FromInteger(BigInteger integer) => new LaserAmount(integer * Scale);
        public static LaserAmount Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return default;
            value = value.Trim();
            int e = value.IndexOfAny(new[] { 'e', 'E' });
            int exponent = 0;
            if (e >= 0)
            {
                exponent = int.Parse(value.Substring(e + 1), CultureInfo.InvariantCulture);
                value = value.Substring(0, e);
            }

            int dot = value.IndexOf('.');
            int decimals = dot < 0 ? 0 : value.Length - dot - 1;
            if (dot >= 0)
                value = value.Remove(dot, 1);
            var digits = BigInteger.Parse(value, CultureInfo.InvariantCulture);
            int power = 6 + exponent - decimals;
            return new LaserAmount(power >= 0 ? digits * BigInteger.Pow(10, power) : digits / BigInteger.Pow(10, -power));
        }

        public static implicit operator LaserAmount(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            return Parse(value.ToString("R", CultureInfo.InvariantCulture));
        }

        public static explicit operator double (LaserAmount value)
        {
            double d = (double)value.Units / 1000000d;
            return double.IsInfinity(d) ? (value.Units.Sign < 0 ? -double.MaxValue : double.MaxValue) : d;
        }

        public static explicit operator float (LaserAmount value) =>
            (float)Math.Max(-float.MaxValue, Math.Min(float.MaxValue, (double)value));
        public static LaserAmount operator +(LaserAmount a, LaserAmount b) => new LaserAmount(a.Units + b.Units);
        public static LaserAmount operator -(LaserAmount a, LaserAmount b) => new LaserAmount(a.Units - b.Units);
        public static LaserAmount operator *(LaserAmount a, LaserAmount b) => new LaserAmount(a.Units * b.Units / Scale);
        public static LaserAmount operator /(LaserAmount a, LaserAmount b) => new LaserAmount(a.Units * Scale / b.Units);
        public static bool operator <(LaserAmount a, LaserAmount b) => a.Units < b.Units;
        public static bool operator>(LaserAmount a, LaserAmount b) => a.Units > b.Units;
        public static bool operator <=(LaserAmount a, LaserAmount b) => a.Units <= b.Units;
        public static bool operator >=(LaserAmount a, LaserAmount b) => a.Units >= b.Units;
        public static bool operator ==(LaserAmount a, LaserAmount b) => a.Units == b.Units;
        public static bool operator !=(LaserAmount a, LaserAmount b) => a.Units != b.Units;
        public int CompareTo(LaserAmount other) => Units.CompareTo(other.Units);
        public bool Equals(LaserAmount other) => Units == other.Units;
        public override bool Equals(object other) => other is LaserAmount amount && Equals(amount);
        public override int GetHashCode() => Units.GetHashCode();
        public override string ToString()
        {
            string digits = BigInteger.Abs(Units).ToString(CultureInfo.InvariantCulture).PadLeft(7, '0');
            string s = digits.Substring(0, digits.Length - 6) + "." + digits.Substring(digits.Length - 6);
            return (Units.Sign < 0 ? "-" : "") + s.TrimEnd('0').TrimEnd('.');
        }

        public string Short()
        {
            if (Units.IsZero)
                return "0";
            string raw = BigInteger.Abs(Units).ToString(CultureInfo.InvariantCulture);
            int exponent = raw.Length - 7;
            if (exponent < 3)
                return ((double)this).ToString("0.##", CultureInfo.InvariantCulture);
            string[] suffix =
            {
                "",
                "K",
                "M",
                "B",
                "T",
                "Qa",
                "Qi",
                "Sx",
                "Sp",
                "Oc",
                "No",
                "Dc"
            };
            int group = exponent / 3;
            if (group >= suffix.Length)
                return (Units.Sign < 0 ? "-" : "") + raw[0] + "." + raw.Substring(1, 2) + "e" + exponent;
            int places = exponent - group * 3 + 1;
            string significant = raw.Substring(0, places) + "." + raw.Substring(places, 2);
            return (Units.Sign < 0 ? "-" : "") + significant.TrimEnd('0').TrimEnd('.') + suffix[group];
        }
    }

    public static class LaserLevelCurve
    {
        public const int StartingXP = 50;
        public const int ReferenceLevel = 44;
        public const int ReferenceXP = 2400000;
        static readonly BigInteger GrowthNumerator = ReferenceXP - StartingXP;
        static readonly BigInteger GrowthDenominator = BigInteger.Pow(ReferenceLevel, 3) - 1;
        // Required(L) ~= 50 + (2,400,000 - 50) * (L^3 - 1) / (44^3 - 1).
        // Round the cumulative sum, so fractional remainders never accumulate or disappear.
        // Sum(k^3, k=1..n) = (n*(n+1)/2)^2: exact at every level, with no level cap.
        public static BigInteger Cumulative(BigInteger level)
        {
            if (level <= 1)
                return BigInteger.Zero;
            var n = level - 1;
            var triangle = n * (n + 1) / 2;
            return (StartingXP * n + GrowthNumerator * (triangle * triangle - n) / GrowthDenominator) * LaserAmount.Scale;
        }

        public static LaserAmount Required(BigInteger level) => LaserAmount.FromUnits(Cumulative(level + 1) - Cumulative(level));
        public static void AddXP(LaserSave data, LaserAmount amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            var total = Cumulative(data.Level) + data.XP.Units + amount.Units;
            // Fourth-root estimate of the cubic curve's cumulative cost, followed by exact checks.
            // Large IAP grants jump directly to the result instead of iterating millions of levels.
            var level = Root(total * 4 * GrowthDenominator / (GrowthNumerator * LaserAmount.Scale), 4) + 1;
            while (level > 1 && Cumulative(level) > total)
                level--;
            while (Cumulative(level + 1) <= total)
                level++;
            data.Level = level;
            data.XP = LaserAmount.FromUnits(total - Cumulative(level));
        }

        static BigInteger Root(BigInteger value, int degree)
        {
            if (value <= 1)
                return value;
            var bytes = value.ToByteArray();
            int bits = (bytes.Length - 1) * 8;
            byte top = bytes[bytes.Length - 1];
            while (top > 0)
            {
                bits++;
                top >>= 1;
            }

            var current = BigInteger.One << ((bits + degree - 1) / degree);
            while (true)
            {
                var next = ((degree - 1) * current + value / BigInteger.Pow(current, degree - 1)) / degree;
                if (next >= current)
                    break;
                current = next;
            }

            while (BigInteger.Pow(current, degree) > value)
                current--;
            while (BigInteger.Pow(current + 1, degree) <= value)
                current++;
            return current;
        }
    }
}
