using System.IO;
using System.Linq;
using Assimp;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using SharpDX.Toolkit.Content;

namespace AblativeDX
{
    public class ModelContentReader : IContentReader
    {
        public object ReadContent(IContentManager contentManager, string assetName, Stream stream, out bool keepStreamOpen)
        {
            keepStreamOpen = false;
            AssimpImporter importer = new AssimpImporter();
            Scene model = importer.ImportFileFromStream(stream,
                PostProcessSteps.Triangulate |
                PostProcessSteps.MakeLeftHanded |
                PostProcessSteps.GenerateSmoothNormals,
                Path.GetExtension(assetName));

            if (model == null)
                return null;

            if (!model.HasMeshes)
                return null;

            // Retrieve graphicsDevice associated with this game instance.
            // Is there a better way to do this?
            GraphicsDevice graphicsDevice = ((IGraphicsDeviceService)contentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;

            Mesh[] meshes = new Mesh[model.MeshCount];
            for (int i = 0; i < model.MeshCount; i++)
            {
                Assimp.Mesh m = model.Meshes[i];

                // Check for bare minimum mesh compliance.
                if (!m.HasNormals || !m.HasTextureCoords(0))
                    return null;

                // Create a new internal mesh object.
                Mesh bMesh = new Mesh(graphicsDevice, 
                    m.Vertices.Select<Vector3D, Vector3>((srcVec, dstVec) => new Vector3(srcVec.X, srcVec.Y, srcVec.Z)).ToArray(),
                    m.GetVertexColors(0).Select<Color4D, Color>((srcColor, dstColor) => new Color(srcColor.R, srcColor.G, srcColor.B, srcColor.A)).ToArray());

                meshes[i] = bMesh;
            }
            return new Model(meshes);
        }
    }
}
