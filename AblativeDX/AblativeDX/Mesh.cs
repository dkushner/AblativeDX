using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System.Linq;

namespace AblativeDX
{
    public sealed class Mesh : System.IDisposable
    {
        public string Name;

        public Buffer<VertexPositionColor> VertexBuffer;
        public VertexInputLayout InputLayout;

        private GraphicsDevice graphicsDevice;
        public BasicEffect Effect;

        internal Mesh(GraphicsDevice graphicsDevice, Vector3[] vertices, Color[] colors)
        {
            this.graphicsDevice = graphicsDevice;
            this.Effect = new BasicEffect(graphicsDevice);

            this.VertexBuffer = Buffer.Vertex.New(graphicsDevice,
                (from vertex in vertices
                from color in colors
                select new VertexPositionColor()
                {
                    Position = vertex,
                    Color = color
                }).ToArray());
            this.InputLayout = VertexInputLayout.FromBuffer(0, VertexBuffer);
        }
        internal void Draw()
        {
            this.graphicsDevice.SetVertexBuffer(VertexBuffer);
            this.graphicsDevice.SetVertexInputLayout(InputLayout);

            Effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.Draw(PrimitiveType.TriangleList, VertexBuffer.ElementCount);
        }
        public void Dispose()
        {
            Effect.Dispose();
        }
    }
}

