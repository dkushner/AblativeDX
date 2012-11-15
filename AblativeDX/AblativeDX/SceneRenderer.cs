using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

using Assimp;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Content;


namespace AblativeDX
{
    using SharpDX.Toolkit.Graphics;

    public class SceneRenderer : Game
    {
        private GraphicsDeviceManager graphicsDeviceManager;
        private BasicEffect basicEffect;
        private Buffer<VertexPositionColor> vertices;
        private VertexInputLayout inputLayout;

        public SceneRenderer()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Assets";
        }

        protected override void Initialize()
        {
            Window.Title = "HelloWorld!";
            base.Initialize();
        }
        protected override void LoadContent()
        {
            Model thing = Content.Load<Model>("Mousetest.fbx");

            base.LoadContent();
        }
        protected override void Update(GameTime gameTime)
        {
            var time = (float)gameTime.TotalGameTime.TotalSeconds;
            basicEffect.World = Matrix.RotationX(time) * Matrix.RotationY(time * 2.0f) * Matrix.RotationZ(time * 0.7f);

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(GraphicsDevice.BackBuffer, Color.CornflowerBlue);

            GraphicsDevice.SetVertexBuffer(vertices);
            GraphicsDevice.SetVertexInputLayout(inputLayout);

            basicEffect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.Draw(PrimitiveType.TriangleList, vertices.ElementCount);

            base.Draw(gameTime);
        }
    }
}
