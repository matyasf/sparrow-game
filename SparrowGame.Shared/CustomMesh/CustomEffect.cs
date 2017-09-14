using OpenGL;
using Sparrow.Rendering;
using Sparrow.ResourceLoading;

namespace SparrowGame.Shared.CustomMesh
{
    public class CustomEffect : MeshEffect
    {
        public static float Xc;
        public static float Yc;
        
        protected override Program CreateProgram()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader("SparrowGame");
            string frag = AddShaderInitCode() + "\n" +
                loader.GetEmbeddedResourceString("FragShader.frag");
            string vert = AddShaderInitCode() + "\n" + 
                loader.GetEmbeddedResourceString("VertShader.vert");
            return new Program(vert, frag);
        }

        protected override void BeforeDraw()
        {
            base.BeforeDraw();

            int lightPos = Program.Uniforms["lightPos"];
            Gl.Uniform2(lightPos, Xc, Yc);
        }

    }
}