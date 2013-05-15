using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using AblativeDX.Framework;
using AblativeDX.Rendering;

using eyecm.PhysX;

using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;

using Buffer = SlimDX.Direct3D11.Buffer;
using MapFlags = SlimDX.Direct3D11.MapFlags;

namespace AblativeDX.Simulations
{
    public class PhysXFluid : Simulation
    {
        private Camera camera;
        private Effect effect;

        // Particle Data
        private Buffer positionBuffer;
        private Buffer colorBuffer;
        private VertexBufferBinding[] bindings;

        private InputLayout layout;
        private InputElement[] elements;

        private RenderTargetView sceneView;
        private DepthStencilView depthView;

        private EffectMatrixVariable modelView;
        private EffectMatrixVariable inverseView;
        private EffectMatrixVariable projection;
        private EffectMatrixVariable modelViewProjection;

        // Simulation Data
        private Physics physics;
        private Scene physicsScene;

        private Point lastMouse = Point.Empty;

        protected override void Initialize()
        {
            CreateDevice();
        }
        protected override void LoadResources()
        {
            CreatePrimaryRenderTarget();
            CreateDepthBuffer();

            Context.OutputMerger.SetTargets(depthView, sceneView);
            Context.Rasterizer.SetViewports(new Viewport(0, 0, WindowWidth, WindowHeight, 0.0f, 1.0f));

            camera = new Camera(WindowWidth, WindowHeight);

            // Fill vertex buffer.
            var generator = new Random();
            var randomPoints = new List<Vector3>();
            for (int i = 0; i < 1024; i++)
            {
                var rp = new Vector3
                (
                    (float)(generator.NextDouble() - 0.5) * 100.0f,
                    (float)(generator.NextDouble() - 0.5) * 100.0f,
                    (float)(generator.NextDouble() - 0.5) * 500.0f
                );
                randomPoints.Add(rp);
            }
            using (var stream = new DataStream(randomPoints.ToArray(), true, true))
            {
                positionBuffer = new Buffer(Device, stream, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = randomPoints.Count * 4 * 3,
                    Usage = ResourceUsage.Default
                });
            }

            // Fill color buffer.
            var randomColors = new List<Color4>();
            for (int i = 0; i < 1024; i++)
            {
                var rc = new Color4
                (
                    1.0f,
                    (float)generator.NextDouble() * 0.5f,
                    (float)generator.NextDouble() * 0.5f,
                    (float)generator.NextDouble() * 0.5f
                );
                randomColors.Add(rc);
            }
            using (var stream = new DataStream(randomColors.ToArray(), true, true))
            {
                colorBuffer = new Buffer(Device, stream, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = randomColors.Count * 16,
                    Usage = ResourceUsage.Default
                });
            }

            // Compile shaders.
            var bytecode = ShaderBytecode.CompileFromFile("Shaders\\PointSystem.fx", "fx_5_0", ShaderFlags.Debug | ShaderFlags.OptimizationLevel0, EffectFlags.None);
            effect = new Effect(Device, bytecode);

            modelView = effect.GetVariableByName("ModelView").AsMatrix();
            inverseView = effect.GetVariableByName("InvView").AsMatrix();
            projection = effect.GetVariableByName("Projection").AsMatrix();
            modelViewProjection = effect.GetVariableByName("ModelViewProjection").AsMatrix();

            var particleTechnique = effect.GetTechniqueByName("RenderParticles");
            var pass = particleTechnique.GetPassByName("DensityDepth");

            layout = new InputLayout(Device, pass.Description.Signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 0, 1)
            });

            bindings = new[]
            {
                new VertexBufferBinding(positionBuffer, 12, 0),
                new VertexBufferBinding(colorBuffer, 16, 0)
            };
        }
        protected override void PreRender()
        {
            base.PreRender();

            Context.OutputMerger.SetTargets(depthView, sceneView);
            Context.ClearRenderTargetView(sceneView, new Color4(0.0f, 0.0f, 0.0f, 0.0f));
            Context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
        }
        protected override void Render()
        {
            base.Render();
            UpdateMatrices();

            Context.InputAssembler.InputLayout = layout;
            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            Context.InputAssembler.SetVertexBuffers(0, bindings);

            effect.GetTechniqueByName("RenderParticles").GetPassByIndex(0).Apply(Context);
            Context.Draw(1024, 0);
        }
        protected override void PostRender()
        {
            SwapChain.Present(0, PresentFlags.None);
        }

        protected override void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W:
                    camera.Position.Z += 10.0f;
                    break;
                case Keys.A:
                    camera.Position.X -= 10.0f;
                    break;
                case Keys.S:
                    camera.Position.Z -= 10.0f;
                    break;
                case Keys.D:
                    camera.Position.X += 10.0f;
                    break;
                case Keys.Space:
                    camera.Position.Y += 10.0f;
                    break;
                case Keys.ControlKey:
                    camera.Position.Y -= 10.0f;
                    break;
                default:
                    break;
            }
        }
        protected override void OnMouseMove(object sender, MouseEventArgs e)
        {
            var delta = Point.Subtract(e.Location, new Size(lastMouse));
            camera.Rotation += new Vector3(delta.Y, delta.X, 0.0f);
            lastMouse = e.Location;
        }

        private void CreatePrimaryRenderTarget()
        {
            Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(SwapChain, 0);
            sceneView = new RenderTargetView(Device, backBuffer);
        }
        private void CreateDepthBuffer()
        {
            Texture2DDescription depthBufferDesc = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.D32_Float,
                Width = 800,
                Height = 600,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };
            Texture2D depthBuffer = new Texture2D(Device, depthBufferDesc);

            DepthStencilViewDescription depthStencilDesc = new DepthStencilViewDescription
            {
                ArraySize = 0,
                Format = Format.D32_Float,
                Dimension = DepthStencilViewDimension.Texture2D,
                MipSlice = 0,
                Flags = 0,
                FirstArraySlice = 0
            };
            depthView = new DepthStencilView(Device, depthBuffer, depthStencilDesc);
        }
        private void UpdateMatrices()
        {
            modelView.SetMatrix(camera.ViewMatrix);
            projection.SetMatrix(camera.ProjectionMatrix);
            modelViewProjection.SetMatrix(camera.ViewMatrix * camera.ProjectionMatrix);
            inverseView.SetMatrix(Matrix.Invert(camera.ViewMatrix));
        }
    }
}
