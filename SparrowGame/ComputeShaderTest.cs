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

        float someNumber = 0;

        public override void Render(RenderSupport support)
        {
            GL.UseProgram(ray_program);

            int testVarLocation = GL.GetUniformLocation(ray_program, "someData");
            // Set the uniform
            someNumber = someNumber < 1 ? someNumber + 0.01f : 0;
            GL.Uniform1(testVarLocation, someNumber);
            GL.DispatchCompute(tex_w, tex_h, 1);
            // make sure writing to image has finished before read. Put this as close to the etx sampler code as possible
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            base.Render(support);
        }


        public string GetComputeShader()
        {
            const string source = @"
            #version 430
            
            uniform float someData; // passed from the app
            
            layout (local_size_x = 1, local_size_y = 1) in;
            layout (rgba8, binding = 0) uniform image2D img_output;
            layout (rgba8, binding = 1) uniform readonly image2D bgTex;
           
            void main () {
              // get index in global work group i.e x,y position
              ivec2 pixel_coords = ivec2 (gl_GlobalInvocationID.xy);

              vec4 bgpix = imageLoad(bgTex, pixel_coords);
              vec4 pixel = vec4 (bgpix.r, someData, 0.0, 0.8);

              float max_x = 5.0;
              float max_y = 5.0;
              ivec2 dims = imageSize (img_output); // fetch image dimensions
              float x = (float(pixel_coords.x * 2 - dims.x) / dims.x);
              float y = (float(pixel_coords.y * 2 - dims.y) / dims.y);
              vec3 ray_o = vec3 (x * max_x, y * max_y, 0.0); // origin
              vec3 ray_d = vec3 (0.0, 0.0, -1.0); // ortho direction
              
              // sphere  
              vec3 sphere_c = vec3 (0.0, 0.0, -10.0);
              float sphere_r = 1.0;
              
              vec3 omc = ray_o - sphere_c;
              float b = dot (ray_d, omc);
              float c = dot (omc, omc) - sphere_r * sphere_r;
              float bsqmc = b * b - c;
              // hit one or both sides
              if (bsqmc >= 0.0) {
                pixel = vec4 (bsqmc, 0.4, 1.0, 1.0);
              }
  
              // output to a specific pixel in the image
              imageStore (img_output, pixel_coords, pixel);
            }";
            return source;
        }
        
        public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, 234, 234);
        }
    }
}
