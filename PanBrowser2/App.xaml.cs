using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Terry 
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        // Used to host WPF content in preview mode, attach HwndSource to parent Win32 window.
        private HwndSource winWPFContent;
        private PanBrowser winSaver;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Preview mode--display in little window in Screen Saver dialog
            // (Not invoked with Preview button, which runs Screen Saver in
            // normal /s mode).
            string arg = "";
            if (e.Args.Length > 0)
                arg = e.Args[0].ToLower();
            if (arg.StartsWith("/p"))        
            {
                winSaver = new PanBrowser();

                Int32 previewHandle = Convert.ToInt32(e.Args[1]);
                //WindowInteropHelper interopWin1 = new WindowInteropHelper(win);
                //interopWin1.Owner = new IntPtr(previewHandle);

                IntPtr pPreviewHnd = new IntPtr(previewHandle);

                Win32API.RECT lpRect = new Win32API.RECT();
                bool bGetRect = Win32API.GetClientRect(pPreviewHnd, out lpRect);

                HwndSourceParameters sourceParams = new HwndSourceParameters("sourceParams");

                sourceParams.PositionX = 0;
                sourceParams.PositionY = 0;
                sourceParams.Height = lpRect.Bottom - lpRect.Top;
                sourceParams.Width = lpRect.Right - lpRect.Left;
                sourceParams.ParentWindow = pPreviewHnd;
                sourceParams.WindowStyle = (int)(Win32API.WindowStyles.WS_VISIBLE | Win32API.WindowStyles.WS_CHILD | Win32API.WindowStyles.WS_CLIPCHILDREN);

                winWPFContent = new HwndSource(sourceParams);
                winWPFContent.Disposed += new EventHandler(winWPFContent_Disposed);
                winWPFContent.RootVisual = winSaver.imagePanel;
            }

            // Normal screensaver mode.  Either screen saver kicked in normally,
            // or was launched from Preview button
            else if (arg.StartsWith("/s"))     
            {
                PanBrowser win = new PanBrowser();
                win.WindowState = WindowState.Maximized;
                win.WindowStyle = WindowStyle.None;
                win.ResizeMode = ResizeMode.NoResize;
                win._grid.ColumnDefinitions[0].Width=new GridLength(0);
                win.screensaver = true;
                win.Show();
            }

            // Config mode, launched from Settings button in screen saver dialog
            else if (arg.StartsWith("/c"))     
            {
                //SettingsWindow win = new SettingsWindow();
                //win.Show();
            }

            // If not running in one of the sanctioned modes, shut down the app
            // immediately (because we don't have a GUI).
            else
            {
                PanBrowser win = new PanBrowser();
                win.Show();
            }
        }

        /// <summary>
        /// Event that triggers when parent window is disposed--used when doing
        /// screen saver preview, so that we know when to exit.  If we didn't
        /// do this, Task Manager would get a new .scr instance every time
        /// we opened Screen Saver dialog or switched dropdown to this saver.
        /// </summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        void winWPFContent_Disposed(object sender, EventArgs e)
        {
            winSaver.Close();
//            Application.Current.Shutdown();
        }
    }
}
