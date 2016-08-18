using System;
using Sparrow.Display;
using Sparrow.Core;
using Sparrow.Geom;
using Sparrow.ResourceLoading;
using Sparrow.Textures;
using SparrowSharp.Utils;
using SparrowGame;
using Sparrow;
using SparrowGame.Shared;

#if __WINDOWS__
using OpenTK.Graphics.OpenGL4;
#elif __ANDROID__
using OpenTK.Graphics.ES20;
using Android.Opengl;
#endif

namespace SparrowSharp.Samples.Desktop
{
    class ComputeShaderTest : Image
    {

        int ray_program;
        int tex_w = 512, tex_h = 512;
        uint tex_output;

        public event Juggler.RemoveFromJugglerHandler RemoveFromJugglerEvent;

        public ComputeShaderTest()
        {
            OpenGLDebugCallback.Init();
            GPUInfo.PrintGPUInfo();

            /*TextureProperties texProps = new TextureProperties
            {
                TextureFormat = TextureFormat.Rgba8888,
                Scale = 1.0f,
                Width = tex_w,
                Height = tex_h,
                NumMipmaps = 0,
                GenerateMipmaps = false,
                PremultipliedAlpha = false
            };
            GLTexture tt = new GLTexture(IntPtr.Zero, texProps);
            tex_output = tt.Name;*/

#if __WINDOWS__
            TextureProperties texProps = new TextureProperties
            {
                TextureFormat = TextureFormat.Rgba8888,
                Scale = 1.0f,
                Width = tex_w,
                Height = tex_h,
                NumMipmaps = 0,
                GenerateMipmaps = false,
                PremultipliedAlpha = false
            };
            GLTexture tt = new GLTexture(IntPtr.Zero, texProps);
            tex_output = tt.Name;

            GLTexture bg = SimpleTextureLoader.LoadImageFromStream(ResourceLoader.GetEmbeddedResourceStream("testbg.png"));
            GLTexture transp = SimpleTextureLoader.LoadImageFromStream(ResourceLoader.GetEmbeddedResourceStream("testbg_transparency.png"));
            GL.BindImageTexture(0, tex_output, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba8);
            GL.BindImageTexture(1, bg.Name, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);
            GL.BindImageTexture(2, transp.Name, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);

            // init compute shader
            int ray_shader = GL.CreateShader(ShaderType.ComputeShader);            
#else
            GL.GenTextures(1, out tex_output);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex_output);
            OpenTK.Graphics.ES31.GL.TexImage2D(
                       OpenTK.Graphics.ES31.TextureTarget.Texture2D,
                       0,
                       OpenTK.Graphics.ES31.PixelInternalFormat.Rgba, // should be a million types..
                       tex_w,
                       tex_h,
                       0,
                       OpenTK.Graphics.ES31.PixelFormat.Rgba,  //luminance,... just a few
                       OpenTK.Graphics.ES31.PixelType.Float,
                       IntPtr.Zero);
            RenderSupport.CheckForOpenGLError();
            OpenTK.Graphics.ES31.GL.BindImageTexture(0, tex_output, 0, false, 0, OpenTK.Graphics.ES31.All.WriteOnly, OpenTK.Graphics.ES31.All.Rgba);
            RenderSupport.CheckForOpenGLError();
            GLTexture tt = new GLTexture(tex_output, tex_w, tex_h, false, 1.0f, false);
            RenderSupport.CheckForOpenGLError();

            GLTexture bg = SimpleTextureLoader.LoadAndroidResource(Resource.Drawable.testbg);
            GLTexture transp = SimpleTextureLoader.LoadAndroidResource(Resource.Drawable.testbg_transparency);

            //OpenTK.Graphics.ES31.GL.BindImageTexture(0, tex_output, 0, false, 0, OpenTK.Graphics.ES31.All.WriteOnly, OpenTK.Graphics.ES31.All.Rgba8);
            OpenTK.Graphics.ES31.GL.BindImageTexture(1, bg.Name, 0, false, 0, OpenTK.Graphics.ES31.All.ReadOnly, OpenTK.Graphics.ES31.All.Rgba8);
            OpenTK.Graphics.ES31.GL.BindImageTexture(2, transp.Name, 0, false, 0, OpenTK.Graphics.ES31.All.ReadOnly, OpenTK.Graphics.ES31.All.Rgba8i);

            //GLES31.GlBindImageTexture(0, (int)tex_output, 0, false, 0, GLES31.GlWriteOnly, GLES30.GlRgba8);
            //GLES31.GlBindImageTexture(1, (int)bg.Name, 0, false, 0, GLES31.GlReadOnly, GLES30.GlRgba8);
            //GLES31.GlBindImageTexture(2, (int)transp.Name, 0, false, 0, GLES31.GlReadOnly, GLES30.GlRgba8);
            // init compute shader
            int ray_shader = OpenTK.Graphics.ES31.GL.CreateShader(OpenTK.Graphics.ES31.ShaderType.ComputeShader);
#endif
            
            string shaderStr = ResourceLoader.GetEmbeddedResourceString("lightComputeShader.glsl");
            
            GL.ShaderSource(ray_shader, 1, new string[] { shaderStr }, (int[])null);
            GL.CompileShader(ray_shader);
            GPUInfo.checkShaderCompileError(ray_shader);
            ray_program = GL.CreateProgram();
            GL.AttachShader(ray_program, ray_shader);
            GL.LinkProgram(ray_program);
            GPUInfo.checkShaderLinkError(ray_program);
            base.InitImage(tt);
        }

        public override void Render(RenderSupport support)
        {
            GL.UseProgram(ray_program);
            float locX, locY;
            int testVarLocation = GL.GetUniformLocation(ray_program, "lightPos");

#if __WINDOWS__
            DesktopViewController dvc = (DesktopViewController)SparrowSharpApp.NativeWindow;
            var mouse = dvc.Mouse;
            locX = mouse.X;
            locY = mouse.Y;
            // Set the uniform
            GL.Uniform2(testVarLocation, locX, locY);
            GL.DispatchCompute(4, 1, 1);
            // make sure writing to image has finished before read. Put this as close to the etx sampler code as possible
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypeError, 3, DebugSeverity.DebugSeverityHigh, 4, "1234");
#else
            Random rnd = new Random();
            GL.Uniform2(testVarLocation, (float)rnd.Next(0, 500), (float)rnd.Next(0, 500));
            OpenTK.Graphics.ES31.GL.DispatchCompute(4, 1, 1); // max 65535 for each dimension
            OpenTK.Graphics.ES31.GL.MemoryBarrier(OpenTK.Graphics.ES31.MemoryBarrierMask.AllBarrierBits);
            /*
            GLES20.GlUniform2f(testVarLocation, (float)rnd.Next(0, 500), (float)rnd.Next(0, 500));
            GLES31.GlDispatchCompute(4, 1, 1);
            GLES31.GlMemoryBarrier(GLES31.GlAllShaderBits); // TODO might work with lower barrier
            */
#endif
            
            base.Render(support);
        }

        public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, 512, 512);
        }
    }
}
