using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageTo3d
{
    class Generator
    {
        private float[,] heights;

        public void ProcessFile(string filename)
        {
            try
            {
                System.Drawing.Image image = System.Drawing.Image.FromFile(filename);
                Console.WriteLine("Size {0} {1} {2}", image.Width, image.Height, image.GetType().ToString());
                Bitmap orignalBitmap = (Bitmap)image;
                var gb = new GaussianBlur(orignalBitmap);
                gb.Process(20);

                float desiredWidthMM = 100;
                float stepSize = 0.2f;
                int desiredPixelWidth = (int)(desiredWidthMM / stepSize);
                int desiredPixelHeight = image.Height * desiredPixelWidth / image.Width;
                float minThick = 0.5f;
                float maxThick = 3.5f;

                Bitmap bm = new Bitmap(desiredPixelWidth, desiredPixelHeight);
                using (Graphics g = Graphics.FromImage(bm))
                {
                    g.DrawImage(image, 0, 0, desiredPixelWidth, desiredPixelHeight);
                }

                var grayScale = new float[bm.Width, bm.Height];
                float maxGray = 0.0f;
                float minGray = 10.0f;

                for (int i = 0; i < bm.Width; i++)
                {
                    for (int j = 0; j < bm.Height; j++)
                    {
                        Color oc = bm.GetPixel(i, j);
                        float grayScaleVal = (float)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11)) / 256.0f;
                        grayScale[i, j] = grayScaleVal;
                        if (grayScaleVal < minGray)
                            minGray = grayScaleVal;
                        if (grayScaleVal > maxGray)
                            maxGray = grayScaleVal;
                    }
                }

                heights = new float[bm.Width, bm.Height];
                for (int i = 0; i < bm.Width; i++)
                {
                    for (int j = 0; j < bm.Height; j++)
                    {
                        float g = (grayScale[i, j] - minGray) / (maxGray - minGray);
                        float height = g * (maxThick - minThick);
                        //heights[bm.Width - 1 - i, x] = height < minThick ? minThick : height;
                        heights[bm.Width - 1 - i, j] = maxThick - height;
                    }
                }

                TextWriter tw = new StreamWriter(Path.GetFileNameWithoutExtension(filename) + ".stl");

                tw.WriteLine("solid lithograph");

                OutputTop(stepSize, bm, tw);
                OutputBottom(stepSize, bm, tw);

                float w = (bm.Width - 1) * stepSize;
                float h = (bm.Height - 1) * stepSize;

                for (int i = 0; i < bm.Width - 1; ++i)
                {
                    Vector3 p1 = new Vector3(i * stepSize, 0, 0);
                    Vector3 p2 = new Vector3((i + 1) * stepSize, 0, 0);
                    Vector3 p3 = new Vector3((i + 1) * stepSize, 0, heights[i + 1, 0]);
                    Vector3 p4 = new Vector3(i * stepSize, 0, heights[i, 0]);

                    OutputTriangle(tw, p1, p2, p4);
                    OutputTriangle(tw, p2, p3, p4);
                }

                for (int i = 0; i < bm.Width - 1; ++i)
                {
                    Vector3 p1 = new Vector3(i * stepSize, h, 0);
                    Vector3 p2 = new Vector3(i * stepSize, h, heights[i, bm.Height - 1]);
                    Vector3 p3 = new Vector3((i + 1) * stepSize, h, heights[i + 1, bm.Height - 1]);
                    Vector3 p4 = new Vector3((i + 1) * stepSize, h, 0);

                    OutputTriangle(tw, p1, p2, p4);
                    OutputTriangle(tw, p2, p3, p4);
                }

                for (int j = 0; j < bm.Height - 1; ++j)
                {
                    tw.WriteLine("facet normal 0.0 0.0 0.0");
                    tw.WriteLine("  outer loop");
                    tw.WriteLine("  vertex {0} {1} {2}", 0, j * stepSize, 0);
                    tw.WriteLine("  vertex {0} {1} {2}", 0, j * stepSize, heights[0, j]);
                    tw.WriteLine("  vertex {0} {1} {2}", 0, (j + 1) * stepSize, heights[0, j + 1]);
                    tw.WriteLine("  endloop");
                    tw.WriteLine("endfacet");

                    tw.WriteLine("facet normal 0.0 0.0 0.0");
                    tw.WriteLine("  outer loop");
                    tw.WriteLine("  vertex {0} {1} {2}", 0, j * stepSize, 0);
                    tw.WriteLine("  vertex {0} {1} {2}", 0, (j + 1) * stepSize, heights[0, j + 1]);
                    tw.WriteLine("  vertex {0} {1} {2}", 0, (j + 1) * stepSize, 0);
                    tw.WriteLine("  endloop");
                    tw.WriteLine("endfacet");

                    tw.WriteLine("facet normal 0.0 0.0 0.0");
                    tw.WriteLine("  outer loop");
                    tw.WriteLine("  vertex {0} {1} {2}", w, j * stepSize, 0);
                    tw.WriteLine("  vertex {0} {1} {2}", w, (j + 1) * stepSize, heights[bm.Width - 1, j + 1]);
                    tw.WriteLine("  vertex {0} {1} {2}", w, j * stepSize, heights[bm.Width - 1, j]);
                    tw.WriteLine("  endloop");
                    tw.WriteLine("endfacet");

                    tw.WriteLine("facet normal 0.0 0.0 0.0");
                    tw.WriteLine("  outer loop");
                    tw.WriteLine("  vertex {0} {1} {2}", w, j * stepSize, 0);
                    tw.WriteLine("  vertex {0} {1} {2}", w, (j + 1) * stepSize, 0);
                    tw.WriteLine("  vertex {0} {1} {2}", w, (j + 1) * stepSize, heights[bm.Width - 1, j + 1]);
                    tw.WriteLine("  endloop");
                    tw.WriteLine("endfacet");
                }

                tw.WriteLine("endsolid lithograph");
                tw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        private void OutputBottom(float stepSize, Bitmap bm, TextWriter tw)
        {
            for (int i = 0; i < bm.Width - 1; ++i)
                for (int j = 0; j < bm.Height - 1; ++j)
                {
                    Vector3 p1 = new Vector3(i * stepSize, j * stepSize, 0);
                    Vector3 p2 = new Vector3(i * stepSize, (j + 1) * stepSize, 0);
                    Vector3 p3 = new Vector3((i + 1) * stepSize, (j + 1) * stepSize, 0);
                    Vector3 p4 = new Vector3((i + 1) * stepSize, j * stepSize, 0);

                    OutputTriangle(tw, p1, p2, p4);
                    OutputTriangle(tw, p2, p3, p4);
                }
        }

        private void OutputTop(float stepSize, Bitmap bm, TextWriter tw)
        {
            for (int i = 0; i < bm.Width - 1; ++i)
                for (int j = 0; j < bm.Height - 1; ++j)
                {
                    Vector3 p1 = new Vector3(i * stepSize, j * stepSize, heights[i, j]);
                    Vector3 p2 = new Vector3((i + 1) * stepSize, j * stepSize, heights[i + 1, j]);
                    Vector3 p3 = new Vector3((i + 1) * stepSize, (j + 1) * stepSize, heights[i + 1, j + 1]);
                    Vector3 p4 = new Vector3(i * stepSize, (j + 1) * stepSize, heights[i, j + 1]);

                    OutputTriangle(tw, p1, p2, p4);
                    OutputTriangle(tw, p2, p3, p4);
                }
        }

        private void OutputTriangle(TextWriter tw, Vector3 p1, Vector3 p2, Vector3 p4)
        {
            tw.WriteLine("facet normal 0.0 0.0 0.0");
            tw.WriteLine("  outer loop");
            tw.WriteLine("  vertex {0}", p1.ToSTLFormat());
            tw.WriteLine("  vertex {0}", p2.ToSTLFormat());
            tw.WriteLine("  vertex {0}", p4.ToSTLFormat());
            tw.WriteLine("  endloop");
            tw.WriteLine("endfacet");
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Generator g = new Generator();
            g.ProcessFile(args[0]);
        }
    }
}
