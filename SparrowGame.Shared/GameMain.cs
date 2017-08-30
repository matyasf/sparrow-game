
using Sparrow.Core;
using Sparrow.Display;
using Sparrow.Textures;

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

        private void AddedToStageHandler(DisplayObject target, DisplayObject currentTarget)
        {
            test = new ComputeShaderTest();
            AddChild(test);

            Button btn = new Button(Texture.FromColor(100, 30, 0x12ef23, 0.8f), "test button");
            btn.X = 520;
            AddChild(btn);
        }

    } 
}