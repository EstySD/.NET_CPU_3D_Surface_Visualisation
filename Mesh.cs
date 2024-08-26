using MyMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static visualisation_lr1.Form1;

namespace Mesh
{
	public class MeshInfo
	{
		Vector[] vertices;
		int[,] indices;
		int N1, N2;
		float uMin, uMax, vMax, vMin, R1, R2;
		surface surf;
		public enum MeshType
		{
			Cube,
			Sphere,
			Torus,
			Mebious
		}
		public void setSettings(surface surf, int N1, int N2, float R1, float R2)
		{
			this.surf = surf;
			this.N1 = N1;
			this.N2 = N2;
			this.R1 = R1;
			this.R2 = R2;
		}
		public void setInterval(float uMin, float uMax, float vMin, float vMax)
		{
			this.uMin = uMin;
			this.uMax = uMax;
			this.vMin = vMin;
			this.vMax = vMax;
		}
		public void calculate(MeshType meshType)
		{
			vertices = new Vector[(N1 + 1) * (N2 + 1)];
			indices = new int[(N1) * (N2) * 2, 3];
			if (meshType == MeshType.Cube)
			{
				calculateCube();
				return;
			}
			float u = uMin;
			float v = vMin;
			float du = (float)(uMax - uMin) / N1;
			float dv = (float)(vMax - vMin) / N2;

			for (int n2 = 0; n2 < N2 + 1; n2++)
			{
				for (int n1 = 0; n1 < N1 + 1; n1++)
				{
					int curIndex = n1 + (N1 + 1) * n2;
					Vector f = surf.calculate(meshType, u, v);
					vertices[curIndex] = new Vector(
						f.x * R1,
						f.y * R1,
						f.z * R2
					);

					u += du;
				}
				v += dv;
				u = uMin;
			}
			int curTriangle = 0;
			for (int n2 = 0; n2 < N2; n2++)
			{
				for (int n1 = 0; n1 < N1; n1++)
				{
					int curVertex = n1 + n2 * N1 + n2; //левый нижний угол каждого квадрата
													   //первый треугольник внутри квадрата
					indices[curTriangle, 0] = curVertex;
					indices[curTriangle, 1] = curVertex + 1 + N1;
					indices[curTriangle, 2] = curVertex + 2 + N1;
					//второй треугольник внутри квадрата
					indices[curTriangle + 1, 0] = curVertex;
					indices[curTriangle + 1, 1] = curVertex + 2 + N1;
					indices[curTriangle + 1, 2] = curVertex + 1;

					curTriangle += 2;
				}
			}
		}
		private void calculateCube()
		{
			//куб
			vertices = new Vector[8]{
					new Vector(-0.5f, 0.5f, 0.5f), // 0 вершина
					  new Vector(-0.5f, 0.5f, -0.5f), // 1 вершина
					  new Vector(0.5f, 0.5f, -0.5f), // 2 вершина
					  new Vector(0.5f, 0.5f, 0.5f), // 3 вершина
					  new Vector(-0.5f, -0.5f, 0.5f), // 4 вершина
					  new Vector(-0.5f, -0.5f, -0.5f), // 5 вершина
					  new Vector(0.5f, -0.5f, -0.5f), // 6 вершина
					  new Vector(0.5f, -0.5f, 0.5f) // 7 вершина
				};
			indices = new int[12, 3]
			{
				  {0, 1, 2}, // 0
				  {0, 2, 3}, // 1

				  {4, 6, 5}, // 2
				  {4, 7, 6}, // 3

				  {0, 5, 1}, // 4
				  {0, 4, 5}, // 5

				  {1, 5, 2}, // 6
				  {6, 2, 5}, // 7

				  {3, 2, 6}, // 8
				  {3, 6, 7}, // 9

				  {3, 4, 0}, // 10
				  {4, 3, 7}, // 11
			};

		}
		public Vector[] getVertices()
		{
			return vertices;
		}
		public int[,] getIndices()
		{
			return indices;
		}
	}
	public class surface
	{

		public int vmin, vmax, umin, umax; // в градусах
		float u;
		float v;
		public surface(int umin, int umax, int vmin, int vmax)
		{
			resize(umin, umax, vmin, vmax);
		}
		public void resize(int umin, int umax, int vmin, int vmax)
		{
			this.umin = umin;
			this.umax = umax;
			this.vmin = vmin;
			this.vmax = vmax;
		}
		public Vector calculate(Mesh.MeshInfo.MeshType meshType, float u, float v)
		{
			this.u = u;
			this.v = v;
			switch (meshType)
			{
				case MeshInfo.MeshType.Sphere:
					return sphere();
				case MeshInfo.MeshType.Torus: return torus();
				case MeshInfo.MeshType.Mebious: return mebious();

			}
			return sphere();
		}
		private Vector sphere()
		{
			return new Vector(
				(float)((Math.Cos(u) * Math.Cos(v))),
				(float)(Math.Sin(u) * Math.Cos(v)),
				(float)Math.Sin(v));
		}
		private Vector torus()
		{
			return new Vector(
				(float)((Math.Cos(u) * (0.4f*Math.Cos(v) + 0.75f))),
				(float)((Math.Sin(u) * (0.4f*Math.Cos(v) + 0.75f))),
				(float)(Math.Sin(v)*0.4f)
				);
		}
		private Vector mebious()
		{
			return new Vector(
				(float)((1 + v / 2 * Math.Cos(u / 2)) * Math.Cos(u)*0.5f),
				(float)((1 + v / 2 * Math.Cos(u / 2)) * Math.Sin(u)*0.5f),
				(float)((v / 2)*Math.Sin(u/2)*0.5f)
				);
		}
	}
}
