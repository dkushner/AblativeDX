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

namespace AblativeDX.Rendering
{
    public class PointSystem
    {
        public BoundingBox SystemBounds
        {
            get;
            set;
        }

        private Device device;
        private Effect effect;

        private int particleCount;

        private Buffer particleBuffer;
        private Buffer colorBuffer;

        private InputElement[] elements;
        private VertexBufferBinding[] bindings;

        public PointSystem(Device device, Effect effect, BoundingBox bounds, int count)
        {
            
        }
    }
}
