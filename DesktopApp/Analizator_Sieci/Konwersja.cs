using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analizator_Sieci
{
    class Konwersja
    {
        int rozdzielczosc_adc = 4096;



        public int[] Split_Sygnalu(string sygnal)
        {
            string[] TabPom = sygnal.Split(' ');
            int [] probki= new int[TabPom.Length];
          
            for(int i=1;i<TabPom.Length-1;i++)
            {

                probki[i] = Convert.ToInt32(TabPom[i]);    
            }

                return probki;
        }
     public double Cyfrowy_Na_Napiecie_Skuteczne(int[] cyfrowy)
        {
            double v_rms = 0;
            double napiecie=0;
            double v_max=0;
         
         
            for (int i=0; i < cyfrowy.Length; i++)
            {
                if (cyfrowy[i] >= rozdzielczosc_adc / 2)
                {
                    napiecie = 5 * (cyfrowy[i] - (rozdzielczosc_adc / 2)) / (rozdzielczosc_adc / 2);

                    if (v_max < napiecie)
                    {
                        v_max = napiecie;
                    }
                }
           
            }
            v_rms = v_max * 230 / 5;
            v_rms = v_rms * Math.Sqrt(2);

            return v_rms;
        }

    }

}
