//---------------------------------------------------------------------------
//
// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Limited Permissive License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/limitedpermissivelicense.mspx
// All other rights reserved.
//
// This file is part of the 3D Tools for Windows Presentation Foundation
// project.  For more information, see:
// 
// http://CodePlex.com/Wiki/View.aspx?ProjectName=3DTools
//
// The following article discusses the mechanics behind this
// trackball implementation: http://viewport3d.com/trackball.htm
//
// Reading the article is not required to use this sample code,
// but skimming it might be useful.
//
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace _3DTools
{
    /// <summary>
    ///     Trackball is a utility class which observes the mouse events
    ///     on a specified FrameworkElement and produces a Transform3D
    ///     with the resultant rotation and scale.
    /// 
    ///     Example Usage:
    /// 
    ///         Trackball trackball = new Trackball();
    ///         trackball.EventSource = myElement;
    ///         myViewport3D.Camera.Transform = trackball.Transform;
    /// 
    ///     Because Viewport3Ds only raise events when the mouse is over the
    ///     rendered 3D geometry (as opposed to not when the mouse is within
    ///     the layout bounds) you usually want to use another element as 
    ///     your EventSource.  For example, a transparent border placed on
    ///     top of your Viewport3D works well:
    ///     
    ///         <Grid>
    ///           <ColumnDefinition />
    ///           <RowDefinition />
    ///           <Viewport3D Name="myViewport" ClipToBounds="True" Grid.Row="0" Grid.Column="0" />
    ///           <Border Name="myElement" Background="Transparent" Grid.Row="0" Grid.Column="0" />
    ///         </Grid>
    ///     
    ///     NOTE: The Transform property may be shared by multiple Cameras
    ///           if you want to have auxilary views following the trackball.
    /// 
    ///           It can also be useful to share the Transform property with
    ///           models in the scene that you want to move with the camera.
    ///           (For example, the Trackport3D's headlight is implemented
    ///           this way.)
    /// 
    ///           You may also use a Transform3DGroup to combine the
    ///           Transform property with additional Transforms.
    /// </summary> 
    public class Trackball
    {
        private FrameworkElement _eventSource;
        private Point _previousPosition2D;
        private Vector3D _previousPosition3D = new Vector3D(0, 0, 1);
        private Vector3D _zAxis = new Vector3D(0, 0, 1);

        private Transform3DGroup _transform;
        private ScaleTransform3D _scale = new ScaleTransform3D();
        private QuaternionAnimation _speed;// = new QuaternionAnimation(new Quaternion(new Vector3D(1,1,1), 30), new Duration(new TimeSpan(0,0,1)));
        private Storyboard _storyboard;
        private QuaternionRotation3D _storyboardRotation;

        public Trackball()
        {
            //_transform = new Transform3DGroup();
            //_transform.Children.Add(_scale);
            //_transform.Children.Add(new RotateTransform3D(_rotation));
            //_speed.IsCumulative = true;
        }


        /// <summary>
        ///     A transform to move the camera or scene to the trackball's
        ///     current orientation and scale.
        /// </summary>
        public Transform3D Transform
        {
            get { return _transform; }
        }

        #region Event Handling

        /// <summary>
        ///     The FrameworkElement we listen to for mouse events.
        /// </summary>
        public FrameworkElement EventSource
        {
            get { return _eventSource; }
            
            set
            {
                if (_eventSource != null)
                {
                    _eventSource.MouseDown -= this.OnMouseDown;
                    _eventSource.MouseUp -= this.OnMouseUp;
                    _eventSource.MouseMove -= this.OnMouseMove;
                }

                _eventSource = value;

                _eventSource.MouseDown += this.OnMouseDown;
                _eventSource.MouseUp += this.OnMouseUp;
                _eventSource.MouseMove += this.OnMouseMove;
                _eventSource.MouseEnter += this.OnMouseEnter;
                _eventSource.MouseLeave += this.OnMouseUp;
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _storyboard.Stop();
            Grab(e);
            Mouse.Capture(EventSource, CaptureMode.Element);
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _storyboard.Stop();
            Grab(e);
        }

        private void Grab(MouseEventArgs e)
        {
            _previousPosition2D = e.GetPosition(EventSource);
            _previousPosition3D = ProjectToTrackball(
                EventSource.ActualWidth,
                EventSource.ActualHeight,
                _previousPosition2D);
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            Mouse.Capture(EventSource, CaptureMode.None);
            _storyboard.Begin();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(EventSource);

            // Prefer tracking to zooming if both buttons are pressed.
            if (e.LeftButton == MouseButtonState.Pressed || Mouse.DirectlyOver == EventSource)
            {
                Track(currentPosition);
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                Zoom(currentPosition);
            }

            _previousPosition2D = currentPosition;
        }

        #endregion Event Handling

        private void Track(Point currentPosition)
        {
            Vector3D currentPosition3D = ProjectToTrackball(
                EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);

            //Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
            //double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);

            Vector3D diff = currentPosition3D - _previousPosition3D;
            Vector3D axis = new Vector3D(diff.Y, diff.X, 0);
            double angle = -Math.Sqrt(Math.Pow(diff.X, 2) + Math.Pow(diff.Y, 2))*10;

            //Debug.WriteLine(axis.ToString() + "\t\t" + angle);
            if (angle == 0) angle = 0.01;
            if (axis.Length == 0) axis.X = 0.01;
            Quaternion delta = new Quaternion(axis, -angle);
            //Debug.WriteLine(delta);

            _previousPosition3D = currentPosition3D;
            _speed.To = new Quaternion(axis, -angle * 100) * _storyboardRotation.Quaternion;
            //_storyboard.SkipToFill();
            _storyboardRotation.Quaternion = delta * _storyboardRotation.Quaternion;

            //if (Vector3D.CrossProduct(_zAxis, axis).LengthSquared> 0)
            //    _axisPointer.Quaternion = new Quaternion(Vector3D.CrossProduct(_zAxis, axis), Vector3D.AngleBetween(_zAxis, axis));
        }

        private Vector3D ProjectToTrackball(double width, double height, Point point)
        {
            double x = point.X / (width / 2);    // Scale so bounds map to [0,0] - [2,2]
            double y = point.Y / (height / 2);

            x = x - 1;                           // Translate 0,0 to the center
            y = 1 - y;                           // Flip so +Y is up instead of down

            double z2 = 1 - x * x - y * y;       // z^2 = 1 - x^2 - y^2
            double z = z2 > 0 ? Math.Sqrt(z2) : 0;

            return new Vector3D(x, y, z);
        }

        private void Zoom(Point currentPosition)
        {
            double yDelta = currentPosition.Y - _previousPosition2D.Y;
            
            double scale = Math.Exp(yDelta / 100);    // e^(yDelta/100) is fairly arbitrary.

            _scale.ScaleX *= scale;
            _scale.ScaleY *= scale;
            _scale.ScaleZ *= scale;
        }

        public void SetAnimation(Storyboard s, QuaternionAnimation a, QuaternionRotation3D r)
        {
            _storyboard = s;
            _storyboardRotation = r;
            _speed = a;
            //_axisPointer = a;
        }
    }
}
