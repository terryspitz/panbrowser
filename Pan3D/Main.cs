using System;
using System.Diagnostics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.Samples.DirectX.UtilityToolkit;

namespace Terry
{

    public class Demo : IFrameworkCallback, IDeviceCreation
    {
        DemoController controller = new DemoController();
        private Framework sampleFramework = null; // Framework for samples
        private Font statsFont = null; // Font for drawing text
        private Sprite textSprite = null; // Sprite for batching text calls
        private ModelViewerCamera camera = new ModelViewerCamera(); // A model viewing camera
        private bool isHelpShowing = false; // If true, renders the UI help text
        private Dialog hud = null; // dialog for standard controls
        private Dialog sampleUi = null; // dialog for sample specific controls
        private StaticText resText = null;
        private Slider slider = null;

        // HUD Ui Control constants
        private const int ToggleFullscreen = 1;
        private const int ToggleReference = 3;
        private const int ChangeDevice = 4;

        /// <summary>
        /// This callback function will be called once at the beginning of every frame. This is the
        /// best location for your application to handle updates to the scene, but is not 
        /// intended to contain actual rendering calls, which should instead be placed in the 
        /// OnFrameRender callback.  
        /// </summary>
        public void OnFrameMove(Device device, double appTime, float elapsedTime)
        {
            controller.OnFrameMove(device, appTime);
            slider.Value = controller.resolution;
            resText.SetText(string.Format("Resolution: {0}", controller.resolution));

            // Update the camera's position based on user input 
            camera.FrameMove(elapsedTime);
        }

        /// <summary>
        /// This callback function will be called at the end of every frame to perform all the 
        /// rendering calls for the scene, and it will also be called if the window needs to be 
        /// repainted. After this function has returned, the sample framework will call 
        /// Device.Present to display the contents of the next buffer in the swap chain
        /// </summary>
        public void OnFrameRender(Device device, double appTime, float elapsedTime)
        {
            bool beginSceneCalled = false;

            // Clear the render target and the zbuffer 
            device.Clear(ClearFlags.ZBuffer | ClearFlags.Target, 0x002D32AA, 1.0f, 0);
            try
            {
                device.BeginScene();
                beginSceneCalled = true;

                // Update the effect's variables.  Instead of using strings, it would 
                // be more efficient to cache a handle to the parameter by calling 
                // Effect.GetParameter
                //effect.SetValue("worldViewProjection", camera.WorldMatrix * camera.ViewMatrix * camera.ProjectionMatrix);
                //effect.SetValue("worldMatrix", camera.WorldMatrix);
                //effect.SetValue("appTime", (float)appTime);

                if(controller.renderData!=null)
                    controller.renderData.Draw(device, camera);

                RenderText();
                RenderHelp();

                //if (controller.renderData2 != null)
                    //controller.renderData2.Draw(device, camera, true);

                // Show UI
                hud.OnRender(elapsedTime);
                sampleUi.OnRender(elapsedTime);
            }
            finally
            {
                if (beginSceneCalled)
                    device.EndScene();
            }
        }

        private void RenderHelp()
        {
            TextHelper txtHelper = new TextHelper(statsFont, textSprite, 15);
            txtHelper.Begin();
            txtHelper.SetInsertionPoint(5, 100);
            if (isHelpShowing)
                txtHelper.DrawTextLine(string.Format("\nKeys:\nF1: Hide help\nF2: Settings\nF3: Toggle Renderer\nF8: Wireframe\nPause\nEscape: Quit\n\n{0}\n\n{1}", Flags.Description, Camera.Description));
            else
                txtHelper.DrawTextLine(string.Format("\nKeys:\nF1: Show help"));
            txtHelper.End();
        }

        /// <summary>
        /// Render the help and statistics text. This function uses the Font object for 
        /// efficient text rendering.
        /// </summary>
        private void RenderText()
        {
            TextHelper txtHelper = new TextHelper(statsFont, textSprite, 15);

            // Output statistics
            txtHelper.Begin();
            txtHelper.SetInsertionPoint(5, 5);
            txtHelper.SetForegroundColor(System.Drawing.Color.Gray);
            txtHelper.DrawTextLine(sampleFramework.FrameStats);
            txtHelper.DrawTextLine(sampleFramework.DeviceStats);

            txtHelper.SetForegroundColor(System.Drawing.Color.White);
            if(controller!=null && controller.renderData!=null)
                txtHelper.DrawTextLine(controller.renderData.Description);

            //txtHelper.SetInsertionPoint(50, sliderY);
            //txtHelper.DrawTextLine("Resolution: {0}", controller.resolution);

            txtHelper.End();
        }

        /// <summary>
        /// Initializes the application
        /// </summary>
        public void InitializeApplication()
        {
            camera.IsPositionMovementEnabled = true;

            int y = 10;
            // Initialize the HUD
            Button fullScreen = hud.AddButton(ToggleFullscreen, "Toggle full screen", 35, y, 125, 22);
            Button toggleRef = hud.AddButton(ToggleReference, "Toggle reference (F3)", 35, y += 24, 125, 22);
            Button changeDevice = hud.AddButton(ChangeDevice, "Change Device (F2)", 35, y += 24, 125, 22);
            // Hook the button events for when these items are clicked
            fullScreen.Click += new EventHandler(OnFullscreenClicked);
            toggleRef.Click += new EventHandler(OnRefClicked);
            changeDevice.Click += new EventHandler(OnChangeDeviceClicked);

            int id = ChangeDevice + 1;
            sampleUi.AddStatic(id++, "Shape:", 35, y += 24, 125, 22);
            ComboBox cb1 = sampleUi.AddComboBox(id++, 35, y += 24, 125, 22);
            foreach (Shapes.ShapeType shape in Enum.GetValues(typeof(Shapes.ShapeType)))
                cb1.AddItem(shape.ToString(), null);
            cb1.SetSelected(controller.shape.ToString());
            cb1.Changed += new EventHandler(cb1_Changed);

            y += 40;
            resText = sampleUi.AddStatic(id++, "Resolution:", 50, y, 100, 10);
            slider = sampleUi.AddSlider(id++, 50, y += 24, 100, 22, 1, 50, controller.resolution, false);
            slider.ValueChanged += new EventHandler(slider_ValueChanged);


            y += 40;
            //sampleUi.AddStatic(id++, "Value2:", 50, y, 100, 10);
            //Slider slider2 = sampleUi.AddSlider(id++, 50, y += 24, 100, 22, 1, 50, controller.value2, false);
            //slider2.ValueChanged += new EventHandler(slider2_ValueChanged);

        }

        void slider_ValueChanged(object sender, EventArgs e)
        {
            controller.resolution = ((Slider)sender).Value;
            controller.OnStateChange();
            resText.SetText(string.Format("Resolution: {0}", controller.resolution));
        }

        void slider2_ValueChanged(object sender, EventArgs e)
        {
            controller.value2 = ((Slider)sender).Value;
            controller.OnStateChange();
            //resText.SetText(string.Format("Resolution: {0}", controller.resolution));
        }

        void cb1_Changed(object sender, EventArgs e)
        {
            FrameworkTimer.Reset();
            controller.shape = (Shapes.ShapeType)Enum.Parse(typeof(Shapes.ShapeType), ((ComboBox)sender).GetSelectedItem().ItemText);
            controller.resolution = 3;
            controller.OnStateChange();
        }

#region Boilerplate
        /// <summary>
        /// Called during device initialization, this code checks the device for some 
        /// minimum set of capabilities, and rejects those that don't pass by returning false.
        /// </summary>
        public bool IsDeviceAcceptable(Caps caps, Format adapterFormat, Format backBufferFormat, bool windowed)
        {
            // Skip back buffer formats that don't support alpha blending
            if (!Manager.CheckDeviceFormat(caps.AdapterOrdinal, caps.DeviceType, adapterFormat, 
                Usage.QueryPostPixelShaderBlending, ResourceType.Textures, backBufferFormat))
                return false;

            return true;
        }

        /// <summary>
        /// This callback function is called immediately before a device is created to allow the 
        /// application to modify the device settings. The supplied settings parameter 
        /// contains the settings that the framework has selected for the new device, and the 
        /// application can make any desired changes directly to this structure.  Note however that 
        /// the sample framework will not correct invalid device settings so care must be taken 
        /// to return valid device settings, otherwise creating the Device will fail.  
        /// </summary>
        public void ModifyDeviceSettings(DeviceSettings settings, Caps caps)
        {
            // If device doesn't support HW T&L or doesn't support 1.1 vertex shaders in HW 
            // then switch to SWVP.
            if ( (!caps.DeviceCaps.SupportsHardwareTransformAndLight) ||
                (caps.VertexShaderVersion < new Version(1,1)) )
            {
                settings.BehaviorFlags = CreateFlags.SoftwareVertexProcessing;
            }
            else
            {
                settings.BehaviorFlags = CreateFlags.HardwareVertexProcessing;
            }

            // This application is designed to work on a pure device by not using 
            // any get methods, so create a pure device if supported and using HWVP.
            if ( (caps.DeviceCaps.SupportsPureDevice) && 
                ((settings.BehaviorFlags & CreateFlags.HardwareVertexProcessing) != 0 ) )
                settings.BehaviorFlags |= CreateFlags.PureDevice;

            // Debugging vertex shaders requires either REF or software vertex processing 
            // and debugging pixel shaders requires REF.  
#if(DEBUG_VS)
            if (settings.DeviceType != DeviceType.Reference )
            {
                settings.BehaviorFlags &= ~CreateFlags.HardwareVertexProcessing;
                settings.BehaviorFlags |= CreateFlags.SoftwareVertexProcessing;
            }
#endif
#if(DEBUG_PS)
            settings.DeviceType = DeviceType.Reference;
#endif
            // For the first device created if its a REF device, optionally display a warning dialog box
            if (settings.DeviceType == DeviceType.Reference)
            {
                Utility.DisplaySwitchingToRefWarning(sampleFramework, "EmptyProject");
            }

        }

        /// <summary>
        /// This event will be fired immediately after the Direct3D device has been 
        /// created, which will happen during application initialization and windowed/full screen 
        /// toggles. This is the best location to create Pool.Managed resources since these 
        /// resources need to be reloaded whenever the device is destroyed. Resources created  
        /// here should be released in the Disposing event. 
        /// </summary>
        private void OnCreateDevice(object sender, DeviceEventArgs e)
        {
            // Initialize the stats font
            statsFont = ResourceCache.GetGlobalInstance().CreateFont(e.Device, 15, 0, FontWeight.Bold, 1, false, CharacterSet.Default,
                Precision.Default, FontQuality.Default, PitchAndFamily.FamilyDoNotCare | PitchAndFamily.DefaultPitch
                , "Arial");

            // Setup the camera's view parameters
            camera.SetViewParameters(new Vector3(0.0f, 0.0f, -5.0f), Vector3.Empty);
            
        }
        
        /// <summary>
        /// This event will be fired immediately after the Direct3D device has been 
        /// reset, which will happen after a lost device scenario. This is the best location to 
        /// create Pool.Default resources since these resources need to be reloaded whenever 
        /// the device is lost. Resources created here should be released in the OnLostDevice 
        /// event. 
        /// </summary>
        private void OnResetDevice(object sender, DeviceEventArgs e)
        {
            System.Drawing.Color col = System.Drawing.Color.LightGreen;

            Material mtrl = new Material();
            mtrl.Diffuse = col;
            mtrl.Ambient = col;
            //e.Device.Material = mtrl;

            e.Device.RenderState.Lighting = true;    // Make sure lighting is enabled
            e.Device.Lights[0].Type = LightType.Directional;
            e.Device.Lights[0].Specular = System.Drawing.Color.White;
            e.Device.Lights[0].Direction = new Vector3(
                            (float)Math.Cos(1),
                            1.0f,
                            (float)Math.Sin(1));
            //e.Device.Lights[0].Enabled = true; // Turn it on
            e.Device.Lights[1].Type = LightType.Directional;
            e.Device.Lights[1].Ambient = System.Drawing.Color.FromArgb(40,40,40);
            e.Device.Lights[1].Diffuse = System.Drawing.Color.WhiteSmoke;
            e.Device.Lights[1].Direction = new Vector3(-1, -0.5f, 0.8f);
            e.Device.Lights[1].Enabled = true; // Turn it on

            e.Device.RenderState.CullMode = Cull.CounterClockwise;
            e.Device.RenderState.DiffuseMaterialSource = ColorSource.Color1;
            //e.Device.RenderState.AlphaBlendEnable = true;
            e.Device.RenderState.SourceBlend = Blend.SourceColor;
            e.Device.RenderState.DestinationBlend = Blend.InvSourceAlpha;
            e.Device.RenderState.SpecularEnable = true;
            
            SurfaceDescription desc = e.BackBufferDescription;
            // Create a sprite to help batch calls when drawing many lines of text
            textSprite = new Sprite(e.Device);

            // Setup the camera's projection parameters
            float aspectRatio = (float)desc.Width / (float)desc.Height;
            camera.SetProjectionParameters((float)Math.PI / 4, aspectRatio, 0.1f, 1000.0f);
            camera.SetWindow(desc.Width, desc.Height);

            // Setup UI locations
            hud.SetLocation(desc.Width-170, 0);
            hud.SetSize(170,170);
            sampleUi.SetLocation(desc.Width - 170, desc.Height - 350);
            sampleUi.SetSize(170,300);

            controller.OnStateChange();
        }

        /// <summary>
        /// This event function will be called fired after the Direct3D device has 
        /// entered a lost state and before Device.Reset() is called. Resources created
        /// in the OnResetDevice callback should be released here, which generally includes all 
        /// Pool.Default resources. See the "Lost Devices" section of the documentation for 
        /// information about lost devices.
        /// </summary>
        private void OnLostDevice(object sender, EventArgs e)
        {
            if (textSprite != null)
            {
                textSprite.Dispose();
                textSprite = null;
            }
        }

        /// <summary>
        /// This callback function will be called immediately after the Direct3D device has 
        /// been destroyed, which generally happens as a result of application termination or 
        /// windowed/full screen toggles. Resources created in the OnCreateDevice callback 
        /// should be released here, which generally includes all Pool.Managed resources. 
        /// </summary>
        private void OnDestroyDevice(object sender, EventArgs e)
        {
        }



        /// <summary>
        /// As a convenience, the sample framework inspects the incoming windows messages for
        /// keystroke messages and decodes the message parameters to pass relevant keyboard
        /// messages to the application.  The framework does not remove the underlying keystroke 
        /// messages, which are still passed to the application's MsgProc callback.
        /// </summary>
        private void OnKeyEvent(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case System.Windows.Forms.Keys.F1:
                    isHelpShowing = !isHelpShowing;
                    break;
                case System.Windows.Forms.Keys.Space:
                    controller.OnStateChange(); //redraw
                    break;
            }
            if (controller.OnKeyPress((char)e.KeyValue))
                controller.OnStateChange();
        }

        /// <summary>
        /// Before handling window messages, the sample framework passes incoming windows 
        /// messages to the application through this callback function. If the application sets 
        /// noFurtherProcessing to true, the sample framework will not process the message
        /// </summary>
        public IntPtr OnMsgProc(IntPtr hWnd, NativeMethods.WindowMessage msg, IntPtr wParam, IntPtr lParam, ref bool noFurtherProcessing)
        {
            // Give the dialog a chance to handle the message first
            noFurtherProcessing = hud.MessageProc(hWnd, msg, wParam, lParam);
            if (noFurtherProcessing)
                return IntPtr.Zero;

            noFurtherProcessing = sampleUi.MessageProc(hWnd, msg, wParam, lParam);
            if (noFurtherProcessing)
                return IntPtr.Zero;

            // Pass all remaining windows messages to camera so it can respond to user input
            camera.HandleMessages(hWnd, msg, wParam, lParam);

            return IntPtr.Zero;
        }

        /// <summary>Called when the change device button is clicked</summary>
        private void OnChangeDeviceClicked(object sender, EventArgs e)
        {
            sampleFramework.ShowSettingsDialog(!sampleFramework.IsD3DSettingsDialogShowing);
        }

        /// <summary>Called when the full screen button is clicked</summary>
        private void OnFullscreenClicked(object sender, EventArgs e)
        {
            sampleFramework.ToggleFullscreen();
        }

        /// <summary>Called when the ref button is clicked</summary>
        private void OnRefClicked(object sender, EventArgs e)
        {
            sampleFramework.ToggleReference();
        }

        /// <summary>
        /// Entry point to the program. Initializes everything and goes into a message processing 
        /// loop. Idle time is used to render the scene.
        /// </summary>
        static int Main() 
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            using(Framework sampleFramework = new Framework())
            {
                Demo sample = new Demo(sampleFramework);
                // Set the callback functions. These functions allow the sample framework to notify
                // the application about device changes, user input, and windows messages.  The 
                // callbacks are optional so you need only set callbacks for events you're interested 
                // in. However, if you don't handle the device reset/lost callbacks then the sample 
                // framework won't be able to reset your device since the application must first 
                // release all device resources before resetting.  Likewise, if you don't handle the 
                // device created/destroyed callbacks then the sample framework won't be able to 
                // recreate your device resources.
                sampleFramework.Disposing += new EventHandler(sample.OnDestroyDevice);
                sampleFramework.DeviceLost += new EventHandler(sample.OnLostDevice);
                sampleFramework.DeviceCreated += new DeviceEventHandler(sample.OnCreateDevice);
                sampleFramework.DeviceReset += new DeviceEventHandler(sample.OnResetDevice);

                sampleFramework.SetWndProcCallback(new WndProcCallback(sample.OnMsgProc));

                sampleFramework.SetCallbackInterface(sample);
                try
                {

                    // Show the cursor and clip it when in full screen
                    sampleFramework.SetCursorSettings(true, true);

                    // Initialize
                    sample.InitializeApplication();

                    // Initialize the sample framework and create the desired window and Direct3D 
                    // device for the application. Calling each of these functions is optional, but they
                    // allow you to set several options which control the behavior of the sampleFramework.
                    sampleFramework.Initialize( true, true, true ); // Parse the command line, handle the default hotkeys, and show msgboxes
                    sampleFramework.CreateWindow("3D Mandlebrot Set");
                    // Hook the keyboard event
                    sampleFramework.Window.KeyDown += new System.Windows.Forms.KeyEventHandler(sample.OnKeyEvent);
                    sampleFramework.CreateDevice( 0, true, Framework.DefaultSizeWidth, Framework.DefaultSizeHeight, 
                        sample);

                    // Pass control to the sample framework for handling the message pump and 
                    // dispatching render calls. The sample framework will call your FrameMove 
                    // and FrameRender callback when there is idle time between handling window messages.
                    sampleFramework.MainLoop();

                }
#if(DEBUG)
                catch (Exception e)
                {
                    // In debug mode show this error (maybe - depending on settings)
                    sampleFramework.DisplayErrorMessage(e);
#else
            catch
            {
                // In release mode fail silently
#endif
                    // Ignore any exceptions here, they would have been handled by other areas
                    return (sampleFramework.ExitCode == 0) ? 1 : sampleFramework.ExitCode; // Return an error code here
                }

                // Perform any application-level cleanup here. Direct3D device resources are released within the
                // appropriate callback functions and therefore don't require any cleanup code here.
                return sampleFramework.ExitCode;
            }
        }

        /// <summary>Create a new instance of the class</summary>
        public Demo(Framework f)
        {
            // Store framework
            sampleFramework = f;
            // Create dialogs
            hud = new Dialog(sampleFramework);
            sampleUi = new Dialog(sampleFramework);
        }
#endregion
    }
}
