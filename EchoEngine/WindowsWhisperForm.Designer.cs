namespace EchoEngine
{
   partial class WindowsWhisperForm
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
         this.groupBoxSTT = new System.Windows.Forms.GroupBox();
         this.comboSTTLang = new System.Windows.Forms.ComboBox();
         this.labelSTTLang = new System.Windows.Forms.Label();
         this.btnStartSTT = new System.Windows.Forms.Button();
         this.textBox = new System.Windows.Forms.TextBox();
         this.groupBoxTTS = new System.Windows.Forms.GroupBox();
         this.labelVolume = new System.Windows.Forms.Label();
         this.labelRate = new System.Windows.Forms.Label();
         this.trackBarVolume = new System.Windows.Forms.TrackBar();
         this.trackBarRate = new System.Windows.Forms.TrackBar();
         this.btnCancel = new System.Windows.Forms.Button();
         this.btnResume = new System.Windows.Forms.Button();
         this.btnPause = new System.Windows.Forms.Button();
         this.btnSpeak = new System.Windows.Forms.Button();
         this.comboVoice = new System.Windows.Forms.ComboBox();
         this.labelVoice = new System.Windows.Forms.Label();
         this.labelStatus = new System.Windows.Forms.Label();
         this.btnBack = new System.Windows.Forms.Button();
         this.groupBoxSTT.SuspendLayout();
         this.groupBoxTTS.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.trackBarRate)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.trackBarVolume)).BeginInit();
         this.SuspendLayout();
         ////////////////////////////////////////////////////////////////////////////// 
         // groupBoxSTT
         ////////////////////////////////////////////////////////////////////////////// 
         this.groupBoxSTT.Controls.Add(this.comboSTTLang);
         this.groupBoxSTT.Controls.Add(this.labelSTTLang);
         this.groupBoxSTT.Controls.Add(this.btnStartSTT);
         this.groupBoxSTT.Location = new System.Drawing.Point(12, 12);
         this.groupBoxSTT.Name = "groupBoxSTT";
         this.groupBoxSTT.Size = new System.Drawing.Size(776, 80);
         this.groupBoxSTT.TabIndex = 0;
         this.groupBoxSTT.TabStop = false;
         this.groupBoxSTT.Text = "STT (Whisper - OpenAI 오픈소스)";

         ////////////////////////////////////////////////////////////////////////////// 
         // comboSTTLang
         ////////////////////////////////////////////////////////////////////////////// 
         this.comboSTTLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboSTTLang.FormattingEnabled = true;
         this.comboSTTLang.Items.AddRange(new object[] {
            "한국어",
            "영어"});
         this.comboSTTLang.Location = new System.Drawing.Point(320, 30);
         this.comboSTTLang.Name = "comboSTTLang";
         this.comboSTTLang.Size = new System.Drawing.Size(120, 20);
         this.comboSTTLang.TabIndex = 2;

         ////////////////////////////////////////////////////////////////////////////// 
         // labelSTTLang
         ////////////////////////////////////////////////////////////////////////////// 
         this.labelSTTLang.AutoSize = true;
         this.labelSTTLang.Location = new System.Drawing.Point(250, 33);
         this.labelSTTLang.Name = "labelSTTLang";
         this.labelSTTLang.Size = new System.Drawing.Size(61, 12);
         this.labelSTTLang.TabIndex = 1;
         this.labelSTTLang.Text = "STT 언어:";

         ////////////////////////////////////////////////////////////////////////////// 
         // btnStartSTT
         ////////////////////////////////////////////////////////////////////////////// 
         this.btnStartSTT.Location = new System.Drawing.Point(14, 28);
         this.btnStartSTT.Name = "btnStartSTT";
         this.btnStartSTT.Size = new System.Drawing.Size(80, 30);
         this.btnStartSTT.TabIndex = 0;
         this.btnStartSTT.Text = "🎤 시작";
         this.btnStartSTT.UseVisualStyleBackColor = true;
         this.btnStartSTT.Click += new System.EventHandler(this.btnStartSTT_Click);

         //////////////////////////////////////////////////////////////////////////////
         // textBox
         //////////////////////////////////////////////////////////////////////////////
         this.textBox.Location = new System.Drawing.Point(12, 98);
         this.textBox.Multiline = true;
         this.textBox.Name = "textBox";
         this.textBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
         this.textBox.Size = new System.Drawing.Size(776, 200);
         this.textBox.TabIndex = 1;

         //////////////////////////////////////////////////////////////////////////////
         // groupBoxTTS
         //////////////////////////////////////////////////////////////////////////////
         this.groupBoxTTS.Controls.Add(this.labelVolume);
         this.groupBoxTTS.Controls.Add(this.labelRate);
         this.groupBoxTTS.Controls.Add(this.trackBarVolume);
         this.groupBoxTTS.Controls.Add(this.trackBarRate);
         this.groupBoxTTS.Controls.Add(this.btnCancel);
         this.groupBoxTTS.Controls.Add(this.btnResume);
         this.groupBoxTTS.Controls.Add(this.btnPause);
         this.groupBoxTTS.Controls.Add(this.btnSpeak);
         this.groupBoxTTS.Controls.Add(this.comboVoice);
         this.groupBoxTTS.Controls.Add(this.labelVoice);
         this.groupBoxTTS.Location = new System.Drawing.Point(12, 304);
         this.groupBoxTTS.Name = "groupBoxTTS";
         this.groupBoxTTS.Size = new System.Drawing.Size(776, 180);
         this.groupBoxTTS.TabIndex = 2;
         this.groupBoxTTS.TabStop = false;
         this.groupBoxTTS.Text = "TTS (Text-to-Speech)";

         //////////////////////////////////////////////////////////////////////////////
         // labelVolume
         //////////////////////////////////////////////////////////////////////////////
         this.labelVolume.AutoSize = true;
         this.labelVolume.Location = new System.Drawing.Point(650, 120);
         this.labelVolume.Name = "labelVolume";
         this.labelVolume.Size = new System.Drawing.Size(55, 12);
         this.labelVolume.TabIndex = 9;
         this.labelVolume.Text = "볼륨: 100";

         //////////////////////////////////////////////////////////////////////////////
         // labelRate
         //////////////////////////////////////////////////////////////////////////////
         this.labelRate.AutoSize = true;
         this.labelRate.Location = new System.Drawing.Point(650, 80);
         this.labelRate.Name = "labelRate";
         this.labelRate.Size = new System.Drawing.Size(43, 12);
         this.labelRate.TabIndex = 8;
         this.labelRate.Text = "속도: 0";

         //////////////////////////////////////////////////////////////////////////////
         // trackBarVolume
         //////////////////////////////////////////////////////////////////////////////
         this.trackBarVolume.Location = new System.Drawing.Point(500, 120);
         this.trackBarVolume.Maximum = 100;
         this.trackBarVolume.Name = "trackBarVolume";
         this.trackBarVolume.Size = new System.Drawing.Size(140, 45);
         this.trackBarVolume.TabIndex = 7;
         this.trackBarVolume.TickFrequency = 10;
         this.trackBarVolume.Value = 100;
         this.trackBarVolume.ValueChanged += new System.EventHandler(this.trackBarVolume_ValueChanged);

         //////////////////////////////////////////////////////////////////////////////
         // trackBarRate
         //////////////////////////////////////////////////////////////////////////////
         this.trackBarRate.Location = new System.Drawing.Point(500, 80);
         this.trackBarRate.Minimum = -10;
         this.trackBarRate.Maximum = 10;
         this.trackBarRate.Name = "trackBarRate";
         this.trackBarRate.Size = new System.Drawing.Size(140, 45);
         this.trackBarRate.TabIndex = 6;
         this.trackBarRate.TickFrequency = 2;
         this.trackBarRate.ValueChanged += new System.EventHandler(this.trackBarRate_ValueChanged);

         //////////////////////////////////////////////////////////////////////////////
         // btnCancel
         //////////////////////////////////////////////////////////////////////////////
         this.btnCancel.Location = new System.Drawing.Point(280, 30);
         this.btnCancel.Name = "btnCancel";
         this.btnCancel.Size = new System.Drawing.Size(80, 30);
         this.btnCancel.TabIndex = 5;
         this.btnCancel.Text = "✋ 취소";
         this.btnCancel.UseVisualStyleBackColor = true;
         this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

         //////////////////////////////////////////////////////////////////////////////
         // btnResume
         //////////////////////////////////////////////////////////////////////////////
         this.btnResume.Enabled = false;
         this.btnResume.Location = new System.Drawing.Point(190, 30);
         this.btnResume.Name = "btnResume";
         this.btnResume.Size = new System.Drawing.Size(80, 30);
         this.btnResume.TabIndex = 4;
         this.btnResume.Text = "▶️ 재개";
         this.btnResume.UseVisualStyleBackColor = true;
         this.btnResume.Click += new System.EventHandler(this.btnResume_Click);

         //////////////////////////////////////////////////////////////////////////////
         // btnPause
         //////////////////////////////////////////////////////////////////////////////
         this.btnPause.Enabled = false;
         this.btnPause.Location = new System.Drawing.Point(100, 30);
         this.btnPause.Name = "btnPause";
         this.btnPause.Size = new System.Drawing.Size(80, 30);
         this.btnPause.TabIndex = 3;
         this.btnPause.Text = "⏸️ 일시정지";
         this.btnPause.UseVisualStyleBackColor = true;
         this.btnPause.Click += new System.EventHandler(this.btnPause_Click);

         //////////////////////////////////////////////////////////////////////////////
         // btnSpeak
         //////////////////////////////////////////////////////////////////////////////
         this.btnSpeak.Location = new System.Drawing.Point(14, 30);
         this.btnSpeak.Name = "btnSpeak";
         this.btnSpeak.Size = new System.Drawing.Size(80, 30);
         this.btnSpeak.TabIndex = 2;
         this.btnSpeak.Text = "🔊 읽기";
         this.btnSpeak.UseVisualStyleBackColor = true;
         this.btnSpeak.Click += new System.EventHandler(this.btnSpeak_Click);

         //////////////////////////////////////////////////////////////////////////////
         // comboVoice
         //////////////////////////////////////////////////////////////////////////////
         this.comboVoice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboVoice.FormattingEnabled = true;
         this.comboVoice.Location = new System.Drawing.Point(60, 77);
         this.comboVoice.Name = "comboVoice";
         this.comboVoice.Size = new System.Drawing.Size(300, 20);
         this.comboVoice.TabIndex = 1;

         //////////////////////////////////////////////////////////////////////////////
         // labelVoice
         //////////////////////////////////////////////////////////////////////////////
         this.labelVoice.AutoSize = true;
         this.labelVoice.Location = new System.Drawing.Point(12, 80);
         this.labelVoice.Name = "labelVoice";
         this.labelVoice.Size = new System.Drawing.Size(33, 12);
         this.labelVoice.TabIndex = 0;
         this.labelVoice.Text = "음성:";

         //////////////////////////////////////////////////////////////////////////////
         // labelStatus
         //////////////////////////////////////////////////////////////////////////////
         this.labelStatus.AutoSize = true;
         this.labelStatus.Location = new System.Drawing.Point(12, 492);
         this.labelStatus.Name = "labelStatus";
         this.labelStatus.Size = new System.Drawing.Size(69, 12);
         this.labelStatus.TabIndex = 3;
         this.labelStatus.Text = "상태: 준비";

         ////////////////////////////////////////////////////////////////////////////// 
         // btnBack
         //////////////////////////////////////////////////////////////////////////////
         this.btnBack.Location = new System.Drawing.Point(688, 490);
         this.btnBack.Name = "btnBack";
         this.btnBack.Size = new System.Drawing.Size(100, 21);
         this.btnBack.TabIndex = 4;
         this.btnBack.Text = "← 뒤로가기";
         this.btnBack.UseVisualStyleBackColor = true;
         this.btnBack.Click += new System.EventHandler(this.btnBack_Click);

         //////////////////////////////////////////////////////////////////////////////
         // WhisperForm
         //////////////////////////////////////////////////////////////////////////////
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(800, 520);
         this.Controls.Add(this.btnBack);
         this.Controls.Add(this.labelStatus);
         this.Controls.Add(this.groupBoxTTS);
         this.Controls.Add(this.textBox);
         this.Controls.Add(this.groupBoxSTT);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
         this.MaximizeBox = false;
         this.Name = "WhisperForm";
         this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
         this.Text = "Whisper STT/TTS (OpenAI 오픈소스)";
         this.groupBoxSTT.ResumeLayout(false);
         this.groupBoxSTT.PerformLayout();
         this.groupBoxTTS.ResumeLayout(false);
         this.groupBoxTTS.PerformLayout();
         ((System.ComponentModel.ISupportInitialize)(this.trackBarRate)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.trackBarVolume)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();
      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxSTT;
      private System.Windows.Forms.ComboBox comboSTTLang;
      private System.Windows.Forms.Label labelSTTLang;
      private System.Windows.Forms.Button btnStartSTT;
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
      private System.Windows.Forms.Button btnBack;
   }
}
