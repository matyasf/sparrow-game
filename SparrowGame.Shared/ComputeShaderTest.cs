﻿
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
            allLightData = new LightData[10];
            allLightData[0].color.x = 1f;
            allLightData[0].color.y = 1f;
            allLightData[0].color.z = 1f;
            allLightData[0].color.w = 1f;

            allLightData[1].color.x = 1f;
            allLightData[1].color.y = 1f;
            allLightData[1].color.z = 1f;
            allLightData[1].color.w = 1f;
            
            buffer = Gl.GenBuffer();
            Gl.BindBufferBase(Gl.UNIFORM_BUFFER, 3, buffer); // bind it to layout binding 3
            uint bSize = (uint) (LightData.Size * allLightData.Length);
            Gl.BufferData(BufferTargetARB.UniformBuffer, bSize, null, BufferUsageARB.DynamicDraw);
            
            Gl.BindBuffer(BufferTargetARB.UniformBuffer, 0); // unbind
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct LightData
        {
            public const int Size = 32;
            public Vertex4f color; // size = 16
            public Vertex2f pos;   // size = 4
            private readonly Vertex2f padding;   // size = 4 Nedeed to pad to a vertex size
        }

        private LightData[] allLightData;
        private uint buffer;

        public override void Render(Painter painter)
        {
            painter.ExcludeFromCache(this);
            
            Gl.UseProgram(_rayProgram);

            allLightData[0].pos.x = _locX;
            allLightData[0].pos.y = _locY;
            allLightData[1].pos.x = _locX;
            allLightData[1].pos.y = _locY+70;
            
            Gl.BindBufferBase(Gl.UNIFORM_BUFFER, 3, buffer); // bind it to layout binding 3
            uint bSize = (uint) (LightData.Size * allLightData.Length);
            Gl.BufferData(BufferTargetARB.UniformBuffer, bSize, allLightData, BufferUsageARB.StaticRead);
           
            int loc = Gl.GetUniformLocation(_rayProgram, "lightNum");
            Gl.Uniform1(loc, (uint)2);
            
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
                _locX = touch.Touches[0].GlobalX;
                _locY = touch.Touches[0].GlobalY;
            }
            Console.WriteLine(_locX + " " + _locY);
        }


        /*public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, tex_w, tex_h);
        }*/
    }
}
