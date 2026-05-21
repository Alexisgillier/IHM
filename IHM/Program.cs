using System;
using System.Windows.Forms;

namespace RucheMQTTApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // On lance "Form1" car c'est le nom de la classe dans l'autre fichier
            Application.Run(new Form1());
        }
    }
}