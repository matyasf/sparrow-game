
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Sparrow.Display;
using Sparrow.ResourceLoading;
using Sparrow.Textures;
using Sparrow.Touches;
using Sparrow.Rendering;
using OpenGL;
using Vector2 = Sparrow.Geom.Vector2;

namespace SparrowGame.Shared
{
    public class ComputeShaderTest : Quad
    {

        private readonly uint _rayProgram;
        private const int TexW = 512;
        private const int TexH = 512;
        private float _locX;
        private float _locY;
        
        public ComputeShaderTest() : base(TexW, TexH)
        {
            Texture tt = Texture.Empty(TexW, TexH, false, 0, false, 1.0f, TextureFormat.Rgba8888);
            var texOutput = tt.Base;

            EmbeddedResourceLoader loader = new EmbeddedResourceLoader("SparrowGame");
            Texture bg = SimpleTextureLoader.LoadImageFromStream(loader.GetEmbeddedResourceStream("testbg.png"));
            Texture transp = SimpleTextureLoader.LoadImageFromStream(loader.GetEmbeddedResourceStream("testbg_transparency.png"));

            Gl.BindImageTexture(0, texOutput, 0, false, 0, Gl.WRITE_ONLY, Gl.RGBA8);
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
            allLightData = new LightData[2];
            allLightData[0].color.x = 1f;
            allLightData[0].color.y = 1f;
            allLightData[0].color.z = 1f;
            allLightData[0].color.w = 1f;
            
            allLightData[1].color.x = 1f;
            allLightData[1].color.y = 1f;
            allLightData[1].color.z = 1f;
            allLightData[1].color.w = 1f;
            
            ssbo = Gl.GenBuffer();
            Gl.BindBufferBase(Gl.SHADER_STORAGE_BUFFER, 3, ssbo); // bind it to layout binding 3
            //Gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, ssbo);
            uint bSize = (uint) (LightData.Size * allLightData.Length);
            Gl.BufferData(BufferTargetARB.ShaderStorageBuffer, bSize, allLightData, BufferUsageARB.StaticRead);
            //Gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, 0); // unbind
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct LightData
        {
            public const int Size = Vertex2f.Size + Vertex4f.Size;
            public Vertex4f color;
            public Vertex2f pos; 
        }

        private LightData[] allLightData;
        private uint ssbo;
        
        private void OnTouch(TouchEvent touch)
        {
            if (touch.Touches.Count > 0)
            {
                _locX = touch.Touches[0].GlobalX;
                _locY = touch.Touches[0].GlobalY;
            }
            Console.WriteLine(_locX + " " + _locY);
        }

        public override void Render(Painter painter)
        {
            painter.ExcludeFromCache(this);
            
            Gl.UseProgram(_rayProgram);

            allLightData[0].pos.x = _locX;
            allLightData[0].pos.y = _locY;
            allLightData[1].pos.x = _locX;
            allLightData[1].pos.y = _locY;
            Gl.BindBufferBase(Gl.SHADER_STORAGE_BUFFER, 3, ssbo); // bind it to layout binding 3
            uint bSize = (uint) (LightData.Size * allLightData.Length);
            Gl.BufferData(BufferTargetARB.ShaderStorageBuffer, bSize, allLightData, BufferUsageARB.StaticRead);
            
            Gl.DispatchCompute(4, 4, 1); // 4x4 = 16 work groups
            // make sure writing to image has finished before read. Put this as close to the tex sampler code as possible
            Gl.MemoryBarrier(Gl.SHADER_IMAGE_ACCESS_BARRIER_BIT);
            base.Render(painter);
        }

        /*public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, tex_w, tex_h);
        }*/
    }
}
