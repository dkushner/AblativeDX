using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

using SlimDX;
using SlimDX.DXGI;
using SlimDX.Windows;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;
using Buffer = SlimDX.Direct3D11.Buffer;

using AblativeDXView.Rendering;

namespace AblativeDXView
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Create window and swap chain description.
            var form = new RenderForm("AblativeDX Viewer");
            var description = new SwapChainDescription()
            {
                BufferCount = 1,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = form.Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard
            };

            Device device;
            SwapChain swapChain;

            // Create the device context.
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, description, out device, out swapChain);

            // Bind the device context to the window.
            using (var factory = swapChain.GetParent<Factory>())
                factory.SetWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAltEnter);
          
            // Set up the back buffer render view.
            Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            var renderView = new RenderTargetView(device, backBuffer);

            // Set up the viewport.
            device.ImmediateContext.OutputMerger.SetTargets(renderView);
            device.ImmediateContext.Rasterizer.SetViewports(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));

            // Create the point system.
            var pointSystem = new PointSystem(device);

            form.KeyDown += (o, e) =>
            {
                if (e.Alt && e.KeyCode == Keys.Enter)
                {
                    swapChain.IsFullScreen = !swapChain.IsFullScreen;
                    return;
                }

                switch (e.KeyCode)
                {
                    case Keys.W:
                        pointSystem.Camera.Position.Z += 10.0f;
                        break;
                    case Keys.A:
                        pointSystem.Camera.Position.X -= 10.0f;
                        break;
                    case Keys.S:
                        pointSystem.Camera.Position.Z -= 10.0f;
                        break;
                    case Keys.D:
                        pointSystem.Camera.Position.X += 10.0f;
                        break;
                    default:
                        break;
                }
                
            };

            var lastMouse = Vector2.Zero;
            form.MouseMove += (o, e) =>
            {
                pointSystem.Camera.Rotation += new Vector3(e.X - lastMouse.X, e.Y - lastMouse.Y, 0.0f);
                lastMouse = new Vector2(e.X, e.Y);
                Console.WriteLine(pointSystem.Camera.Rotation);
            };


            // Run the loop.
            MessagePump.Run(form, () =>
            {
                device.ImmediateContext.ClearRenderTargetView(renderView, Color.Black);

                pointSystem.Draw();
                
                swapChain.Present(0, PresentFlags.None);
            });

            swapChain.Dispose();
            device.Dispose();
        }
    }
}
