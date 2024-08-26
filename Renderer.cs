using MyMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RenderSpace
{
    public class Renderer
    {
        private Vector[] vertices;
        private int[,] indices;

        Color fColor=Color.Green, bColor=Color.Red;


		private Bitmap bmp;
        public enum CullSetting {
            None,
            BackFace,
            FrontFace,
            All
        }
        
        //for tris
        BitmapData bmpData;
        byte[] rgbValues;
        int realStride;
        IntPtr ptr;
        int bytes;

		float[,] ZBUFFER;
        public Shader shader;
		public Renderer(int width, int height)
        {
            bmp= new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            bmp.SetResolution(width, height);
			bmpData = bmp.LockBits(
				new Rectangle(0, 0, bmp.Width, bmp.Height), 
                ImageLockMode.ReadWrite, 
                bmp.PixelFormat);

			ptr = bmpData.Scan0;

			realStride = bmpData.Stride / bmpData.Width;
			bytes = realStride * bmpData.Height * bmpData.Width;
			rgbValues = new byte[bytes];

			float drawDistance = 1000;
			ZBUFFER = new float[width, height];
			for (int i = 0; i < width; i++) for(int j=0; j<height; j++) ZBUFFER[i,j] = -drawDistance;
		}
        public void updateData(Vector[] vertices, int[,] indices)
        {
            this.vertices = vertices;
            this.indices = indices;
        }
        public void updateColor(Color fColor, Color bColor)
        {
            this.fColor = fColor;
            this.bColor = bColor; 
        }
        public void renderPass(CullSetting cull, Shader shader)
        {
            this.shader = shader;
            shader.updateClipSize(bmp.Width, bmp.Height);
            if (cull == Renderer.CullSetting.All) return;
            Vector[] triNormals = new Vector[indices.GetLength(0)];
            Vector[] verNormals = new Vector[vertices.Length];
            for (int i =0; i<verNormals.GetLength(0); i++) verNormals[i] = new Vector(0,0,0);
            int[] verTrisCount = new int[vertices.Length];
            //
            for (int i = 0; i < indices.GetLength(0); i++)
            {
                //точки для треугольника
                Vector t1 = vertices[indices[i, 0]];
                Vector t2 = vertices[indices[i, 1]];
                Vector t3 = vertices[indices[i, 2]];

                Vector normal = Vector.crossProduct(Vector.substract(t2, t3), Vector.substract(t1, t2)).normalise();

                //backface
                if (cull == CullSetting.FrontFace)normal = normal.scale(-1);

                triNormals[i] = normal;
				verNormals[indices[i, 0]] = Vector.add(verNormals[indices[i, 0]], normal);
				verTrisCount[indices[i, 0]] += 1;
				verNormals[indices[i, 1]] = Vector.add(verNormals[indices[i, 1]], normal);
				verTrisCount[indices[i, 1]] += 1;
				verNormals[indices[i, 2]] = Vector.add(verNormals[indices[i, 2]], normal);
				verTrisCount[indices[i, 2]] += 1;
			}
            //усреднение
            for (int i = 0; i < verNormals.Length; i++)
            {
                verNormals[i] = new Vector(verNormals[i].x / verTrisCount[i], verNormals[i].y / verTrisCount[i], verNormals[i].z / verTrisCount[i]).normalise();
            }
			//прорисовка треугольников
			for (int i = 0; i < indices.GetLength(0); i++)
			{
				if (Vector.dotProduct(triNormals[i], shader.cameraDir) > 0) continue;
				Color baseColor = fColor;
				if (cull == CullSetting.FrontFace) baseColor = bColor;
				DrawTri(baseColor,
					new Vector[3]{
					 vertices[indices[i, 0]],
					 vertices[indices[i, 1]],
					 vertices[indices[i, 2]]
					},
					triNormals[i],
					new Vector[3]
					{
						 verNormals[indices[i, 0]],
						 verNormals[indices[i, 1]],
						 verNormals[indices[i, 2]]
					}
					);
			}
			/*Parallel.For(0, indices.GetLength(0),
				i =>
				{
					if (Vector.dotProduct(triNormals[i], shader.cameraDir) > 0) return;
					Color baseColor = fColor;
					if (cull == CullSetting.FrontFace) baseColor = bColor;
					DrawTri(baseColor,
						new Vector[3]{
					vertices[indices[i, 0]],
					vertices[indices[i, 1]],
					vertices[indices[i, 2]]
						},
						triNormals[i],
						new Vector[3]
						{
						verNormals[indices[i, 0]],
						verNormals[indices[i, 1]],
						verNormals[indices[i, 2]]
						}
						);
				});*/

		}
        private void SetPixel(int i, int j, Shader.Fragment fragment)
		{
			if (fragment.ZValue < ZBUFFER[i, j]) return;
			ZBUFFER[i,j]= fragment.ZValue;
			int counter = (i * bmpData.Width + j);
			int curPixel = counter * realStride;
			rgbValues[curPixel] = fragment.color.B;
			rgbValues[curPixel + 1] = fragment.color.G;
			rgbValues[curPixel + 2] = fragment.color.R;
			rgbValues[curPixel + 3] = fragment.color.A;

		}
        public void DrawTri(Color baseColor, Vector[] vertices, Vector triNormal, Vector[] normals)
		{


			shader.setTri(vertices, normals, triNormal);
			shader.vertexShader(baseColor);
            Point[] points = shader.GetPoints();
			int x0 = points[0].X, y0 = points[0].Y;
			int x1 = points[1].X, y1 = points[1].Y;
			int x2 = points[2].X, y2 = points[2].Y;


            // РАСТЕРИЗАЦИЯ
			//верхняя часть треугольника
			for (int i = y0; i < y1; i++)
            {
				if (i < 0 || i >= bmpData.Height) continue;
				int xBorder01 = (i - y0) * (x0 - x1) / (y0 - y1) + x0;
                int xBorder02 = (i - y0) * (x0 - x2) / (y0 - y2) + x0;
                if (xBorder01 > xBorder02)
                {
                    int temp = xBorder01;
                    xBorder01 = xBorder02;
                    xBorder02 = temp;
				}
                xBorder01 = Math.Max(xBorder01, 0);
                xBorder02 = Math.Min(xBorder02, bmpData.Width-1);
               
				
				for (int j = xBorder01; j <= xBorder02; j++)
                {

					Shader.Fragment fragmentColor = shader.fragmentShader(new Point(j, i), xBorder01, xBorder02); /// J-X, I-Y
                    SetPixel(i, j, fragmentColor);
                }
            }
            //нижняя часть треугольника
            for (int i = y1; i < y2; i++)
			{
				if (i < 0 || i >= bmpData.Height) continue;
				int xBorder02 = (i - y2) * (x2 - x0) / (y2 - y0) + x2;
                int xBorder12 = (i - y2) * (x2 - x1) / (y2 - y1) + x2;
                if (xBorder02 > xBorder12)
                {
                    int temp = xBorder02;
                    xBorder02 = xBorder12;
                    xBorder12 = temp;
				}

				xBorder02 = Math.Max(xBorder02, 0);
				xBorder12 = Math.Min(xBorder12, bmpData.Width-1);
				
				for (int j = xBorder02; j <= xBorder12; j++)
                {

					Shader.Fragment fragmentColor = shader.fragmentShader(new Point(j, i), xBorder02, xBorder12); /// J-X, I-Y
					SetPixel(i, j, fragmentColor);
				}
            }
		}

		public void DrawAxis(float lineLength, int thickness)
        {
            Vector triNormal = new Vector(0,0,1);
            Vector[] normals = new Vector[3]
            {
                new Vector(0,0,1),
                new Vector(0,0,1),
                new Vector(0,0,1),
            };
			DrawTri(
                Color.FromArgb(255, 255, 0, 0),
                new Vector[3]{
				    new Vector(0, 0),
                    new Vector(lineLength, 0f),
                    new Vector(lineLength, 0.001f * thickness)
				},
                triNormal,
                normals
                );
			DrawTri(
				Color.FromArgb(255, 255, 0, 0),
				new Vector[3]{
					new Vector(0, 0),
					new Vector(lineLength, 0.001f * thickness),
					new Vector(0, 0.001f * thickness)
				},
				triNormal,
				normals
				);
			DrawTri(
				Color.FromArgb(255, 0, 255, 0),
				new Vector[3]{
					new Vector(0, 0),
					new Vector(0, lineLength),
					new Vector(0.001f * thickness, lineLength)
				},
				triNormal,
				normals
				);
			DrawTri(
				Color.FromArgb(255, 0, 255, 0),
				new Vector[3]{
					new Vector(0, 0),
					new Vector(0, lineLength),
					new Vector(0.001f * thickness, lineLength)
				},
				triNormal,
				normals
				);
			DrawTri(
				Color.FromArgb(255, 0, 0, 255),
				new Vector[3]{
					new Vector(0, 0),
					new Vector(0, 0.003f * thickness),
					new Vector(0.003f * thickness, 0.003f * thickness)
				},
				triNormal,
				normals
				);
			DrawTri(
				Color.FromArgb(255, 0, 0, 255),
				new Vector[3]{
					new Vector(0, 0),
					new Vector(0.003f * thickness, 0.001f * thickness),
					new Vector(0.003f * thickness, 0)
				},
				triNormal,
				normals
				);
		}
        public Bitmap getImage()
		{
			Marshal.Copy(rgbValues, 0, ptr, bytes);
			bmp.UnlockBits(bmpData);
			return bmp;
        }
        public void clear()
        {
            bmp.Dispose();
        }
    }
}
