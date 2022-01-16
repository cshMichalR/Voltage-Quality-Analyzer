using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MathWorks;
using Matlab_testy;
namespace Analizator_Sieci
{
    class Matlab
    {

        public void Rysuj_Wykres()
        {
        
            Plot abc = new Plot();
          

            try
            {
                abc.test_plot();
            
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }
    }
}
