using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WaterFlowSim.Control;

namespace WaterFlowSim.WaterFlowCore.Control
{
    class CameraManager
    {
        GeometryAndSettings _geometryAndSettings;

        public CameraManager(GeometryAndSettings geometryAndSettings)
        {
            _geometryAndSettings = geometryAndSettings;
        }


        public void ProcessInput(float timeDelta)
        {
            // rotate camera
            MouseState currentMouseState = Mouse.GetState();
            if (currentMouseState != _geometryAndSettings.originalMouseState)
            {
                float xDifference = currentMouseState.X - _geometryAndSettings.originalMouseState.X;
                float yDifference = currentMouseState.Y - _geometryAndSettings.originalMouseState.Y;
                _geometryAndSettings.leftrightRot -= GeometryAndSettings.rotationSpeed * xDifference * timeDelta;
                _geometryAndSettings.updownRot -= GeometryAndSettings.rotationSpeed * yDifference * timeDelta;
                Mouse.SetPosition(_geometryAndSettings.device.Viewport.Width / 2, _geometryAndSettings.device.Viewport.Height / 2);
                UpdateViewMatrix();
            }

            // move camera
            Vector3 moveVector = new Vector3(0, 0, 0);
            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.W))
                moveVector += new Vector3(0, 0, -1);
            if (keyState.IsKeyDown(Keys.S))
                moveVector += new Vector3(0, 0, 1);
            if (keyState.IsKeyDown(Keys.D))
                moveVector += new Vector3(1, 0, 0);
            if (keyState.IsKeyDown(Keys.A))
                moveVector += new Vector3(-1, 0, 0);
            if (keyState.IsKeyDown(Keys.Q))
                moveVector += new Vector3(0, 1, 0);
            if (keyState.IsKeyDown(Keys.Z))
                moveVector += new Vector3(0, -1, 0);

            AddToCameraPosition(moveVector * timeDelta);

            // emitter cursor location
            if (keyState.IsKeyDown(Keys.Up))
                CursorLocate.AlterZ(1);
            if (keyState.IsKeyDown(Keys.Down))
                CursorLocate.AlterZ(-1);
            if (keyState.IsKeyDown(Keys.Right))
                CursorLocate.AlterZ(1);
            if (keyState.IsKeyDown(Keys.Left))
                CursorLocate.AlterX(-1);
            if (keyState.IsKeyDown(Keys.End))
                CursorLocate.ResetToCenter();
        }

        private void AddToCameraPosition(Vector3 vectorToAdd)
        {
            Matrix cameraRotation = Matrix.CreateRotationX(_geometryAndSettings.updownRot) * Matrix.CreateRotationY(_geometryAndSettings.leftrightRot);
            Vector3 rotatedVector = Vector3.Transform(vectorToAdd, cameraRotation);
            _geometryAndSettings.cameraPosition += GeometryAndSettings.moveSpeed * rotatedVector;
            UpdateViewMatrix();
        }

        public void UpdateViewMatrix()
        {
            Matrix cameraRotation = Matrix.CreateRotationX(_geometryAndSettings.updownRot) * Matrix.CreateRotationY(_geometryAndSettings.leftrightRot);

            Vector3 cameraOriginalTarget = new Vector3(0, 0, -1);
            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);

            Vector3 cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            Vector3 cameraFinalTarget = _geometryAndSettings.cameraPosition + cameraRotatedTarget;

            Vector3 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            _geometryAndSettings.viewMatrix = Matrix.CreateLookAt(_geometryAndSettings.cameraPosition, cameraFinalTarget, cameraRotatedUpVector);


            Vector3 reflCameraPosition = _geometryAndSettings.cameraPosition;
            reflCameraPosition.Y = -_geometryAndSettings.cameraPosition.Y + _geometryAndSettings.waterGlobalValue * 2;
            Vector3 reflTargetPos = cameraFinalTarget;
            reflTargetPos.Y = -cameraFinalTarget.Y + _geometryAndSettings.waterGlobalValue * 2;

            Vector3 cameraRight = Vector3.Transform(new Vector3(1, 0, 0), cameraRotation);
            Vector3 invUpVector = Vector3.Cross(cameraRight, reflTargetPos - reflCameraPosition);

            _geometryAndSettings.reflectionViewMatrix = Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);
        }
    }
}
