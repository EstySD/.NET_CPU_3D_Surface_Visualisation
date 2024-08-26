using System;
using System.Runtime.Remoting.Messaging;
using static MyMath.Matrix;

namespace MyMath
{
	public static class BaseMath {
		public static float PI = 3.1415f;
        public static float Clamp(float num, float min, float max)
        {
            return Math.Min(Math.Max(num, min), max);
        }
		public static int Clamp(int num, int min, int max)
		{
			return Math.Min(Math.Max(num, min), max);
		}
		public static float ConvertToRad(int angle)
		{
			return (float)(PI / 180 * Convert.ToDouble(angle));
        }
	}

	public class Vector
	{
		public float x { get; set; }
		public float y { get; set; }
		public float z { get; set; }
		public float w { get; set; }
		public Vector(float x=0f, float y = 0f, float z = 0f, float w=1f)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
		public float getLength()
		{
			return (float)Math.Sqrt(Math.Pow(x,2)+ Math.Pow(y, 2)+Math.Pow(z, 2));
		}
		//добавить к вектору
		public static Vector add(Vector v1, Vector v2)
		{
			return new Vector(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
		}
		
        public static Vector substract(Vector v1, Vector v2)
        {
            return new Vector(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }
        public Vector normalise()
        {
			float length = this.getLength();
			if(length==0) return new Vector(0,0);
            return new Vector(this.x/length, this.y/length, this.z/length);
        }
        public Vector scale(float scale)
        {
            return new Vector(this.x * scale, this.y * scale, this.z * scale);
        }
        //скалярное произведение косинус
        public static float dotProduct(Vector v1, Vector v2)
		{
			return v1.x*v2.x+v1.y*v2.y+v1.z*v2.z;
		}
		public static Vector crossProduct(Vector v1, Vector v2)
		{
			float xtest = v1.y * v2.z - v1.z * v2.y;
			float ytest = v1.z * v2.x - v1.x * v2.z;
			float ztest = v1.x * v2.y - v1.y * v2.x;
            return new Vector(xtest, ytest, ztest);

		}

		//центр медиан
		public static Vector center(Vector v1, Vector v2, Vector v3)
		{
			return new Vector((v1.x + v2.x + v3.x) / 3, (v1.y + v2.y + v3.y) / 3, (v1.z + v2.z + v3.z) / 3);
		}

		//перегрузка операторов
		/*public static bool operator ==(Vector v1, Vector v2)
		{
			if (v1.x == v2.x && v1.y==v2.y && v1.z == v2.z) return true;
			else return false;
		}
		public static bool operator !=(Vector v1, Vector v2)
		{
			if (v1.x != v2.x && v1.y != v2.y && v1.z != v2.z) return true;
			else
				return false;
		}*/
	}
	class Matrix
	{
		
		//умножение
		//на вектор
		public static Vector multiplyVector(float[,] m, Vector v)
		{
			return new Vector(
				m[0, 0] * v.x + m[0, 1] * v.y + m[0, 2] * v.z + m[0, 3] * v.w,
				m[1, 0] * v.x + m[1, 1] * v.y + m[1, 2] * v.z + m[1, 3] * v.w,
				m[2, 0] * v.x + m[2, 1] * v.y + m[2, 2] * v.z + m[2, 3] * v.w,
				m[3, 0] * v.x + m[3, 1] * v.y + m[3, 2] * v.z + m[3, 3] * v.w
			);
		}

		//на матрицу
        public static float[,] multiplyMatrix(float[,] A, float[,] B)
        {
            int rA = A.GetLength(0);
            int cA = A.GetLength(1);
            int rB = B.GetLength(0);
            int cB = B.GetLength(1);

            if (cA != rB)
			{
                throw new ArgumentException("Can not multiply cA!=rB", nameof(A));
            }
            else
            {
                float temp = 0;
                float[,] m = new float[rA, cB];

                for (int i = 0; i < rA; i++)
                {
                    for (int j = 0; j < cB; j++)
                    {
                        temp = 0;
                        for (int k = 0; k < cA; k++)
                        {
                            temp += A[i, k] * B[k, j];
                        }
                        m[i, j] = temp;
                    }
                }
                return m;
            }
        }
        // получение transformation matrix
        public static float[,] getUnit()
        {
            float[,] m = new float[4, 4]{
                { 1, 0, 0, 0},
                { 0, 1, 0, 0},
                { 0, 0, 1, 0},
                { 0, 0, 0, 1 }
            };
            return m;
        }
        public static float[,] getTrans(float dx, float dy, float dz)
        {
            float[,] m = new float[4, 4]{
                { 1, 0, 0, dx},
                { 0, 1, 0, dy},
                { 0, 0, 0, dz},
                { 0, 0, 0, 1 }
            };
            return m;
        }
        public static float[,] getScale(float sx, float sy, float sz)
        {
            float[,] m = new float[4, 4]{
                {sx, 0, 0, 0},
                {0, sy, 0, 0},
                {0, 0, sz, 0},
                {0, 0, 0, 1},
            };
            return m;
        }
        // rotation
        public enum Axis
		{
			X,
			Y,
			Z
		}
		public static float[,] getRotation(int angle, Axis axis)
		{
			float rad = BaseMath.ConvertToRad(angle);
			float[,] m= new float[4,4];

            switch (axis)
			{
				case Axis.X:
					m= new float[4, 4]{
						{ 1,0, 0, 0},
						{0, (float)Math.Cos(rad),-(float)Math.Sin(rad),  0},
						{0, (float)Math.Sin(rad), (float)Math.Cos(rad), 0},
						{0, 0, 0, 1}
					};
				break;
				case Axis.Y:
					m = new float[4, 4]
					{
						{(float)Math.Cos(rad), 0, (float)Math.Sin(rad), 0},
						{0, 1, 0, 0},
						{-(float)Math.Sin(rad), 0, (float)Math.Cos(rad), 0},
						{0, 0, 0, 1}
					};
					break;
				case Axis.Z:
					m = new float[4, 4]
                    {
                        {(float)Math.Cos(rad),- (float)Math.Sin(rad), 0, 0},
                        {(float)Math.Sin(rad), (float)Math.Cos(rad), 0, 0},
                        {0, 0, 1, 0},
                        {0, 0, 0, 1}
                    };
					break;
            }
			
			return m;
		}
		public static float[,] getViewMatrix(Vector eye, Vector target, Vector up)
		{
			Vector vz = Vector.substract(eye, target).normalise();
			Vector vx = Vector.crossProduct(up, vz).normalise();
			Vector vy = Vector.crossProduct(vz, vz).normalise();

			return Matrix.multiplyMatrix(
				Matrix.getTrans(-eye.x, -eye.y, -eye.z),
				new float[4, 4] {
                    {vx.x, vx.y, vx.z,0 },
					{ vy.x, vy.y, vy.z, 0},
					{ vz.x, vz.y, vz.z, 0},
					{ 0, 0, 0, 1},
                }
			);
		}
	}
		
}
