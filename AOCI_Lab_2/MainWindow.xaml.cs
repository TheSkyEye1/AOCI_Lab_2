using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        #region ChannelOperations
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

        #endregion

        #region GrayAndSepia
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
        private void ConvertToGrayClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                ImageBox2.Source = bgrToGrayCustom(sourceMat).ToWriteableBitmap(PixelFormats.Gray8);
            }
        }

        private byte AddColorsWithCoefs(List<int> colors, List<double> coefs)
        {
            double result = 0;
            for(int i = 0; i<colors.Count; i++)
            {
                result += (double)(colors[i] * coefs[i]);
            }


            if (result > 255) return 255;
            else return (byte)result;
        }

        private Mat imageToSepia(Mat source)
        {
            Mat image = new Mat(source.Size(), MatType.CV_8UC3);

            List<double> redCoefs = new List<double>() { 0.393, 0.769, 0.189};
            List<double> greenCoefs = new List<double>() { 0.349, 0.686, 0.168};
            List<double> blueCoefs = new List<double>() { 0.272, 0.534, 0.131};

            for (int y = 0; y < image.Rows; y++)
            {
                for (int x = 0; x < image.Cols; x++)
                {
                    Vec3b pixel = source.At<Vec3b>(y, x);
                    List<int> colors = new List<int> { pixel.Item2, pixel.Item1, pixel.Item0 };

                    byte red = AddColorsWithCoefs(colors, redCoefs);
                    byte green = AddColorsWithCoefs(colors, greenCoefs);
                    byte blue = AddColorsWithCoefs(colors, blueCoefs);

                    image.At<Vec3b>(y, x) = new Vec3b(blue,green,red);
                }
            }
            return image;
        }

        private void ConvertToSepiaClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                ImageBox2.Source = imageToSepia(sourceMat).ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        #endregion

        #region ContrastAndBrightness
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

        #endregion

        #region ImagesMathOperations
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

        private Mat CombineImages(Mat image1, Mat image2, MathOperation operation, bool applyCoef)
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
                        byte k3;
                        if (applyCoef) k3 = operation(k1 * ((double)_overlay_coef1 / 100), k2 * ((double)_overlay_coef2 / 100));
                        else k3 = operation(k1, k2);
                        colorRes[channel] = k3;
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
            }
        }

        private void AddImages(object sender, RoutedEventArgs e)
        {
            MathOperation operation = AddPixels;
            ImageBox2.Source = CombineImages(sourceMat, overlayMat, operation, true).ToWriteableBitmap(PixelFormats.Bgr24);
        }

        private void SubImages(object sender, RoutedEventArgs e)
        {
            MathOperation operation = SubPixels;
            ImageBox2.Source = CombineImages(sourceMat, overlayMat, operation, true).ToWriteableBitmap(PixelFormats.Bgr24);
        }

        private void MultImages(object sender, RoutedEventArgs e)
        {
            MathOperation operation = MultPixels;
            ImageBox2.Source = CombineImages(sourceMat, overlayMat, operation, false).ToWriteableBitmap(PixelFormats.Bgr24);
        }

        private void DivImages(object sender, RoutedEventArgs e)
        {
            MathOperation operation = DivPixels;
            ImageBox2.Source = CombineImages(sourceMat, overlayMat, operation, false).ToWriteableBitmap(PixelFormats.Bgr24);
        }

        #region Delegates

        private delegate byte MathOperation(double num1, double num2);

        private byte AddPixels(double a, double b)
        {
            if (a + b > 255) return 255;
            else return (byte)(a + b);
        }

        private byte SubPixels(double a, double b)
        {
            if (a - b < 0) return 0;
            else return (byte)(a - b);
        }

        private byte MultPixels(double a, double b)
        {
            if (a * b > 255) return 255;
            else return (byte)(a * b);
        }

        private byte DivPixels(double a, double b)
        {
            if (a / b < 0) return 0;
            else return (byte)(a / b);
        }

        #endregion

        #endregion

        #region WindowFilters

        private Mat ApplyWindowFilter(Mat source, List<int> windowFilter, MatType imgFormat, WindowFilterOperation op)
        {
            Mat image1 = new Mat(source.Size(), imgFormat);
            Mat image2 = new Mat(source.Size(), imgFormat); 
            
            if (imgFormat == MatType.CV_8UC1) image2 = bgrToGrayCustom(source);
            else source.CopyTo(image2);

            for (int y = 1; y < image1.Rows - 1; y++)
            {
                for (int x = 1; x < image1.Cols - 1; x++)
                {
                    if(imgFormat == MatType.CV_8UC3)
                    {
                        for (int c = 0; c < 3; c++) 
                        {
                            List<int> colors = new List<int>();
                            int index = 0;
                            for (int k1 = -1; k1 < 2; k1++)
                                for (int k2 = -1; k2 < 2; k2++)
                                {
                                    colors.Add(image2.At<Vec3b>(y+k1, x+k2)[c] * windowFilter[index]);
                                    index++;
                                }

                            image1.At<Vec3b>(y,x)[c] = op(colors);
                        }
                    }
                    if(imgFormat == MatType.CV_8UC1)
                    {
                        List<int> colors = new List<int>();
                        int index = 0;
                        for (int k1 = -1; k1 < 2; k1++)
                            for (int k2 = -1; k2 < 2; k2++)
                            {
                                colors.Add(image2.At<byte>(y+k1, x+k2) * windowFilter[index]);
                                index++;
                            }

                        image1.At<byte>(y, x) = op(colors);
                    }
                }
            }

            return image1;
        }

        private delegate byte WindowFilterOperation(List<int> list);
        
        private byte addWindowFilter(List<int> list)
        {
            if (list.Sum() / list.Count > 255) return 255;
            else return (byte)(list.Sum() / list.Count);
        }
        private byte middleValue(List<int> list)
        {
            list.Sort();
            return (byte)list[list.Count / 2];
        }
        private void BlurFiltrationClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                List<int> filter = new List<int>() { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                ImageBox2.Source = ApplyWindowFilter(sourceMat, filter, MatType.CV_8UC3, middleValue).ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }
        private void SharpenFiltrationClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                List<int> filter = new List<int>() { -1, -1, -1, -1, 8, -1, -1, -1, -1 };
                ImageBox2.Source = ApplyWindowFilter(sourceMat, filter, MatType.CV_8UC1, addWindowFilter).ToWriteableBitmap(PixelFormats.Gray8);
            }
        }
        private void EmbossFilterClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                List<int> filter = new List<int>() { -4, -2, 0, -2, 1, 2, 0, 2, 4 };
                ImageBox2.Source = ApplyWindowFilter(sourceMat, filter, MatType.CV_8UC1, addWindowFilter).ToWriteableBitmap(PixelFormats.Gray8);
            }
        }
        private void EdgeFilterClick(object sender, RoutedEventArgs e)
        {
            if (sourceMat != null)
            {
                List<int> filter = new List<int>() { 0, 0, 0, -4, 4, 0, 0, 0, 0 };
                ImageBox2.Source = ApplyWindowFilter(sourceMat, filter, MatType.CV_8UC1, addWindowFilter).ToWriteableBitmap(PixelFormats.Gray8);
            }
        }
        private List<int> CreateCustomFilter()
        {
            List<int> values = new List<int>();

            foreach (var child in CustomFiltersGrid.Children)
            {
                if (child is TextBox textBox)
                {
                    if (int.TryParse(textBox.Text, out int value))
                    {
                        values.Add(value);
                    }
                }
            }
            return values;
        }
        private void CustomFilterClick(object sender, RoutedEventArgs e)
        {
            List<int> filt = CreateCustomFilter();
            if (sourceMat != null && filt.Count == 9 && grayRB.IsChecked == true)
            {
                ImageBox2.Source = ApplyWindowFilter(sourceMat, filt, MatType.CV_8UC1, addWindowFilter).ToWriteableBitmap(PixelFormats.Gray8);
            }
            if (sourceMat != null && filt.Count == 9 && rgbRB.IsChecked == true)
            {
                ImageBox2.Source = ApplyWindowFilter(sourceMat, filt, MatType.CV_8UC3, addWindowFilter).ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        #endregion

        #region HSVConvertion

        private Mat ChangeHueValue(Mat source)
        {

            double hueShift = HueSlider.Value;

            // Создаем копию HSV-изображения
            Mat adjustedHsvImage = source.Clone();

            // Изменяем канал Hue
            for (int y = 0; y < adjustedHsvImage.Rows; y++)
            {
                for (int x = 0; x < adjustedHsvImage.Cols; x++)
                {
                    Vec3b pixel = adjustedHsvImage.At<Vec3b>(y, x);
                    pixel.Item0 = (byte)((pixel.Item0 + hueShift) % 180); // Hue range is 0-179
                    adjustedHsvImage.Set(y, x, pixel);
                }
            }

            // Преобразуем обратно в BGR
            Mat resultImage = new Mat();
            Cv2.CvtColor(adjustedHsvImage, resultImage, ColorConversionCodes.HSV2BGR);
        }


        #endregion

    }
}
