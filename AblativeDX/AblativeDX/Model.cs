using SharpDX;
using SharpDX.Toolkit.Graphics;
using SharpDX.Toolkit.Content;
using System.Collections.Generic;

namespace AblativeDX
{
    [ContentReader(typeof(ModelContentReader))]
    public class Model
    {
        private List<Mesh> meshes;
        public List<Mesh> Meshes
        {
            get
            {
                return meshes;
            }
        }

        internal Model( Mesh[] meshes)
        {
            this.meshes = new List<Mesh>(meshes);
        }
        public void Draw(Matrix world, Matrix view, Matrix projection)
        {
            foreach (Mesh m in meshes)
            {
                m.Effect.World = world;
                m.Effect.View = view;
                m.Effect.Projection = projection;

                m.Draw();
            }
        }
    }
}
