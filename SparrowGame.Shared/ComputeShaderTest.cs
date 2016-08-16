using System;
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


#if __WINDOWS__
            GLTexture bg = new TextureLoader().LoadLocalImage("../../../assets/testbg.png");
            GLTexture transp = new TextureLoader().LoadLocalImage("../../../assets/testbg_transparency.png");
            GL.BindImageTexture(0, tex_output, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba8);
            GL.BindImageTexture(1, bg.Name, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);
            GL.BindImageTexture(2, transp.Name, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);

            // init compute shader
            int ray_shader = GL.CreateShader(ShaderType.ComputeShader);            
#else
            GLTexture bg = SimpleTextureLoader.LoadAndroidResource(SparrowGame.Droid.Resource.Drawable.testbg);
            GLTexture transp = SimpleTextureLoader.LoadAndroidResource(SparrowGame.Droid.Resource.Drawable.testbg_transparency);
            GLES31.GlBindImageTexture(0, (int)tex_output, 0, false, 0, GLES31.GlWriteOnly, GLES30.GlRgba8);
            GLES31.GlBindImageTexture(1, (int)bg.Name, 0, false, 0, GLES31.GlReadOnly, GLES30.GlRgba8);
            GLES31.GlBindImageTexture(2, (int)transp.Name, 0, false, 0, GLES31.GlReadOnly, GLES30.GlRgba8);
            // init compute shader
            int ray_shader = GLES31.GlCreateShader(GLES31.GlComputeShader);
#endif

            //string shaderStr = LoadString("SparrowGame.lightComputeShader.glsl");
            string shaderStr = @"#version 430
            
uniform vec2 lightPos; // passed from the app
            
layout (local_size_x = 512, local_size_y = 1) in; // should be a multiple of 32 on Nvidia, 64 on AMD; >256 might not work
layout (rgba8, binding = 0) uniform image2D img_output;
layout (rgba8, binding = 1) uniform readonly image2D colorTex; // determines color
layout (rgba8, binding = 2) uniform readonly image2D transpTex; // determines transparency

void main () {
    uint global_coords = gl_WorkGroupID.x; // postion in global work group; 0 = left, 1 = right, 2 = top, 3 = bottom
    uint local_coords = gl_LocalInvocationID.x; // get position in local work group
    uint txrsiz = 512; 
    // determine coordinates where rendering ends
    uvec2 endPoint = uvec2(0, 0);
    if (global_coords < 2) {// on the left or right
    endPoint.y = local_coords;
        if (global_coords == 1) { // right
            endPoint.x = txrsiz;
        }
    }
    else {// on the top or bottom
        endPoint.x = local_coords;
        if (global_coords == 3) {
            endPoint.y = txrsiz;
        }
    }
    // calculate light to the endpoint
    uint i;
    vec2 t;
    vec2 dt;
    vec4 outPixel;
    vec4 transpPixel;
    vec4 colorPixel;
    ivec2 coords;
    float transmit = 0;// light transmission constant coeficient <0,1>
    float currentAlpha = 1.0;
    dt = normalize(endPoint - lightPos);
    outPixel = vec4(1.0, 1.0, 1.0, 1.0);  
    t = lightPos;
    if (dot(endPoint-t, dt) > 0.0) {
		for (i = 0; i < txrsiz; i++) {
            coords.x = int(t.x);
            coords.y = int(t.y);

			// calculate transparency
			transpPixel = imageLoad(transpTex, coords);   
            currentAlpha = (transpPixel.b + transpPixel.g * 10.0 + transpPixel.r * 100.0) / 111.0;
			
			// calculate color
			colorPixel = imageLoad(colorTex, coords);
            //outPixel.rgb = colorPixel.rgb;
            outPixel.rgb = min(colorPixel.rgb, outPixel.rgb);
            outPixel.rgb = outPixel.rgb - (1.0 - currentAlpha) - transmit; 

			imageStore(img_output, coords, outPixel);

			if (dot(endPoint - t, dt) <= 0.000f) break;
			//if (outPixel.r + outPixel.g + outPixel.b <= 0.001f) break;
			t += dt;
		}
    }
}";
            GL.ShaderSource(ray_shader, 1, new string[] { shaderStr }, (int[])null);
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
#else
            GLES20.GlUniform2f(testVarLocation, 234.0f, 234.0f);
            GLES31.GlDispatchCompute(4, 1, 1);
            GLES31.GlMemoryBarrier(GLES31.GlAllShaderBits); // TODO might work with lower barrier 
#endif
            
            base.Render(support);
        }

        public override Rectangle BoundsInSpace(DisplayObject targetSpace)
        {
            return new Rectangle(0, 0, 512, 512);
        }
    }
}
