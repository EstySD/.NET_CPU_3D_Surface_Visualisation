using MyMath;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderSpace
{
	public class Shader
	{
		Shader.ShadingSetting shading;
		Vector cameraPos = new Vector(0, 0, 0), lightPos = new Vector(0, 0, 0);
		public Vector cameraDir = new Vector(0, 0, -1);
		int bmpWidth, bmpHeight;

		Vector triNormal;
		Color baseColor;
		Color triColor;

		Vector[] vertices = new Vector[3];
		Point[] points;

		Vector[] verNormals = new Vector[3];
		Color[] verColors = new Color[3];

		float ambientStrength, diffuseStrength, specularStrength;

		//для интерполятора
		Color iColor;
		Vector iNormal;
		Vector iVertex;
		float iZValue;

		public enum ShadingSetting
		{
			Carcass,
			Flat,
			Gouraud,
			Phong
		}
		public struct Fragment
		{
			public int i;
			public int j;
			public Color color;
			public float ZValue;
		}
		public Shader(ShadingSetting shading)
		{
			this.shading = shading;
		}
		public void updateCamera(Vector cameraPos, Vector cameraDir)
		{
			this.cameraPos = cameraPos;
			this.cameraDir = cameraDir;
		}
		public void updadeLight(Vector lightPos)
		{
			this.lightPos = lightPos;
		}
		public void updateLightStrength(float ambientStrength, float diffuseStrength, float specularStrength)
		{
			this.ambientStrength = ambientStrength;
			this.diffuseStrength = diffuseStrength;
			this.specularStrength = specularStrength;
		}
		public void updateClipSize(int bmpWidth, int bmpHeight)
		{
			this.bmpWidth = bmpWidth;
			this.bmpHeight = bmpHeight;
		}
		private Point convertToPixel(Vector vec)
		{
			int CenterX, CenterY;
			CenterX = bmpHeight / 2;
			CenterY = bmpWidth / 2;
			return new Point(Convert.ToInt32(vec.x * CenterX + CenterX), (int)(-vec.y * CenterY + CenterY));
		}
		private Color ScaleColor(Color color, float scale)
		{
			return Color.FromArgb(255,
				Convert.ToInt16(BaseMath.Clamp(color.R*scale, 0, 255)),
				Convert.ToInt16(BaseMath.Clamp(color.G * scale, 0, 255)),
				Convert.ToInt16(BaseMath.Clamp(color.B * scale, 0, 255))
			);
		}
		public void setTri(Vector[] vertices, Vector[] verNormals, Vector triNormal)
		{
			this.triNormal = triNormal;


			// сортировка по y

			points = new Point[]
			{
				convertToPixel(vertices[0]),
				convertToPixel(vertices[1]),
				convertToPixel(vertices[2])
			};
			//пузырьком
			Point tempPoint;
			int[] order = new int[3]
			{
				0,1,2
			};
			int tempOrder;
			for (int i=0; i< points.Length; i++)
			{
				for (int j=0; j < points.Length-1-i; j++)
				{
					if (points[j].Y > points[j + 1].Y)
					{
						tempPoint = points[j];
						points[j] = points[j+1];
						points[j+1] = tempPoint;
						tempOrder = order[j];
						order[j] = order[j + 1];
						order[j + 1] = tempOrder;
					}
				}
			}
			this.vertices = new Vector[3]
			{
				vertices[order[0]],
				vertices[order[1]],
				vertices[order[2]],
			};
			this.verNormals = new Vector[3]
			{
				verNormals[order[0]],
				verNormals[order[1]],
				verNormals[order[2]],
			};
		}
		public void vertexShader(Color color)
		{
			this.baseColor = color;
			float lightStrength;
			for (int i = 0; i < 3; i++)
			{
				lightStrength = calculateLightStrength(vertices[i], verNormals[i]);
				verColors[i] = ScaleColor(color, lightStrength);
			}


			lightStrength = calculateLightStrength(Vector.center(vertices[0], vertices[1], vertices[2]), triNormal);
			triColor = ScaleColor(color, lightStrength);
		}

		void interpolate(Point p)
		{
			float x0= points[0].X; float y0= points[0].Y;
			float x1= points[1].X; float y1= points[1].Y;
			float x2= points[2].X; float y2= points[2].Y;
			float W1 = ((y1 - y2) * (p.X - x2) + (x2 - x1)*(p.Y-y2))/
				((y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2));
			float W2 = ((y2 - y0) * (p.X - x2) + (x0 - x2) * (p.Y - y2)) /
				((y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2));
			if((y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2) == 0)
			{
				W1 = 1;
				W2 = 0;
			}
			float W3 = 1 - W1 - W2;
			iColor = Color.FromArgb(255,
				Convert.ToInt16(BaseMath.Clamp(W1 * verColors[0].R + W2 * verColors[1].R + W3 * verColors[2].R, 0, 255)),
				Convert.ToInt16(BaseMath.Clamp(W1 * verColors[0].G + W2 * verColors[1].G + W3 * verColors[2].G, 0, 255)),
				Convert.ToInt16(BaseMath.Clamp(W1 * verColors[0].B + W2 * verColors[1].B + W3 * verColors[2].B, 0, 255))
			);
			iNormal = new Vector(
				verNormals[0].x * W1 + verNormals[1].x * W2 + verNormals[2].x*W3,
				verNormals[0].y * W1 + verNormals[1].y * W2 + verNormals[2].y*W3,
				verNormals[0].z * W1 + verNormals[1].z * W2 + verNormals[2].z*W3
				).normalise();
			iVertex = new Vector(
				vertices[0].x * W1 + vertices[1].x * W2 + vertices[2].x * W3,
				vertices[0].y * W1 + vertices[1].y * W2 + vertices[2].y * W3,
				vertices[0].z * W1 + vertices[1].z * W2 + vertices[2].z * W3
				);
		}
		public Fragment fragmentShader(Point p, int lBorder, int rBorder)
		{
			Fragment fragment = new Fragment();
			fragment.i = p.X; fragment.j = p.Y;
			fragment.color = Color.Transparent;
			interpolate(p);
			fragment.ZValue = iVertex.z;
			switch (shading)
			{
				case ShadingSetting.Flat:
					fragment.color = triColor;
					break;
				case ShadingSetting.Gouraud:
					fragment.color = iColor;
					break;
				case ShadingSetting.Phong:
					float lightStrength = calculateLightStrength(iVertex, iNormal);
					fragment.color = ScaleColor(baseColor, lightStrength);
					break;
				case ShadingSetting.Carcass:
					if (p.X == lBorder || p.X == rBorder|| p.X == lBorder+1 || p.X == rBorder-1) fragment.color = triColor; 
					break;
			}
			return fragment;
		}
		float calculateLightStrength(Vector t, Vector normal)
		{
			//освещение
			float ambient = ambientStrength;

			Vector toLightDir = Vector.substract(lightPos, t).normalise();
			float diffuse = diffuseStrength * Math.Max(Vector.dotProduct(normal, toLightDir), 0);
			/*Trace.WriteLine(" diffuse: " + diffuse + " lightPos: " + lightPos.x + " " + lightPos.y + " " + lightPos.z);*/
			Vector fromCameraDir = Vector.substract(t, cameraPos).normalise();

			Vector reflected = Vector.substract(toLightDir, normal.scale(2).scale(Vector.dotProduct(toLightDir, normal)));
			float specular = Vector.dotProduct(reflected, fromCameraDir);
			specular = specularStrength * (float)Math.Pow(specular, 32); // коэффициент блеска


			return ambient + diffuse +specular;
		}
		float calculateZValue()
		{
			return 0;
		}
		public Point[] GetPoints()
		{
			return points;
		}
	}
}
