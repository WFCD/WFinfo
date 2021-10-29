using System.Drawing;

namespace WFInfo.WFInfoUtil
{
    public class GraphicFns
    {
        public Pen orange { get; private set; }
        public SolidBrush red { get; private set; }
        public SolidBrush green { get; private set; }
        public Pen greenp { get; private set; }
        public Pen pinkP { get; private set; }
        public Font font { get; private set; }

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