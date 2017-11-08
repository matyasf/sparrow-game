
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
        private int _locX;
        private int _locY;
        
        private readonly LightData[] allLightData;
        private struct LightData
        {
            public Vertex4f color;
            public Vertex2i pos;
        }

        private RenderTexture backgroundColorTex;
        private Quad bg;
        private SampleMovingStuff enemy;
        
        public ComputeShaderTest() : base(TexW, TexH)
        {
            enemy = new SampleMovingStuff();
            backgroundColorTex = new RenderTexture(512, 512, false);
            
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader("SparrowGame");
            Texture bgTex = SimpleTextureLoader.LoadImageFromStream(loader.GetEmbeddedResourceStream("testbg_google.png"));
            bg = new Quad(512, 512);
            bg.Texture = bgTex;
            
            Texture transp = SimpleTextureLoader.LoadImageFromStream(loader.GetEmbeddedResourceStream("testbg_white.png"));

            Texture tt = Texture.Empty(TexW, TexH, false, 0, false, 1.0f, TextureFormat.Rgba8888);

            Gl.BindImageTexture(0, tt.Base, 0, false, 0, BufferAccess.ReadWrite, InternalFormat.Rgba8);
            Gl.BindImageTexture(1, backgroundColorTex.Base, 0, false, 0, BufferAccess.ReadWrite, InternalFormat.Rgba8);
            Gl.BindImageTexture(2, transp.Base, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.Rgba8);

            uint computeShader = Gl.CreateShader(ShaderType.ComputeShader);
            string shaderStr = loader.GetEmbeddedResourceString("lightComputeShaderOneLight.glsl");
            Gl.ShaderSource(computeShader, new[] { shaderStr });
            Gl.CompileShader(computeShader);

            _rayProgram = Gl.CreateProgram();
            Gl.AttachShader(_rayProgram, computeShader);
            Gl.LinkProgram(_rayProgram);

            Texture = tt;

            Touch += OnTouch;
            
            allLightData = new LightData[2];
            for (int i = 0; i < allLightData.Length; i++)
            {
                allLightData[i].color.x = 1f;
                allLightData[i].color.y = 1f;
                allLightData[i].color.z = 1f;
                allLightData[i].color.w = 1f;                
            }
        }

        public override void Render(Painter painter)
        {
            painter.ExcludeFromCache(this);
            
            // render background
            backgroundColorTex.DrawBundled(delegate
            {
                backgroundColorTex.Draw(bg);
                backgroundColorTex.Draw(enemy);
            });
            
            // render lights
            Texture.Root.Clear(0,1);
            Gl.UseProgram(_rayProgram);

            for (int i = 0; i < allLightData.Length; i++)
            {
                allLightData[i].pos.x = _locX;
                allLightData[i].pos.y = _locY + i * 110;
                
                // light position
                int loc = Gl.GetUniformLocation(_rayProgram, "lightPos");
                Gl.Uniform2(loc, allLightData[i].pos.x, allLightData[i].pos.y);
                // light color
                loc = Gl.GetUniformLocation(_rayProgram, "lightColor");
                Gl.Uniform4(loc, allLightData[i].color.x, allLightData[i].color.y, allLightData[i].color.z, allLightData[i].color.w);
            
                Gl.DispatchCompute(4, 4, 1);
                // make sure writing to image has finished before read. Put this as close to the tex sampler code as possible
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            }
            
            base.Render(painter);
        }
                
        private void OnTouch(TouchEvent touch)
        {
            if (touch.Touches.Count > 0)
            {
                _locX = (int)touch.Touches[0].GlobalX;
                _locY = (int)touch.Touches[0].GlobalY;
            }
        }
    }
}
