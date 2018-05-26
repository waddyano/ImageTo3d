using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageTo3d
{
    class Options
    {
        public bool Binary = true;
        public bool Negative = false;
        public bool MirrorX = false;
        public bool MirrorY = false;
        public float DesiredWidth = 100.0f; // In millimeters
        public float MinThickness = 0.5f; // In millimeters
        public float MaxThickness = 3.5f; // In millimeters
    }

    class Generator
    {
        private float[,] heights;
        private TextWriter tw;
        private BinaryWriter bw;
        private uint nTriangles;
        private Options options;

        public Generator(Options options)
        {
            this.options = options;
        }

        public void ProcessFile(string filename, string outputFilename)
        {
            try
            {
                System.Drawing.Image image = System.Drawing.Image.FromFile(filename);

                Console.WriteLine("Size {0} {1} {2}", image.Width, image.Height, image.GetType().ToString());

                float desiredWidthMM = options.DesiredWidth;
                float stepSize = 0.2f;
                int desiredPixelWidth = (int)(desiredWidthMM / stepSize);
                int desiredPixelHeight = image.Height * desiredPixelWidth / image.Width;
                float minThick = options.MinThickness;
                float maxThick = options.MaxThickness;

                Bitmap bm = new Bitmap(desiredPixelWidth, desiredPixelHeight);
                using (Graphics g = Graphics.FromImage(bm))
                {
                    g.DrawImage(image, 0, 0, desiredPixelWidth, desiredPixelHeight);
                }

                var gb = new GaussianBlur(bm);
                gb.Process(10);

                var grayScale = new float[bm.Width, bm.Height];
                float maxGray = 0.0f;
                float minGray = 10.0f;

                for (int i = 0; i < bm.Width; i++)
                {
                    for (int j = 0; j < bm.Height; j++)
                    {
                        Color oc = bm.GetPixel(i, j);
                        float grayScaleVal = (float)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11)) / 256.0f;
                        grayScale[options.MirrorX ? bm.Width - 1 - i : i, j] = grayScaleVal;
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

                        if (options.Negative)
                            g = 1.0f - g;

                        float height = g * (maxThick - minThick);
                        //heights[bm.Width - 1 - i, x] = height < minThick ? minThick : height;
                        heights[bm.Width - 1 - i, j] = maxThick - height;
                    }
                }

                if (options.Binary)
                {
                    bw = new BinaryWriter(new FileStream(outputFilename != null ? outputFilename : (Path.GetFileNameWithoutExtension(filename) + ".stl"), FileMode.OpenOrCreate, FileAccess.ReadWrite));
                }
                else
                {
                    tw = new StreamWriter(outputFilename != null ? outputFilename : (Path.GetFileNameWithoutExtension(filename) + ".stl"));
                }

                nTriangles = 0;


                float w = (bm.Width - 1) * stepSize;
                float h = (bm.Height - 1) * stepSize;

                OutputHeader();

                OutputFront(stepSize, bm);
                OutputBack(stepSize, bm);
                OutputTop(stepSize, bm);
                OutputBottom(stepSize, bm, h);
                OutputLeft(stepSize, bm);
                OutputRight(stepSize, bm, w);

                OutputTrailer();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        // The right edge, x == w
        private void OutputRight(float stepSize, Bitmap bm, float w)
        {
            for (int j = 0; j < bm.Height - 1; ++j)
            {
                OutputTriangle(new Vector3(w, j * stepSize, 0), new Vector3(w, (j + 1) * stepSize, heights[bm.Width - 1, j + 1]), new Vector3(w, j * stepSize, heights[bm.Width - 1, j]));
                OutputTriangle(new Vector3(w, j * stepSize, 0), new Vector3(w, (j + 1) * stepSize, 0), new Vector3(w, (j + 1) * stepSize, heights[bm.Width - 1, j + 1]));
            }
        }

        // The left edge, x == 0
        private void OutputLeft(float stepSize, Bitmap bm)
        {
            for (int j = 0; j < bm.Height - 1; ++j)
            {
                OutputTriangle(new Vector3(0, j * stepSize, 0), new Vector3(0, j * stepSize, heights[0, j]), new Vector3(0, (j + 1) * stepSize, heights[0, j + 1]));
                OutputTriangle(new Vector3(0, j * stepSize, 0), new Vector3(0, (j + 1) * stepSize, heights[0, j + 1]), new Vector3(0, (j + 1) * stepSize, 0));
            }
        }

        // The bottom edge, y == h
        private void OutputBottom(float stepSize, Bitmap bm, float h)
        {
            for (int i = 0; i < bm.Width - 1; ++i)
            {
                Vector3 p1 = new Vector3(i * stepSize, h, 0);
                Vector3 p2 = new Vector3(i * stepSize, h, heights[i, bm.Height - 1]);
                Vector3 p3 = new Vector3((i + 1) * stepSize, h, heights[i + 1, bm.Height - 1]);
                Vector3 p4 = new Vector3((i + 1) * stepSize, h, 0);

                OutputTriangle(p1, p2, p4);
                OutputTriangle(p2, p3, p4);
            }
        }

        // The top edge, y == 0
        private void OutputTop(float stepSize, Bitmap bm)
        {
            for (int i = 0; i < bm.Width - 1; ++i)
            {
                Vector3 p1 = new Vector3(i * stepSize, 0, 0);
                Vector3 p2 = new Vector3((i + 1) * stepSize, 0, 0);
                Vector3 p3 = new Vector3((i + 1) * stepSize, 0, heights[i + 1, 0]);
                Vector3 p4 = new Vector3(i * stepSize, 0, heights[i, 0]);

                OutputTriangle(p1, p2, p4);
                OutputTriangle(p2, p3, p4);
            }
        }

        // The flat size, z == 0
        private void OutputBack(float stepSize, Bitmap bm)
        {
            for (int i = 0; i < bm.Width - 1; ++i)
                for (int j = 0; j < bm.Height - 1; ++j)
                {
                    Vector3 p1 = new Vector3(i * stepSize, j * stepSize, 0);
                    Vector3 p2 = new Vector3(i * stepSize, (j + 1) * stepSize, 0);
                    Vector3 p3 = new Vector3((i + 1) * stepSize, (j + 1) * stepSize, 0);
                    Vector3 p4 = new Vector3((i + 1) * stepSize, j * stepSize, 0);

                    OutputTriangle(p1, p2, p4);
                    OutputTriangle(p2, p3, p4);
                }
        }

        // The picture side, z > 0
        private void OutputFront(float stepSize, Bitmap bm)
        {
            for (int i = 0; i < bm.Width - 1; ++i)
                for (int j = 0; j < bm.Height - 1; ++j)
                {
                    Vector3 p1 = new Vector3(i * stepSize, j * stepSize, heights[i, j]);
                    Vector3 p2 = new Vector3((i + 1) * stepSize, j * stepSize, heights[i + 1, j]);
                    Vector3 p3 = new Vector3((i + 1) * stepSize, (j + 1) * stepSize, heights[i + 1, j + 1]);
                    Vector3 p4 = new Vector3(i * stepSize, (j + 1) * stepSize, heights[i, j + 1]);

                    OutputTriangle(p1, p2, p4);
                    OutputTriangle(p2, p3, p4);
                }
        }

        private void OutputHeader()
        {
            if (bw != null)
            {
                byte[] header = new byte[80];
                for (int i = 0; i < 80; ++i)
                    header[i] = (int)' ';
                bw.Write(header);
                bw.Write((uint)0);
            }
            else
            {
                tw.WriteLine("solid lithograph");
            }
        }

        private void OutputTrailer()
        {
            if (bw != null)
            {
                bw.BaseStream.Position = 80;
                bw.Write(nTriangles);
                bw.Close();
                bw = null;
            }
            else
            {
                tw.WriteLine("endsolid lithograph");
                tw.Close();
                tw = null;
            }
        }

        private void OutputTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            ++nTriangles;
            if (bw != null)
            {
                bw.Write(0.0f); bw.Write(0.0f); bw.Write(0.0f);
                bw.Write(p1.X); bw.Write(p1.Y); bw.Write(p1.Z);
                bw.Write(p2.X); bw.Write(p2.Y); bw.Write(p2.Z);
                bw.Write(p3.X); bw.Write(p3.Y); bw.Write(p3.Z);
                bw.Write((ushort)0);
            }
            else
            {
                tw.WriteLine("facet normal 0.0 0.0 0.0");
                tw.WriteLine("  outer loop");
                tw.WriteLine("  vertex {0}", p1.ToSTLFormat());
                tw.WriteLine("  vertex {0}", p2.ToSTLFormat());
                tw.WriteLine("  vertex {0}", p3.ToSTLFormat());
                tw.WriteLine("  endloop");
                tw.WriteLine("endfacet");
            }
        }
    }

    class Program
    {
        static void Help()
        {
            Options defaults = new Options();

            string[] help = new string[]
            {
                "ImageTo3D: converts an image file to a 3d STL file.",
                "    ImageTo3D [-b] [-t] [-n] [-mx] [-my] [-w <width-in-mm>] [-minthick <thick-in-mm>]",
                "              [-maxthick <thick-in-mm>] <image-file> [<output-stl-file>]",
                "",
                "       -b          set output format to binary (default)",
                "       -t          set output format to text",
                "       -n          use the negative image",
                "       -mx         mirror image in X",
                "       -my         mirror image in Y",
                "       -w          set desired width (default " + defaults.DesiredWidth + ")",
                "       -minthick   set minimum thickness in millimeters (default " + defaults.MinThickness + ")",
                "       -maxthick   set mmaximum thickness in millimeters (default " + defaults.MaxThickness + ")"
            };

            foreach (string s in help)
                Console.WriteLine(s);
        }

        static void Main(string[] args)
        {
            Options options = new Options();

            string inFile = null;
            string outFile = null;

            try
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string arg = args[i];
                    if (arg[0] == '-')
                    {
                        if (arg == "-b")
                            options.Binary = true;
                        else if (arg == "-t")
                            options.Binary = false;
                        else if (arg == "-n")
                            options.Negative = true;
                        else if (arg == "-mx")
                            options.MirrorX = true;
                        else if (arg == "-my")
                            options.MirrorY = true;
                        else if (arg == "-w")
                        {
                            if (i >= args.Length - 1) throw new ArgumentException("Expected value after -w");
                            options.DesiredWidth = Single.Parse(args[++i]);
                        }
                        else if (arg == "-minthick")
                        {
                            if (i >= args.Length - 1) throw new ArgumentException("Expected value after -minthick");
                            options.MinThickness = Single.Parse(args[++i]);
                        }
                        else if (arg == "-maxthick")
                        {
                            if (i >= args.Length - 1) throw new ArgumentException("Expected value after -maxhick");
                            options.MaxThickness = Single.Parse(args[++i]);
                        }
                        else if (arg == "-help")
                        {
                            Help();
                            System.Environment.Exit(0);
                        }
                        else
                        {
                            throw new ArgumentException("Unrecognized switch: " + arg);
                        }
                    }
                    else if (inFile == null)
                        inFile = arg;
                    else if (outFile == null)
                        outFile = arg;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Bad command line: {0}, use -help for description", ex.Message);
                System.Environment.Exit(1);
            }

            if (inFile == null)
            {
                Console.WriteLine("No input file given");
                System.Environment.Exit(1);
            }

            Generator g = new Generator(options);
            g.ProcessFile(inFile, outFile);
        }
    }
}
