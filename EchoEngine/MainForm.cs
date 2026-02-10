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
   public partial class MainForm : Form
   {
      public MainForm()
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

      private void btnChrome_Click(object sender, EventArgs e)
      {
         using (var form = new ChromeWebViewForm())
         {
            form.ShowDialog();
         }
      }
   }
}
