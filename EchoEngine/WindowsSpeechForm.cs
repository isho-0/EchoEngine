using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Globalization;

namespace EchoEngine
{
   public partial class WindowsSpeechForm : Form
   {
      private SpeechRecognitionEngine recognizer;
      private SpeechSynthesizer synthesizer;
      private bool isRecognizing = false;
      private bool isSpeaking = false;

      public WindowsSpeechForm()
      {
         InitializeComponent();
         InitializeSpeech();
      }

      private void btnBack_Click(object sender, EventArgs e)
      {
         this.Close();
      }

      private void InitializeSpeech()
      {
         try
         {
            // TTS 초기화
            synthesizer = new SpeechSynthesizer();
            LoadVoices();

            // 설치된 STT 인식기 확인
            var installedRecognizers = SpeechRecognitionEngine.InstalledRecognizers();
            string statusMsg = "초기화 완료";

            // Windows 11 감지
            bool isWindows11 = Environment.OSVersion.Version.Major >= 10 &&
                               Environment.OSVersion.Version.Build >= 22000;

            if (installedRecognizers.Any())
            {
               var recognizerNames = string.Join(", ", installedRecognizers.Select(r => r.Culture.Name));
               statusMsg += $"\n설치된 STT 인식기: {recognizerNames}";
            }
            else
            {
               if (isWindows11)
               {
                  statusMsg += "\n⚠ System.Speech API 인식기를 찾을 수 없습니다.";
                  statusMsg += "\n\nWindows 11에서는 Windows Runtime API를 사용합니다.";
                  statusMsg += "\n(설정에서 '기본 음성 인식'이 설치되어 있어도";
                  statusMsg += "\n System.Speech API는 다른 인식기를 찾습니다)";
                  statusMsg += "\n\n해결 방법:";
                  statusMsg += "\n1. Whisper STT 사용 (오프라인, 권장)";
                  statusMsg += "\n2. .NET 6+로 마이그레이션하여 Windows Runtime API 사용";
               }
               else
               {
                  statusMsg += "\n⚠ STT 인식기가 설치되어 있지 않습니다.";
                  statusMsg += "\n(설정 > 시간 및 언어 > 언어 > 한국어 옵션 > 음성 인식 다운로드)";
               }
            }

            UpdateStatus(statusMsg);
         }
         catch (Exception ex)
         {
            MessageBox.Show($"음성 인식 초기화 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void LoadVoices()
      {
         comboVoice.Items.Clear();
         foreach (InstalledVoice voice in synthesizer.GetInstalledVoices())
         {
            comboVoice.Items.Add(voice.VoiceInfo.Name);
         }
         if (comboVoice.Items.Count > 0)
         {
            comboVoice.SelectedIndex = 0;
         }
      }

      private void SetupRecognition()
      {
         // DictationGrammar 사용 (자유 발화)
         recognizer.LoadGrammar(new DictationGrammar());

         recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
         recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
         recognizer.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
      }

      private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
      {
         if (e.Result.Confidence > 0.7)
         {
            string text = textBox.Text;
            if (!string.IsNullOrEmpty(text) && !text.EndsWith("\r\n"))
            {
               textBox.AppendText("\r\n");
            }
            textBox.AppendText(e.Result.Text);
            UpdateStatus($"인식됨: {e.Result.Text}");
         }
      }

      private void Recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
      {
         if (checkBoxInterim.Checked)
         {
            UpdateStatus($"중간 결과: {e.Result.Text}");
         }
      }

      private void Recognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
      {
         UpdateStatus("인식 실패");
      }

      private void btnStartSTT_Click(object sender, EventArgs e)
      {
         if (isRecognizing) return;

         try
         {
            // 기존 인식기가 있으면 정리
            if (recognizer != null)
            {
               try
               {
                  recognizer.RecognizeAsyncStop();
                  recognizer.Dispose();
               }
               catch { }
               recognizer = null;
            }

            // 언어 선택 확인
            if (comboSTTLang.SelectedItem == null)
            {
               MessageBox.Show("언어를 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
               return;
            }

            string lang = comboSTTLang.SelectedItem.ToString();
            CultureInfo culture;

            if (lang == "한국어")
            {
               culture = new CultureInfo("ko-KR");
            }
            else
            {
               culture = new CultureInfo("en-US");
            }

            // 설치된 인식기 확인
            var installedRecognizers = SpeechRecognitionEngine.InstalledRecognizers();

            // Windows 11 감지
            bool isWindows11 = Environment.OSVersion.Version.Major >= 10 &&
                               Environment.OSVersion.Version.Build >= 22000;

            if (!installedRecognizers.Any())
            {
               string message = "System.Speech API 인식기를 찾을 수 없습니다.\n\n";

               if (isWindows11)
               {
                  message += "⚠ Windows 11 호환성 문제:\n";
                  message += "Windows 11에서는 Windows Runtime API를 사용합니다.\n";
                  message += "설정에서 '기본 음성 인식'이 설치되어 있어도,\n";
                  message += "System.Speech API는 다른 레거시 인식기를 찾습니다.\n\n";
                  message += "해결 방법:\n";
                  message += "1. Whisper STT 사용 (오프라인, 권장)\n";
                  message += "   - 메인 메뉴에서 'Whisper STT' 선택\n";
                  message += "   - whisper.exe와 모델 파일 필요\n\n";
                  message += "2. .NET 6 이상으로 마이그레이션\n";
                  message += "   - Windows Runtime API 직접 사용 가능\n";
                  message += "   - Windows 11의 Windows+H와 동일한 API\n\n";
                  message += "3. Windows 10으로 다운그레이드 (비권장)";
               }
               else
               {
                  message += "Windows 음성 인식 기능이 설치되어 있지 않습니다.\n\n";
                  message += "해결 방법:\n";
                  message += "1. 설정 > 시간 및 언어 > 언어\n";
                  message += "2. 한국어 옵션 클릭\n";
                  message += "3. '음성' 섹션에서 한국어 음성 인식 다운로드";
               }

               MessageBox.Show(
                   message,
                   "음성 인식 기능 없음",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Warning);
               return;
            }

            // 사용 가능한 인식기 확인
            CultureInfo selectedCulture = null;

            // 먼저 선택한 언어 시도
            try
            {
               var testRecognizer = new SpeechRecognitionEngine(culture);
               testRecognizer.Dispose();
               selectedCulture = culture;
            }
            catch
            {
               // 설치된 인식기 중에서 찾기
               foreach (var recognizerInfo in installedRecognizers)
               {
                  try
                  {
                     var testRecognizer = new SpeechRecognitionEngine(recognizerInfo.Culture);
                     testRecognizer.Dispose();
                     selectedCulture = recognizerInfo.Culture;
                     break;
                  }
                  catch { }
               }
            }

            if (selectedCulture == null)
            {
               string availableCultures = string.Join(", ", installedRecognizers.Select(r => r.Culture.Name));
               MessageBox.Show(
                   $"선택한 언어({culture.Name})의 인식기를 사용할 수 없습니다.\n\n" +
                   $"설치된 인식기: {availableCultures}\n\n" +
                   $"Windows 설정 > 시간 및 언어 > 언어에서 해당 언어의 음성 인식기를 설치해주세요.",
                   "오류",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Error);
               return;
            }

            if (selectedCulture.Name != culture.Name)
            {
               MessageBox.Show(
                   $"선택한 언어({culture.Name})의 인식기가 설치되어 있지 않습니다.\n" +
                   $"{selectedCulture.Name} 인식기를 사용합니다.",
                   "알림",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Information);
            }

            culture = selectedCulture;

            // 인식기 생성
            recognizer = new SpeechRecognitionEngine(culture);
            SetupRecognition();

            // Windows 11 호환성: 마이크 권한 확인
            try
            {
               recognizer.SetInputToDefaultAudioDevice();
            }
            catch (InvalidOperationException micEx)
            {
               string micErrorMsg = $"마이크 접근 오류: {micEx.Message}\n\n";
               micErrorMsg += "Windows 11에서 해결 방법:\n";
               micErrorMsg += "1. Windows 설정 > 개인 정보 > 마이크\n";
               micErrorMsg += "2. '마이크 액세스' 켜기\n";
               micErrorMsg += "3. '데스크톱 앱이 마이크에 액세스하도록 허용' 켜기\n";
               micErrorMsg += "4. 앱을 관리자 권한으로 실행해보세요";

               MessageBox.Show(micErrorMsg, "마이크 접근 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);

               if (recognizer != null)
               {
                  recognizer.Dispose();
                  recognizer = null;
               }

               isRecognizing = false;
               btnStartSTT.Enabled = true;
               btnStopSTT.Enabled = false;
               return;
            }

            recognizer.RecognizeAsync(RecognizeMode.Multiple);

            isRecognizing = true;
            btnStartSTT.Enabled = false;
            btnStopSTT.Enabled = true;
            UpdateStatus($"STT 인식 중... (언어: {culture.Name}) 마이크로 말해보세요.");
         }
         catch (Exception ex)
         {
            string errorMsg = $"STT 시작 실패: {ex.Message}\n\n";

            // 설치된 인식기 정보 추가
            try
            {
               var installedRecognizers = SpeechRecognitionEngine.InstalledRecognizers();
               if (installedRecognizers.Any())
               {
                  errorMsg += $"설치된 인식기: {string.Join(", ", installedRecognizers.Select(r => r.Culture.Name))}\n\n";
               }
               else
               {
                  errorMsg += "설치된 인식기가 없습니다.\n\n";
               }
            }
            catch { }

            errorMsg += "해결 방법:\n";
            errorMsg += "1. Windows 설정 > 시간 및 언어 > 언어\n";
            errorMsg += "2. 한국어 옵션 > 음성 인식 다운로드\n\n";
            errorMsg += "Windows 11 추가 확인사항:\n";
            errorMsg += "3. Windows 설정 > 개인 정보 > 마이크\n";
            errorMsg += "4. '마이크 액세스' 및 '데스크톱 앱이 마이크에 액세스하도록 허용' 켜기\n";
            errorMsg += "5. 앱을 관리자 권한으로 실행\n\n";
            errorMsg += "참고: Windows 11의 Windows+H는 Windows Runtime API를 사용합니다.\n";
            errorMsg += "현재 코드는 System.Speech API를 사용합니다.";

            MessageBox.Show(
                errorMsg,
                "오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            isRecognizing = false;
            btnStartSTT.Enabled = true;
            btnStopSTT.Enabled = false;
         }
      }

      private void btnStopSTT_Click(object sender, EventArgs e)
      {
         if (recognizer != null && isRecognizing)
         {
            recognizer.RecognizeAsyncStop();
            isRecognizing = false;
            btnStartSTT.Enabled = true;
            btnStopSTT.Enabled = false;
            UpdateStatus("STT 중지됨");
         }
      }

      private void btnSpeak_Click(object sender, EventArgs e)
      {
         if (isSpeaking)
         {
            synthesizer.SpeakAsyncCancelAll();
            isSpeaking = false;
            btnSpeak.Enabled = true;
            btnPause.Enabled = false;
            btnResume.Enabled = false;
            UpdateStatus("TTS 취소됨");
            return;
         }

         string text = textBox.Text.Trim();
         if (string.IsNullOrEmpty(text))
         {
            MessageBox.Show("읽을 텍스트가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
         }

         try
         {
            if (comboVoice.SelectedItem != null)
            {
               synthesizer.SelectVoice(comboVoice.SelectedItem.ToString());
            }

            synthesizer.Rate = trackBarRate.Value;
            synthesizer.Volume = trackBarVolume.Value;

            synthesizer.SpeakCompleted += (s, args) =>
            {
               isSpeaking = false;
               btnSpeak.Enabled = true;
               btnPause.Enabled = false;
               btnResume.Enabled = false;
               UpdateStatus("TTS 완료");
            };

            synthesizer.SpeakAsync(text);
            isSpeaking = true;
            btnSpeak.Enabled = false;
            btnPause.Enabled = true;
            UpdateStatus("TTS 읽는 중...");
         }
         catch (Exception ex)
         {
            MessageBox.Show($"TTS 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void btnPause_Click(object sender, EventArgs e)
      {
         if (synthesizer != null && isSpeaking)
         {
            synthesizer.Pause();
            btnPause.Enabled = false;
            btnResume.Enabled = true;
            UpdateStatus("TTS 일시정지");
         }
      }

      private void btnResume_Click(object sender, EventArgs e)
      {
         if (synthesizer != null && isSpeaking)
         {
            synthesizer.Resume();
            btnPause.Enabled = true;
            btnResume.Enabled = false;
            UpdateStatus("TTS 재개");
         }
      }

      private void btnCancel_Click(object sender, EventArgs e)
      {
         if (synthesizer != null)
         {
            synthesizer.SpeakAsyncCancelAll();
            isSpeaking = false;
            btnSpeak.Enabled = true;
            btnPause.Enabled = false;
            btnResume.Enabled = false;
            UpdateStatus("TTS 취소됨");
         }
      }

      private void UpdateStatus(string message)
      {
         labelStatus.Text = $"상태: {message}";
      }

      private void trackBarRate_ValueChanged(object sender, EventArgs e)
      {
         labelRate.Text = $"속도: {trackBarRate.Value}";
         if (synthesizer != null && isSpeaking)
         {
            synthesizer.Rate = trackBarRate.Value;
         }
      }

      private void trackBarVolume_ValueChanged(object sender, EventArgs e)
      {
         labelVolume.Text = $"볼륨: {trackBarVolume.Value}";
         if (synthesizer != null && isSpeaking)
         {
            synthesizer.Volume = trackBarVolume.Value;
         }
      }

      protected override void OnFormClosing(FormClosingEventArgs e)
      {
         if (recognizer != null)
         {
            recognizer.RecognizeAsyncStop();
            recognizer.Dispose();
         }
         if (synthesizer != null)
         {
            synthesizer.SpeakAsyncCancelAll();
            synthesizer.Dispose();
         }
         base.OnFormClosing(e);
      }
   }
}
