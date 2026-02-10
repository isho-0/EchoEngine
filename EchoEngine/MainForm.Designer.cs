
namespace EchoEngine
{
   partial class MainForm
   {
      /// <summary>
      /// 필수 디자이너 변수입니다.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// 사용 중인 모든 리소스를 정리합니다.
      /// </summary>
      /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form 디자이너에서 생성한 코드

      /// <summary>
      /// 디자이너 지원에 필요한 메서드입니다. 
      /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
      /// </summary>
      private void InitializeComponent()
      {
         this.btnWindows = new System.Windows.Forms.Button();
         this.btnChrome = new System.Windows.Forms.Button();
         this.labelTitle = new System.Windows.Forms.Label();
         this.SuspendLayout();
         // 
         // btnWindows
         // 
         this.btnWindows.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
         this.btnWindows.Location = new System.Drawing.Point(150, 150);
         this.btnWindows.Name = "btnWindows";
         this.btnWindows.Size = new System.Drawing.Size(200, 80);
         this.btnWindows.TabIndex = 0;
         this.btnWindows.Text = "Windows STT/TTS\r\n(System.Speech)";
         this.btnWindows.UseVisualStyleBackColor = true;
         this.btnWindows.Click += new System.EventHandler(this.btnWindows_Click);
         // 
         // btnChrome
         // 
         this.btnChrome.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
         this.btnChrome.Location = new System.Drawing.Point(450, 150);
         this.btnChrome.Name = "btnChrome";
         this.btnChrome.Size = new System.Drawing.Size(200, 80);
         this.btnChrome.TabIndex = 1;
         this.btnChrome.Text = "Chrome STT/TTS\r\n(WebView2)";
         this.btnChrome.UseVisualStyleBackColor = true;
         this.btnChrome.Click += new System.EventHandler(this.btnChrome_Click);
         // 
         // labelTitle
         // 
         this.labelTitle.AutoSize = true;
         this.labelTitle.Font = new System.Drawing.Font("맑은 고딕", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
         this.labelTitle.Location = new System.Drawing.Point(280, 60);
         this.labelTitle.Name = "labelTitle";
         this.labelTitle.Size = new System.Drawing.Size(240, 30);
         this.labelTitle.TabIndex = 2;
         this.labelTitle.Text = "STT/TTS 엔진 선택";
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(800, 350);
         this.Controls.Add(this.labelTitle);
         this.Controls.Add(this.btnChrome);
         this.Controls.Add(this.btnWindows);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
         this.MaximizeBox = false;
         this.Name = "MainForm";
         this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
         this.Text = "EchoEngine - STT/TTS 테스트";
         this.ResumeLayout(false);
         this.PerformLayout();
      }

      #endregion

      private System.Windows.Forms.Button btnWindows;
      private System.Windows.Forms.Button btnChrome;
      private System.Windows.Forms.Label labelTitle;
   }
}

