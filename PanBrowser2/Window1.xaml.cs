using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.FSharp.Core;
using System.Collections.Generic;

namespace Terry
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PanBrowser : Window
    {
        protected string myImageName;
        protected string myTransformName;

        //a delegate to hold the simple pan function
        public FastFunc<Pan.Point, Pan.Color> myImageFn;
        public int myRequestNumber = 0;

        public delegate void ShowStatus(string text);
        public ShowStatus StatusEvent { get; set; }

        private class Request
        {
            public int number;
            public PanBrowser _this;
            public int width, height;
        }

        public PanBrowser()
        {
            InitializeComponent();
            sliderList.ItemsSource = Sliders.Variables;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Type transformType1 = typeof(FastFunc<,>);  //can't do typeof(FastFunc<Pan.Point,>)
            //Type transformType1 = typeof(Pan).GetMethod("tile").;

            ///basic functions taking a Point 
            MethodInfo[] methods = typeof(Pan).GetMethods();
            foreach (MethodInfo method in methods)
            {
                if (method.GetParameters().Length > 0 && method.GetParameters()[0].ParameterType == typeof(Pan.Point))
                {
                    if (method.ReturnType == typeof(Pan.Color))
                        imageCombo1.Items.Add(method.Name);
                    else if (method.ReturnType == typeof(bool))
                        imageCombo1.Items.Add(method.Name);
                    else if (method.ReturnType == typeof(Double))
                        imageCombo1.Items.Add(method.Name);
                    else if (method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == typeof(Pan.Point)
                    && method.ReturnType == typeof(Pan.Point))    //transform
                        transformCombo1.Items.Add(method.Name);
                }
                else if (method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType.IsGenericType
                    && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == transformType1
                    && method.ReturnType.IsGenericType
                    && method.ReturnType.GetGenericTypeDefinition() == transformType1
                    )
                {
                    transformCombo1.Items.Add(method.Name);
                }
            }

            //higher level f# functions use as type Func1, i.e. take a function and return a function
            Type imageType1 = typeof(FastFunc<Pan.Point, Pan.Color>);
            Type imageType2 = typeof(FastFunc<Pan.Point, bool>);

            PropertyInfo[] fields = typeof(Pan).GetProperties();
            foreach (PropertyInfo field in fields)
            {
                if (field.PropertyType == imageType1 || field.PropertyType.IsSubclassOf(imageType1)
                    || field.PropertyType == imageType2 || field.PropertyType.IsSubclassOf(imageType2))
                    imageCombo1.Items.Add(field.Name);
                //if (field.PropertyType == transformType1 || field.PropertyType.IsSubclassOf(transformType1)
                //    || field.PropertyType == transformType2 || field.PropertyType.IsSubclassOf(transformType2))
                //    transformCombo1.Items.Add(field.Name);
            }
            imageCombo1.SelectedIndex = 0;
        }

        
        public void Recalculate()
        {
            Interlocked.Increment(ref myRequestNumber);
            Request r = new Request();
            r._this = this;
            r.number = myRequestNumber;
            r.width = (int)imagePanel.ActualWidth;
            r.height = (int)imagePanel.ActualHeight;
#if DOIT
            ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), r);
#else
            ThreadProc(r);
#endif
        }
        static void ThreadProc(Object stateInfo)
        {
            Request r = (Request)stateInfo;
            for (int step = 3; step >= -1; step--)
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
                    result = DrawImage.drawImageCol(myImageFn, Width, Height, step);
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

        private void imageCombo1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            myImageName = e.AddedItems[0] as string;
            SetImage();
        }

        private void transform1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            myTransformName = e.AddedItems[0] as string;
            SetImage();
        }

        public void SetImage()
        {
            myImageFn = null;

            //Simple Pan functions are methods, higher level functions are (property) members returning a Func1 function
            MethodInfo fn = typeof(Pan).GetMethod(myImageName);
            PropertyInfo prop = typeof(Pan).GetProperty(myImageName);
            if (fn != null)
            {
                if (fn.ReturnType == typeof(bool))
                    myImageFn = DrawImage.boolToColImage((Converter<Pan.Point, bool>)Delegate.CreateDelegate(typeof(Converter<Pan.Point, bool>), typeof(Pan), myImageName));
                else if (fn.ReturnType == typeof(double))
                    myImageFn = DrawImage.doubleToColImage((Converter<Pan.Point, double>)Delegate.CreateDelegate(typeof(Converter<Pan.Point, double>), typeof(Pan), myImageName));
                else if (fn.ReturnType == typeof(Pan.Color))
                    myImageFn = (Converter<Pan.Point, Pan.Color>)Delegate.CreateDelegate(typeof(Converter<Pan.Point, Pan.Color>), typeof(Pan), myImageName);
            }
            else if (prop != null)
            {
                //if (fld.GetType().IsSubclassOf(typeof(Microsoft.FSharp.FastFunc<Pan.Point, Pan.Color>)))
                myImageFn = prop.GetGetMethod().Invoke(null, null) as FastFunc<Pan.Point, Pan.Color>;
                //if (fld.GetType().IsSubclassOf(typeof(Microsoft.FSharp.FastFunc<Pan.Point, bool>)))
                if(myImageFn==null)
                    myImageFn = Pan.byImage(prop.GetGetMethod().Invoke(null, null) as FastFunc<Pan.Point, bool>);
            }

            if (!string.IsNullOrEmpty(myTransformName))
            {
                MethodInfo transformMethodInfo = typeof(Pan).GetMethod(myTransformName);
                if (transformMethodInfo != null)
                {
                    if (transformMethodInfo.ReturnType == typeof(Pan.Point))    //transform
                    {
                        myImageFn = transformImage(
                            (Converter<Pan.Point, Pan.Point>)Delegate.CreateDelegate(typeof(Converter<Pan.Point, Pan.Point>), typeof(Pan), myTransformName),
                            myImageFn);
                    }
                    else
                    {
                        try
                        {
                            MethodInfo genericMethodInfo = transformMethodInfo.MakeGenericMethod(new Type[] { typeof(Pan.Color) });
                            object newImageFn = genericMethodInfo.Invoke(null, new object[] { myImageFn });
                            myImageFn = newImageFn as FastFunc<Pan.Point, Pan.Color>;
                        }
                        catch(Exception)
                        {
                        }
                    }
                }
            }

            //StatusEvent("");
            //Sliders.HideAll();            //reset sliders to minimum
            //Invalidate(true);
            Recalculate();

        }

        protected static FastFunc<Pan.Point, Pan.Color> transformImage(Converter<Pan.Point, Pan.Point> transform, FastFunc<Pan.Point, Pan.Color> image)
        {
            return (Converter<Pan.Point, Pan.Color>)((Pan.Point p) => { return image.Invoke(transform.Invoke(p)); });
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Recalculate();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Recalculate();
        }



    }
}
