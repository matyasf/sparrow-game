using Sparrow.Rendering;
using Sparrow.Styles;

namespace SparrowGame.Shared.CustomMesh
{
    public class CustomStyle : MeshStyle
    {
        public override MeshEffect CreateEffect()
        {
            return new CustomEffect();
        }
    }
}