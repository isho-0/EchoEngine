namespace EchoEngine
{
   partial class ChromeForm
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
        
      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxSTT;
      private System.Windows.Forms.Button btnStartSTT;
      private System.Windows.Forms.Button btnStopSTT;
      private System.Windows.Forms.CheckBox checkBoxInterim;
      private System.Windows.Forms.TextBox textBox;
      private System.Windows.Forms.GroupBox groupBoxTTS;
      private System.Windows.Forms.Label labelVoice;
      private System.Windows.Forms.ComboBox comboVoice;
      private System.Windows.Forms.Button btnSpeak;
      private System.Windows.Forms.Button btnPause;
      private System.Windows.Forms.Button btnResume;
      private System.Windows.Forms.Button btnCancel;
      private System.Windows.Forms.TrackBar trackBarRate;
      private System.Windows.Forms.TrackBar trackBarVolume;
      private System.Windows.Forms.Label labelRate;
      private System.Windows.Forms.Label labelVolume;
      private System.Windows.Forms.Label labelStatus;
      private System.Windows.Forms.ComboBox comboSTTLang;
      private System.Windows.Forms.Label labelSTTLang;
      private System.Windows.Forms.Button btnBack;
   }
}
