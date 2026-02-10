using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EchoEngine
{
   public partial class MainWindow : Form
   {
      public MainWindow()
      {
         InitializeComponent();
      }

      private void btnWindows_Click(object sender, EventArgs e)
      {
         using (var form = new WindowsSpeechForm())
         {
            form.ShowDialog();
         }
      }

      private void btnWhisper_Click(object sender, EventArgs e)
      {
         using (var form = new WindowsWhisperForm())
         {
            form.ShowDialog();
         }
      }

      private void btnVosk_Click(object sender, EventArgs e)
      {
         using (var form = new WindowsVoskForm())
         {
            form.ShowDialog();
         }
      }
   }
}
