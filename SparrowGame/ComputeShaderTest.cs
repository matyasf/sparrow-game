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
            GetGPUInfo();

            GL.GenTextures(1, out tex_output);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex_output);
            int clampToEdge = (int)All.ClampToEdge;
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ref clampToEdge);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ref clampToEdge);

            int texFilter = (int)TextureMagFilter.Linear;
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ref texFilter);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ref texFilter);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, tex_w, tex_h, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.BindImageTexture(0, tex_output, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

            int ray_shader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(ray_shader, 1, new string[] { GetComputeShader() }, (int[])null);
            GL.CompileShader(ray_shader);
            // check for compilation errors as per normal here

            ray_program = GL.CreateProgram();
            GL.AttachShader(ray_program, ray_shader);
            GL.LinkProgram(ray_program);
            // check for linking errors and validate program as per normal here
            GLTexture tx = new GLTexture(tex_output, tex_w, tex_h, false, 1.0f, false);
            base.InitImage(tx);
        }

        public override void Render(RenderSupport support)
        {
            GL.UseProgram(ray_program);
            GL.DispatchCompute(tex_w, tex_h, 1);
            // make sure writing to image has finished before read
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            base.Render(support);
        }


        public string GetComputeShader()
        {
            const string source =
                @"
            #version 430
            layout (local_size_x = 1, local_size_y = 1) in;
            layout (rgba32f, binding = 0) uniform image2D img_output;
            
            void main () {
              // base pixel colour for image
              vec4 pixel = vec4 (0.0, 0.7, 0.0, 1.0);
              // get index in global work group i.e x,y position
              ivec2 pixel_coords = ivec2 (gl_GlobalInvocationID.xy);
  
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
                //pixel = vec4 (0.4, 0.4, 1.0, 1.0);
                pixel = vec4 (bsqmc, 0.4, 1.0, 1.0);
              }
  
              // output to a specific pixel in the image
              imageStore (img_output, pixel_coords, pixel);
            }";
            return source;
        }
        
        private void GetGPUInfo()
        {
            string versionOpenGL = GL.GetString(StringName.Version);
            Console.Out.WriteLine("GL version:" + versionOpenGL);

            int[] work_grp_cnt = new int[3];
            GetIndexedPName maxWorkGroupCount = (GetIndexedPName)All.MaxComputeWorkGroupCount;
            GL.GetInteger(maxWorkGroupCount, 0, out work_grp_cnt[0]);
            GL.GetInteger(maxWorkGroupCount, 1, out work_grp_cnt[1]);
            GL.GetInteger(maxWorkGroupCount, 2, out work_grp_cnt[2]);

            Console.Out.WriteLine("max global (total) work group size " + string.Join(",", work_grp_cnt));

            int[] work_grp_size = new int[3];
            GetIndexedPName maxComputeGroupSize = (GetIndexedPName)All.MaxComputeWorkGroupSize;
            GL.GetInteger(maxComputeGroupSize, 0, out work_grp_size[0]);
            GL.GetInteger(maxComputeGroupSize, 1, out work_grp_size[1]);
            GL.GetInteger(maxComputeGroupSize, 2, out work_grp_size[2]);

            Console.Out.WriteLine("max local (in one shader) work group sizes " + string.Join(",", work_grp_size));
        }

       
        public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, 234, 234);
        }
    }
}
