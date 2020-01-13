using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static Process Warframe;

        //todo  implemenet Tesseract
        //      implemenet pre-prossesing

        internal static int findRewards(Bitmap image)
        {
            Main.updatedStatus("test", 1);
            //updateCenter();
            //refreshDPIscaling();
            //refreshUiscaling();
            return 0;
            //throw new NotImplementedException();
        }

        public static void verifyWarframe()
        {
            if(Warframe != null){return;}

            foreach (Process process in Process.GetProcesses()){
                try{

                }catch{

                }
            }
            throw new NotImplementedException();
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
