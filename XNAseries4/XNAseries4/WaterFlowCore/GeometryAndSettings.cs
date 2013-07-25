using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WaterFlowSim.Control;

namespace WaterFlowSim.WaterFlowCore
{
    public class GeometryAndSettings
    {
        public GraphicsDeviceManager graphics;
        public GraphicsDevice device;

        public int terrainWidth;
        public int terrainLength;
        public float[,] heightData;

        public VertexBuffer landVertexBuffer;
        public IndexBuffer sharedIndexBuffer;
        public VertexMultitextured[] vertices;
        public VertexPositionTexture[] waterVertices;
        public int[] sharedIndices;

        public VertexMultitextured[] landVertices;

        public Effect effect;
        public Effect bbEffect;

        public Matrix viewMatrix;
        public Matrix projectionMatrix;
        public Matrix reflectionViewMatrix;

        public Vector3 cameraPosition = new Vector3(130, 30, -50);
        public float leftrightRot = MathHelper.PiOver2;
        public float updownRot = -MathHelper.Pi / 10.0f;
        public const float rotationSpeed = 0.1f;
        public const float moveSpeed = 30.0f;
        public MouseState originalMouseState;

        public Texture2D grassTexture;
        public Texture2D sandTexture;
        public Texture2D rockTexture;
        public Texture2D snowTexture;
        public Texture2D cloudMap;
        public Texture2D waterBumpMap;
        public Texture2D treeTexture;

        public Model skyDome;

        public RenderTarget2D refractionRenderTarget;
        public Texture2D refractionMap;

        public RenderTarget2D reflectionRenderTarget;
        public Texture2D reflectionMap;

        public Vector3 windDirection = new Vector3(1, 0, 0);

        public VertexBuffer waterVertexBuffer;
        public float waterExistenceThreshold = 0.0001f;

        public WaterAndSaturation[] waterValueModel;

        // config block
        public bool WireFramesOnly = false; // overwritten by input state

        public float emitterBaseStrength = 0.1f;
        public float cursorEmitterStrength;
        public float globalEmitterStrength;

        public float landMultStrength = 1.10f;
        public float uphillDampening = 0.6f;
        public float downhillDampening = 0.6f;

        public float waterGlobalValue = 0.0f;
        public bool drainWaterFromEdges = true;
        public bool autoEmmiter = false;
        public string terrainTextureName = @"heightmaps\tinymap"; // tinymap //"islandmap"; //thankyou, islandmap, rivermap, fractalmap, stairsmap, mazemap, mazemap2, valleymap

        public int findCursor()
        {
            int targetSlot = CursorLocate.xLoc + (CursorLocate.zLoc * terrainLength);
            return (targetSlot);
        }
    }
}
