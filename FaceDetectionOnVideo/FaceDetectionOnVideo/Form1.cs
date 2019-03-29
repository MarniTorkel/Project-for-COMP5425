using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using WMPLib;
using AxWMPLib;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.UI;
using Emgu.CV.Structure;

using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Common.Contract;


namespace FaceDetectionOnVideo
{
    public partial class Form1 : Form
    {
        const String APIKEY = "3baa3e42380844f599aa466d11230f1f";
        private readonly IFaceServiceClient faceServiceClinet = new FaceServiceClient(APIKEY);

        //const String APIKEY = "e8f93c3953f54e8d82f78f2149ba3385";
        //EmotionServiceClient emotionServiceClient = new EmotionServiceClient(APIKEY);
        //Emotion[] emotionResult;

        // Store frames and filenames in lists
        List<Image<Bgr, Byte>> matchList = new List<Image<Bgr, byte>>();
        List<String> fnameList = new List<string>();

        Stopwatch watch;

        public Form1()
        {
            InitializeComponent();
            this.Size = new Size(300, 285);
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All files (*.*)|*.*";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                axWindowsMediaPlayer1.URL = ofd.FileName;

                // Read video files into a folder
                GenerateVideoFiles(ofd.FileName);

                btnDetect.Visible = true;
                this.Size = new Size(300, 330);
            }
        }

        // Write video frames to a folder
        public void GenerateVideoFiles(String videoFile)
        {
            Capture file;
            Mat frame;

            String filename;
            int counter = 0; // Number of frames
            int length = 5; // filename 0 padding

            // Video files
            //Capture file;
            file = new Capture(videoFile);
            long videoTime;
            watch = Stopwatch.StartNew();

            // Create a directory if it does not exist
            string folderName = @"C:/Users/Marni/Pictures";
            string pathString = System.IO.Path.Combine(folderName, "videos");
            System.IO.Directory.CreateDirectory(pathString);

            bool Reading = true;

            while (Reading)
            {
                frame = file.QueryFrame();
                if (frame != null)
                {
                    // store the input file names in a list
                    filename = "video" + counter.ToString().PadLeft(length, '0');

                    // Copy frames to files in a folder
                    frame.ToImage<Bgr, Byte>().ToBitmap().Save("C:/Users/Marni/Pictures/videos/" + filename + ".jpg");

                    if (CvInvoke.WaitKey(0) == 27)
                        break;
                    counter++;
                }
                else
                {
                    Reading = false;
                }

            }
            watch.Stop();
            videoTime = watch.ElapsedMilliseconds;
            Debug.WriteLine("Writing video files time:" + videoTime);
        }


        private void btnDetect_Click(object sender, EventArgs e)
        {
            pnlFrames.Controls.Clear();
            matchList.Clear();
            fnameList.Clear();
            // Search frames
            matchList = searchFrames(ref fnameList);
            this.Size = new Size(566, 330);

            Debug.WriteLine("File name list" + fnameList.Count);
            Debug.WriteLine("Match list" + matchList);
            if (matchList.Count != 0)
            {
                foreach (Image<Bgr, Byte> matchImage in matchList)
                {
                    // Write to flow layout
                    PictureBox mImage = new PictureBox();
                    mImage.Image = matchImage.Bitmap;
                    mImage.Size = new Size(100, 80);
                    mImage.SizeMode = PictureBoxSizeMode.Zoom;
                    pnlFrames.Controls.Add(mImage);
                }
                // The number of match images
                lblText.Text = "Number of frames detected  : " + matchList.Count;

            }
        }

        // Detect frames and place rectangle on faces
        static List<Image<Bgr, Byte>> searchFrames(ref List<String> fnameList)
        {
            List<Image<Bgr, Byte>> fileList = new List<Image<Bgr, byte>>();
            List<String> filenames = new List<String>();

            string path = @"C:/Users/Marni/Pictures/videos";
            string[] filePaths = Directory.GetFiles(path, "*.jpg");


            foreach (String filename in filePaths)
            {
                if (filename != null)
                {
                    Image<Bgr, byte> image = new Image<Bgr, byte>(filename);
                    // Check if the frame has face
                    List<Rectangle> faces = new List<Rectangle>();
                    Image<Gray, Byte> gray = new Image<Gray, byte>(filename);
                    CascadeClassifier face = new CascadeClassifier(
                        "haarcascade_frontalface_default.xml");
                    CvInvoke.CvtColor(image, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                    //normalizes brightness and increases contrast of the image 
                    CvInvoke.EqualizeHist(gray, gray);
                    Rectangle[] facesDetected = face.DetectMultiScale(
                                                            gray,
                                                            1.1,
                                                            10,
                                                            new Size(20, 20));
                    if (facesDetected.Length > 0)
                    {
                        foreach (Rectangle face1 in facesDetected)
                        {
                            image.Draw(face1, new Bgr(Color.Cyan), 3);
                        }
                        fileList.Add(image);
                        fnameList.Add(filename);
                    }
                }
            }
            return fileList;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<Image<Bgr, Byte>> uniqueList = new List<Image<Bgr, byte>>();

            if (matchList.Count > 0)
            {
                uniqueList = getUnique(matchList, ref fnameList);
            }
            if (uniqueList.Count > 0)
            {
                pnlFrames.Controls.Clear();
                matchList.Clear();
                foreach (Image<Bgr, Byte> uImage in uniqueList)
                {
                    // Write to flow layout
                    PictureBox mImage = new PictureBox();
                    mImage.Image = uImage.Bitmap;
                    mImage.Size = new Size(100, 80);
                    mImage.SizeMode = PictureBoxSizeMode.Zoom;
                    pnlFrames.Controls.Add(mImage);
                    matchList.Add(uImage);
                }
                // The number of match images
                lblText.Text = "Unique number of frames  : " + matchList.Count;
            }
            Debug.WriteLine("MachList size :" + matchList.Count);
            Debug.WriteLine("File List" + fnameList.Count);

        }

        // Get the near unique frames using color histogram method
        static List<Image<Bgr, Byte>> getUnique(List<Image<Bgr, Byte>> completeList, ref List<String> fnameList)
        {
            List<Image<Bgr, Byte>> uList = new List<Image<Bgr, byte>>();
            List<String> nameList = new List<String>();

            // Compare image in the file to remove near duplicate
            int rStep = 4;
            int gStep = 4;
            int bStep = 4;
            double[] firstHist = new double[0];
            double intersectSum = 0.0;
            double threshold = 0.7;
            bool firstImage = true;
            int counter = 0;

            if (completeList.Count > 0)
            {
                foreach (Image<Bgr, byte> frame in completeList)
                {
                    intersectSum = 0;
                    if (firstImage)
                    {
                        firstImage = false;
                        firstHist = colorHistogram(frame, rStep, gStep, bStep);
                        uList.Add(frame);
                        nameList.Add(fnameList.ElementAt(counter));
                    }
                    else
                    {
                        double[] secondHist = colorHistogram(frame, rStep, gStep, bStep);
                        for (int i = 0; i < secondHist.Length; i++)
                        {
                            if (firstHist[i] > secondHist[i])
                            {
                                intersectSum += secondHist[i];
                            }
                            else
                            {
                                intersectSum += firstHist[i];
                            }
                        }
                        if (intersectSum < threshold)
                        {
                            firstHist = secondHist;
                            uList.Add(frame);
                            nameList.Add(fnameList.ElementAt(counter));
                        }
                    }
                    counter++;
                }
                // Regenerare the file List
                fnameList.Clear();
                foreach (String name in nameList)
                {
                    fnameList.Add(name);
                }
            }
            return uList;
        }

        // calculate color histogram for a given image img by uniformly quantizing R, G, and B
        // components into rStep, gStep, and bStep levels, respectively.
        static double[] colorHistogram(Image<Bgr, Byte> img, int rStep, int gStep, int bStep)
        {
            double[] histogram = null;

            // dimension of the image 
            int width = img.Width;
            int height = img.Height;

            int total_pixels = (int)(width * height);

            // Number of bins
            int rBins = (int)(256 / rStep);
            int gBins = (int)(256 / gStep);
            int bBins = (int)(256 / bStep);

            // Setup rgb buckets
            double[] blueBuckets = new double[bBins];
            double[] redBuckets = new double[rBins];
            double[] greenBuckets = new double[gBins];
            double[] hist = new double[rBins * gBins * bBins];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    //Get rgb from each pixel

                    Bgr pixel = img[j, i];
                    //Console.WriteLine(pixel);
                    double red = pixel.Red;
                    double green = pixel.Green;
                    double blue = pixel.Blue;

                    // Convert rgb to number of bins
                    int bBin = (int)(blue / bStep);
                    int gBin = (int)(green / gStep);
                    int rBin = (int)(red / rStep);

                    hist[gBins * bBins * rBin + bBins * gBin + bBin] += 1;
                }
            }
            // Divide the total values with total pixels to normalise it
            for (int k = 0; k < hist.Length; k++)
            {
                hist[k] /= total_pixels;
            }
            histogram = hist;
            return histogram;
        }

        private async void btnAttr_Click(object sender, EventArgs e)
        {
            this.Size = new Size(660, 325);

            // Connect to Azure Face API for frames
            if (fnameList.Count > 0)
            {
                foreach (String fname in fnameList)
                {
                    string filePath = "C:/Users/Marni/Pictures/videos" + fname;
                    Debug.WriteLine("File path : " + fname);
                    //emotionResult = await emotionServiceClient.RecognizeAsync(filePath);

                    /*Face[] faces = await UploadAndDetectFaces(fname);
                    
                    if (faces.Length > 0)
                    {
                        Debug.WriteLine("Face length :" + faces.Length);
                        Image<Bgr, byte> image = new Image<Bgr, byte>(fname);
                        var faceBitmap = image.ToBitmap();

                        using (var g = Graphics.FromImage(faceBitmap))
                        {
                            foreach (var face in faces)
                            {
                                var fr = face.FaceRectangle;
                                var fa = face.FaceAttributes;

                                var faceRect = new Rectangle(fr.Left, fr.Top, fr.Width, fr.Height);
                                g.DrawImage(image.ToBitmap(), faceRect, faceRect, GraphicsUnit.Pixel);
                                g.DrawRectangle(Pens.Aqua, faceRect);

                                // Draw face information
                                g.FillRectangle(new SolidBrush(Color.FromArgb(200, Color.Bisque)), fr.Left - 10, fr.Top + fr.Height + 10, fr.Width < 120 ? 120 : fr.Width + 20, 25);
                                g.DrawString(string.Format("{0:0.0}/{1}/{2}", fa.Age, fa.Gender, fa.Emotion.ToRankedList().OrderByDescending(x => x.Value).First().Key),
                                    this.Font, Brushes.Black,
                                    fr.Left - 8,
                                    fr.Top + fr.Height + 14);

                            }
                        }
                        // Write to flow layout
                        PictureBox mImage = new PictureBox();
                        mImage.Image = faceBitmap;
                        mImage.Size = new Size(100, 80);
                        mImage.SizeMode = PictureBoxSizeMode.Zoom;
                        pnlFrames.Controls.Add(mImage);
                    }*/
                }
            }           
        }

        private async Task<Face[]> UploadAndDetectFaces(String imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var faces = await faceServiceClinet.DetectAsync(imageFileStream,
                        true,
                        true,
                        new FaceAttributeType[] {
                            FaceAttributeType.Gender,
                            FaceAttributeType.Age,
                            FaceAttributeType.Emotion });
                    Debug.WriteLine(faces.ToString());
                    return faces.ToArray();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return new Face[0];
            }
        }

    }
}
