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

            // init texture, set its parameters
            GL.GenTextures(1, out tex_output); // generate texture name
            GL.ActiveTexture(TextureUnit.Texture0); // activates a texture unit
            GL.BindTexture(TextureTarget.Texture2D, tex_output); // bind a named texture to a texturing target
            int clampToEdge = (int)All.ClampToEdge;
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ref clampToEdge);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ref clampToEdge);
            int texFilter = (int)TextureMagFilter.Linear;
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ref texFilter);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ref texFilter);
            //TexImage2D loads the supplied pixel data into a texture
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, tex_w, tex_h, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            // Bind a single image from the texture to image unit 0
            GL.BindImageTexture(0, tex_output, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
           
            // init compute shader
            int ray_shader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(ray_shader, 1, new string[] { GetComputeShader() }, (int[])null);
            GL.CompileShader(ray_shader);

            GPUInfo.checkShaderCompileError(ray_shader);

            ray_program = GL.CreateProgram();
            GL.AttachShader(ray_program, ray_shader);
            GL.LinkProgram(ray_program);

            GPUInfo.checkShaderLinkError(ray_program);

            GLTexture tx = new GLTexture(tex_output, tex_w, tex_h, false, 1.0f, false);
            base.InitImage(tx);
        }

        float someNumber = 0;

        public override void Render(RenderSupport support)
        {
            // TODO move around lights
            // TODO upload geometry texture
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
            layout (rgba32f, binding = 0) uniform image2D img_output; // output is image unit 0
            
            void main () {
              // base pixel colour for image
              vec4 pixel = vec4 (0.0, someData, 0.0, 1.0);
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
        
        public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, 234, 234);
        }
    }
}
