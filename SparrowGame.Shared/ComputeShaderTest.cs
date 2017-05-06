
using Sparrow.Display;
using Sparrow.ResourceLoading;
using Sparrow.Textures;
using Sparrow.Touches;
using Sparrow.Rendering;
using OpenGL;

namespace SparrowGame.Shared
{
    class ComputeShaderTest : Quad
    {

        uint ray_program;
        int tex_w = 512, tex_h = 512;
        uint tex_output;
        float locX, locY;
        
        public ComputeShaderTest() : base(512, 512)
        {
            
            Texture tt = Texture.Empty(tex_w, tex_h, false, 0, false, 1.0f, TextureFormat.Rgba8888);
            tex_output = tt.Base;

            EmbeddedResourceLoader loader = new EmbeddedResourceLoader("SparrowGame");
            Texture bg = SimpleTextureLoader.LoadImageFromStream(loader.GetEmbeddedResourceStream("testbg.png"));
            Texture transp = SimpleTextureLoader.LoadImageFromStream(loader.GetEmbeddedResourceStream("testbg_transparency.png"));

            Gl.BindImageTexture(0, tex_output, 0, false, 0, Gl.WRITE_ONLY, Gl.RGBA8);
            Gl.BindImageTexture(1, bg.Base, 0, false, 0, Gl.READ_ONLY, Gl.RGBA8);
            Gl.BindImageTexture(2, transp.Base, 0, false, 0, Gl.READ_ONLY, Gl.RGBA8);

            uint computeShader = Gl.CreateShader(Gl.COMPUTE_SHADER);

            GPUInfo.CheckForOpenGLError();
            string shaderStr = loader.GetEmbeddedResourceString("lightComputeShader.glsl");
            
            Gl.ShaderSource(computeShader, new string[] { shaderStr });
            Gl.CompileShader(computeShader);

            //GPUInfo.checkShaderCompileError(computeShader);

            ray_program = Gl.CreateProgram();
            Gl.AttachShader(ray_program, computeShader);
            Gl.LinkProgram(ray_program);
            //GPUInfo.checkShaderLinkError(ray_program);

            Texture = tt;

            Touch += onTouch;
        }

        private void onTouch(TouchEvent touch)
        {
            if (touch.Touches.Count > 0)
            {
                locX = touch.Touches[0].GlobalX;
                locY = touch.Touches[0].GlobalY;
            }
            //Console.WriteLine(locX + " " + locY);
        }

        public override void Render(Painter painter)
        {
            painter.ExcludeFromCache(this);
            
            Gl.UseProgram(ray_program);
            int lightPos = Gl.GetUniformLocation(ray_program, "lightPos");
            Gl.Uniform2(lightPos, locX, locY);

            int lightColor = Gl.GetUniformLocation(ray_program, "lightColor");
            Gl.Uniform4(lightColor, 1f, 1f, 1f, 1f);
#if __WINDOWS__
            Gl.DispatchCompute(4, 4, 1);
            // make sure writing to image has finished before read. Put this as close to the tex sampler code as possible
            Gl.MemoryBarrier(Gl.SHADER_IMAGE_ACCESS_BARRIER_BIT);
#else
            OpenTK.Graphics.ES31.GL.DispatchCompute(4, 4, 1); // max 65535 for each dimension
            OpenTK.Graphics.ES31.GL.MemoryBarrier(OpenTK.Graphics.ES31.MemoryBarrierMask.AllBarrierBits);
#endif
            base.Render(painter);
        }

        /*public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, tex_w, tex_h);
        }*/
    }
}
