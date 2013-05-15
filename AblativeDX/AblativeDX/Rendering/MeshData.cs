using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;

namespace AblativeDX.Rendering
{
    public class MeshData
    {
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector2[] TextureCoordinates;
        public int[] Indices;
    }
}
