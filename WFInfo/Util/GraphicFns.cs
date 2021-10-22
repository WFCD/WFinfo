using System.Drawing;

namespace WFInfo.Util
{
    public class GraphicFns
    {
        public Pen orange;
        public SolidBrush red;
        public SolidBrush green;
        public Pen greenp;
        public Pen pinkP;
        public Font font;

        public GraphicFns()
        {
            orange = new Pen(Brushes.Orange);
            red = new SolidBrush(Color.FromArgb(100, 139, 0, 0));
            green = new SolidBrush(Color.FromArgb(100, 255, 165, 0));
            greenp = new Pen(green);
            pinkP = new Pen(Brushes.Pink);
            font = new Font("Arial", 16);
        }

        public void Dispose()
        {
            orange.Dispose();
            red.Dispose();
            green.Dispose();
            greenp.Dispose();
            pinkP.Dispose();
            font.Dispose();
        }
    }
}