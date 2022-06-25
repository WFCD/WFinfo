using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using WFInfo.Settings;

namespace WFInfo
{
    public static class Translator
    {
        public static Dictionary<string,string> frPartTranslations = new Dictionary<string, string>()
        {
            {"String","Corde" },
            {"Barrel","Canon" },
            {"Stock","Crosse" },
            {"Receiver","Culasse" },
            {"Blueprint","Schéma" },
            {"Link","Lien" },
            {"Blades","Lames" },
            {"Blade","Lame" },
            {"Gauntlet","Gantelet" },
            {"Lower Limb","Partie Inférieure" },
            {"Upper Limb","Partie Supérieure" },
            {"Neuroptics","Neuroptiques" },
            {"Systems","Systèmes" },
            {"Handle","Manche" },
            {"Ornament","Ornement" },
            {"Cerebrum","Cerveau" },
            {"Grip","Prise" },
            {"Head","Tête" },
            {"Disc","Disque" },
            {"Pouch","Pochette" },
            {"Stars","Étoiles" },
            {"Collar","Collier" },
            {"Band","Lanière" },
            {"Buckle","Boucle" },
            {"Boot","Botte" },
            {"Hilt","Garde" },
            {"Chain","Chaîne" },
            {"Harness","Harnais" },
            {"Wings","Ailes" },
            {"Guard","Quillon" },
            {"Exilus Weapon Adapter","Adaptateur d'arme Exilus" },
            {"Riven Sliver","Brisure Riven" },
            {"Ayatan Amber Star","Étoile Ayatan Ambre" },
        };
        /*
         * Replace each known words to be translated (given in localePartTranslation Dictionnary) by its english translation
         */
        public static string TranslateParName(string partName,string locale)
        {
            if (!string.IsNullOrEmpty(partName))
            {
                switch (locale)
                {
                    case "fr":
                        string localPartName = partName;
                        foreach (string key in frPartTranslations.Keys)
                        {
                            string partTranslation;
                            frPartTranslations.TryGetValue(key, out partTranslation);
                            localPartName = localPartName.Replace(key, partTranslation);
                        }
                        
                        return localPartName.Length == 0 ? partName : localPartName;
                    default:
                        return partName;
                }
            }
            else
            {
                return null;
            }
            
        }
    }
}
