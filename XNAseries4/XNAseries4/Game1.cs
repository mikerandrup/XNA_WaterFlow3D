using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;
using XNAseries4;

//TODO: Refactor this mega class
//TODO: remove shader and other code from tutorial that aren't used

namespace XNASeries4
{
    public struct WaterAndSaturation
    {
        public float waterValue;
        public float saturationValue;
    }

    public struct VertexPositionNormalTexture
    {
        public Vector3 Position;
        public Color Color;
        public Vector3 Normal;

        public static int SizeInBytes = 7 * 4;
        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
              (
                  new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                  new VertexElement(sizeof(float) * 3, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                  new VertexElement(sizeof(float) * 3 + 4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
              );
    }

    public struct VertexMultitextured
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 TextureCoordinate;
        public Vector4 TexWeights;

        public static int SizeInBytes = (3 + 3 + 4 + 4) * sizeof(float);
        public static VertexElement[] VertexElements = new VertexElement[] {
         new VertexElement(  0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0 ),
         new VertexElement(  sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0 ),
         new VertexElement(  sizeof(float) * 6, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0 ),
         new VertexElement(  sizeof(float) * 10, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1 ),
     };
    }

    public class WaterSim : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        GraphicsDevice device;

        int terrainWidth;
        int terrainLength;
        float[,] heightData;

        VertexBuffer landVertexBuffer;
        IndexBuffer sharedIndexBuffer;
        VertexMultitextured[] vertices;
        VertexPositionTexture[] waterVertices;
        int[] indices;

        VertexMultitextured[] landVertices;

        Effect effect;
        Effect bbEffect;

        Matrix viewMatrix;
        Matrix projectionMatrix;
        Matrix reflectionViewMatrix;

        CursorLocate cursorLocate;

        Vector3 cameraPosition = new Vector3(130, 30, -50);
        float leftrightRot = MathHelper.PiOver2;
        float updownRot = -MathHelper.Pi / 10.0f;
        const float rotationSpeed = 0.1f;
        const float moveSpeed = 30.0f;
        MouseState originalMouseState;

        Texture2D grassTexture;
        Texture2D sandTexture;
        Texture2D rockTexture;
        Texture2D snowTexture;
        Texture2D cloudMap;
        Texture2D waterBumpMap;
        Texture2D treeTexture;

        Model skyDome;

        RenderTarget2D refractionRenderTarget;
        Texture2D refractionMap;

        RenderTarget2D reflectionRenderTarget;
        Texture2D reflectionMap;

        Vector3 windDirection = new Vector3(1, 0, 0);

        VertexBuffer waterVertexBuffer;
        float waterExistenceThreshold = 0.0001f;

        WaterAndSaturation[] waterValueModel;

        // config block
        bool WireFramesOnly = false; // overwritten by input state

        float emitterBaseStrength = 0.1f;
        float cursorEmitterStrength;
        float globalEmitterStrength;

        float landMultStrength = 1.10f;
        float uphillDampening = 0.6f;
        float downhillDampening = 0.6f;

        float waterGlobalValue = 0.0f;
        bool drainWaterFromEdges = true;
        bool autoEmmiter = false;
        string terrainTextureName = "tinymap"; // tinymap //"islandmap"; //thankyou, islandmap, rivermap, fractalmap, stairsmap, mazemap, mazemap2, valleymap

        // water stuff
        private void actionDrainWaterAll()
        {
            for (int i = 0; i < landVertices.Length; i++)
            {
                waterValueModel[i].waterValue -= globalEmitterStrength;
            }
        }

        private void actionResetState()
        {
            autoEmmiter = false;

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainLength; y++)
                {
                    int cellIndex = x + y * terrainWidth;
                    landVertices[cellIndex].Position.Y = heightData[x, y];
                }
            }

            device.SetVertexBuffer(null);
            landVertexBuffer.SetData(landVertices);
        }


        private void actionEmitWaterAll()
        {
            for (int i = 0; i < landVertices.Length; i++)
            {
                waterValueModel[i].waterValue += globalEmitterStrength;
            }
        }
        private void actionEmitWaterCursor()
        {

            autoEmmiter = true;


            int cursorSlot = findCursor();
            waterValueModel[cursorSlot].waterValue += cursorEmitterStrength;
            waterValueModel[cursorSlot].saturationValue = Erosion.INITIAL_SATURATION;
        }
        private void actionDrainWaterCursor()
        {
            int cursorSlot = findCursor();
            waterValueModel[cursorSlot].waterValue -= cursorEmitterStrength;
        }
        private void actionEliminateWater()
        {
            for (int i = 0; i < waterValueModel.Length; i++)
            {
                waterValueModel[i].waterValue = 0;
                waterValueModel[i].saturationValue = Erosion.INITIAL_SATURATION;
            }
        }
        private void actionTidalWave()
        {
            for (int i = 0; i < terrainWidth; i++)
            {
                waterValueModel[i].waterValue += globalEmitterStrength * terrainLength;
            }
        }

        // land stuff
        private void actionScaleLandUp()
        {
            for (int i = 0; i < landVertices.Length; i++)
            {
                landVertices[i].Position.Y *= landMultStrength; // scale appropriate to terrain
            }
            device.SetVertexBuffer(null);
            landVertexBuffer.SetData(landVertices);
        }
        private void actionScaleLandDown()
        {
            for (int i = 0; i < landVertices.Length; i++)
            {
                landVertices[i].Position.Y *= 1 / landMultStrength; //reciprocal, yo!
            }
            device.SetVertexBuffer(null);
            landVertexBuffer.SetData(landVertices);
        }
        private void actionEmitLandCursor()
        {

            int cursorSlot = findCursor();
            float effectiveEmitterStrength = cursorEmitterStrength * 0.003f;

            landVertices[cursorSlot].Position.Y += effectiveEmitterStrength; // scale appropriate to terrain

            landVertexBuffer.SetData(landVertices);
        }

        private void toggleWireFramesOnly()
        { // TODO implement better toggle feature through keyboard state
            if (WireFramesOnly) WireFramesOnly = false;
            else WireFramesOnly = true;
        }

        private int findCursor()
        {
            int targetSlot = cursorLocate.xLoc + (cursorLocate.zLoc * terrainLength);
            return (targetSlot);
        }

        public WaterSim()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            IsMouseVisible = false;
            graphics.PreferredBackBufferWidth = 640;
            graphics.PreferredBackBufferHeight = 480;

            //this.graphics.IsFullScreen = true;

            graphics.ApplyChanges();

            //emmitterbase value adjustments
            cursorEmitterStrength = emitterBaseStrength * 20.0f;
            globalEmitterStrength = emitterBaseStrength / 100.0f;

            Window.Title = "Mike Randrup's WaterFlow Sim (built on Riemer's 3D Tutorials & XNA)";

            base.Initialize();
        }

        protected override void LoadContent()
        {
            device = GraphicsDevice;

            effect = Content.Load<Effect>("Series4Effects");
            UpdateViewMatrix();

            viewMatrix = Matrix.CreateLookAt(new Vector3(130, 30, -50), new Vector3(0, 0, -40), new Vector3(0, 1, 0));
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.3f, 1000.0f);

            skyDome = Content.Load<Model>("dome"); skyDome.Meshes[0].MeshParts[0].Effect = effect.Clone();

            PresentationParameters pp = device.PresentationParameters;
            refractionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, pp.DepthStencilFormat);

            reflectionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, pp.DepthStencilFormat);

            Mouse.SetPosition(device.Viewport.Width / 2, device.Viewport.Height / 2);
            originalMouseState = Mouse.GetState();

            bbEffect = Content.Load<Effect>("bbEffect");

            LoadVertices();
            LoadTextures();

            cursorLocate = new CursorLocate(terrainWidth, terrainLength);
        }

        private void LoadVertices()
        {
            Texture2D heightMap = Content.Load<Texture2D>(terrainTextureName);
            LoadHeightData(heightMap);
            VertexMultitextured[] terrainVertices = SetUpLandVertices();
            int[] terrainIndices = SetUpSharedIndices();
            terrainVertices = CalculateNormals(terrainVertices, terrainIndices);
            CopyToTerrainBuffers(terrainVertices, terrainIndices);

            SetUpWaterVertices();
        }

        private void LoadTextures()
        {
            
            grassTexture = 
            rockTexture = 
            snowTexture = 
            treeTexture =
            sandTexture = Content.Load<Texture2D>("beachsand");

            cloudMap = Content.Load<Texture2D>("cloudMap");
            waterBumpMap = Content.Load<Texture2D>("waterbump");
        }

        private void LoadHeightData(Texture2D heightMap)
        {

            terrainWidth = heightMap.Width;
            terrainLength = heightMap.Height;

            Color[] heightMapColors = new Color[terrainWidth * terrainLength];
            heightMap.GetData(heightMapColors);

            heightData = new float[terrainWidth, terrainLength];
            for (int x = 0; x < terrainWidth; x++)
                for (int y = 0; y < terrainLength; y++)
                {
                    heightData[x, y] = (heightMapColors[x + y * terrainWidth].R / 255f) * 30f;
                }

        }

        private VertexMultitextured[] SetUpLandVertices()
        {
            landVertices = new VertexMultitextured[terrainWidth * terrainLength];
            vertices = new VertexMultitextured[terrainWidth * terrainLength];

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainLength; y++)
                {
                    int cellIndex = x + y * terrainWidth;

                    landVertices[cellIndex].Position = new Vector3(x, heightData[x, y], -y);
                    landVertices[cellIndex].TextureCoordinate.X = (float)x / 30.0f;
                    landVertices[cellIndex].TextureCoordinate.Y = (float)y / 30.0f;

                    landVertices[cellIndex].TexWeights.X = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 0) / 8.0f, 0, 1);
                    landVertices[cellIndex].TexWeights.Y = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 12) / 6.0f, 0, 1);
                    landVertices[cellIndex].TexWeights.Z = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 20) / 6.0f, 0, 1);
                    landVertices[cellIndex].TexWeights.W = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 30) / 6.0f, 0, 1);

                    float total = landVertices[cellIndex].TexWeights.X;
                    total += landVertices[cellIndex].TexWeights.Y;
                    total += landVertices[cellIndex].TexWeights.Z;
                    total += landVertices[cellIndex].TexWeights.W;

                    landVertices[cellIndex].TexWeights.X /= total;
                    landVertices[cellIndex].TexWeights.Y /= total;
                    landVertices[cellIndex].TexWeights.Z /= total;
                    landVertices[cellIndex].TexWeights.W /= total;
                }
            }

            return landVertices;
        }

        private int[] SetUpSharedIndices()
        {
            int counter = 0;
            indices = new int[(terrainWidth - 1) * (terrainLength - 1) * 6];

            for (int y = 0; y < terrainLength - 1; y++)
            {
                for (int x = 0; x < terrainWidth - 1; x++)
                {
                    int lowerLeft = x + y * terrainWidth;
                    int lowerRight = (x + 1) + y * terrainWidth;
                    int topLeft = x + (y + 1) * terrainWidth;
                    int topRight = (x + 1) + (y + 1) * terrainWidth;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = lowerLeft;

                    indices[counter++] = topLeft;
                    indices[counter++] = topRight;
                    indices[counter++] = lowerRight;
                }
            }

            return indices;
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

            landVertexBuffer = new VertexBuffer(device, vertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            landVertexBuffer.SetData(vertices.ToArray());

            sharedIndexBuffer = new IndexBuffer(device, typeof(int), indices.Length, BufferUsage.WriteOnly);
            sharedIndexBuffer.SetData(indices);
        }

        private void SetUpWaterVertices()
        {
            waterVertices = new VertexPositionTexture[terrainWidth * terrainLength];
            waterValueModel = new WaterAndSaturation[terrainWidth * terrainLength];

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainLength; y++)
                {
                    waterValueModel[x + y * terrainWidth].waterValue = waterGlobalValue;
                    waterVertices[x + y * terrainWidth] = new VertexPositionTexture(
                        new Vector3(
                            (landVertices[x + y * terrainWidth].Position.X),
                            0.0f, // base value, actually set in update loop
                            (landVertices[x + y * terrainWidth].Position.Z)
                            ),
                        new Vector2(x / terrainWidth, y / terrainLength)
                    );
                }
            }

            waterVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionTexture.VertexDeclaration, waterVertices.Count(), BufferUsage.WriteOnly);

            waterVertexBuffer.SetData(waterVertices);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape)) this.Exit();

            if (keyboardState.IsKeyDown(Keys.OemPeriod)) toggleWireFramesOnly();

            if (keyboardState.IsKeyDown(Keys.R)) actionResetState();

            if (keyboardState.IsKeyDown(Keys.T)) actionEmitWaterCursor();
            if (keyboardState.IsKeyDown(Keys.G)) actionDrainWaterCursor();
            if (keyboardState.IsKeyDown(Keys.N)) actionEliminateWater();
            if (keyboardState.IsKeyDown(Keys.U)) actionDrainWaterAll();
            if (keyboardState.IsKeyDown(Keys.J)) actionEmitWaterAll();
            if (keyboardState.IsKeyDown(Keys.M)) actionTidalWave();

            if (keyboardState.IsKeyDown(Keys.I)) actionScaleLandUp();
            if (keyboardState.IsKeyDown(Keys.K)) actionScaleLandDown();
            if (keyboardState.IsKeyDown(Keys.Y)) actionEmitLandCursor();

            float timeDifference = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            ProcessInput(timeDifference);
            UpdateWaterModel();
            UpdateWaterAndLand();

            if (autoEmmiter) actionEmitWaterCursor();

            base.Update(gameTime);
        }

        private void UpdateWaterModel()
        {
            // this section is to resolve it all
            float cellWater, cellLand, cellTotal;
            int cellSlot;

            float checkWater, checkLand, checkTotal, lowestNeighborValue;
            int checkSlot, lowestNeighborSlot;

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int z = 0; z < terrainLength; z++)
                {
                    lowestNeighborValue = 1000000.0f; // set artificially very high
                    lowestNeighborSlot = -1; // used as a default flag not to process water

                    cellSlot = x + z * terrainWidth;
                    cellWater = waterValueModel[cellSlot].waterValue;
                    cellLand = landVertices[cellSlot].Position.Y;
                    cellTotal = cellWater + cellLand;

                    float waterDelta;
                    float flowDampening; // set every iteration based on choice of behavior

                    float verticalChangeForCell = 0;

                    if (cellWater > waterExistenceThreshold)
                    { // if there is water here to process

                        for (int checkX = -1; checkX <= 1; checkX++)
                        {
                            for (int checkZ = -1; checkZ <= 1; checkZ++)
                            {
                                checkSlot = (checkX + x) + ((z + checkZ) * terrainWidth);
                                if ((checkSlot >= 0) && (checkSlot < (terrainWidth * terrainLength)))
                                { // array bounds check
                                    if (cellSlot != checkSlot)
                                    { // skip the self cell case
                                        checkWater = waterValueModel[checkSlot].waterValue;
                                        checkLand = landVertices[checkSlot].Position.Y;
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
                            if (landVertices[lowestNeighborSlot].Position.Y > cellLand)
                                flowDampening = downhillDampening;
                            else
                                flowDampening = uphillDampening;

                            float idealLevel =
                                (cellTotal +
                                (waterValueModel[lowestNeighborSlot].waterValue + landVertices[lowestNeighborSlot].Position.Y))
                                / 2;
                            idealLevel -= landVertices[cellSlot].Position.Y; // adjust for land presence

                            waterDelta = (cellWater - idealLevel) * flowDampening;

                            waterValueModel[cellSlot].waterValue -= waterDelta;
                            waterValueModel[lowestNeighborSlot].waterValue += waterDelta;
                            cellWater -= waterDelta;

                            float cellSaturationAverage = (waterValueModel[lowestNeighborSlot].saturationValue + waterValueModel[cellSlot].saturationValue) / 2;
                            waterValueModel[lowestNeighborSlot].saturationValue = cellSaturationAverage * 1.5f;
                            waterValueModel[cellSlot].saturationValue = cellSaturationAverage * 0.5f;

                            verticalChangeForCell = cellTotal - (waterValueModel[lowestNeighborSlot].waterValue + landVertices[lowestNeighborSlot].Position.Y);

                            Erosion.PerformErosion(
                                ref waterValueModel[lowestNeighborSlot].waterValue,
                                ref waterValueModel[lowestNeighborSlot].saturationValue,
                                ref landVertices[lowestNeighborSlot].Position.Y,
                                verticalChangeForCell
                            );
                        }
                    }
                }
            }

            if (drainWaterFromEdges)
            {
                for (int x = 0; x < terrainWidth; x++)
                {
                    // top edge
                    waterValueModel[x + 0].waterValue = waterGlobalValue;
                    waterValueModel[x + 0].saturationValue = Erosion.INITIAL_SATURATION;
                    // bottom edge
                    waterValueModel[x + terrainWidth*(terrainLength-1)].waterValue = waterGlobalValue;
                    waterValueModel[x + terrainWidth * (terrainLength - 1)].saturationValue = Erosion.INITIAL_SATURATION;
                }
                for (int z = 0; z < terrainLength; z++)
                {
                    // left edge
                    waterValueModel[0 + z * terrainWidth].waterValue = waterGlobalValue;
                    waterValueModel[0 + z * terrainWidth].saturationValue = Erosion.INITIAL_SATURATION;
                    // right edge
                    waterValueModel[(terrainLength-1) + z * terrainWidth].waterValue = waterGlobalValue;
                    waterValueModel[(terrainLength-1) + z * terrainWidth].saturationValue = Erosion.INITIAL_SATURATION;
                }
            }
        }

        private void ProcessInput(float amount)
        {
            // rotate camera
            MouseState currentMouseState = Mouse.GetState();
            if (currentMouseState != originalMouseState)
            {
                float xDifference = currentMouseState.X - originalMouseState.X;
                float yDifference = currentMouseState.Y - originalMouseState.Y;
                leftrightRot -= rotationSpeed * xDifference * amount;
                updownRot -= rotationSpeed * yDifference * amount;
                Mouse.SetPosition(device.Viewport.Width / 2, device.Viewport.Height / 2);
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

            AddToCameraPosition(moveVector * amount);

            // emitter cursor location
            if (keyState.IsKeyDown(Keys.Up))
                cursorLocate.AlterZ(1);
            if (keyState.IsKeyDown(Keys.Down))
                cursorLocate.AlterZ(-1);
            if (keyState.IsKeyDown(Keys.Right))
                cursorLocate.AlterZ(1);
            if (keyState.IsKeyDown(Keys.Left))
                cursorLocate.AlterX(-1);
            if (keyState.IsKeyDown(Keys.End))
                cursorLocate.ResetToCenter();

        }

        private void AddToCameraPosition(Vector3 vectorToAdd)
        {
            Matrix cameraRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(leftrightRot);
            Vector3 rotatedVector = Vector3.Transform(vectorToAdd, cameraRotation);
            cameraPosition += moveSpeed * rotatedVector;
            UpdateViewMatrix();
        }

        private void UpdateViewMatrix()
        {
            Matrix cameraRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(leftrightRot);

            Vector3 cameraOriginalTarget = new Vector3(0, 0, -1);
            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);

            Vector3 cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            Vector3 cameraFinalTarget = cameraPosition + cameraRotatedTarget;

            Vector3 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            viewMatrix = Matrix.CreateLookAt(cameraPosition, cameraFinalTarget, cameraRotatedUpVector);


            Vector3 reflCameraPosition = cameraPosition;
            reflCameraPosition.Y = -cameraPosition.Y + waterGlobalValue * 2;
            Vector3 reflTargetPos = cameraFinalTarget;
            reflTargetPos.Y = -cameraFinalTarget.Y + waterGlobalValue * 2;

            Vector3 cameraRight = Vector3.Transform(new Vector3(1, 0, 0), cameraRotation);
            Vector3 invUpVector = Vector3.Cross(cameraRight, reflTargetPos - reflCameraPosition);

            reflectionViewMatrix = Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);
        }

        protected override void Draw(GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalMilliseconds / 100.0f;
            RasterizerState rs = new RasterizerState();
            if (WireFramesOnly)
            {
                rs.FillMode = FillMode.WireFrame;
            }
            rs.CullMode = CullMode.None; // CullCounterClockwiseFace;

            device.RasterizerState = rs;

            DrawRefractionMap();
            DrawReflectionMap();

            Color bgColor = new Color(0.94140625f, 0.7421875f, 0.21484375f);

            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, bgColor, 1.0f, 0);

            DrawSkyDome(viewMatrix);

            DrawTerrain(viewMatrix);

            DrawWater(time / 10);

            base.Draw(gameTime);
        }

        private void DrawTerrain(Matrix currentViewMatrix)
        {
            effect.CurrentTechnique = effect.Techniques["MultiTextured"];
            effect.Parameters["xTexture0"].SetValue(sandTexture);
            effect.Parameters["xTexture1"].SetValue(grassTexture);
            effect.Parameters["xTexture2"].SetValue(rockTexture);
            effect.Parameters["xTexture3"].SetValue(snowTexture);

            Matrix worldMatrix = Matrix.Identity;
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xView"].SetValue(currentViewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);

            effect.Parameters["xEnableLighting"].SetValue(true);
            effect.Parameters["xAmbient"].SetValue(0.4f);
            effect.Parameters["xLightDirection"].SetValue(new Vector3(-0.5f, -1, -0.5f));
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.Indices = sharedIndexBuffer;
                device.SetVertexBuffer(landVertexBuffer);

                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, indices.Length / 3);

            }
        }

        private void DrawSkyDome(Matrix currentViewMatrix)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Matrix[] modelTransforms = new Matrix[skyDome.Bones.Count];
            skyDome.CopyAbsoluteBoneTransformsTo(modelTransforms);
            Matrix wMatrix = Matrix.CreateTranslation(0, -0.3f, 0) * Matrix.CreateScale(100) * Matrix.CreateTranslation(cameraPosition);
            foreach (ModelMesh mesh in skyDome.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = modelTransforms[mesh.ParentBone.Index] * wMatrix;
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Textured"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(currentViewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xTexture"].SetValue(cloudMap);
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
            Plane refractionPlane = CreatePlane(waterGlobalValue + 1.5f, new Vector3(0, -1, 0), viewMatrix, false);

            effect.Parameters["ClipPlane0"].SetValue(new Vector4(refractionPlane.Normal, refractionPlane.D));
            effect.Parameters["Clipping"].SetValue(true);    // Allows the geometry to be clipped for the purpose of creating a refraction map
            device.SetRenderTarget(refractionRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            //DrawTerrain(viewMatrix);
            device.SetRenderTarget(null);
            effect.Parameters["Clipping"].SetValue(false);   // Make sure you turn it back off so the whole scene doesnt keep rendering as clipped
            refractionMap = refractionRenderTarget;

        }

        private void DrawReflectionMap()
        {
            Plane reflectionPlane = CreatePlane(waterGlobalValue - 0.5f, new Vector3(0, -1, 0), reflectionViewMatrix, true);

            effect.Parameters["ClipPlane0"].SetValue(new Vector4(reflectionPlane.Normal, reflectionPlane.D));

            effect.Parameters["Clipping"].SetValue(true);    // Allows the geometry to be clipped for the purpose of creating a refraction map
            device.SetRenderTarget(reflectionRenderTarget);


            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            DrawSkyDome(reflectionViewMatrix);
            DrawTerrain(reflectionViewMatrix);


            effect.Parameters["Clipping"].SetValue(false);

            device.SetRenderTarget(null);

            reflectionMap = reflectionRenderTarget;
        }

        private void DrawWater(float time)
        {
            effect.CurrentTechnique = effect.Techniques["Water"];
            Matrix worldMatrix = Matrix.Identity;
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xReflectionView"].SetValue(reflectionViewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xReflectionMap"].SetValue(reflectionMap);
            effect.Parameters["xWaveLength"].SetValue(0.1f);
            effect.Parameters["xWaveHeight"].SetValue(0.3f);
            effect.Parameters["xTime"].SetValue(time);
            effect.Parameters["xWindForce"].SetValue(0.002f);
            effect.Parameters["xWindDirection"].SetValue(windDirection);

            effect.CurrentTechnique.Passes[0].Apply();

            //device.SetVertexBuffer(waterVertexBuffer);
            //device.DrawPrimitives(PrimitiveType.TriangleList, 0, waterVertexBuffer.VertexCount / 3);

            device.Indices = sharedIndexBuffer;
            device.SetVertexBuffer(waterVertexBuffer);

            //BlendState blendState = new BlendState();
            //blendState.AlphaSourceBlend = Blend.One;
            //blendState.AlphaDestinationBlend = Blend.One;
            //blendState.ColorBlendFunction = BlendFunction.Add;
            //device.BlendState = blendState;

            device.BlendState = BlendState.Additive;
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, indices.Length / 3);
            device.BlendState = BlendState.Opaque;
        }

        private void UpdateWaterAndLand()
        {
            float waterModelValue;
            float landModelValue;

            float waterOffset = 0.1f;

            device.SetVertexBuffer(null); // so we can pass modified geometry to it

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainLength; y++)
                {
                    int cellIndex = x + y * terrainWidth;

                    waterModelValue = waterValueModel[cellIndex].waterValue;
                    landModelValue = landVertices[cellIndex].Position.Y;

                    if (waterModelValue > waterExistenceThreshold)
                    {
                        waterVertices[cellIndex].Position.Y = waterModelValue + landModelValue + waterOffset;
                    }
                    else
                    {
                        waterVertices[cellIndex].Position.Y = -1.0f;
                    }
                }
            }

            landVertexBuffer.SetData(landVertices);
            waterVertexBuffer.SetData(waterVertices);
        }


    }
}