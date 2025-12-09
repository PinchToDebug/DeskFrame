using System.Windows;
using System.Windows.Media.Effects;

namespace DeskFrame.Shaders
{
    public class GrayscaleEffect : ShaderEffect
    {
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(GrayscaleEffect), 0);
        public GrayscaleEffect()
        {
            PixelShader pixelShader = new PixelShader();
            pixelShader.UriSource = new Uri("/DeskFrame;component/Shaders/GrayscaleEffect.ps", UriKind.Relative);
            this.PixelShader = pixelShader;

            this.UpdateShaderValue(InputProperty);
        }
        public Brush Input
        {
            get
            {
                return ((Brush)(this.GetValue(InputProperty)));
            }
            set
            {
                this.SetValue(InputProperty, value);
            }
        }
    }
}