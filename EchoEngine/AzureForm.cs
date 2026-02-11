using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Threading;
using NAudio.Wave;
using System.IO;

namespace EchoEngine
{
   public partial class AzureForm : Form
   {
      private SpeechRecognizer recognizer;
      private SpeechSynthesizer synthesizer;
      private SpeechConfig speechConfig;
      private bool isRecognizing = false;
      private bool isSpeaking = false;
      private string speechKey = "";
      private string speechEndpoint = "";
      private List<VoiceInfo> availableVoices = new List<VoiceInfo>();
      private WaveOutEvent waveOut; // NAudio 재생용
      private WaveFileReader waveReader; // NAudio Reader (재생 중 유지 필요)
      private bool isPlayingAudio = false; // NAudio 재생 중 플래그 (중복 재생 방지)

      public AzureForm()
      {
         InitializeComponent();
         LoadAzureCredentials();
         InitializeAzureSpeech();
      }

      private void LoadAzureCredentials()
      {
         // 환경 변수에서 Azure Speech 키와 엔드포인트 가져오기
         speechKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? "";
         speechEndpoint = Environment.GetEnvironmentVariable("AZURE_SPEECH_ENDPOINT") ?? "";

         // 환경 변수가 없으면 사용자에게 입력 요청
         if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechEndpoint))
         {
            using (var dialog = new Form())
            {
               dialog.Text = "Azure Speech 설정";
               dialog.Size = new Size(400, 240);
               dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
               dialog.StartPosition = FormStartPosition.CenterParent;
               dialog.MaximizeBox = false;
               dialog.MinimizeBox = false;

               var lblKey = new Label { Text = "Speech Key:", Location = new Point(10, 20), AutoSize = true };
               var txtKey = new TextBox { Location = new Point(10, 40), Size = new Size(360, 20), UseSystemPasswordChar = true };
               txtKey.Text = speechKey;

               var lblEndpoint = new Label { Text = "Speech Endpoint:", Location = new Point(10, 70), AutoSize = true };
               var txtEndpoint = new TextBox { Location = new Point(10, 90), Size = new Size(360, 20) };
               txtEndpoint.Text = speechEndpoint;

               var lblInfo = new Label 
               { 
                  Text = "예: https://koreacentral.tts.speech.microsoft.com/cognitiveservices/v1", 
                  Location = new Point(10, 115), 
                  AutoSize = true,
                  ForeColor = Color.Gray,
                  Font = new Font("맑은 고딕", 7.5f)
               };

               var lblError = new Label 
               { 
                  Text = "", 
                  Location = new Point(10, 135), 
                  AutoSize = true,
                  ForeColor = Color.Red,
                  Font = new Font("맑은 고딕", 8f)
               };

               var btnOK = new Button { Text = "OK", Location = new Point(200, 160), Size = new Size(80, 30) };
               var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(290, 160), Size = new Size(80, 30) };

               // 입력 검증 함수
               Func<bool> validateInput = () =>
               {
                  string key = txtKey.Text.Trim();
                  string endpoint = txtEndpoint.Text.Trim();

                  if (string.IsNullOrEmpty(key))
                  {
                     lblError.Text = "Speech Key를 입력해주세요.";
                     return false;
                  }

                  if (string.IsNullOrEmpty(endpoint))
                  {
                     lblError.Text = "Speech Endpoint를 입력해주세요.";
                     return false;
                  }

                  // Endpoint URL 형식 검증
                  if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri) || 
                      (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                  {
                     lblError.Text = "올바른 Endpoint URL 형식이 아닙니다.";
                     return false;
                  }

                  lblError.Text = "";
                  return true;
               };

               // 텍스트 변경 시 검증
               txtKey.TextChanged += (s, e) => { btnOK.Enabled = validateInput(); };
               txtEndpoint.TextChanged += (s, e) => { btnOK.Enabled = validateInput(); };

               btnOK.Click += (s, e) =>
               {
                  if (validateInput())
                  {
                     speechKey = txtKey.Text.Trim();
                     speechEndpoint = txtEndpoint.Text.Trim();
                     dialog.DialogResult = DialogResult.OK;
                     dialog.Close();
                  }
               };

               dialog.Controls.AddRange(new Control[] { lblKey, txtKey, lblEndpoint, txtEndpoint, lblInfo, lblError, btnOK, btnCancel });
               dialog.CancelButton = btnCancel;

               // 초기 검증
               btnOK.Enabled = validateInput();

               if (dialog.ShowDialog() == DialogResult.OK)
               {
                  // 이미 검증 완료됨
               }
               else
               {
                  UpdateStatus("Azure Speech 설정이 취소되었습니다.");
               }
            }
         }
      }

      private void InitializeAzureSpeech()
      {
         try
         {
            if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechEndpoint))
            {
               UpdateStatus("⚠ Azure Speech 키와 엔드포인트를 설정해주세요.");
               btnStartSTT.Enabled = false;
               btnSpeak.Enabled = false;
               return;
            }

            // Speech Config 생성 (Endpoint 사용)
            speechConfig = SpeechConfig.FromEndpoint(new Uri(speechEndpoint), speechKey);

            // TTS 음성 목록 로드
            LoadVoices();

            // 언어 선택 기본값 설정
            if (comboSTTLang.Items.Count > 0)
            {
               comboSTTLang.SelectedIndex = 0; // 기본값: 한국어
            }

            UpdateStatus("Azure Speech 초기화 완료");
         }
         catch (Exception ex)
         {
            MessageBox.Show($"Azure Speech 초기화 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("초기화 실패");
         }
      }

      private async void LoadVoices()
      {
         try
         {
            if (speechConfig == null) return;

            using (var tempSynthesizer = new SpeechSynthesizer(speechConfig))
            {
               var result = await tempSynthesizer.GetVoicesAsync();
               comboVoice.Items.Clear();
               availableVoices.Clear();

               foreach (var voice in result.Voices)
               {
                  availableVoices.Add(voice);
                  comboVoice.Items.Add($"{voice.Name} ({voice.Locale})");
               }

               if (comboVoice.Items.Count > 0)
               {
                  for (int i = 0; i < availableVoices.Count; i++)
                  {
                     if (availableVoices[i].Locale.Contains("ko-KR"))
                     {
                        comboVoice.SelectedIndex = i;
                        break;
                     }
                  }

                  if (comboVoice.SelectedIndex == -1)
                  {
                     comboVoice.SelectedIndex = 0;
                  }
               }
            }
         }
         catch (Exception ex)
         {
            UpdateStatus($"음성 목록 로드 실패: {ex.Message}");
         }
      }

      private async void btnBack_Click(object sender, EventArgs e)
      {
         // 인식 중지
         if (isRecognizing)
         {
            await StopRecognition();
         }
         
         // NAudio 재생 중지
         if (waveOut != null)
         {
            try
            {
               waveOut.Stop();
               waveOut.Dispose();
            }
            catch { }
            waveOut = null;
         }
         
         if (waveReader != null)
         {
            try
            {
               waveReader.Dispose();
            }
            catch { }
            waveReader = null;
         }
         
         // Synthesizer 정리
         if (synthesizer != null)
         {
            try
            {
               await synthesizer.StopSpeakingAsync();
            }
            catch { }
            
            try
            {
               synthesizer.Dispose();
            }
            catch { }
            synthesizer = null;
         }
         
         // Recognizer 정리
         if (recognizer != null)
         {
            try
            {
               recognizer.Dispose();
            }
            catch { }
            recognizer = null;
         }
         
         this.Close();
      }

      private async void btnStartSTT_Click(object sender, EventArgs e)
      {
         if (isRecognizing)
         {
            await StopRecognition();
         }
         else
         {
            await StartRecognition();
         }
      }

      private async Task StartRecognition()
      {
         try
         {
            if (speechConfig == null)
            {
               MessageBox.Show("Azure Speech 설정이 필요합니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }

            // 언어 선택
            string language = "ko-KR"; // 기본값: 한국어
            if (comboSTTLang.SelectedItem != null)
            {
               string selectedLang = comboSTTLang.SelectedItem.ToString();
               language = selectedLang == "한국어" ? "ko-KR" : "en-US";
            }

            speechConfig.SpeechRecognitionLanguage = language;

            // 오디오 입력 설정
            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();

            // 인식기 생성
            recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            // 이벤트 핸들러 등록
            recognizer.Recognizing += (s, e) =>
            {
               if (this.IsDisposed || !this.IsHandleCreated)
                  return;
                  
               if (checkBoxInterim.Checked && !string.IsNullOrEmpty(e.Result.Text))
               {
                  try
                  {
                     this.Invoke((MethodInvoker)delegate
                     {
                        if (!this.IsDisposed)
                        {
                           UpdateStatus($"인식 중: {e.Result.Text}");
                        }
                     });
                  }
                  catch (ObjectDisposedException) { }
                  catch (InvalidOperationException) { }
               }
            };

            recognizer.Recognized += (s, e) =>
            {
               if (this.IsDisposed || !this.IsHandleCreated)
                  return;
                  
               try
               {
                  this.Invoke((MethodInvoker)delegate
                  {
                     if (!this.IsDisposed && textBox != null && !textBox.IsDisposed)
                     {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                           string currentText = textBox.Text;
                           if (!string.IsNullOrEmpty(currentText) && !currentText.EndsWith("\r\n") && !currentText.EndsWith("\n"))
                           {
                              textBox.AppendText(" ");
                           }
                           textBox.AppendText(e.Result.Text);
                           UpdateStatus($"인식: {e.Result.Text}");
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                           UpdateStatus("인식된 내용이 없습니다.");
                        }
                     }
                  });
               }
               catch (ObjectDisposedException) { }
               catch (InvalidOperationException) { }
            };

            recognizer.Canceled += (s, e) =>
            {
               if (this.IsDisposed || !this.IsHandleCreated)
                  return;
                  
               try
               {
                  this.Invoke((MethodInvoker)delegate
                  {
                     if (!this.IsDisposed)
                     {
                        if (e.Reason == CancellationReason.Error)
                        {
                           UpdateStatus($"인식 오류: {e.ErrorDetails}");
                           try
                           {
                              MessageBox.Show($"인식 오류: {e.ErrorDetails}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                           }
                           catch { }
                        }
                        else
                        {
                           UpdateStatus("인식 취소됨");
                        }
                        isRecognizing = false;
                        if (btnStartSTT != null && !btnStartSTT.IsDisposed)
                        {
                           btnStartSTT.Text = "🎤 시작";
                           btnStartSTT.Enabled = true;
                        }
                     }
                  });
               }
               catch (ObjectDisposedException) { }
               catch (InvalidOperationException) { }
            };

            recognizer.SessionStopped += (s, e) =>
            {
               if (this.IsDisposed || !this.IsHandleCreated)
                  return;
                  
               try
               {
                  this.Invoke((MethodInvoker)delegate
                  {
                     if (!this.IsDisposed)
                     {
                        isRecognizing = false;
                        if (btnStartSTT != null && !btnStartSTT.IsDisposed)
                        {
                           btnStartSTT.Text = "🎤 시작";
                           btnStartSTT.Enabled = true;
                        }
                        UpdateStatus("인식 세션 종료");
                     }
                  });
               }
               catch (ObjectDisposedException) { }
               catch (InvalidOperationException) { }
            };

            // 인식 시작
            await recognizer.StartContinuousRecognitionAsync();
            isRecognizing = true;
            btnStartSTT.Text = "🛑 중지";
            btnStartSTT.Enabled = true;
            UpdateStatus("인식 중... 말씀하세요.");
         }
         catch (Exception ex)
         {
            MessageBox.Show($"인식 시작 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("인식 시작 실패");
            isRecognizing = false;
            btnStartSTT.Text = "🎤 시작";
            btnStartSTT.Enabled = true;
         }
      }

      private async Task StopRecognition()
      {
         try
         {
            if (recognizer != null)
            {
               await recognizer.StopContinuousRecognitionAsync();
            }
            isRecognizing = false;
            
            // UI 업데이트는 안전하게 처리 (폼이 닫혔을 수 있음)
            if (!this.IsDisposed && this.IsHandleCreated)
            {
               if (this.InvokeRequired)
               {
                  this.Invoke((MethodInvoker)delegate
                  {
                     if (!this.IsDisposed && btnStartSTT != null && !btnStartSTT.IsDisposed)
                     {
                        btnStartSTT.Text = "🎤 시작";
                        btnStartSTT.Enabled = true;
                     }
                     UpdateStatus("인식 중지됨");
                  });
               }
               else
               {
                  if (btnStartSTT != null && !btnStartSTT.IsDisposed)
                  {
                     btnStartSTT.Text = "🎤 시작";
                     btnStartSTT.Enabled = true;
                  }
                  UpdateStatus("인식 중지됨");
               }
            }
         }
         catch (Exception ex)
         {
            // 폼이 닫혔을 때는 오류 메시지 표시하지 않음
            if (!this.IsDisposed && this.IsHandleCreated)
            {
               try
               {
                  MessageBox.Show($"인식 중지 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
               catch { }
            }
         }
      }

      private async void btnSpeak_Click(object sender, EventArgs e)
      {
         // 이미 재생 중이면 기존 재생 중지
         if (isSpeaking)
         {
            // NAudio 재생 중지
            if (waveOut != null)
            {
               try
               {
                  waveOut.Stop();
                  waveOut.Dispose();
               }
               catch { }
               waveOut = null;
            }
            
            if (waveReader != null)
            {
               try
               {
                  waveReader.Dispose();
               }
               catch { }
               waveReader = null;
            }
            
            // Azure SDK 합성 중지 (이미 완료되었을 수도 있음)
            if (synthesizer != null)
            {
               try
               {
                  await synthesizer.StopSpeakingAsync();
                  await Task.Delay(300); // 완전히 중지될 때까지 대기
               }
               catch { }
            }
            
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
            if (speechConfig == null)
            {
               MessageBox.Show("Azure Speech 설정이 필요합니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }
            
            // 재생 시작 전에 UI 상태 업데이트 (버튼 활성화)
            isSpeaking = true;
            btnSpeak.Enabled = false;
            btnPause.Enabled = true;
            btnResume.Enabled = false;
            UpdateStatus("TTS 합성 중...");

            // 음성 선택
            string voiceName = "ko-KR-SunHiNeural"; // 기본값
            string locale = "ko-KR";
            if (comboVoice.SelectedIndex >= 0 && comboVoice.SelectedIndex < availableVoices.Count)
            {
               // 저장된 VoiceInfo 리스트에서 직접 가져오기
               var selectedVoice = availableVoices[comboVoice.SelectedIndex];
               voiceName = selectedVoice.Name;
               locale = selectedVoice.Locale;
            }
            else if (comboVoice.SelectedItem != null)
            {
               // 폴백: 문자열 파싱
               string selectedVoiceStr = comboVoice.SelectedItem.ToString();
               if (selectedVoiceStr.Contains("("))
               {
                  int parenIndex = selectedVoiceStr.IndexOf('(');
                  voiceName = selectedVoiceStr.Substring(0, parenIndex).Trim();
                  
                  int lastParenStart = selectedVoiceStr.LastIndexOf('(');
                  int lastParenEnd = selectedVoiceStr.LastIndexOf(')');
                  if (lastParenStart >= 0 && lastParenEnd > lastParenStart)
                  {
                     locale = selectedVoiceStr.Substring(lastParenStart + 1, lastParenEnd - lastParenStart - 1).Trim();
                  }
               }
               else
               {
                  voiceName = selectedVoiceStr;
               }
            }
            
            // TTS용 SpeechConfig 생성 - FromSubscription 사용 (FromEndpoint는 문제 발생 가능)
            // Endpoint에서 region 추출, 실패 시 기본값 "koreacentral" 사용
            string region = ExtractRegionFromEndpoint(speechEndpoint);
            if (string.IsNullOrEmpty(region))
            {
               region = "koreacentral"; // 기본값
               System.Diagnostics.Debug.WriteLine($"Region 추출 실패, 기본값 사용: {region}");
            }
            
            // FromSubscription 사용 (강력 권장 - FromEndpoint는 TTS에서 문제 발생 가능)
            SpeechConfig ttsConfig = SpeechConfig.FromSubscription(speechKey, region);
            ttsConfig.SpeechSynthesisVoiceName = voiceName;
            // PCM/WAV 포맷 사용 (MP3는 NAudio 디코딩 실패 가능성 높음)
            ttsConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

            // Synthesizer 생성 - AudioConfig 제거 (NAudio로 직접 재생하므로 불필요)
            // 기존 synthesizer가 실행 중이면 먼저 중지
            if (synthesizer != null)
            {
               try
               {
                  await synthesizer.StopSpeakingAsync();
                  await Task.Delay(300); // 완전히 중지될 때까지 대기
               }
               catch { }
               
               try
               {
                  synthesizer.Dispose();
               }
               catch (InvalidOperationException)
               {
                  // 여전히 실행 중이면 잠시 대기 후 재시도
                  await Task.Delay(500);
                  try
                  {
                     synthesizer.Dispose();
                  }
                  catch { }
               }
               catch { }
               synthesizer = null;
            }
            
            // AudioConfig 없이 생성 (오디오 데이터만 받아서 NAudio로 재생)
            synthesizer = new SpeechSynthesizer(ttsConfig);
            
            // 이벤트 핸들러 등록 (중복 재생 방지 및 상태 관리용)
            // 주의: 이벤트 핸들러에서 재생을 트리거하지 않도록 주의 (NAudio로 직접 재생)
            synthesizer.SynthesisStarted += OnSynthesisStarted;
            synthesizer.SynthesisCompleted += OnSynthesisCompleted;
            synthesizer.SynthesisCanceled += OnSynthesisCanceled;
            
            System.Diagnostics.Debug.WriteLine($"SpeechSynthesizer 생성 완료: Region={region}, Voice={voiceName} (NAudio 재생 사용)");

            // 속도나 볼륨 조절이 필요한 경우 SSML 사용, 아니면 간단한 텍스트 변환
            bool useAdvancedFeatures = (trackBarRate.Value != 0 || trackBarVolume.Value != 100);
            
            System.Diagnostics.Debug.WriteLine($"TTS 시작: 텍스트 길이 = {text.Length}, 음성 = {voiceName}, 지역 = {locale}, 고급 기능 = {useAdvancedFeatures}");
            
            SpeechSynthesisResult result;
            
            if (useAdvancedFeatures)
            {
               // SSML을 사용하여 속도와 볼륨 조절
               double rate = trackBarRate.Value * 10.0; // -100% ~ +100%
               int volume = trackBarVolume.Value;
               string ratePercent = rate >= 0 ? $"+{rate}%" : $"{rate}%";
               
               // XML 특수 문자 이스케이프
               string escapedText = text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
               
               string ssml = $@"<speak version='1.0' xml:lang='{locale}'>
   <voice name='{voiceName}'>
      <prosody rate='{ratePercent}' volume='{volume}%'>
         {escapedText}
      </prosody>
   </voice>
</speak>";
               
               System.Diagnostics.Debug.WriteLine($"SSML 사용: {ssml.Substring(0, Math.Min(100, ssml.Length))}...");
               result = await synthesizer.SpeakSsmlAsync(ssml);
            }
            else
            {
               // Azure Speech SDK의 기본 TTS 사용
               System.Diagnostics.Debug.WriteLine($"SpeakTextAsync 사용");
               result = await synthesizer.SpeakTextAsync(text);
            }
            
            // 결과 확인 및 NAudio로 직접 재생
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
               long audioLength = result.AudioData?.Length ?? 0;
               System.Diagnostics.Debug.WriteLine($"TTS 성공: AudioData 길이 = {audioLength} bytes");
               
               // 오디오 데이터가 0이면 문제
               if (audioLength == 0)
               {
                  MessageBox.Show("TTS가 완료되었지만 오디오 데이터가 생성되지 않았습니다.", 
                     "TTS 경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  isSpeaking = false;
                  btnSpeak.Enabled = true;
                  btnPause.Enabled = false;
                  btnResume.Enabled = false;
                  UpdateStatus("TTS 완료 (오디오 데이터 없음)");
               }
               else
               {
                  // NAudio로 직접 재생 (Azure SDK 자동 재생 우회)
                  try
                  {
                     await PlayAudioWithNAudio(result.AudioData);
                  }
                  catch (Exception ex)
                  {
                     MessageBox.Show($"오디오 재생 실패: {ex.Message}", "재생 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                     isSpeaking = false;
                     btnSpeak.Enabled = true;
                     btnPause.Enabled = false;
                     btnResume.Enabled = false;
                     UpdateStatus("재생 실패");
                  }
               }
            }
            else
            {
               // 실패한 경우 상세 오류 정보 표시
               var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
               string errorMsg = $"TTS 실패\n\n원인: {cancellation.Reason}\n상세: {cancellation.ErrorDetails}";
               if (cancellation.ErrorCode != CancellationErrorCode.NoError)
               {
                  errorMsg += $"\n오류 코드: {cancellation.ErrorCode}";
               }
               
               System.Diagnostics.Debug.WriteLine($"TTS 실패: {errorMsg}");
               MessageBox.Show(errorMsg, "TTS 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
               
               // UI 상태 업데이트
               isSpeaking = false;
               btnSpeak.Enabled = true;
               btnPause.Enabled = false;
               btnResume.Enabled = false;
               UpdateStatus($"TTS 실패: {cancellation.ErrorDetails}");
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show($"TTS 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            isSpeaking = false;
            btnSpeak.Enabled = true;
            btnPause.Enabled = false;
            btnResume.Enabled = false;
            UpdateStatus("TTS 오류");
         }
      }

      /// <summary>
      /// NAudio를 사용하여 오디오 데이터를 직접 재생 (Azure SDK 자동 재생 우회)
      /// PCM/WAV 포맷 사용 (MP3는 NAudio 디코딩 실패 가능성 높음)
      /// </summary>
      private async Task PlayAudioWithNAudio(byte[] audioData)
      {
         // 중복 재생 방지
         if (isPlayingAudio)
         {
            System.Diagnostics.Debug.WriteLine("경고: PlayAudioWithNAudio가 이미 실행 중입니다. 중복 호출 무시.");
            return;
         }
         
         isPlayingAudio = true;
         
         try
         {
            // 기존 재생 중지 및 정리
            if (waveOut != null)
            {
               try
               {
                  waveOut.Stop();
                  waveOut.Dispose();
               }
               catch { }
               waveOut = null;
            }
            
            if (waveReader != null)
            {
               try
               {
                  waveReader.Dispose();
               }
               catch { }
               waveReader = null;
            }

            // 메모리 스트림에서 WAV(PCM) 읽기 (재생 중 유지 필요)
            var ms = new MemoryStream(audioData);
            waveReader = new WaveFileReader(ms);
            
            // WaveOut 생성 및 초기화
            waveOut = new WaveOutEvent();
            
            // 볼륨 설정 (0.0 ~ 1.0)
            float volume = trackBarVolume.Value / 100.0f;
            waveOut.Volume = volume;
            
            waveOut.Init(waveReader);
            
            // 재생 시작
            waveOut.Play();
            UpdateStatus("TTS 재생 중...");
            
            // UI 업데이트 (재생 시작 시 버튼 상태) - UI 스레드에서 실행
            if (!this.IsDisposed && this.InvokeRequired)
            {
               this.Invoke((MethodInvoker)delegate
               {
                  if (!this.IsDisposed)
                  {
                     btnPause.Enabled = true;
                     btnResume.Enabled = false;
                  }
               });
            }
            else if (!this.IsDisposed)
            {
               btnPause.Enabled = true;
               btnResume.Enabled = false;
            }
            
            // 재생 완료 대기
            while (waveOut != null && (waveOut.PlaybackState == PlaybackState.Playing || waveOut.PlaybackState == PlaybackState.Paused))
            {
               await Task.Delay(100);
               
               // 취소 확인
               if (!isSpeaking)
               {
                  if (waveOut != null)
                  {
                     waveOut.Stop();
                  }
                  break;
               }
            }
            
            // 재생 완료
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Stopped)
            {
               UpdateStatus("TTS 재생 완료");
            }
            
            // 정리
            if (waveOut != null)
            {
               waveOut.Dispose();
               waveOut = null;
            }
            
            if (waveReader != null)
            {
               waveReader.Dispose();
               waveReader = null;
            }
            
            // UI 상태 업데이트 (재생 완료)
            if (!this.IsDisposed)
            {
               if (this.InvokeRequired)
               {
                  this.Invoke((MethodInvoker)delegate
                  {
                     if (!this.IsDisposed)
                     {
                        isSpeaking = false;
                        btnSpeak.Enabled = true;
                        btnPause.Enabled = false;
                        btnResume.Enabled = false;
                     }
                  });
               }
               else
               {
                  isSpeaking = false;
                  btnSpeak.Enabled = true;
                  btnPause.Enabled = false;
                  btnResume.Enabled = false;
               }
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show($"오디오 재생 오류: {ex.Message}", "재생 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            if (waveOut != null)
            {
               try
               {
                  waveOut.Stop();
                  waveOut.Dispose();
               }
               catch { }
               waveOut = null;
            }
            
            if (waveReader != null)
            {
               try
               {
                  waveReader.Dispose();
               }
               catch { }
               waveReader = null;
            }
            
            // UI 상태 업데이트 (오류 발생 시)
            if (!this.IsDisposed)
            {
               if (this.InvokeRequired)
               {
                  this.Invoke((MethodInvoker)delegate
                  {
                     if (!this.IsDisposed)
                     {
                        isSpeaking = false;
                        btnSpeak.Enabled = true;
                        btnPause.Enabled = false;
                        btnResume.Enabled = false;
                     }
                  });
               }
               else
               {
                  isSpeaking = false;
                  btnSpeak.Enabled = true;
                  btnPause.Enabled = false;
                  btnResume.Enabled = false;
               }
            }
            
            throw;
         }
         finally
         {
            // 재생 완료 플래그 해제 (중복 재생 방지)
            isPlayingAudio = false;
            System.Diagnostics.Debug.WriteLine("PlayAudioWithNAudio 완료: isPlayingAudio = false");
         }
      }

      private void btnPause_Click(object sender, EventArgs e)
      {
         // NAudio 재생 일시정지
         try
         {
            if (waveOut != null)
            {
               var state = waveOut.PlaybackState;
               System.Diagnostics.Debug.WriteLine($"일시정지 버튼 클릭: waveOut 상태 = {state}");
               
               if (state == PlaybackState.Playing)
               {
                  waveOut.Pause();
                  btnPause.Enabled = false;
                  btnResume.Enabled = true;
                  UpdateStatus("TTS 일시정지");
                  System.Diagnostics.Debug.WriteLine("일시정지 성공");
               }
               else
               {
                  System.Diagnostics.Debug.WriteLine($"일시정지 실패: 현재 상태가 Playing이 아님 (상태: {state})");
               }
            }
            else
            {
               System.Diagnostics.Debug.WriteLine("일시정지 실패: waveOut이 null이거나 disposed됨");
            }
         }
         catch (Exception ex)
         {
            System.Diagnostics.Debug.WriteLine($"일시정지 예외: {ex.Message}");
            MessageBox.Show($"일시정지 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void btnResume_Click(object sender, EventArgs e)
      {
         // NAudio 재생 재개
         if (waveOut != null && waveOut.PlaybackState == PlaybackState.Paused)
         {
            try
            {
               waveOut.Play();
               btnPause.Enabled = true;
               btnResume.Enabled = false;
               UpdateStatus("TTS 재개");
            }
            catch (Exception ex)
            {
               MessageBox.Show($"재개 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
         }
      }

      private async void btnCancel_Click(object sender, EventArgs e)
      {
         if (isSpeaking)
         {
            // NAudio 재생 중지
            if (waveOut != null)
            {
               try
               {
                  waveOut.Stop();
                  waveOut.Dispose();
               }
               catch { }
               waveOut = null;
            }
            
            // WaveReader 정리
            if (waveReader != null)
            {
               try
               {
                  waveReader.Dispose();
               }
               catch { }
               waveReader = null;
            }
            
            // Azure SDK 합성 중지 (이미 완료되었을 수도 있음)
            if (synthesizer != null)
            {
               try
               {
                  await synthesizer.StopSpeakingAsync();
               }
               catch { }
            }
            
            isSpeaking = false;
            btnSpeak.Enabled = true;
            btnPause.Enabled = false;
            btnResume.Enabled = false;
            UpdateStatus("TTS 취소됨");
         }
      }

      /// <summary>
      /// Endpoint URL에서 Region 추출
      /// 예: https://koreacentral.tts.speech.microsoft.com/cognitiveservices/v1 -> koreacentral
      /// </summary>
      private string ExtractRegionFromEndpoint(string endpoint)
      {
         try
         {
            if (string.IsNullOrEmpty(endpoint))
               return null;

            Uri uri = new Uri(endpoint);
            string host = uri.Host;

            // 형식: {region}.tts.speech.microsoft.com 또는 {region}.speech.microsoft.com
            string[] parts = host.Split('.');
            if (parts.Length >= 4)
            {
               // 첫 번째 부분이 region
               return parts[0];
            }

            return null;
         }
         catch
         {
            return null;
         }
      }

      // TTS 이벤트 핸들러
      private void OnSynthesisStarted(object sender, SpeechSynthesisEventArgs e)
      {
         if (this.IsDisposed || !this.IsHandleCreated)
            return;
            
         try
         {
            this.Invoke((MethodInvoker)delegate
            {
               if (!this.IsDisposed)
               {
                  isSpeaking = true;
                  if (btnSpeak != null && !btnSpeak.IsDisposed)
                  {
                     btnSpeak.Enabled = false;
                  }
                  if (btnPause != null && !btnPause.IsDisposed)
                  {
                     btnPause.Enabled = false;
                  }
                  UpdateStatus("TTS 읽는 중...");
               }
            });
         }
         catch (ObjectDisposedException) { }
         catch (InvalidOperationException) { }
      }

      private void OnSynthesisCompleted(object sender, SpeechSynthesisEventArgs e)
      {
         // 주의: 이 이벤트는 Azure SDK의 합성 완료를 알리는 것이며,
         // 실제 오디오 재생은 PlayAudioWithNAudio에서 이미 처리됨
         // 여기서는 상태만 업데이트하고 재생을 다시 트리거하지 않음
         if (this.IsDisposed || !this.IsHandleCreated)
            return;
            
         try
         {
            this.Invoke((MethodInvoker)delegate
            {
               if (!this.IsDisposed)
               {
                  // NAudio 재생이 완료되면 PlayAudioWithNAudio에서 이미 상태를 업데이트하므로
                  // 여기서는 중복 업데이트를 방지하기 위해 최소한만 처리
                  System.Diagnostics.Debug.WriteLine($"Azure SDK 합성 완료: AudioData 길이 = {e.Result.AudioData?.Length ?? 0} bytes (NAudio 재생은 별도 처리)");
               }
            });
         }
         catch (ObjectDisposedException) { }
         catch (InvalidOperationException) { }
      }

      private void OnSynthesisCanceled(object sender, SpeechSynthesisEventArgs e)
      {
         if (this.IsDisposed || !this.IsHandleCreated)
            return;
            
         try
         {
            this.Invoke((MethodInvoker)delegate
            {
               if (!this.IsDisposed)
               {
                  isSpeaking = false;
                  if (btnSpeak != null && !btnSpeak.IsDisposed)
                  {
                     btnSpeak.Enabled = true;
                  }
                  if (btnPause != null && !btnPause.IsDisposed)
                  {
                     btnPause.Enabled = false;
                  }
                  if (btnResume != null && !btnResume.IsDisposed)
                  {
                     btnResume.Enabled = false;
                  }
                  
                  // 오류 정보 확인 (CancellationDetails를 통해 접근)
                  var cancellation = SpeechSynthesisCancellationDetails.FromResult(e.Result);
                  System.Diagnostics.Debug.WriteLine($"TTS 취소: Reason = {cancellation.Reason}, ErrorDetails = {cancellation.ErrorDetails}");
                  
                  if (cancellation.Reason == CancellationReason.Error)
                  {
                     string errorMsg = $"TTS 오류: {cancellation.ErrorDetails}";
                     UpdateStatus(errorMsg);
                     MessageBox.Show($"TTS 오류가 발생했습니다:\n\n{cancellation.ErrorDetails}\n\n오류 코드: {cancellation.ErrorCode}", 
                        "TTS 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  }
                  else if (cancellation.Reason == CancellationReason.EndOfStream)
                  {
                     UpdateStatus("TTS 완료");
                  }
                  else
                  {
                     UpdateStatus("TTS 취소됨");
                  }
               }
            });
         }
         catch (ObjectDisposedException) { }
         catch (InvalidOperationException) { }
      }

      private void UpdateStatus(string message)
      {
         // 폼이 닫혔거나 컨트롤이 삭제된 경우 안전하게 처리
         if (this.IsDisposed || !this.IsHandleCreated || labelStatus == null || labelStatus.IsDisposed)
            return;
            
         if (labelStatus.InvokeRequired)
         {
            try
            {
               labelStatus.Invoke((MethodInvoker)delegate
               {
                  if (!this.IsDisposed && labelStatus != null && !labelStatus.IsDisposed)
                  {
                     labelStatus.Text = $"상태: {message}";
                  }
               });
            }
            catch (ObjectDisposedException)
            {
               // 폼이 닫혔을 때는 무시
            }
            catch (InvalidOperationException)
            {
               // 컨트롤이 삭제되었을 때는 무시
            }
         }
         else
         {
            if (!this.IsDisposed && labelStatus != null && !labelStatus.IsDisposed)
            {
               labelStatus.Text = $"상태: {message}";
            }
         }
      }

      private void trackBarRate_ValueChanged(object sender, EventArgs e)
      {
         labelRate.Text = $"속도: {trackBarRate.Value}";
      }

      private void trackBarVolume_ValueChanged(object sender, EventArgs e)
      {
         labelVolume.Text = $"볼륨: {trackBarVolume.Value}";
      }

      protected override void OnFormClosing(FormClosingEventArgs e)
      {
         // 인식 중지 (동기적으로 처리)
         if (isRecognizing && recognizer != null)
         {
            try
            {
               recognizer.StopContinuousRecognitionAsync().Wait(TimeSpan.FromSeconds(1));
            }
            catch { }
         }
         
         // NAudio 재생 중지
         if (waveOut != null)
         {
            try
            {
               waveOut.Stop();
               waveOut.Dispose();
            }
            catch { }
            waveOut = null;
         }
         
         // WaveReader 정리
         if (waveReader != null)
         {
            try
            {
               waveReader.Dispose();
            }
            catch { }
            waveReader = null;
         }
         
         // Synthesizer 정리 (간단하게 처리 - WindowsWhisperForm 방식 참고)
         if (synthesizer != null)
         {
            try
            {
               synthesizer.StopSpeakingAsync().Wait(TimeSpan.FromSeconds(1));
            }
            catch { }
            
            try
            {
               synthesizer.Dispose();
            }
            catch { }
            synthesizer = null;
         }
         
         // Recognizer 정리
         if (recognizer != null)
         {
            try
            {
               recognizer.Dispose();
            }
            catch { }
            recognizer = null;
         }
         
         base.OnFormClosing(e);
      }
   }
}
