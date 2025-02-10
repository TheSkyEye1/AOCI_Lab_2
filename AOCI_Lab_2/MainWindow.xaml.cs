using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace AOCI_Lab_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        Mat sourceMat;
        Mat overlayMat;
        VideoCapture capture;
        Mat frame;

        int _overlay_coef1 = 50;
        int _overlay_coef2 = 50;

        int _brightness = 0;
        double _contrast = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImageButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff";
            if (openFileDialog.ShowDialog() == true)
            {
                //if (timer != null)
                //{
                //    timer.Stop();
                //}
                //capture = null;
                //frame = null;
                //videoFilterState = 0;

                string FilePath = openFileDialog.FileName;
                sourceMat = Cv2.ImRead(FilePath, ImreadModes.Color);
                sourceMat = sourceMat.Resize(new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Linear);
                ImageBox1.Source = sourceMat.ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        private void LoadVideoButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private Mat getChannelImage(Mat source, int channel)
        {
            Mat imageCopy = new Mat();
            source.CopyTo(imageCopy);
            for (int h = 0; h < imageCopy.Cols; h++)
                for (int r = 0; r < imageCopy.Rows; r++) // обход по пискелям
                {
                    Vec3b pixel = source.At<Vec3b>(r, h);
                    Vec3b p = new Vec3b(0, 0, 0);
                    p[channel] = pixel[channel];
                    imageCopy.Set(r, h, p);
                }
            return imageCopy;
        }

        private Mat bgrToGrayCustom(Mat source)
        {
            Mat grayImage = new Mat(source.Size(), MatType.CV_8UC1);

            for (int y = 0; y < grayImage.Rows; y++)
            {
                for (int x = 0; x < grayImage.Cols; x++)
                {
                    Vec3b pixel = source.At<Vec3b>(y, x);
                    byte blue = pixel.Item0;
                    byte green = pixel.Item1;
                    byte red = pixel.Item2;

                    byte grayValue = Convert.ToByte(0.299 * red + 0.587 * green + 0.114 * blue);

                    grayImage.At<byte>(y, x) = grayValue;
                }
            }

            return grayImage;
        }
        private Mat ChangeBrightness(Mat source)
        {
            Mat imageCopy = new Mat();
            source.CopyTo(imageCopy);
            for (int h = 0; h < imageCopy.Cols; h++)
                for (int r = 0; r < imageCopy.Rows; r++)
                {
                    Vec3b color = imageCopy.At<Vec3b>(r, h);
                    for (int channel = 0; channel < imageCopy.Channels(); channel++)
                    {
                        int k = color[channel];
                        k += _brightness;
                        if (k > 255) k = 255;
                        if (k < 0) k = 0;
                        color[channel] = (byte)k;
                    }
                    imageCopy.Set(r, h, color);
                }

            return imageCopy;
        }

        private Mat ChangeContrast(Mat source)
        {
            Mat imageCopy = new Mat();
            source.CopyTo(imageCopy);
            for (int h = 0; h < imageCopy.Cols; h++)
                for (int r = 0; r < imageCopy.Rows; r++)
                {
                    Vec3b color = imageCopy.At<Vec3b>(r, h);
                    for (int channel = 0; channel < imageCopy.Channels(); channel++)
                    {
                        double k = color[channel];
                        if (_contrast > 0) k *= _contrast;
                        if(_contrast < 0) k /= _contrast;
                        if (k > 255) k = 255;
                        if (k < 0) k = 0;
                        color[channel] = (byte)k;
                    }
                    imageCopy.Set(r, h, color);
                }

            return imageCopy;
        }

        private void RedChannelClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                ImageBox2.Source = getChannelImage(sourceMat, 2).ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        private void BlueChannelClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                ImageBox2.Source = getChannelImage(sourceMat, 1).ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        private void GreenChannelClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                ImageBox2.Source = getChannelImage(sourceMat, 0).ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        private void ConvertToGrayClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                ImageBox2.Source = bgrToGrayCustom(sourceMat).ToWriteableBitmap(PixelFormats.Gray8);
            }
        }

        private void BrightnessValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _brightness = (int)BrightnessSlider.Value;
            BrightnessTB.Text = _brightness.ToString();
            if (sourceMat != null)
            {
                ImageBox2.Source = ChangeBrightness(sourceMat).ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        private void ContsrastValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _contrast = ContrastSlider.Value;
            ContrastTB.Text = _contrast.ToString();
            if (sourceMat != null && _contrast != 0)
            {
                ImageBox2.Source = ChangeContrast(sourceMat).ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        private void LoadOverlayImageClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff";
            if (openFileDialog.ShowDialog() == true)
            {
                string FilePath = openFileDialog.FileName;
                overlayMat = Cv2.ImRead(FilePath, ImreadModes.Color);
                overlayMat = overlayMat.Resize(new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Linear);
                ImageBox3.Source = overlayMat.ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        private Mat CombineImages(Mat image1, Mat image2)
        {
            Mat combined = new Mat(image1.Size(), MatType.CV_8UC3);
            for (int h = 0; h < combined.Cols; h++)
                for (int r = 0; r < combined.Rows; r++)
                {
                    Vec3b colorRes = combined.At<Vec3b>(r, h);
                    Vec3b color1 = image1.At<Vec3b>(r, h);
                    Vec3b color2 = image2.At<Vec3b>(r, h);
                    for (int channel = 0; channel < 3; channel++)
                    {
                        double k1 = color1[channel];
                        double k2 = color2[channel];
                        double k3 = (double)(k1 * ((double)_overlay_coef1/100)) + (double)(k2 * ((double)_overlay_coef2 /100));
                        if (k3 > 255) k3 = 255;
                        if (k3 < 0) k3 = 0;
                        colorRes[channel] = (byte)k3;
                    }
                    combined.Set(r, h, colorRes);
                }
            return combined;
        }

        private void OverlaySlider1Change(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded == true)
            {
                _overlay_coef1 = (int)OverlayValue1.Value;
                _overlay_coef2 = 100 - _overlay_coef1;

                OverlayValue2.Value = _overlay_coef2;
                OverlayValue1TB.Text = _overlay_coef1.ToString();
                OverlayValue2TB.Text = _overlay_coef2.ToString();

                if (sourceMat != null && overlayMat != null)
                {
                    ImageBox2.Source = CombineImages(sourceMat, overlayMat).ToWriteableBitmap(PixelFormats.Bgr24);
                }
            }
        }

        private void OverlaySlider2Change(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded == true)
            {
                _overlay_coef2 = (int)OverlayValue2.Value;
                _overlay_coef1 = 100 - _overlay_coef2;

                OverlayValue1.Value = _overlay_coef1;
                OverlayValue1TB.Text = _overlay_coef1.ToString();
                OverlayValue2TB.Text = _overlay_coef2.ToString();

                if (sourceMat != null && overlayMat != null)
                {
                    ImageBox2.Source = CombineImages(sourceMat, overlayMat).ToWriteableBitmap(PixelFormats.Bgr24);
                }
            }
        }
    }
}
