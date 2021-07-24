using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace SpeechToTextfileWPF
{
    /// <summary>
    /// Viewboxの拡張
    /// </summary>
    /// <seealso cref="https://ja.stackoverflow.com/questions/7670/"/>
    class ViewboxEx : Viewbox
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            FrameworkElement child = this.Child as FrameworkElement;
            if (child == null)
            {
                return base.MeasureOverride(availableSize);
            }
            child.Width = double.NaN;
            child.Height = double.NaN;
            switch (this.StretchDirection)
            {
                case StretchDirection.Both:
                    child.ClearValue(FrameworkElement.MinWidthProperty);
                    child.ClearValue(FrameworkElement.MaxWidthProperty);
                    break;
                case StretchDirection.DownOnly:
                    child.MinWidth = availableSize.Width;//DownOnly
                    child.ClearValue(FrameworkElement.MaxWidthProperty);
                    break;
                case StretchDirection.UpOnly:
                    child.ClearValue(FrameworkElement.MinWidthProperty);
                    child.MaxWidth = availableSize.Width;//DownOnly
                    break;
                default:
                    break;
            }
            Size sz = base.MeasureOverride(availableSize);
            if (sz.Width == 0 || sz.Height == 0)
            {
            }
            else
            {
                Size csz = Child.DesiredSize;
                double thisRatio = availableSize.Width / availableSize.Height;
                double childRatio = child.DesiredSize.Width / child.DesiredSize.Height;
                if (childRatio != thisRatio)
                {
                    double div = 1;
                    child.Width = child.DesiredSize.Height * thisRatio;
                    child.Height = double.NaN;
                    sz = base.MeasureOverride(availableSize);
                    for (int i = 0; i < 10; i++)
                    {
                        childRatio = child.DesiredSize.Width / child.DesiredSize.Height;
                        if (childRatio < thisRatio)
                        {
                            child.Width = child.DesiredSize.Width + csz.Width / div;
                        }
                        else if (childRatio > thisRatio)
                        {
                            child.Width = Math.Max(0, child.DesiredSize.Width - csz.Width / div);
                        }
                        else if (childRatio == thisRatio)
                        {
                            break;
                        }
                        sz = base.MeasureOverride(availableSize);
                        div *= 2;
                    }
                }
            }
            return sz;
        }
    }
}
