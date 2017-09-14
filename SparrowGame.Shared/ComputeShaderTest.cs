
using System;
using System.Runtime.InteropServices;
using Sparrow.Display;
using Sparrow.ResourceLoading;
using Sparrow.Textures;
using Sparrow.Touches;
using Sparrow.Rendering;
using OpenGL;

namespace SparrowGame.Shared
{
    public class ComputeShaderTest : Quad
    {

        private readonly uint _rayProgram;
        private const int TexW = 512;
        private const int TexH = 512;
        private int _locX;
        private int _locY;
        
        public ComputeShaderTest() : base(TexW, TexH)
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader("SparrowGame");
            Texture bg = SimpleTextureLoader.LoadImageFromStream(loader.GetEmbeddedResourceStream("testbg_google.png"));
            Texture transp = SimpleTextureLoader.LoadImageFromStream(loader.GetEmbeddedResourceStream("testbg_white.png"));

            Texture tt = Texture.Empty(TexW, TexH, false, 0, false, 1.0f, TextureFormat.Rgba8888);
            var texOutput = tt.Base;

            Gl.BindImageTexture(0, texOutput, 0, false, 0, Gl.READ_WRITE, Gl.RGBA8);
            Gl.BindImageTexture(1, bg.Base, 0, false, 0, Gl.READ_ONLY, Gl.RGBA8);
            Gl.BindImageTexture(2, transp.Base, 0, false, 0, Gl.READ_ONLY, Gl.RGBA8);

            uint computeShader = Gl.CreateShader(Gl.COMPUTE_SHADER);
            string shaderStr = loader.GetEmbeddedResourceString("lightComputeShader.glsl");
            Gl.ShaderSource(computeShader, new[] { shaderStr });
            Gl.CompileShader(computeShader);

            _rayProgram = Gl.CreateProgram();
            Gl.AttachShader(_rayProgram, computeShader);
            Gl.LinkProgram(_rayProgram);

            Texture = tt;

            Touch += OnTouch;
            
            // SSBO to send variable length data
            allLightData = new LightData[20];
            for (int i = 0; i < allLightData.Length; i++)
            {
                allLightData[i].color.x = 1f;
                allLightData[i].color.y = 1f;
                allLightData[i].color.z = 1f;
                allLightData[i].color.w = 1f;                
            }
            
            buffer = Gl.GenBuffer();
            Gl.BindBufferBase(Gl.UNIFORM_BUFFER, 3, buffer); // bind it to layout binding 3
            uint bSize = (uint) (LightData.Size * allLightData.Length);
            Gl.BufferData(BufferTargetARB.UniformBuffer, bSize, null, BufferUsageARB.DynamicDraw);
            
            Gl.BindBuffer(BufferTargetARB.UniformBuffer, 0); // unbind
        }

        public sealed override Texture Texture
        {
            get { return base.Texture; }
            set { base.Texture = value; }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct LightData
        {
            public const int Size = 32;
            public Vertex4f color; // size = 16
            public Vertex2i pos;   // size = 4
            private readonly Vertex2i padding;   // size = 4 Nedeed to pad to a vertex size
        }

        private LightData[] allLightData;
        private uint buffer;

        public override void Render(Painter painter)
        {
            painter.ExcludeFromCache(this);
            
            Texture.Root.Clear(0,1);
            
            Gl.UseProgram(_rayProgram);

            for (int i = 0; i < allLightData.Length; i++)
            {
                allLightData[i].pos.x = _locX;
                allLightData[i].pos.y = _locY + i * 200;                
            }

            
            Gl.BindBufferBase(Gl.UNIFORM_BUFFER, 3, buffer); // bind it to layout binding 3
            uint bSize = (uint) (LightData.Size * allLightData.Length);
            Gl.BufferData(BufferTargetARB.UniformBuffer, bSize, allLightData, BufferUsageARB.StaticRead);
           
            int loc = Gl.GetUniformLocation(_rayProgram, "lightNum");
            Gl.Uniform1(loc, (uint)2); // NUM LIGHTS
            
            Gl.BindBuffer(BufferTargetARB.UniformBuffer, 0); // unbind
            
            Gl.DispatchCompute(4, 4, 1); // 4x4 = 16 work groups
            // make sure writing to image has finished before read. Put this as close to the tex sampler code as possible
            Gl.MemoryBarrier(Gl.SHADER_IMAGE_ACCESS_BARRIER_BIT);
            base.Render(painter);
        }
                
        private void OnTouch(TouchEvent touch)
        {
            if (touch.Touches.Count > 0)
            {
                _locX = (int)touch.Touches[0].GlobalX;
                _locY = (int)touch.Touches[0].GlobalY;
            }
            TraceMouse();
            Console.WriteLine(_locX + " " + _locY);
        }

        private Quad _mousePos;

        private void TraceMouse()
        {
            if (_mousePos == null)
            {
                _mousePos = new Quad(1,1,0x12ff12);
                Parent.AddChild(_mousePos);
            }
            _mousePos.X = _locX;
            _mousePos.Y = _locY;
        }
    }
}
