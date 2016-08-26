using System;
using Sparrow.Display;
using Sparrow.Core;
using Sparrow.Geom;
using Sparrow.ResourceLoading;
using Sparrow.Textures;
using SparrowGame;
using SparrowGame.Shared;
using Sparrow.Touches;

#if __WINDOWS__
using OpenTK.Graphics.OpenGL4;
#elif __ANDROID__
using OpenTK.Graphics.ES30;
#endif

namespace SparrowSharp.Samples.Desktop
{
    class ComputeShaderTest : Image
    {

        int ray_program;
        int tex_w = 512, tex_h = 512;
        int tex_output;
        float locX, locY;
        
        public ComputeShaderTest()
        {
            OpenGLDebugCallback.Init();
            GPUInfo.PrintGPUInfo();
            
            TextureProperties texProps = new TextureProperties
                {
                    TextureFormat = TextureFormat.Rgba8888,
                    Scale = 1.0f,
                    Width = tex_w,
                    Height = tex_h,
                    NumMipmaps = 0,
                    PremultipliedAlpha = false
                };
            GLTexture tt = new GLTexture(IntPtr.Zero, texProps);
            tex_output = (int)tt.Name;

            GLTexture bg = SimpleTextureLoader.LoadImageFromStream(ResourceLoader.GetEmbeddedResourceStream("testbg.png"));
            GLTexture transp = SimpleTextureLoader.LoadImageFromStream(ResourceLoader.GetEmbeddedResourceStream("testbg_transparency.png"));
#if __WINDOWS__
            GL.BindImageTexture(0, tex_output, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba8);
            GL.BindImageTexture(1, bg.Name, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);
            GL.BindImageTexture(2, transp.Name, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);
            
            int computeShader = GL.CreateShader(ShaderType.ComputeShader);
#else
            OpenTK.Graphics.ES31.GL.BindImageTexture(0, tex_output, 0, false, 0, OpenTK.Graphics.ES31.All.WriteOnly, OpenTK.Graphics.ES31.All.Rgba8);
            OpenTK.Graphics.ES31.GL.BindImageTexture(1, bg.Name, 0, false, 0, OpenTK.Graphics.ES31.All.ReadOnly, OpenTK.Graphics.ES31.All.Rgba8);
            OpenTK.Graphics.ES31.GL.BindImageTexture(2, transp.Name, 0, false, 0, OpenTK.Graphics.ES31.All.ReadOnly, OpenTK.Graphics.ES31.All.Rgba8);
            
            int computeShader = OpenTK.Graphics.ES31.GL.CreateShader(OpenTK.Graphics.ES31.ShaderType.ComputeShader);
#endif
            RenderSupport.CheckForOpenGLError();
            string shaderStr = ResourceLoader.GetEmbeddedResourceString("lightComputeShader.glsl");
            
            GL.ShaderSource(computeShader, 1, new string[] { shaderStr }, (int[])null);
            GL.CompileShader(computeShader);
            GPUInfo.checkShaderCompileError(computeShader);
            ray_program = GL.CreateProgram();
            GL.AttachShader(ray_program, computeShader);
            GL.LinkProgram(ray_program);
            GPUInfo.checkShaderLinkError(ray_program);
            base.InitImage(tt);

            Touch += onTouch;
        }

        private void onTouch(TouchEvent touch)
        {
            if (touch.Touches.Count > 0)
            {
                locX = touch.Touches[0].GlobalX;
                locY = touch.Touches[0].GlobalY;
            }
        }

        public override void Render(RenderSupport support)
        {
            GL.UseProgram(ray_program);
            int lightPos = GL.GetUniformLocation(ray_program, "lightPos");
            GL.Uniform2(lightPos, locX, locY);

            int lightColor = GL.GetUniformLocation(ray_program, "lightColor");
            GL.Uniform4(lightColor, 1f, 1f, 1f, 1f);
#if __WINDOWS__
            GL.DispatchCompute(4, 4, 1);
            // make sure writing to image has finished before read. Put this as close to the tex sampler code as possible
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
#else
            OpenTK.Graphics.ES31.GL.DispatchCompute(4, 4, 1); // max 65535 for each dimension
            OpenTK.Graphics.ES31.GL.MemoryBarrier(OpenTK.Graphics.ES31.MemoryBarrierMask.AllBarrierBits);
#endif
            base.Render(support);
        }

        public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, tex_w, tex_h);
        }
    }
}
