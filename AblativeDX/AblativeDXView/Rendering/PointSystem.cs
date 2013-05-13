using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;

using Device = SlimDX.Direct3D11.Device;
using Buffer = SlimDX.Direct3D11.Buffer;
using MapFlags = SlimDX.Direct3D11.MapFlags;

namespace AblativeDXView.Rendering
{
    public class PointSystem
    {
        public Camera Camera
        {
            get
            {
                return camera;
            }
        }

        private Device device;

        private Buffer positionBuffer;
        private Buffer colorBuffer;
        private Buffer matrixBuffer;
        private VertexBufferBinding[] bindings;

        private InputLayout layout;

        private Texture2D sceneBuffer;
        private Texture2D depthBuffer;

        private RenderTargetView scene;
        private DepthStencilView depth;

        private Effect effect;
        private Camera camera;

        private EffectMatrixVariable modelView;
        private EffectMatrixVariable inverseView;
        private EffectMatrixVariable projection;
        private EffectMatrixVariable modelViewProjection;

        public PointSystem(Device device)
        {
            this.device = device;
            this.camera = new Camera(device.ImmediateContext.Rasterizer.GetViewports()[0]);
            this.camera.Position.Z = -200.0f;
            this.camera.LookTarget = Vector3.UnitZ;

            var generator = new Random();
            var randomPoints = new List<Vector3>();
            for (int i = 0; i < 64; i++)
            {
                var rp = new Vector3
                (
                    (float)generator.NextDouble(),
                    (float)generator.NextDouble(),
                    (float)generator.NextDouble()
                );
                randomPoints.Add(rp * 50.0f);
            }

            // Create and fill the vertex buffer.
            using (var stream = new DataStream(randomPoints.ToArray(), true, true))
            {
                positionBuffer = new Buffer(device, stream, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer | BindFlags.StreamOutput,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = randomPoints.Count * 4 * 3,
                    Usage = ResourceUsage.Default
                });
            }

            var randomColors = new List<Color4>();
            for (int i = 0; i < 64; i++)
            {
                var rc = new Color4
                (
                    (float)generator.NextDouble(),
                    (float)generator.NextDouble(),
                    (float)generator.NextDouble(),
                    (float)generator.NextDouble()
                );
                randomColors.Add(rc);
            }
            // Create and fill the color buffer.
            using (var stream = new DataStream(randomColors.ToArray(), true, true))
            {
                colorBuffer = new Buffer(device, stream, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer | BindFlags.StreamOutput,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = randomColors.Count * 16,
                    Usage = ResourceUsage.Default
                });
            }
       
            // Compile shaders.
            var bytecode = ShaderBytecode.CompileFromFile("Shaders\\PointSystem.fx", "fx_5_0", ShaderFlags.Debug, EffectFlags.None);
            effect = new Effect(device, bytecode);

            modelView = effect.GetVariableByName("ModelView").AsMatrix();
            inverseView = effect.GetVariableByName("InvView").AsMatrix();
            projection = effect.GetVariableByName("Projection").AsMatrix();
            modelViewProjection = effect.GetVariableByName("ModelViewProjection").AsMatrix();

            var particleTech = effect.GetTechniqueByName("RenderParticles");
            var pass = particleTech.GetPassByIndex(0);

            // Set up the buffer layout.
            layout = new InputLayout(device, pass.Description.Signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 0, 1)
            });

            // Set up buffer bindings.
            bindings = new[]
            {
                new VertexBufferBinding(positionBuffer, 12, 0),
                new VertexBufferBinding(colorBuffer, 16, 0)
            };
        }

        public void Draw()
        {
            UpdateConstantBuffers();

            device.ImmediateContext.InputAssembler.InputLayout = layout;
            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, bindings);
            device.ImmediateContext.VertexShader.SetConstantBuffer(matrixBuffer, 1);

            effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device.ImmediateContext);
            device.ImmediateContext.Draw(64, 0);
        }


        private void UpdateConstantBuffers()
        {
            modelView.SetMatrix(Matrix.Identity * camera.ViewMatrix);
            projection.SetMatrix(camera.ProjectionMatrix);
            modelViewProjection.SetMatrix(camera.ViewMatrix * camera.ProjectionMatrix);
            inverseView.SetMatrix(Matrix.Invert(camera.ViewMatrix));
        }
    }
}
