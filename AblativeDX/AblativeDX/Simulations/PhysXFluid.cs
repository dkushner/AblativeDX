using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

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
        private ComputeShader blurH;
        private ComputeShader blurV;

        // Particle Data
        private Buffer positionBuffer;
        private Buffer streamOutBuffer;
        private VertexBufferBinding[] bindings;
        private VertexBufferBinding[] streamInBindings;
        private StreamOutputBufferBinding[] streamOutBindings;

        private InputLayout layout;
        private InputLayout streamLayout;

        private RenderTargetView sceneView;
        private RenderTargetView densityView;
        private ShaderResourceView densityResView;
        private DepthStencilView depthView;
        private ShaderResourceView depthResView;

        private EffectMatrixVariable modelView;
        private EffectMatrixVariable inverseView;
        private EffectMatrixVariable projection;
        private EffectMatrixVariable modelViewProjection;
        private EffectVectorVariable fluidColor;

        // Simulation Data
        private Physics physics;
        private Scene physicsScene;
        private Point lastMouse = Point.Empty;

        [StructLayout(LayoutKind.Sequential)]
        private struct StreamOutData
        {
            Vector4 Position;
            Vector2 TexCoord;
            Vector3 EyePosition;
        }

        protected override void Initialize()
        {
            CreateDevice();
        }
        protected override void LoadResources()
        {
            CreateRenderTargets();

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

            streamOutBuffer = new Buffer(Device, new BufferDescription()
            {
                BindFlags = BindFlags.StreamOutput | BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = randomPoints.Count * Marshal.SizeOf(typeof(StreamOutData)) * 4,
                Usage = ResourceUsage.Default
            });

            // Compile shaders.
            var bytecode = ShaderBytecode.CompileFromFile("Shaders\\PointSystem.fx", "fx_5_0", ShaderFlags.Debug | ShaderFlags.OptimizationLevel0, EffectFlags.None);
            effect = new Effect(Device, bytecode);

            modelView = effect.GetVariableByName("ModelView").AsMatrix();
            inverseView = effect.GetVariableByName("InvView").AsMatrix();
            projection = effect.GetVariableByName("Projection").AsMatrix();
            modelViewProjection = effect.GetVariableByName("ModelViewProjection").AsMatrix();

            var particleTechnique = effect.GetTechniqueByName("RenderParticles");
            var depthPass = particleTechnique.GetPassByName("DepthPass");
            var densityPass = particleTechnique.GetPassByName("DensityPass");


            layout = new InputLayout(Device, depthPass.Description.Signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0)
            });
            streamLayout = new InputLayout(Device, densityPass.Description.Signature, new[]
            {
                new InputElement("SV_POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0, 1),
                new InputElement("TEXCOORD", 1, Format.R32G32B32_Float, 0, 2)
            });

            var defines = new[]
            {
                new ShaderMacro("BLK_SIZE", "128")
            };
            bytecode = ShaderBytecode.CompileFromFile("Shaders\\BilateralFilter.hlsl", "CSBilateralH", "cs_5_0", ShaderFlags.Debug | ShaderFlags.OptimizationLevel0, EffectFlags.None, defines, null);
            blurH = new ComputeShader(Device, bytecode);

            bytecode = ShaderBytecode.CompileFromFile("Shaders\\BilateralFilter.hlsl", "CSBilateralV", "cs_5_0", ShaderFlags.Debug | ShaderFlags.OptimizationLevel0, EffectFlags.None, defines, null);
            blurV = new ComputeShader(Device, bytecode);

            bindings = new[]
            {
                new VertexBufferBinding(positionBuffer, 12, 0),
            };
            streamOutBindings = new[]
            {
                new StreamOutputBufferBinding(streamOutBuffer, 0)
            };
            streamInBindings = new[]
            {
                new VertexBufferBinding(streamOutBuffer, 36, 0),
            };
        }
        protected override void PreRender()
        {
            base.PreRender();

            Context.OutputMerger.SetTargets(depthView, null, null);
            Context.Rasterizer.SetViewports(new Viewport(0, 0, WindowWidth, WindowHeight, 0.0f, 1.0f));

            Context.ClearRenderTargetView(sceneView, new Color4(0.0f, 0.0f, 0.0f, 0.0f));
            Context.ClearRenderTargetView(densityView, new Color4(0.0f, 0.0f, 0.0f, 0.0f));
            Context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
        
        }
        protected override void Render()
        {
            base.Render();
            UpdateMatrices();

            Context.InputAssembler.InputLayout = layout;
            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            Context.InputAssembler.SetVertexBuffers(0, bindings);
            Context.StreamOutput.SetTargets(streamOutBindings);
            
            var technique = effect.GetTechniqueByIndex(0);

            // Render depth pass.
            technique.GetPassByName("DepthPass").Apply(Context);
            Context.Draw(1024, 0);

            // Unbind stream output and depth. Bind density render target.
            Context.StreamOutput.SetTargets(null);
            Context.OutputMerger.SetTargets((DepthStencilView)null, null, densityView);

            Context.InputAssembler.InputLayout = streamLayout;
            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            Context.InputAssembler.SetVertexBuffers(0, streamInBindings);

            // Render density pass.
            technique.GetPassByName("DensityPass").Apply(Context);
            Context.DrawAuto();

            // Bind depth texture to CS.
            Context.ComputeShader.SetShaderResource(depthResView, 0);
            Context.ComputeShader.Set(blurH);

            Context.ComputeShader.SetShaderResource(null, 0);

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

        private void CreateRenderTargets()
        {
            Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(SwapChain, 0);
            sceneView = new RenderTargetView(Device, backBuffer);

            Texture2DDescription depthBufferDesc = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R32_Typeless,
                Width = WindowWidth,
                Height = WindowHeight,
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
            ShaderResourceViewDescription depthResViewDesc = new ShaderResourceViewDescription
            {
                ArraySize = 0,
                Format = Format.R32_Float,
                Dimension = ShaderResourceViewDimension.Texture2D,
                MipLevels = 1,
                Flags = 0,
                FirstArraySlice = 0
            };
            depthResView = new ShaderResourceView(Device, depthBuffer, depthResViewDesc);

            Texture2DDescription densityBufferDesc = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R8G8B8A8_UNorm,
                Width = WindowWidth,
                Height = WindowHeight,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };
            Texture2D densityBuffer = new Texture2D(Device, densityBufferDesc);
            
            RenderTargetViewDescription densityViewDesc = new RenderTargetViewDescription
            {
                ArraySize = 0,
                Format = Format.R8G8B8A8_UNorm,
                Dimension = RenderTargetViewDimension.Texture2D,
                MipSlice = 0,
                FirstArraySlice = 0
            };
            densityView = new RenderTargetView(Device, densityBuffer, densityViewDesc);
            ShaderResourceViewDescription densityResViewDesc = new ShaderResourceViewDescription
            {
                ArraySize = 0,
                Format = Format.R8G8B8A8_UNorm,
                Dimension = ShaderResourceViewDimension.Texture2D,
                MipLevels = 1,
                Flags = 0,
                FirstArraySlice = 0
            };
            densityResView = new ShaderResourceView(Device, densityBuffer, densityResViewDesc);
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
