using System.Windows;
using System.Windows.Media.Effects;

namespace DeskFrame.Shaders
{
    public class GrayscaleEffect : ShaderEffect
    {
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(GrayscaleEffect), 0);
        public static readonly DependencyProperty StrengthProperty = DependencyProperty.Register("Strength", typeof(double), typeof(GrayscaleEffect), new UIPropertyMetadata(((double)(0D)), PixelShaderConstantCallback(0)));
        public GrayscaleEffect()
        {
            PixelShader pixelShader = new PixelShader();
            pixelShader.UriSource = new Uri("/DeskFrame;component/Shaders/GrayscaleEffect.ps", UriKind.Relative);
            this.PixelShader = pixelShader;

            this.UpdateShaderValue(InputProperty);
            this.UpdateShaderValue(StrengthProperty);
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
        public double Strength
        {
            get
            {
                return ((double)(this.GetValue(StrengthProperty)));
            }
            set
            {
                this.SetValue(StrengthProperty, value);
            }
        }
    }
}