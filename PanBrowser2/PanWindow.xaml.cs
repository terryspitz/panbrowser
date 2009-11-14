using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.FSharp.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using System.Windows.Markup;
using System.Xml;

namespace Terry
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PanBrowser : Window
    {
        protected string myImageName;
        protected string myTransformName;
        protected ComboBox transformCombo1;

        //a delegate to hold the simple pan function
        public FastFunc<Pan.Point, Pan.Color> myImageFn;
        public int myRequestNumber = 0;

        public delegate void ShowStatus(string text);
        public ShowStatus StatusEvent { get; set; }
        private PanWrapper PanWrapper = new PanWrapper();
        public List<ObservableCollection<SliderAttribute>> myAttributes = new List<ObservableCollection<SliderAttribute>>();
        protected SliderDouble sizeSlider = new SliderDouble("Size", -10, 10, 0);

        private class Request
        {
            public int number;
            public PanBrowser _this;
            public int width, height;
        }

        public PanBrowser()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ObservableCollection<SliderAttribute> imageAttributes = new ObservableCollection<SliderAttribute>();
            imageSliderList.ItemsSource = imageAttributes;
            myAttributes.Add(imageAttributes);

            ObservableCollection<SliderAttribute> transformAttributes = new ObservableCollection<SliderAttribute>();
            FrameworkElement transform1 = CloneModel(imagecontrols);
            designerPanel.Children.Add(transform1);
            transformCombo1 = (ComboBox)transform1.FindName("transformCombo1");
            transformCombo1.SelectionChanged += TransformCombo1_SelectionChanged;
            ((ListView)transform1.FindName("transformSliderList")).ItemsSource = transformAttributes;
            myAttributes.Add(transformAttributes);

            //make sure this is after we've clones the imagecontrols (above)
            imageAttributes.Add(sizeSlider);
            foreach (string image in PanWrapper.Images)
                imageCombo1.Items.Add(image);
            
            foreach (string transform in PanWrapper.Transforms)
                transformCombo1.Items.Add(transform);

            RegistryKey k = Registry.CurrentUser.OpenSubKey("Software\\Terry\\PanBrowser", true);
            if (k != null)
            {
                imageCombo1.SelectedValue = k.GetValue("image", PanWrapper.Images[0]);
                transformCombo1.SelectedValue = k.GetValue("transform", PanWrapper.Transforms[0]);
            }
            else
            {
                imageCombo1.SelectedIndex = 0;
                transformCombo1.SelectedIndex = 0;
            }

            Console.WriteLine(string.Format("{0} images; {1} transforms", PanWrapper.Images.Count, PanWrapper.Transforms.Count));
        }

        
        public void Recalculate()
        {
            Interlocked.Increment(ref myRequestNumber);
            Request r = new Request();
            r._this = this;
            r.number = myRequestNumber;
            r.width = (int)imagePanel.ActualWidth;
            r.height = (int)imagePanel.ActualHeight;
#if !SINGLETHREAD
            ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), r);
#else
            ThreadProc(r);
#endif
        }
        static void ThreadProc(Object stateInfo)
        {
            Request r = (Request)stateInfo;
            for (int step = 4; step >= -1; step--)
            {
                r._this.Draw(step, r.number, r.width, r.height);
                if (r.number != r._this.myRequestNumber)
                    break;
            }
        }

        protected void Draw(int step, int requestNo, int Width, int Height)
        {
            try
            {
                Tuple<byte[], int, int, PixelFormat, int, int> result = null;
                if (myImageFn != null)
                {
                    result = DrawImage.drawImageCol(myImageFn, Width, Height, step, sizeSlider.Val);
                }
                
                if (requestNo != myRequestNumber)   //don't update if new request has been received
                    return;

                if (result != null && result.Item1 != null && result.Item1.Length>0)
                {
                    int pixels;
                    lock (result.Item1)
                    {
                        bitmap.Dispatcher.Invoke(
                            (Action<Tuple<byte[], int, int, PixelFormat, int, int>>)((Tuple<byte[], int, int, PixelFormat, int, int> request) => 
                                { 
                                    bitmap.Source = BitmapSource.Create(result.Item2, result.Item3, 96.0, 96.0, result.Item4, null, result.Item1, result.Item5); 
                                }
                                ), result);
                        pixels = result.Item2* result.Item3;
                    }
                    long elapsed = Math.Max(1, result.Item6);
                    string status= String.Format("Drew {0} pixels in {1} ms\n{2} pixels/ms, {3} frames/s",
                        pixels, elapsed.ToString("n0"),
                        (pixels / elapsed).ToString("n0"), (1000d / elapsed).ToString("n2"));
                    Console.WriteLine(status);
                    //StatusEvent(status);
                }
                else
                {
                    //if(this.InvokeRequired)
                    //StatusEvent("null image");
                }
            }
            catch (Exception ex)
            {
                if (StatusEvent != null)
                    StatusEvent(ex.ToString());
            }

        }

        private FrameworkElement CloneModel(FrameworkElement input)
        {
            string gridXaml = XamlWriter.Save(input);
            //gridXaml.Replace("<Label>Image:</Label>", "<Label>Transform:</Label>");
            gridXaml = gridXaml.Replace("Image", "Transform");
            gridXaml = gridXaml.Replace("image", "transform");
            StringReader stringReader = new StringReader(gridXaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            FrameworkElement newModel = (FrameworkElement)XamlReader.Load(xmlReader);
            return newModel;
        }


        private void imageCombo1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            myImageName = e.AddedItems[0] as string;
            SetImage();
            RegistryKey k= Registry.CurrentUser.CreateSubKey("Software\\Terry\\PanBrowser");
            k.SetValue("image", myImageName);
        }

        private void TransformCombo1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            myTransformName = e.AddedItems[0] as string;
            SetImage();
            RegistryKey k = Registry.CurrentUser.CreateSubKey("Software\\Terry\\PanBrowser");
            k.SetValue("transform", myTransformName);
        }

        public void SetImage()
        {
            try
            {
                if (myImageName == null)
                    return;

                List<SliderAttribute> sliders = DoSliders(myImageName, myAttributes[0]);
                myImageFn = PanWrapper.GetImageFunction(myImageName, sliders);

                if (myTransformName != PanWrapper.None)
                {
                    sliders = DoSliders(myTransformName, myAttributes[1]);
                    myImageFn = PanWrapper.GetTransformFunction(myTransformName, myImageFn, sliders);
                }

                Recalculate();
            }
            catch(Exception)
            {}

        }

        /// <summary>
        /// Get the relevant parameters from the Pan function and update the sliders on the GUI
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        private List<SliderAttribute> DoSliders(string functionName, ObservableCollection<SliderAttribute> attributes)
        {
            List<SliderAttribute> sliders = new List<SliderAttribute>();

            var ret = PanWrapper.GetSliders(functionName);

            List<string> toDelete = new List<string>();
            foreach (SliderAttribute a in attributes)
                if (a != sizeSlider)
                    toDelete.Add(a.Name);

            foreach (SliderAttribute a in ret)
            {
                if (!attributes.Contains(a))
                    attributes.Add(a);

                sliders.Add(attributes[attributes.IndexOf(a)]);

                //if(toDelete.Contains(a.Name))
                toDelete.Remove(a.Name);
            }
            foreach (string name in toDelete)
                attributes.Remove(new SliderAttribute(name));
            return sliders;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetImage();
            Recalculate();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetImage();
            Recalculate();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetImage();
            Recalculate();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            JpegBitmapEncoder jpg = new JpegBitmapEncoder();
            jpg.Frames.Add(BitmapFrame.Create((BitmapSource)bitmap.Source));
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = imageCombo1.SelectedValue.ToString(); // Default file name
            dlg.DefaultExt = ".jpg"; // Default file extension
            dlg.Filter = "Images (.jpg)|*.jpg"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                using(FileStream fs = File.Open(dlg.FileName, FileMode.OpenOrCreate))
                    jpg.Save(fs);
            }
        }



    }
}
