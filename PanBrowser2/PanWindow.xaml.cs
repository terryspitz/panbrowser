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
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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
        protected FSharpFunc<Pan.Point, Pan.Color> myImageFn;
        protected int myRequestNumber = 0;
        static protected int minstep = 4;        //stored step size to start drawing with

        protected delegate void ShowStatus(string text);
        protected ShowStatus StatusEvent { get; set; }
        protected PanWrapper panWrapper = new PanWrapper();
        protected List<ObservableCollection<SliderAttribute>> myAttributes = new List<ObservableCollection<SliderAttribute>>();
        //protected SliderDouble sizeSlider = new SliderDouble("Size", -10, 10, 0);
        protected Slider[] transformStandardSliders = new Slider[4];

        protected BrillWpf.BrillDockPanel brill = null;
        protected BrillRenderer brillRenderer;

        protected WriteableBitmap bitmapSource;
        protected TimingStore timings = new TimingStore();
        
        public bool screensaver = false;
        protected DispatcherTimer timer;

        protected readonly string Brill3D = "Brill3D";

        protected class Request
        {
            public int requestNo;
            public PanBrowser _this;
            public int width, height, scale;
            public delegate bool IsCancelled(int reqno);
            public IsCancelled cancelCallback;
        }

        protected class TimingStore
        {
            static int history = 10;
            protected int[] array = new int[history];
            protected int next = 0;
            public void add(int i)
            {
                lock (this)
                {
                    array[next] = i;
                    next = (next+ 1) % history;
                }
            }
            public int getLast()
            {
                lock (this)
                    return array[next];
            }
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

            //clone the basic image combo and controls to create the transforms combo/controls
            ObservableCollection<SliderAttribute> transformAttributes = new ObservableCollection<SliderAttribute>();
            FrameworkElement transform1 = CloneModel(imagecontrols);
            designerPanel.Children.Add(transform1);
            transformCombo1 = (ComboBox)transform1.FindName("transformCombo1");
            transformCombo1.SelectionChanged += TransformCombo1_SelectionChanged;
            transformCombo1.ItemsSource = panWrapper.Transforms;
            ((ListView)transform1.FindName("transformSliderList")).ItemsSource = transformAttributes;
            myAttributes.Add(transformAttributes);

            transformStandardSliders[0] = (Slider)transform1.FindName("transformSizeSlider");
            transformStandardSliders[1] = (Slider)transform1.FindName("transformRotateSlider");
            transformStandardSliders[2] = (Slider)transform1.FindName("transformXSlider");
            transformStandardSliders[3] = (Slider)transform1.FindName("transformYSlider");
            foreach (Slider s in transformStandardSliders)
                s.ValueChanged += Slider_ValueChanged;

            //after cloning
            panWrapper.Images.Add(Brill3D);
            imageCombo1.ItemsSource = panWrapper.Images;

            bitmapSource = new WriteableBitmap((int)imagePanel.ActualWidth, (int)imagePanel.ActualHeight, 96.0, 96.0, PixelFormats.Rgb24, null);
            bitmap.Source = bitmapSource;

            RegistryKey k = Registry.CurrentUser.OpenSubKey("Software\\Terry\\PanBrowser", true);
            if (k != null)
            {
                imageCombo1.SelectedValue = k.GetValue("image", panWrapper.Images[0]);
                transformCombo1.SelectedValue = k.GetValue("transform", panWrapper.Transforms[0]);
            }
            if(imageCombo1.SelectedIndex==-1)
            {
                imageCombo1.SelectedValue = "TerryImages.bumpSwirl";
                transformCombo1.SelectedValue = "TerryImages.star2";
            }

            Console.WriteLine(string.Format("{0} images; {1} transforms", panWrapper.Images.Count, panWrapper.Transforms.Count));

            if(screensaver)
            {
                timer = new DispatcherTimer();
                timer.Tick += new EventHandler(Tick);
                timer.Interval = TimeSpan.FromMilliseconds(100);
                timer.Start();
            }
        }

        
        public void Recalculate()
        {
            Interlocked.Increment(ref myRequestNumber);
            Request r = new Request();
            r._this = this;
            r.requestNo = myRequestNumber;
            r.width = (int)imagePanel.ActualWidth;
            r.height = (int)imagePanel.ActualHeight;
            r.scale = 0;// (int)(imageSizeSlider.Value * 2) - 10;
            r.cancelCallback = IsCancelled;
            //Draw(minstep-1, r.number, r.width, r.height, r.scale);  //do a quick one in this thread before returning
#if !DOIT
            ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), r);
#else
            ThreadProc(r);
#endif
        }

        bool IsCancelled(int r)
        {
            return r != myRequestNumber;
        }

        static void ThreadProc(Object stateInfo)
        {
            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));
            Request r = (Request)stateInfo;
            if (r.cancelCallback(r.requestNo))
                return;
            //iterate through successively higher resolutions, -1 being four bitmap pixes per screen pixel for antialiasing
            //remember the step to start with that's fast enough for recalculation e.g. resizing operations
            const int maxstep = 0;
            for (int step = minstep; step >= maxstep; step--)
                //int step = 0;
            {
                int elapsed = r._this.Draw(step, r.requestNo, r.width, r.height, r.scale);
                if (r.requestNo != r._this.myRequestNumber)
                    break;
                if (elapsed < 50 && minstep>step)  //assume 200ms is quick enough not to notice
                    minstep = step;
            }
        }

        protected int Draw(int step, int requestNo, int Width, int Height, int scale)
        {
            try
            {
                Tuple<byte[], int, int, PixelFormat, int, int> result = null;
                if (myImageFn != null)
                {
                    result = DrawImage.drawImageCol(myImageFn, Width, Height, step, scale);
                }
                
                if (IsCancelled(requestNo))   //don't update if new request has been received
                    return 0;

                if (result != null && result.Item1 != null && result.Item1.Length>0)
                {
                    int pixels;
                    lock (result.Item1)
                    {
                        timings.add(result.Item6);
                        if (bitmap.Dispatcher.Thread == Thread.CurrentThread)
                        {
#if !NEW
                            bitmap.Source = BitmapSource.Create(result.Item2, result.Item3, 96.0, 96.0, result.Item4, null, result.Item1, result.Item5);
#else
                            bitmapSource.WritePixels(
                                new Int32Rect(0, 0, result.Item2, result.Item3), result.Item1, result.Item5, 0
                                );
#endif
                        }
                        else
                        {
                            // dispatch to Main thread
                            bitmap.Dispatcher.Invoke(
                                (Action<Tuple<byte[], int, int, PixelFormat, int, int>>)((Tuple<byte[], int, int, PixelFormat, int, int> request) =>
                                    {
#if !NEW
                                        bitmap.Source = BitmapSource.Create(result.Item2, result.Item3, 96.0, 96.0, result.Item4, null, result.Item1, result.Item5);
#else
                                        bitmapSource.WritePixels(
                                            new Int32Rect(0, 0, result.Item2, result.Item3), result.Item1, result.Item5, 0
                                            );
#endif
                                    }
                                    ), result);
                        }
                        pixels = result.Item2* result.Item3;
                    }
                    int elapsed = Math.Max(1, result.Item6);
                    string status= String.Format("{5}: Drew {0} pixels with step \t{4} in \t{1} ms\t{2} pixels/ms\t{3} frames/s",
                        pixels, elapsed.ToString("n0")
                        , (pixels / elapsed).ToString("n0")
                        , (1000d / elapsed).ToString("n2")
                        ,step
                        ,"" //,DateTime.Now.ToString("HH:mm:ss.fff")
                        );
                    Console.WriteLine(status);
                    //StatusEvent(status);
                    return elapsed;
                }
                else
                {
                    //if(this.InvokeRequired)
                    //StatusEvent("null image");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                if (StatusEvent != null)
                    StatusEvent(ex.ToString());
                return 0;
            }

        }

        private FrameworkElement CloneModel(FrameworkElement input)
        {
            string gridXaml = XamlWriter.Save(input);
            //gridXaml.Replace("<Label>Image:</Label>", "<Label>Transform:</Label>");
            gridXaml = gridXaml.Replace("Image", "Transform");
            gridXaml = gridXaml.Replace("image", "transform");
            gridXaml = gridXaml.Replace("GUID", Guid.NewGuid().ToString());
            StringReader stringReader = new StringReader(gridXaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            FrameworkElement newModel = (FrameworkElement)XamlReader.Load(xmlReader);
            return newModel;
        }

        public void SetImage()
        {
            try
            {
                if (myImageName == null)
                    return;
                else if (myImageName == Brill3D || myImageName.StartsWith("Pan3D"))
                {
                    if (brill == null)
                    {
                        brill = new BrillWpf.BrillDockPanel();
                        _grid.Children.Add(brill);
                        Grid.SetColumn(brill, 1);
                    }
                    brill.Visibility = Visibility.Visible;

                    if (myImageName.StartsWith("Pan3D"))
                    {
                        List<SliderAttribute> sliders = DoSliders(myImageName, myAttributes[0]);
                        Converter<Vector3D, double> panFn = (FSharpFunc<Vector3D, double>)panWrapper.GetImageFunction(myImageName, sliders);
                        Sampler sampler = new Sampler(panFn);
                        sampler.Resolution = 10;
                        sampler.OnTimerTick(10);
                        Flags flags = new Flags();
                        flags.ShowFaces = true;
                        sampler.Calculate(flags);
                        CubeRenderer renderer = new CubeRenderer();
                        RenderData r = renderer.Render(sampler, flags);
                        brill.SetGeometry(r.Mesh);

                    }
                    else if (myImageName == Brill3D)
                    {
                        if (brillRenderer == null)
                        {
                            brillRenderer = new BrillRenderer();
                            //brillRenderer.Storyboard = _storyboard;
                            brillRenderer.Init();
                        }
                        brill.Fill(brillRenderer);
                    }
                    myImageFn = (FSharpFunc<Pan.Point, Pan.Color>)panWrapper.GetImageFunction("Pan.wavyRings", new List<SliderAttribute>());
                }
                else
                {
                    if(brill!=null)
                        brill.Visibility = Visibility.Hidden;

                    List<SliderAttribute> sliders = DoSliders(myImageName, myAttributes[0]);
                    myImageFn = (FSharpFunc<Pan.Point, Pan.Color>)panWrapper.GetImageFunction(myImageName, sliders);
                    myImageFn = (FSharpFunc<Pan.Point, Pan.Color>)panWrapper.StandardTransforms(
                        myImageFn, imageSizeSlider.Value, imageRotateSlider.Value*36, imageXSlider.Value, imageYSlider.Value);

                    if (myTransformName != panWrapper.None && !string.IsNullOrEmpty(myTransformName))
                    {
                        sliders = DoSliders(myTransformName, myAttributes[1]);
                        myImageFn = panWrapper.GetTransformFunction(myTransformName, myImageFn, sliders);
                        myImageFn = (FSharpFunc<Pan.Point, Pan.Color>)panWrapper.StandardTransforms(
                            myImageFn, transformStandardSliders[0].Value, transformStandardSliders[1].Value * 36,
                            -transformStandardSliders[2].Value, -transformStandardSliders[3].Value);
                    }

                    Recalculate();
                }

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

            var ret = panWrapper.GetSliders(functionName);

            List<string> toDelete = new List<string>();
            foreach (SliderAttribute a in attributes)
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

        private void imageCombo1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            myImageName = e.AddedItems[0] as string;
            minstep = 4;    //reset it
            SetImage();
            RegistryKey k = Registry.CurrentUser.CreateSubKey("Software\\Terry\\PanBrowser");
            k.SetValue("image", myImageName);
        }

        private void TransformCombo1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            myTransformName = e.AddedItems[0] as string;
            minstep = 4;    //reset it
            SetImage();
            RegistryKey k = Registry.CurrentUser.CreateSubKey("Software\\Terry\\PanBrowser");
            k.SetValue("transform", myTransformName);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //SetImage();
            minstep = 4;    //reset it
            Recalculate();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((string)((Slider)sender).Tag == "Size" && brill != null)
            {
                brill.SetSize(e.NewValue);
            }
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

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            //if (sizeSliderControl==null)
            //    sizeSliderControl = VisualTreeHelperExtensions.FindDescendent<Slider>(imageSliderList);
            imageSizeSlider.Value *= Math.Pow( 1.1, e.Delta / 100);

        }

        private void PlayButton3D_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object tag = ((UserControl)e.Source).Tag;
            if (tag is Storyboard)
            {
                Storyboard story = tag as Storyboard;
                if(story.GetCurrentState()==ClockState.Active)
                    story.Stop();
                else
                    story.Begin();
            }
            else
            {
                Slider s = ((Panel)((UserControl)e.Source).Parent).FindName("Slider") as Slider;
                DoubleAnimation myDoubleAnimation = new DoubleAnimation();
                myDoubleAnimation.From = s.Minimum;
                myDoubleAnimation.To = s.Maximum;
                myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(20));
                myDoubleAnimation.AccelerationRatio = 0.2;
                myDoubleAnimation.DecelerationRatio = 0.2;
                myDoubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
                myDoubleAnimation.AutoReverse = true;
                Storyboard.SetTarget(myDoubleAnimation, s);
                Storyboard.SetTargetProperty(myDoubleAnimation, new PropertyPath(Slider.ValueProperty));

                Storyboard myStoryboard = new Storyboard();
                myStoryboard.Children.Add(myDoubleAnimation);
                myStoryboard.Begin();
                ((UserControl)e.Source).Tag = myStoryboard;
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (screensaver)
            {
                //Application.Current.Shutdown();
            }
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (screensaver)
            {
                if (e.Key == System.Windows.Input.Key.Down)
                    imageCombo1.SelectedIndex = (imageCombo1.SelectedIndex + 1)%imageCombo1.Items.Count;
                else if (e.Key == System.Windows.Input.Key.Up)
                    imageCombo1.SelectedIndex = (imageCombo1.SelectedIndex + imageCombo1.Items.Count -1) % imageCombo1.Items.Count;
                else
                    Application.Current.Shutdown();
            }
        }

        private void Tick(object s, EventArgs a)
        {
            if (myImageName == null || myImageName==Brill3D)
                return;

            List<SliderAttribute> sliders = DoSliders(myImageName, myAttributes[0]);
            if (sliders.Count > 0)
            {
                sliders[0].Bump();
                SetImage();
            }
        }
    }
}
