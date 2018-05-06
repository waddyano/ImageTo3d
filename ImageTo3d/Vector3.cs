using System;

namespace ImageTo3d
{
    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public override string ToString()
        {
            return String.Format("({0}, {1}, {2})", X, Y, Z);
        }

        public string ToSTLFormat()
        {
            return String.Format("{0} {1} {2}", X, Y, Z);
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 MinValue
        {
            get
            {
                Vector3 v;
                v.X = v.Y = v.Z = Single.MinValue;
                return v;
            }
        }

        public static Vector3 MaxValue
        {
            get
            {
                Vector3 v;
                v.X = v.Y = v.Z = Single.MaxValue;
                return v;
            }
        }

        public static Vector3 Min(Vector3 a, Vector3 b)
        {
            Vector3 v = new Vector3();
            v.X = a.X < b.X ? a.X : b.X;
            v.Y = a.Y < b.Y ? a.Y : b.Y;
            v.Z = a.Z < b.Z ? a.Z : b.Z;
            return v;
        }

        public static Vector3 Max(Vector3 a, Vector3 b)
        {
            Vector3 v = new Vector3();
            v.X = a.X > b.X ? a.X : b.X;
            v.Y = a.Y > b.Y ? a.Y : b.Y;
            v.Z = a.Z > b.Z ? a.Z : b.Z;
            return v;
        }

        public static Vector3 operator /(Vector3 a, double d)
        {
            Vector3 v = new Vector3();
            v.X = a.X / (float)d;
            v.Y = a.Y / (float)d;
            v.Z = a.Z / (float)d;
            return v;
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            Vector3 v = new Vector3();
            v.X = a.X + b.X;
            v.Y = a.Y + b.Y;
            v.Z = a.Z + b.Z;
            return v;
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            Vector3 v = new Vector3();
            v.X = a.X - b.X;
            v.Y = a.Y - b.Y;
            v.Z = a.Z - b.Z;
            return v;
        }

        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            Vector3 v = new Vector3();
            v.X = a.Y * b.Z - a.Z * b.Y; 
            v.Y = a.Z * b.X - a.X * b.Z;
            v.Z = a.X * b.Y - a.Y * b.X;
            return v;
        }

        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public float Length
        {
            get
            {
                return (float)Math.Sqrt(Dot(this, this));
            }
        }

        public Vector3 Unitize()
        {
            float len = Length;
            return this / len;
        }
    }
}
