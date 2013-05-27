using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;

using Device = SlimDX.Direct3D11.Device;

namespace AblativeDX.Framework
{
    public class Simulation : IDisposable
    {
        public int WindowWidth
        {
            get
            {
                return configuration.WindowWidth;
            }
        }
        public int WindowHeight
        {
            get
            {
                return configuration.WindowHeight;
            }
        }
        public float FrameDelta
        {
            get;
            private set;
        }
        public Device Device
        {
            get;
            private set;
        }
        public SwapChain SwapChain
        {
            get;
            private set;
        }
        public DeviceContext Context
        {
            get;
            private set;
        }

        private readonly Clock clock = new Clock();
        private Configuration configuration;
        private Form mainForm;
        private FormWindowState mainFormState;
        private float frameAccumulator;
        private int frameCount;
        //private UserInterface ui;
        //private UserInterfaceRenderer uiRenderer;
        private bool fullScreen = false;

        public void Run()
        {
            configuration = Configure();
            mainForm = CreateForm(configuration);
            mainFormState = mainForm.WindowState;

            bool formClosed = false;
            bool formResized = false;

            mainForm.MouseClick += OnMouseClick;
            mainForm.MouseMove += OnMouseMove;
            mainForm.KeyDown += OnKeyDown;
            mainForm.KeyUp += OnKeyUp;
            mainForm.Resize += (o, e) =>
            {
                if(mainForm.WindowState != mainFormState)
                {
                    OnResize(o, e);
                }
                mainFormState = mainForm.WindowState;
            };
            mainForm.ResizeBegin += (o, e) =>
            {
                formResized = true;
            };
            mainForm.ResizeEnd += (o, e) =>
            {
                formResized = false;
                OnResize(o, e);
            };
            mainForm.FormClosed += (o, e) =>
            {
                formClosed = true;
            };

            Initialize();
            LoadResources();

            clock.Start();
            MessagePump.Run(mainForm, () =>
            {
                if (formClosed)
                    return;

                Tick();
                if (!formResized)
                    Frame();
            });
            UnloadResources();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public virtual void Dispose(bool disposeManagedResources)
        {
            if (disposeManagedResources)
            {
                Device.Dispose();
                mainForm.Dispose();
            }
        }

        protected virtual Configuration Configure()
        {
            return new Configuration();
        }
        protected virtual Form CreateForm(Configuration config)
        {
            return new RenderForm(config.WindowTitle)
            {
                ClientSize = new Size(config.WindowWidth, config.WindowHeight)
            };
        }
        protected void CreateDevice()
        {
            var description = new SwapChainDescription()
            {
                BufferCount = 1,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = mainForm.Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard
            };

            Device device;
            SwapChain swapChain;

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, description, out device, out swapChain);
            using (var factory = swapChain.GetParent<Factory>())
                factory.SetWindowAssociation(mainForm.Handle, WindowAssociationFlags.IgnoreAltEnter);

            Device = device;
            SwapChain = swapChain;
            Context = device.ImmediateContext;
        }

        protected virtual void Initialize() { }
        protected virtual void LoadResources() { }
        protected virtual void Update() { }
        protected virtual void PreRender() { }
        protected virtual void Render() { }
        protected virtual void PostRender() { }
        protected virtual void UnloadResources() { }

        private void Frame()
        {
            // TODO: Calculate FPS and update UI controls.

            PreRender();
            Render();
            PostRender();
        }
        private void Tick()
        {
            FrameDelta = clock.Update();
            Update();
        }

        protected virtual void OnMouseClick(object sender, MouseEventArgs e) { }
        protected virtual void OnMouseMove(object sender, MouseEventArgs e) { }
        protected virtual void OnKeyDown(object sender, KeyEventArgs e) { }
        protected virtual void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode == Keys.Enter)
            {
                UnloadResources();
                fullScreen = !fullScreen;
                if (SwapChain != null)
                {
                    SwapChain.ResizeBuffers(1, WindowWidth, WindowHeight, SwapChain.Description.ModeDescription.Format, SwapChain.Description.Flags); 
                }
                LoadResources();
            }
        }
        protected virtual void OnResize(object sender, EventArgs e)
        {
            if (mainForm.WindowState == FormWindowState.Minimized)
                return;

            UnloadResources();
            if (SwapChain != null)
            {
                SwapChain.ResizeBuffers(1, WindowWidth, WindowHeight, SwapChain.Description.ModeDescription.Format, SwapChain.Description.Flags);
            }
            LoadResources();
        }
    }
}
