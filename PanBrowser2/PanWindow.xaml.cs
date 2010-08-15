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
using System.ComponentModel;

namespace Terry
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PanBrowser : Window
    {
        protected string myImageName;
        protected List<ComboBox> myTransformCombos = new List<ComboBox>();
        protected string myImageXaml;

        //a delegate to hold the simple pan function
        protected FSharpFunc<Pan.Point, Pan.Color> myImageFn;
        protected int myRequestNumber = 0;
        static protected int minstep = 4;        //stored step size to start drawing with

        protected delegate void ShowStatus(string text);
        protected ShowStatus StatusEvent { get; set; }
        protected PanWrapper panWrapper = new PanWrapper();
        protected List<ObservableCollection<SliderAttribute>> myAttributes = new List<ObservableCollection<SliderAttribute>>();

        protected BrillWpf.BrillDockPanel brill = null;
        protected BrillRenderer brillRenderer;
        private GridLength defaultToolWidth;

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
            myImageXaml = XamlWriter.Save(imagecontrols);
            AddTransformPanel(0);

            //after cloning
            panWrapper.Images.Add(Brill3D);
            imageCombo1.ItemsSource = panWrapper.Images;
            RemoveTransform.Visibility = System.Windows.Visibility.Hidden; //not valid for image, only transforms

            bitmapSource = new WriteableBitmap((int)imagePanel.ActualWidth, (int)imagePanel.ActualHeight, 96.0, 96.0, PixelFormats.Rgb24, null);
            bitmap.Source = bitmapSource;

            LoadSettings();

            Console.WriteLine(string.Format("{0} images; {1} transforms", panWrapper.Images.Count, panWrapper.Transforms.Count));

            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(Tick);
            timer.Interval = TimeSpan.FromMilliseconds(100);

            if (screensaver)
                timer.Start();
        }

        private FrameworkElement AddTransformPanel(int insertAt)
        {
            FrameworkElement newpanel = CloneModel(myImageXaml);
            designerPanel.Children.Insert(insertAt+2, newpanel);
            (newpanel.FindName("AddTransform") as Button).Click += AddTransform_Click;
            (newpanel.FindName("RemoveTransform") as Button).Click += RemoveTransform_Click;
            ComboBox transformCombo = (ComboBox)newpanel.FindName("transformCombo1");
            transformCombo.SelectionChanged += TransformCombo1_SelectionChanged;
            transformCombo.ItemsSource = panWrapper.Transforms;
            myTransformCombos.Insert(insertAt, transformCombo);
            ObservableCollection<SliderAttribute> transformAttributes = new ObservableCollection<SliderAttribute>();
            ((ItemsControl)newpanel.FindName("transformSliderList")).ItemsSource = transformAttributes;
            myAttributes.Insert(insertAt+1, transformAttributes);
            return newpanel;
        }

        private void RemoveTransformPanel(int at)
        {
            designerPanel.Children.RemoveAt(at+2);
            myTransformCombos.RemoveAt(at);
            myAttributes.RemoveAt(at+1);
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

        private FrameworkElement CloneModel(string xaml)
        {
            xaml = xaml.Replace("Image", "Transform");
            xaml = xaml.Replace("image", "transform");
            xaml = xaml.Replace("GUID", Guid.NewGuid().ToString());
            StringReader stringReader = new StringReader(xaml);
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
                    Set3DImage();
                }
                else //2D image
                {
                    if(brill!=null)
                        brill.Visibility = Visibility.Hidden;

                    List<SliderAttribute> sliders = DoSliders(myImageName, myAttributes[0]);
                    myImageFn = (FSharpFunc<Pan.Point, Pan.Color>)panWrapper.GetImageFunction(myImageName, sliders);

                    System.Diagnostics.Debug.Assert(myTransformCombos.Count+1 == myAttributes.Count);
                    for (int i = 0; i < myTransformCombos.Count; ++i)
                    {
                        string name = (string)myTransformCombos[i].SelectedValue;
                        if (name != panWrapper.None && !string.IsNullOrEmpty(name))
                        {
                            sliders = DoSliders(name, myAttributes[i+1]);
                            myImageFn = panWrapper.GetTransformFunction(name, myImageFn, sliders);
                        }
                    }

                    Recalculate();
                }

            }
            catch(Exception)
            {
                Recalculate();
            }

        }

        private void Set3DImage()
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
            //myImageFn = (FSharpFunc<Pan.Point, Pan.Color>)panWrapper.GetImageFunction("Pan.wavyRings", new List<SliderAttribute>());
            myImageFn = null;
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
        }

        private void TransformCombo1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            minstep = 4;    //reset it
            SetImage();
        }

        private void LoadSettings()
        {
            RegistryKey k = Registry.CurrentUser.OpenSubKey("Software\\Terry\\PanBrowser", true);
            if (k != null)
            {
                imageCombo1.SelectedValue = k.GetValue("image", panWrapper.Images[0]);
                int i = 0, panels=0;
                while (k.GetValue("transform" + i.ToString(), null) != null)
                {
                    object transform = k.GetValue("transform" + i.ToString(), null);
                    if (transform != null && transform as string != panWrapper.None)
                    {
                        FrameworkElement panel = AddTransformPanel(panels);
                        (panel.FindName("transformCombo1") as ComboBox).SelectedValue = transform;
                        panels++;
                    }
                    i++;
                }
            }
            if (imageCombo1.SelectedIndex == -1)
            {
                imageCombo1.SelectedValue = "TerryImages.bumpSwirl";
                myTransformCombos[0].SelectedValue = "TerryImages.star2";
            }
            SetImage();
        }

        private void SaveSettings()
        {
            RegistryKey k = Registry.CurrentUser.CreateSubKey("Software\\Terry\\PanBrowser");
            k.SetValue("image", myImageName);
            int i;
            for (i = 0; i < myTransformCombos.Count; ++i)
                k.SetValue("transform" + i.ToString(),
                    myTransformCombos[i].SelectedValue == null ? panWrapper.None : (string)myTransformCombos[i].SelectedValue);
            if(k.GetValue("transform" + i.ToString(), null)!=null)
                k.DeleteValue("transform" + i.ToString());
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //SetImage();
            minstep = 4;    //reset it
            Recalculate();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((string)(sender as Slider).Tag == "Size" && brill != null)
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
        }

        private void PlayButton3D_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Play();
        }
        private void Play()
        {
            timer.Start();
            play.Visibility = Visibility.Collapsed;
            pause.Visibility = Visibility.Visible;
        }
        private void PauseButton3D_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            timer.Stop();
            play.Visibility = Visibility.Visible;
            pause.Visibility = Visibility.Collapsed;
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
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                _grid.ColumnDefinitions[0].Width = defaultToolWidth;
            }

        }

        private void Tick(object s, EventArgs a)
        {
            if (myImageName == null || myImageName==Brill3D)
                return;

            foreach (ObservableCollection<SliderAttribute> list in myAttributes)
                foreach (SliderAttribute slider in list)
                    slider.Bump();

            //List<SliderAttribute> sliders = DoSliders(myImageName, myAttributes[0]);
            //if (sliders.Count > 0)
            {
                //sliders[0].Bump();
                SetImage();
            }
        }

        private void AddTransform_Click(object sender, RoutedEventArgs e)
        {
            ComboBox combo = (ComboBox)((FrameworkElement)(((FrameworkElement)sender).Parent)).FindName("transformCombo1");
            int index = myTransformCombos.FindIndex( delegate(ComboBox b){ return b==combo; } );
            AddTransformPanel(index+1);
        }

        private void RemoveTransform_Click(object sender, RoutedEventArgs e)
        {
            ComboBox combo = (ComboBox)((FrameworkElement)(((FrameworkElement)sender).Parent)).FindName("transformCombo1");
            int index = myTransformCombos.FindIndex(delegate(ComboBox b) { return b == combo; });
            RemoveTransformPanel(index);
            SetImage();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private void Screensaver_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            defaultToolWidth = _grid.ColumnDefinitions[0].Width;
            _grid.ColumnDefinitions[0].Width = new GridLength(0);

            if (!timer.IsEnabled)
                Play();
        }

    }
}
