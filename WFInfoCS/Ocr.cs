using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WFInfoCS
{
    class Ocr
    {
        private int UIscaling = Settings.Scaling;
        private int DPIscaling;
        //todo  implemenet Tesseract
        //      implemenet pre-prossesing
        internal static int findRewards(Bitmap image)
        {
            updateCenter();
            refreshDPIscaling();
            refreshUiscaling();
            return 0;
            //throw new NotImplementedException();
        }

        private static Bitmap turnBW(Bitmap source)
        {
            throw new NotImplementedException();
        }

        private static void refreshUiscaling()
        {
            throw new NotImplementedException();
        }

        private static void refreshDPIscaling()
        {
            throw new NotImplementedException();
        }

        private static void updateCenter()
        {
            throw new NotImplementedException();
        }

        internal static Bitmap getReward(int rewardSlot, int TotalRewards)
        {
            throw new NotImplementedException();
        }

        internal static void proces(Bitmap reward)
        {
            reward = turnBW(reward);
            throw new NotImplementedException();
        }
    }
}
