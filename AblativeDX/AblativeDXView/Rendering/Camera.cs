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
    public class Camera
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 LookTarget;
        public Matrix ViewMatrix
        {
            get
            {
                return CreateView();
            }
        }
        public Matrix ProjectionMatrix
        {
            get
            {
                return projectionMatrix;
            }
        }

        private Matrix projectionMatrix;

        public Camera(int width, int height)
        {
            LookTarget = Vector3.UnitZ;

            var aspect = width / height;
            var fov = 40.0f * ((float)Math.PI / 180.0f);
            projectionMatrix = Matrix.PerspectiveFovLH(fov, aspect, 1.0f, 1000.0f);
            projectionMatrix[2, 2] = projectionMatrix[2, 2] / 1000.0f;
            projectionMatrix[3, 2] = projectionMatrix[3, 2] / 1000.0f;
        }
        private Matrix CreateView()
        {
			Matrix rotMatrix;
            var up = Vector3.UnitY;
            var look = LookTarget;
            var rot = Rotation * ((float)Math.PI / 180.0f);

            Matrix.RotationYawPitchRoll(rot.Y, rot.X, rot.Z, out rotMatrix);
            Vector3.TransformCoordinate(ref look, ref rotMatrix, out look);
            Vector3.TransformCoordinate(ref up, ref rotMatrix, out up);
            return Matrix.LookAtLH(Position, Position + look, up);
        }
    }
}
