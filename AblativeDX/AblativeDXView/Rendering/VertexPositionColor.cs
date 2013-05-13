using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.DXGI;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;

namespace AblativeDXView.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionColor
    {
        public Vector3 Position;
        public Color4 Color;
    }
}
