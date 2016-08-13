using System;
using OpenTK.Graphics.OpenGL4;
using Sparrow.Display;
using Sparrow.Core;
using Sparrow.Geom;
using Sparrow.ResourceLoading;
using Sparrow.Textures;
using SparrowSharp.Utils;
using SparrowGame;
using OpenTK.Input;
using System.IO;
using System.Reflection;
using Sparrow;

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
            GPUInfo.PrintGPUInfo();
            
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
            GL.BindImageTexture(0, tex_output, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba8);

            GLTexture bg = new TextureLoader().LoadLocalImage("../../testbg.png");
            GL.BindImageTexture(1, bg.Name, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);

            GLTexture transp = new TextureLoader().LoadLocalImage("../../testbg_transparency.png");
            GL.BindImageTexture(2, transp.Name, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);

            // init compute shader
            int ray_shader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(ray_shader, 1, new string[] { LoadString("SparrowGame.lightComputeShader.glsl") }, (int[])null);
            GL.CompileShader(ray_shader);
            GPUInfo.checkShaderCompileError(ray_shader);
            ray_program = GL.CreateProgram();
            GL.AttachShader(ray_program, ray_shader);
            GL.LinkProgram(ray_program);
            GPUInfo.checkShaderLinkError(ray_program);
            
            base.InitImage(tt);
        }

        public string LoadString(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(filename);
            try
            {
                StreamReader reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (ArgumentNullException ex)
            {
                Console.Out.WriteLine("unable to load file "  + filename + " " + ex);
                return "";
            }
        }

        public override void Render(RenderSupport support)
        {
            GL.UseProgram(ray_program);

            DesktopViewController dvc = (DesktopViewController)SparrowSharpApp.NativeWindow;
            var mouse = dvc.Mouse;

            // Set the uniform
            int testVarLocation = GL.GetUniformLocation(ray_program, "lightPos");
            //var mouse = Mouse.GetState();
            //Console.Out.WriteLine("m " + mouseCoords[0] + " " + mouseCoords[1]);
            GL.Uniform2(testVarLocation, (float)mouse.X, (float)mouse.Y);
            GL.DispatchCompute(4, 1, 1);
            // make sure writing to image has finished before read. Put this as close to the etx sampler code as possible
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            base.Render(support);
        }

        public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, 512, 512);
        }
    }
}
