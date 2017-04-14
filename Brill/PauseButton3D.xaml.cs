using System.Windows.Controls;
using System.Windows.Media;
using _3DTools;
using Terry;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows;

namespace Terry
{
    /// <summary>
    /// Interaction logic for PlayButton3D.xaml
    /// </summary>
    public partial class PauseButton3D : UserControl
    {
        public PauseButton3D()
        {
            InitializeComponent();
            Trackball trackball = new Trackball();
            trackball.EventSource = _viewport;
            trackball.SetAnimation(_storyboard, _quaternionAnimation, _modelRotation);
        }
    }
}
