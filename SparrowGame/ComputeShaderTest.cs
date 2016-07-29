using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using Sparrow.Display;
using Sparrow.Core;
using Sparrow;
using Sparrow.Geom;
using Sparrow.ResourceLoading;
using Sparrow.Textures;
using SparrowSharp.Display;
using SparrowSharp.Utils;
using SparrowGame;
using OpenTK.Input;
using OpenTK;

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
                PremultipliedAlpha = true
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
            GL.ShaderSource(ray_shader, 1, new string[] { GetComputeShader() }, (int[])null);
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

            // Set the uniform
            int testVarLocation = GL.GetUniformLocation(ray_program, "lightPos");
            var mouse = Mouse.GetState();
            //Console.Out.WriteLine("m " + mouseCoords[0] + " " + mouseCoords[1]);
            GL.Uniform2(testVarLocation, (float)mouse.X, (float)mouse.Y);
            GL.DispatchCompute(tex_w, tex_h, 1);
            // make sure writing to image has finished before read. Put this as close to the etx sampler code as possible
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            base.Render(support);
        }


        public string GetComputeShader()
        {
            const string source = @"
            #version 430
            
            uniform vec2 lightPos; // passed from the app
            
            layout (local_size_x = 1, local_size_y = 1) in; // should be a multiple of 32 on Nvidia, 64 on AMD; >256 might not work
            layout (rgba8, binding = 0) uniform image2D img_output;
            layout (rgba8, binding = 1) uniform readonly image2D bgTex; // determines color TODO remove
            layout (rgba8, binding = 1) uniform readonly image2D transpTex; // determines transparency

            void main () {
              ivec2 global_coords = ivec2 (gl_GlobalInvocationID.xy); // get postion in global work group
              ivec2 local_coords = ivec2 (gl_LocalInvocationID.xy);// get position in local work group
              // determine coordinates where rendering ends
              // do the algo
              vec4 bgpix = imageLoad(bgTex, global_coords);
              vec2 lightP = lightPos / 100;
              vec4 pixel = vec4 (bgpix.r, lightP.x, lightP.y, 0.8);

              // output to a specific pixel in the image
              imageStore (img_output, global_coords, pixel);
            }";
            return source;
        }
        
        public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, 234, 234);
        }
    }
}
