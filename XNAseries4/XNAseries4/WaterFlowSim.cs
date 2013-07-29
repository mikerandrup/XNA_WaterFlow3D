using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WaterFlowSim.WaterFlowCore;
using WaterFlowSim.WaterFlowCore.Control;
using WaterFlowSim.Control;

//TODO: Refactor this mega class
//TODO: remove shader and other code from tutorial that aren't used

namespace WaterFlowSim
{
    public class WaterSim : Microsoft.Xna.Framework.Game
    {
        GeometryAndSettings _geometryAndSettings;
        WaterControl _waterControl;
        LandControl _landControl;
        RenderControl _renderControl;
        CameraManager _cameraManager;

        public WaterSim()
        {
            _geometryAndSettings = new GeometryAndSettings()
            {
                graphics = new GraphicsDeviceManager(this)
            };
            
            _waterControl = new WaterControl(_geometryAndSettings);
            _landControl = new LandControl(_geometryAndSettings);
            _renderControl = new RenderControl(_geometryAndSettings);
            _cameraManager = new CameraManager(_geometryAndSettings);

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            IsMouseVisible = false;
            _geometryAndSettings.graphics.PreferredBackBufferWidth = 640;
            _geometryAndSettings.graphics.PreferredBackBufferHeight = 480;

            //this.graphics.IsFullScreen = true;

            _geometryAndSettings.graphics.ApplyChanges();

            //emmitterbase value adjustments
            _geometryAndSettings.cursorEmitterStrength = _geometryAndSettings.emitterBaseStrength * 20.0f;
            _geometryAndSettings.globalEmitterStrength = _geometryAndSettings.emitterBaseStrength / 100.0f;

            Window.Title = "Mike Randrup's WaterFlow Sim (built on Riemer's 3D Tutorials & XNA)";

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _geometryAndSettings.device = GraphicsDevice;

            _geometryAndSettings.effect = Content.Load<Effect>("shaders/Series4Effects");
            _cameraManager.UpdateViewMatrix();

            _geometryAndSettings.viewMatrix = Matrix.CreateLookAt(new Vector3(130, 30, -50), new Vector3(0, 0, -40), new Vector3(0, 1, 0));
            _geometryAndSettings.projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, _geometryAndSettings.device.Viewport.AspectRatio, 0.3f, 1000.0f);

            _geometryAndSettings.skyDome = Content.Load<Model>("geometry/dome"); _geometryAndSettings.skyDome.Meshes[0].MeshParts[0].Effect = _geometryAndSettings.effect.Clone();

            PresentationParameters pp = _geometryAndSettings.device.PresentationParameters;
            _geometryAndSettings.refractionRenderTarget = new RenderTarget2D(_geometryAndSettings.device, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, pp.DepthStencilFormat);

            _geometryAndSettings.reflectionRenderTarget = new RenderTarget2D(_geometryAndSettings.device, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, pp.DepthStencilFormat);

            Mouse.SetPosition(_geometryAndSettings.device.Viewport.Width / 2, _geometryAndSettings.device.Viewport.Height / 2);
            _geometryAndSettings.originalMouseState = Mouse.GetState();

            _geometryAndSettings.bbEffect = Content.Load<Effect>(@"shaders\bbEffect");

            LoadVertices();
            LoadTextures();

            CursorLocate.Initialize(_geometryAndSettings.terrainWidth, _geometryAndSettings.terrainLength);
        }

        private void LoadVertices()
        {
            Texture2D heightMap = Content.Load<Texture2D>(_geometryAndSettings.terrainTextureName);
            LoadHeightData(heightMap);
            VertexMultitextured[] terrainVertices = SetUpLandVertices();
            int[] terrainIndices = SetUpSharedIndices();
            terrainVertices = CalculateNormals(terrainVertices, terrainIndices);
            CopyToTerrainBuffers(terrainVertices, terrainIndices);

            SetUpWaterVertices();
        }

        private void LoadTextures()
        {

            _geometryAndSettings.grassTexture =
            _geometryAndSettings.rockTexture =
            _geometryAndSettings.snowTexture =
            _geometryAndSettings.treeTexture =
            _geometryAndSettings.sandTexture = Content.Load<Texture2D>("textures/beachsand");

            _geometryAndSettings.cloudMap = Content.Load<Texture2D>("textures/cloudMap");
            _geometryAndSettings.waterBumpMap = Content.Load<Texture2D>("textures/waterbump");
        }

        private void LoadHeightData(Texture2D heightMap)
        {

            _geometryAndSettings.terrainWidth = heightMap.Width;
            _geometryAndSettings.terrainLength = heightMap.Height;

            Color[] heightMapColors = new Color[_geometryAndSettings.terrainWidth * _geometryAndSettings.terrainLength];
            heightMap.GetData(heightMapColors);

            _geometryAndSettings.heightData = new float[_geometryAndSettings.terrainWidth, _geometryAndSettings.terrainLength];
            for (int x = 0; x < _geometryAndSettings.terrainWidth; x++)
                for (int y = 0; y < _geometryAndSettings.terrainLength; y++)
                {
                    _geometryAndSettings.heightData[x, y] = (heightMapColors[x + y * _geometryAndSettings.terrainWidth].R / 255f) * 30f;
                }

        }

        private VertexMultitextured[] SetUpLandVertices()
        {
            _geometryAndSettings.landVertices = new VertexMultitextured[_geometryAndSettings.terrainWidth * _geometryAndSettings.terrainLength];
            _geometryAndSettings.vertices = new VertexMultitextured[_geometryAndSettings.terrainWidth * _geometryAndSettings.terrainLength];

            for (int x = 0; x < _geometryAndSettings.terrainWidth; x++)
            {
                for (int y = 0; y < _geometryAndSettings.terrainLength; y++)
                {
                    int cellIndex = x + y * _geometryAndSettings.terrainWidth;

                    _geometryAndSettings.landVertices[cellIndex].Position = new Vector3(x, _geometryAndSettings.heightData[x, y], -y);
                    _geometryAndSettings.landVertices[cellIndex].TextureCoordinate.X = (float)x / 30.0f;
                    _geometryAndSettings.landVertices[cellIndex].TextureCoordinate.Y = (float)y / 30.0f;

                    _geometryAndSettings.landVertices[cellIndex].TexWeights.X = MathHelper.Clamp(1.0f - Math.Abs(_geometryAndSettings.heightData[x, y] - 0) / 8.0f, 0, 1);
                    _geometryAndSettings.landVertices[cellIndex].TexWeights.Y = MathHelper.Clamp(1.0f - Math.Abs(_geometryAndSettings.heightData[x, y] - 12) / 6.0f, 0, 1);
                    _geometryAndSettings.landVertices[cellIndex].TexWeights.Z = MathHelper.Clamp(1.0f - Math.Abs(_geometryAndSettings.heightData[x, y] - 20) / 6.0f, 0, 1);
                    _geometryAndSettings.landVertices[cellIndex].TexWeights.W = MathHelper.Clamp(1.0f - Math.Abs(_geometryAndSettings.heightData[x, y] - 30) / 6.0f, 0, 1);

                    float total = _geometryAndSettings.landVertices[cellIndex].TexWeights.X;
                    total += _geometryAndSettings.landVertices[cellIndex].TexWeights.Y;
                    total += _geometryAndSettings.landVertices[cellIndex].TexWeights.Z;
                    total += _geometryAndSettings.landVertices[cellIndex].TexWeights.W;

                    _geometryAndSettings.landVertices[cellIndex].TexWeights.X /= total;
                    _geometryAndSettings.landVertices[cellIndex].TexWeights.Y /= total;
                    _geometryAndSettings.landVertices[cellIndex].TexWeights.Z /= total;
                    _geometryAndSettings.landVertices[cellIndex].TexWeights.W /= total;
                }
            }

            return _geometryAndSettings.landVertices;
        }

        private int[] SetUpSharedIndices()
        {
            int counter = 0;
            _geometryAndSettings.sharedIndices = new int[(_geometryAndSettings.terrainWidth - 1) * (_geometryAndSettings.terrainLength - 1) * 6];

            for (int y = 0; y < _geometryAndSettings.terrainLength - 1; y++)
            {
                for (int x = 0; x < _geometryAndSettings.terrainWidth - 1; x++)
                {
                    int lowerLeft = x + y * _geometryAndSettings.terrainWidth;
                    int lowerRight = (x + 1) + y * _geometryAndSettings.terrainWidth;
                    int topLeft = x + (y + 1) * _geometryAndSettings.terrainWidth;
                    int topRight = (x + 1) + (y + 1) * _geometryAndSettings.terrainWidth;

                    _geometryAndSettings.sharedIndices[counter++] = topLeft;
                    _geometryAndSettings.sharedIndices[counter++] = lowerRight;
                    _geometryAndSettings.sharedIndices[counter++] = lowerLeft;

                    _geometryAndSettings.sharedIndices[counter++] = topLeft;
                    _geometryAndSettings.sharedIndices[counter++] = topRight;
                    _geometryAndSettings.sharedIndices[counter++] = lowerRight;
                }
            }

            return _geometryAndSettings.sharedIndices;
        }

        private VertexMultitextured[] CalculateNormals(VertexMultitextured[] vertices, int[] indices)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal = new Vector3(0, 0, 0);

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int index1 = indices[i * 3];
                int index2 = indices[i * 3 + 1];
                int index3 = indices[i * 3 + 2];

                Vector3 side1 = vertices[index1].Position - vertices[index3].Position;
                Vector3 side2 = vertices[index1].Position - vertices[index2].Position;
                Vector3 normal = Vector3.Cross(side1, side2);

                vertices[index1].Normal += normal;
                vertices[index2].Normal += normal;
                vertices[index3].Normal += normal;
            }

            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal.Normalize();

            return vertices;
        }

        private void CopyToTerrainBuffers(VertexMultitextured[] vertices, int[] indices)
        {

            VertexDeclaration vertexDeclaration = new VertexDeclaration(VertexMultitextured.VertexElements);

            _geometryAndSettings.landVertexBuffer = new VertexBuffer(_geometryAndSettings.device, vertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            _geometryAndSettings.landVertexBuffer.SetData(vertices.ToArray());

            _geometryAndSettings.sharedIndexBuffer = new IndexBuffer(_geometryAndSettings.device, typeof(int), indices.Length, BufferUsage.WriteOnly);
            _geometryAndSettings.sharedIndexBuffer.SetData(indices);
        }

        private void SetUpWaterVertices()
        {
            _geometryAndSettings.waterVertices = new VertexPositionTexture[_geometryAndSettings.terrainWidth * _geometryAndSettings.terrainLength];
            _geometryAndSettings.waterValueModel = new WaterAndSaturation[_geometryAndSettings.terrainWidth * _geometryAndSettings.terrainLength];

            for (int x = 0; x < _geometryAndSettings.terrainWidth; x++)
            {
                for (int y = 0; y < _geometryAndSettings.terrainLength; y++)
                {
                    _geometryAndSettings.waterValueModel[x + y * _geometryAndSettings.terrainWidth].waterValue = _geometryAndSettings.waterGlobalValue;
                    _geometryAndSettings.waterVertices[x + y * _geometryAndSettings.terrainWidth] = new VertexPositionTexture(
                        new Vector3(
                            (_geometryAndSettings.landVertices[x + y * _geometryAndSettings.terrainWidth].Position.X),
                            0.0f, // base value, actually set in update loop
                            (_geometryAndSettings.landVertices[x + y * _geometryAndSettings.terrainWidth].Position.Z)
                            ),
                        new Vector2(x / _geometryAndSettings.terrainWidth, y / _geometryAndSettings.terrainLength)
                    );
                }
            }

            _geometryAndSettings.waterVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionTexture.VertexDeclaration, _geometryAndSettings.waterVertices.Count(), BufferUsage.WriteOnly);

            _geometryAndSettings.waterVertexBuffer.SetData(_geometryAndSettings.waterVertices);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape)) this.Exit();

            if (keyboardState.IsKeyDown(Keys.OemPeriod)) _renderControl.toggleWireFramesOnly();

            if (keyboardState.IsKeyDown(Keys.R)) _waterControl.actionResetState();

            if (keyboardState.IsKeyDown(Keys.T)) _waterControl.actionEmitWaterCursor();
            if (keyboardState.IsKeyDown(Keys.G)) _waterControl.actionDrainWaterCursor();
            if (keyboardState.IsKeyDown(Keys.N)) _waterControl.actionEliminateWater();
            if (keyboardState.IsKeyDown(Keys.U)) _waterControl.actionDrainWaterAll();
            if (keyboardState.IsKeyDown(Keys.J)) _waterControl.actionEmitWaterAll();
            if (keyboardState.IsKeyDown(Keys.M)) _waterControl.actionTidalWave();

            if (keyboardState.IsKeyDown(Keys.I)) _landControl.actionScaleLandUp();
            if (keyboardState.IsKeyDown(Keys.K)) _landControl.actionScaleLandDown();
            if (keyboardState.IsKeyDown(Keys.Y)) _landControl.actionEmitLandCursor();

            float timeDifference = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            _cameraManager.ProcessInput(timeDifference);
            UpdateWaterModel();
            UpdateWaterAndLand();

            if (_geometryAndSettings.autoEmmiter) _waterControl.actionEmitWaterCursor();

            base.Update(gameTime);
        }

        private void UpdateWaterModel()
        {
            // this section is to resolve it all
            float cellWater, cellLand, cellTotal;
            int cellSlot;

            float checkWater, checkLand, checkTotal, lowestNeighborValue;
            int checkSlot, lowestNeighborSlot;

            for (int x = 0; x < _geometryAndSettings.terrainWidth; x++)
            {
                for (int z = 0; z < _geometryAndSettings.terrainLength; z++)
                {
                    lowestNeighborValue = 1000000.0f; // set artificially very high
                    lowestNeighborSlot = -1; // used as a default flag not to process water

                    cellSlot = x + z * _geometryAndSettings.terrainWidth;
                    cellWater = _geometryAndSettings.waterValueModel[cellSlot].waterValue;
                    cellLand = _geometryAndSettings.landVertices[cellSlot].Position.Y;
                    cellTotal = cellWater + cellLand;

                    float waterDelta;
                    float flowDampening; // set every iteration based on choice of behavior

                    float verticalChangeForCell = 0;

                    if (cellWater > _geometryAndSettings.waterExistenceThreshold)
                    { // if there is water here to process

                        for (int checkX = -1; checkX <= 1; checkX++)
                        {
                            for (int checkZ = -1; checkZ <= 1; checkZ++)
                            {
                                checkSlot = (checkX + x) + ((z + checkZ) * _geometryAndSettings.terrainWidth);
                                if ((checkSlot >= 0) && (checkSlot < (_geometryAndSettings.terrainWidth * _geometryAndSettings.terrainLength)))
                                { // array bounds check
                                    if (cellSlot != checkSlot)
                                    { // skip the self cell case
                                        checkWater = _geometryAndSettings.waterValueModel[checkSlot].waterValue;
                                        checkLand = _geometryAndSettings.landVertices[checkSlot].Position.Y;
                                        checkTotal = checkWater + checkLand;
                                        if (checkTotal <= cellTotal)
                                        {
                                            if (lowestNeighborValue > checkTotal)
                                            {
                                                lowestNeighborValue = checkTotal;
                                                lowestNeighborSlot = checkSlot;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (lowestNeighborSlot > -1)
                        { // we found a lower neighbor in our pass
                            if (_geometryAndSettings.landVertices[lowestNeighborSlot].Position.Y > cellLand)
                                flowDampening = _geometryAndSettings.downhillDampening;
                            else
                                flowDampening = _geometryAndSettings.uphillDampening;

                            float idealLevel =
                                (cellTotal +
                                (_geometryAndSettings.waterValueModel[lowestNeighborSlot].waterValue + _geometryAndSettings.landVertices[lowestNeighborSlot].Position.Y))
                                / 2;
                            idealLevel -= _geometryAndSettings.landVertices[cellSlot].Position.Y; // adjust for land presence

                            waterDelta = (cellWater - idealLevel) * flowDampening;

                            _geometryAndSettings.waterValueModel[cellSlot].waterValue -= waterDelta;
                            _geometryAndSettings.waterValueModel[lowestNeighborSlot].waterValue += waterDelta;
                            cellWater -= waterDelta;

                            float cellSaturationAverage = (_geometryAndSettings.waterValueModel[lowestNeighborSlot].saturationValue + _geometryAndSettings.waterValueModel[cellSlot].saturationValue) / 2;
                            _geometryAndSettings.waterValueModel[lowestNeighborSlot].saturationValue = cellSaturationAverage * 1.5f;
                            _geometryAndSettings.waterValueModel[cellSlot].saturationValue = cellSaturationAverage * 0.5f;

                            verticalChangeForCell = cellTotal - (_geometryAndSettings.waterValueModel[lowestNeighborSlot].waterValue + _geometryAndSettings.landVertices[lowestNeighborSlot].Position.Y);

                            Erosion.PerformErosion(
                                ref _geometryAndSettings.waterValueModel[lowestNeighborSlot].waterValue,
                                ref _geometryAndSettings.waterValueModel[lowestNeighborSlot].saturationValue,
                                ref _geometryAndSettings.landVertices[lowestNeighborSlot].Position.Y,
                                verticalChangeForCell
                            );
                        }
                    }
                }
            }

            if (_geometryAndSettings.drainWaterFromEdges)
            {
                for (int x = 0; x < _geometryAndSettings.terrainWidth; x++)
                {
                    // top edge
                    _geometryAndSettings.waterValueModel[x + 0].waterValue = _geometryAndSettings.waterGlobalValue;
                    _geometryAndSettings.waterValueModel[x + 0].saturationValue = Erosion.INITIAL_SATURATION;
                    // bottom edge
                    _geometryAndSettings.waterValueModel[x + _geometryAndSettings.terrainWidth * (_geometryAndSettings.terrainLength - 1)].waterValue = _geometryAndSettings.waterGlobalValue;
                    _geometryAndSettings.waterValueModel[x + _geometryAndSettings.terrainWidth * (_geometryAndSettings.terrainLength - 1)].saturationValue = Erosion.INITIAL_SATURATION;
                }
                for (int z = 0; z < _geometryAndSettings.terrainLength; z++)
                {
                    // left edge
                    _geometryAndSettings.waterValueModel[0 + z * _geometryAndSettings.terrainWidth].waterValue = _geometryAndSettings.waterGlobalValue;
                    _geometryAndSettings.waterValueModel[0 + z * _geometryAndSettings.terrainWidth].saturationValue = Erosion.INITIAL_SATURATION;
                    // right edge
                    _geometryAndSettings.waterValueModel[(_geometryAndSettings.terrainLength - 1) + z * _geometryAndSettings.terrainWidth].waterValue = _geometryAndSettings.waterGlobalValue;
                    _geometryAndSettings.waterValueModel[(_geometryAndSettings.terrainLength - 1) + z * _geometryAndSettings.terrainWidth].saturationValue = Erosion.INITIAL_SATURATION;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalMilliseconds / 100.0f;
            RasterizerState rs = new RasterizerState();
            if (_geometryAndSettings.WireFramesOnly)
            {
                rs.FillMode = FillMode.WireFrame;
            }
            rs.CullMode = CullMode.None; // CullCounterClockwiseFace;

            _geometryAndSettings.device.RasterizerState = rs;

            DrawRefractionMap();
            DrawReflectionMap();

            Color bgColor = new Color(0.94140625f, 0.7421875f, 0.21484375f);

            _geometryAndSettings.device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, bgColor, 1.0f, 0);

            DrawSkyDome(_geometryAndSettings.viewMatrix);

            DrawTerrain(_geometryAndSettings.viewMatrix);

            DrawWater(time / 10);

            base.Draw(gameTime);
        }

        private void DrawTerrain(Matrix currentViewMatrix)
        {
            _geometryAndSettings.effect.CurrentTechnique = _geometryAndSettings.effect.Techniques["MultiTextured"];
            _geometryAndSettings.effect.Parameters["xTexture0"].SetValue(_geometryAndSettings.sandTexture);
            _geometryAndSettings.effect.Parameters["xTexture1"].SetValue(_geometryAndSettings.grassTexture);
            _geometryAndSettings.effect.Parameters["xTexture2"].SetValue(_geometryAndSettings.rockTexture);
            _geometryAndSettings.effect.Parameters["xTexture3"].SetValue(_geometryAndSettings.snowTexture);

            Matrix worldMatrix = Matrix.Identity;
            _geometryAndSettings.effect.Parameters["xWorld"].SetValue(worldMatrix);
            _geometryAndSettings.effect.Parameters["xView"].SetValue(currentViewMatrix);
            _geometryAndSettings.effect.Parameters["xProjection"].SetValue(_geometryAndSettings.projectionMatrix);

            _geometryAndSettings.effect.Parameters["xEnableLighting"].SetValue(true);
            _geometryAndSettings.effect.Parameters["xAmbient"].SetValue(0.4f);
            _geometryAndSettings.effect.Parameters["xLightDirection"].SetValue(new Vector3(-0.5f, -1, -0.5f));
            foreach (EffectPass pass in _geometryAndSettings.effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                _geometryAndSettings.device.Indices = _geometryAndSettings.sharedIndexBuffer;
                _geometryAndSettings.device.SetVertexBuffer(_geometryAndSettings.landVertexBuffer);

                _geometryAndSettings.device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _geometryAndSettings.vertices.Length, 0, _geometryAndSettings.sharedIndices.Length / 3);

            }
        }

        private void DrawSkyDome(Matrix currentViewMatrix)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Matrix[] modelTransforms = new Matrix[_geometryAndSettings.skyDome.Bones.Count];
            _geometryAndSettings.skyDome.CopyAbsoluteBoneTransformsTo(modelTransforms);
            Matrix wMatrix = Matrix.CreateTranslation(0, -0.3f, 0) * Matrix.CreateScale(100) * Matrix.CreateTranslation(_geometryAndSettings.cameraPosition);
            foreach (ModelMesh mesh in _geometryAndSettings.skyDome.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = modelTransforms[mesh.ParentBone.Index] * wMatrix;
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Textured"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(currentViewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(_geometryAndSettings.projectionMatrix);
                    currentEffect.Parameters["xTexture"].SetValue(_geometryAndSettings.cloudMap);
                    currentEffect.Parameters["xEnableLighting"].SetValue(false);
                }
                mesh.Draw();
            }
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        private Plane CreatePlane(float height, Vector3 planeNormalDirection, Matrix currentViewMatrix, bool clipSide)
        {
            planeNormalDirection.Normalize();
            Vector4 planeCoeffs = new Vector4(planeNormalDirection, height);
            if (clipSide) planeCoeffs *= -1;
            Plane finalPlane = new Plane(planeCoeffs);
            return finalPlane;
        }

        private void DrawRefractionMap()
        {
            Plane refractionPlane = CreatePlane(_geometryAndSettings.waterGlobalValue + 1.5f, new Vector3(0, -1, 0), _geometryAndSettings.viewMatrix, false);

            _geometryAndSettings.effect.Parameters["ClipPlane0"].SetValue(new Vector4(refractionPlane.Normal, refractionPlane.D));
            _geometryAndSettings.effect.Parameters["Clipping"].SetValue(true);    // Allows the geometry to be clipped for the purpose of creating a refraction map
            _geometryAndSettings.device.SetRenderTarget(_geometryAndSettings.refractionRenderTarget);
            _geometryAndSettings.device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            //DrawTerrain(viewMatrix);
            _geometryAndSettings.device.SetRenderTarget(null);
            _geometryAndSettings.effect.Parameters["Clipping"].SetValue(false);   // Make sure you turn it back off so the whole scene doesnt keep rendering as clipped
            _geometryAndSettings.refractionMap = _geometryAndSettings.refractionRenderTarget;

        }

        private void DrawReflectionMap()
        {
            Plane reflectionPlane = CreatePlane(_geometryAndSettings.waterGlobalValue - 0.5f, new Vector3(0, -1, 0), _geometryAndSettings.reflectionViewMatrix, true);

            _geometryAndSettings.effect.Parameters["ClipPlane0"].SetValue(new Vector4(reflectionPlane.Normal, reflectionPlane.D));

            _geometryAndSettings.effect.Parameters["Clipping"].SetValue(true);    // Allows the geometry to be clipped for the purpose of creating a refraction map
            _geometryAndSettings.device.SetRenderTarget(_geometryAndSettings.reflectionRenderTarget);


            _geometryAndSettings.device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            DrawSkyDome(_geometryAndSettings.reflectionViewMatrix);
            DrawTerrain(_geometryAndSettings.reflectionViewMatrix);


            _geometryAndSettings.effect.Parameters["Clipping"].SetValue(false);

            _geometryAndSettings.device.SetRenderTarget(null);

            _geometryAndSettings.reflectionMap = _geometryAndSettings.reflectionRenderTarget;
        }

        private void DrawWater(float time)
        {
            _geometryAndSettings.effect.CurrentTechnique = _geometryAndSettings.effect.Techniques["Water"];
            Matrix worldMatrix = Matrix.Identity;
            _geometryAndSettings.effect.Parameters["xWorld"].SetValue(worldMatrix);
            _geometryAndSettings.effect.Parameters["xView"].SetValue(_geometryAndSettings.viewMatrix);
            _geometryAndSettings.effect.Parameters["xReflectionView"].SetValue(_geometryAndSettings.reflectionViewMatrix);
            _geometryAndSettings.effect.Parameters["xProjection"].SetValue(_geometryAndSettings.projectionMatrix);
            _geometryAndSettings.effect.Parameters["xReflectionMap"].SetValue(_geometryAndSettings.reflectionMap);
            _geometryAndSettings.effect.Parameters["xWaveLength"].SetValue(0.1f);
            _geometryAndSettings.effect.Parameters["xWaveHeight"].SetValue(0.3f);
            _geometryAndSettings.effect.Parameters["xTime"].SetValue(time);
            _geometryAndSettings.effect.Parameters["xWindForce"].SetValue(0.002f);
            _geometryAndSettings.effect.Parameters["xWindDirection"].SetValue(_geometryAndSettings.windDirection);

            _geometryAndSettings.effect.CurrentTechnique.Passes[0].Apply();

            //device.SetVertexBuffer(waterVertexBuffer);
            //device.DrawPrimitives(PrimitiveType.TriangleList, 0, waterVertexBuffer.VertexCount / 3);

            _geometryAndSettings.device.Indices = _geometryAndSettings.sharedIndexBuffer;
            _geometryAndSettings.device.SetVertexBuffer(_geometryAndSettings.waterVertexBuffer);

            //BlendState blendState = new BlendState();
            //blendState.AlphaSourceBlend = Blend.One;
            //blendState.AlphaDestinationBlend = Blend.One;
            //blendState.ColorBlendFunction = BlendFunction.Add;
            //device.BlendState = blendState;

            _geometryAndSettings.device.BlendState = BlendState.Additive;
            _geometryAndSettings.device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _geometryAndSettings.vertices.Length, 0, _geometryAndSettings.sharedIndices.Length / 3);
            _geometryAndSettings.device.BlendState = BlendState.Opaque;
        }

        private void UpdateWaterAndLand()
        {
            float waterModelValue;
            float landModelValue;

            float waterOffset = 0.1f;

            _geometryAndSettings.device.SetVertexBuffer(null); // so we can pass modified geometry to it

            for (int x = 0; x < _geometryAndSettings.terrainWidth; x++)
            {
                for (int y = 0; y < _geometryAndSettings.terrainLength; y++)
                {
                    int cellIndex = x + y * _geometryAndSettings.terrainWidth;

                    waterModelValue = _geometryAndSettings.waterValueModel[cellIndex].waterValue;
                    landModelValue = _geometryAndSettings.landVertices[cellIndex].Position.Y;

                    if (waterModelValue > _geometryAndSettings.waterExistenceThreshold)
                    {
                        _geometryAndSettings.waterVertices[cellIndex].Position.Y = waterModelValue + landModelValue + waterOffset;
                    }
                    else
                    {
                        _geometryAndSettings.waterVertices[cellIndex].Position.Y = -1.0f;
                    }
                }
            }

            _geometryAndSettings.landVertexBuffer.SetData(_geometryAndSettings.landVertices);
            _geometryAndSettings.waterVertexBuffer.SetData(_geometryAndSettings.waterVertices);
        }

    }
}