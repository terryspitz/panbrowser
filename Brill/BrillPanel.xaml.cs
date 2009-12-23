using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Terry;
using System.Windows.Media.Media3D;
using _3DTools;
using System.Windows.Markup;
using System.IO;
using System.Xml;
using System.Windows.Media.Animation;

namespace BrillWpf
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class BrillDockPanel : DockPanel
    {
        public BrillDockPanel()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
//            stars.ImageSource = new BitmapImage(new Uri("pack://application:,,,/hubble.jpg"));
            //stars2.ImageSource = new BitmapImage(new Uri("pack://application:,,,/hubble.jpg"));

            Trackball trackball = new Trackball();
            trackball.EventSource = _viewport;
            //((Transform3DGroup)_geomModel.Transform).Children.Add(trackball.Transform);
            trackball.SetAnimation(_storyboard, _qAnimation, _modelRotation);
            //viewport.Camera.Transform = trackball.Transform;
            //skybox.Camera.Transform = trackball.Transform;
          
            //CloneModelAndTrigger(trackball);


        }

        private Trackball CloneModelAndTrigger(Trackball trackball)
        {
            ModelVisual3D newModel = CloneModel();
            newModel.Transform = new TranslateTransform3D(2, 0, 0);
            //_viewport.Children.Add(newModel);

            string xaml = XamlWriter.Save(_eventTrigger);
            StringReader stringReader = new StringReader(xaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            EventTrigger newTrigger = (EventTrigger)XamlReader.Load(xmlReader);
            BeginStoryboard t = (BeginStoryboard)newTrigger.Actions[0];
            //t.Storyboard.
            QuaternionRotation3D qr = (((newModel.Content.Transform as Transform3DGroup).Children[1] as RotateTransform3D).Rotation as QuaternionRotation3D);

            (t.Storyboard.Children[0] as QuaternionAnimation).Name += "1";
            Storyboard.SetTargetName(t, (t.Storyboard.Children[0] as QuaternionAnimation).Name);
            _viewport.Triggers.Add(newTrigger);

            trackball = new Trackball();
            trackball.EventSource = _viewport;
            //((Transform3DGroup)_geomModel.Transform).Children.Add(trackball.Transform);
            trackball.SetAnimation(t.Storyboard, t.Storyboard.Children[0] as QuaternionAnimation,
                qr);
            return trackball;
        }

        private ModelVisual3D CloneModel()
        {
            string gridXaml = XamlWriter.Save(_model);
            StringReader stringReader = new StringReader(gridXaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            ModelVisual3D newModel = (ModelVisual3D)XamlReader.Load(xmlReader);
            return newModel;
        }

        public void Fill(BrillRenderer renderer)
        {
            double max;
            _geomModel.Geometry = renderer.Fill(0, out max);
            _modelScale.ScaleX = _modelScale.ScaleY = _modelScale.ScaleZ = 1 / max;
        }

        /*
         * private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N)
            {
                renderer.Init();
            }
            else if (e.Key == Key.S)
            {
                //_storyboard
                _storyboard.Begin();
                return;
            }
            else if (e.Key == Key.Escape)
                Application.Current.Shutdown();

            renderer.OnNextShape();
            Fill();
        }*/

        public void SetGeometry(MeshGeometry3D mesh)
        {
            _geomModel.Geometry = mesh;
        }

        public void SetSize(double size)
        {
            _modelScale.ScaleX = _modelScale.ScaleY = _modelScale.ScaleZ = size;
        }
    }
}
