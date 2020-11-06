using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DivinityModManager.Controls
{
    // Source: https://stackoverflow.com/a/32870521
    public partial class SelectableTextBlock : TextBlock
    {
        TextPointer StartSelectPosition;
        TextPointer EndSelectPosition;
        public String SelectedText = "";

        public delegate void TextSelectedHandler(string SelectedText);
        public event TextSelectedHandler TextSelected;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Point mouseDownPoint = e.GetPosition(this);
            StartSelectPosition = this.GetPositionFromPoint(mouseDownPoint, true);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            Point mouseUpPoint = e.GetPosition(this);
            EndSelectPosition = this.GetPositionFromPoint(mouseUpPoint, true);

            TextRange otr = new TextRange(this.ContentStart, this.ContentEnd);
            otr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.GreenYellow));

            TextRange ntr = new TextRange(StartSelectPosition, EndSelectPosition);
            ntr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.White));

            SelectedText = ntr.Text;
            if (!(TextSelected == null))
            {
                TextSelected(SelectedText);
            }
        }
    }
}
