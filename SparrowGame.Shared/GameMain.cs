
using System;
using Sparrow.Core;
using Sparrow.Display;
using Sparrow.ResourceLoading;
using Sparrow.Textures;
using Sparrow.Touches;
using SparrowGame.Shared.CustomMesh;

namespace SparrowGame.Shared
{
    public class GameMain : Sprite
    {
        private ComputeShaderTest test;

        public GameMain()
        {
            SparrowSharp.EnableErrorChecking();
            AddedToStage += AddedToStageHandler;
        }

        private CustomStyle _customStyle;
        
        private void AddedToStageHandler(DisplayObject target, DisplayObject currentTarget)
        {
            Stage.Color = 0xa4a1ef;
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader("SparrowGame");
            Texture bg = SimpleTextureLoader.LoadImageFromStream(loader.GetEmbeddedResourceStream("testbg_google.png"));
            Quad quad = new Quad(512, 512);
            quad.Texture = bg;
           // _customStyle = new CustomStyle();
           // quad.SetStyle(_customStyle);
            AddChild(quad);
            quad.Touch += OnTouch;
            
            AddChild(new ComputeShaderTest());
            
            Button btn = new Button(Texture.FromColor(100, 30, 0x12ef23, 0.8f), "test button");
            btn.X = 470;
            AddChild(btn);
           
        }
        
        private void OnTouch(TouchEvent touch)
        {
            if (touch.Touches.Count > 0)
            {
                CustomEffect.Xc = touch.Touches[0].GlobalX / 512.0f;
                CustomEffect.Yc = touch.Touches[0].GlobalY / 512.0f;
                Console.Out.WriteLine(CustomEffect.Xc +" " + CustomEffect.Yc);
            }
        }

    } 
}