using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Speech.Synthesis;
using NAudio.Wave;
using System.Threading;
using Vosk;

namespace EchoEngine
{
   public partial class WindowsVoskForm : Form
   {
      private WaveInEvent waveIn;
      private WaveFileWriter writer;
      private SpeechSynthesizer synthesizer;
      private bool isRecording = false;
      private bool isSpeaking = false;
      private string voskModelPath = "models\\vosk-model-small-ko-0.22";

      // Vosk 관련 변수
      private Vosk.Model voskModel;
      private VoskRecognizer voskRecognizer;

      // 실시간 처리를 위한 변수
      private System.Windows.Forms.Timer realtimeTimer;
      private MemoryStream audioBuffer;
      private WaveFormat audioFormat;
      private int bufferDurationSeconds = 2; // 2초마다 처리 (Vosk는 더 빠름)
      private int bufferSizeBytes;
      private object bufferLock = new object();
      private bool isProcessing = false;

      public WindowsVoskForm()
      {
         InitializeComponent();
         InitializeSpeech();
         CheckVoskModel();

         // 언어 선택 기본값 설정
         if (comboSTTLang.Items.Count > 0)
         {
            comboSTTLang.SelectedIndex = 0; // 기본값: 한국어
         }

         // 실시간 처리 타이머 초기화
         realtimeTimer = new System.Windows.Forms.Timer();
         realtimeTimer.Interval = bufferDurationSeconds * 1000; // 2초마다
         realtimeTimer.Tick += RealtimeTimer_Tick;
      }

      private void InitializeSpeech()
      {
         try
         {
            synthesizer = new SpeechSynthesizer();
            LoadVoices();
            UpdateStatus("초기화 완료");
         }
         catch (Exception ex)
         {
            MessageBox.Show($"TTS 초기화 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

      private void CheckVoskModel()
      {
         string modelPath = Path.IsPathRooted(voskModelPath)
            ? voskModelPath
            : Path.Combine(Application.StartupPath, voskModelPath);

         // 모델 폴더 검색
         if (!Directory.Exists(modelPath))
         {
            string alternativeModel = FindVoskModel();
            if (!string.IsNullOrEmpty(alternativeModel))
            {
               voskModelPath = alternativeModel;
               modelPath = Path.IsPathRooted(alternativeModel)
                  ? alternativeModel
                  : Path.Combine(Application.StartupPath, alternativeModel);
            }
         }

         if (!Directory.Exists(modelPath))
         {
            UpdateStatus("⚠ Vosk 모델을 찾을 수 없습니다. (models\\vosk-model-small-ko-0.22 필요)");
            btnStartSTT.Enabled = true;
            return;
         }

         try
         {
            // Vosk 모델 로드
            voskModel = new Vosk.Model(modelPath);
            voskRecognizer = new VoskRecognizer(voskModel, 16000.0f);
            UpdateStatus("Vosk 준비 완료 (한국어 모델)");
            btnStartSTT.Enabled = true;
         }
         catch (Exception ex)
         {
            UpdateStatus($"⚠ Vosk 모델 로드 실패: {ex.Message}");
            btnStartSTT.Enabled = false;
         }
      }

      private void btnBack_Click(object sender, EventArgs e)
      {
         this.Close();
      }

      private void btnStartSTT_Click(object sender, EventArgs e)
      {
         if (isRecording)
         {
            StopRecording();
         }
         else
         {
            StartRecording();
         }
      }

      private void StartRecording()
      {
         // Vosk 모델 확인
         if (voskModel == null || voskRecognizer == null)
         {
            MessageBox.Show(
                "Vosk 모델이 로드되지 않았습니다.\n\n" +
                "필요한 파일:\n" +
                $"- vosk-model-small-ko-0.22 폴더\n" +
                $"- 예상 위치: {Path.Combine(Application.StartupPath, voskModelPath)}\n\n" +
                "다운로드:\n" +
                "https://alphacephei.com/vosk/models\n\n" +
                "한국어 모델: vosk-model-small-ko-0.22",
                "모델 없음",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
         }

         try
         {
            waveIn = new WaveInEvent();
            audioFormat = new WaveFormat(16000, 1); // 16kHz, Mono
            waveIn.WaveFormat = audioFormat;

            // 버퍼 크기 계산 (3초치)
            bufferSizeBytes = audioFormat.AverageBytesPerSecond * bufferDurationSeconds;
            audioBuffer = new MemoryStream();

            // 실시간 처리를 위한 버퍼에 직접 저장
            waveIn.DataAvailable += (s, a) =>
            {
               lock (bufferLock)
               {
                  if (audioBuffer != null && isRecording)
                  {
                     audioBuffer.Write(a.Buffer, 0, a.BytesRecorded);
                  }
               }
            };

            waveIn.RecordingStopped += (s, a) =>
            {
               lock (bufferLock)
               {
                  if (audioBuffer != null)
                  {
                     audioBuffer.Dispose();
                     audioBuffer = null;
                  }
               }
               if (waveIn != null)
               {
                  waveIn.Dispose();
                  waveIn = null;
               }
            };

            waveIn.StartRecording();
            isRecording = true;

            // 실시간 처리 타이머 시작
            realtimeTimer.Start();

            btnStartSTT.Text = "🛑 중지";
            btnStartSTT.Enabled = true;
            UpdateStatus("실시간 인식 중... 말씀하세요.");
         }
         catch (Exception ex)
         {
            MessageBox.Show($"녹음 시작 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void StopRecording()
      {
         try
         {
            // 실시간 처리 타이머 중지
            if (realtimeTimer != null)
            {
               realtimeTimer.Stop();
            }

            if (waveIn != null)
            {
               waveIn.StopRecording();
            }
            isRecording = false;
            btnStartSTT.Text = "🎤 시작";
            btnStartSTT.Enabled = true;
            UpdateStatus("녹음 중지됨");
         }
         catch (Exception ex)
         {
            MessageBox.Show($"녹음 중지 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            isRecording = false;
            btnStartSTT.Text = "🎤 시작";
            btnStartSTT.Enabled = true;
         }
      }

      /// <summary>
      /// 실시간 처리 타이머 이벤트 - 주기적으로 버퍼를 처리
      /// </summary>
      private void RealtimeTimer_Tick(object sender, EventArgs e)
      {
         if (!isRecording || isProcessing)
            return;

         byte[] bufferData = null;

         lock (bufferLock)
         {
            if (audioBuffer == null || audioBuffer.Length < 16000) // 최소 1초치 데이터 필요
               return;

            // 버퍼 복사 및 새 버퍼 생성
            bufferData = audioBuffer.ToArray();
            audioBuffer.SetLength(0);
            audioBuffer.Position = 0;
         }

         // 백그라운드에서 처리
         if (bufferData != null)
         {
            Task.Run(() => ProcessRealtimeAudio(bufferData));
         }
      }

      /// <summary>
      /// 실시간 오디오 버퍼 처리 (Vosk 사용)
      /// </summary>
      private void ProcessRealtimeAudio(byte[] audioData)
      {
         if (isProcessing || audioData == null || audioData.Length < 8000 || voskRecognizer == null)
            return;

         isProcessing = true;
         try
         {
            // Vosk는 16kHz, 16bit, Mono PCM 데이터를 직접 처리
            lock (bufferLock)
            {
               if (voskRecognizer.AcceptWaveform(audioData, audioData.Length))
               {
                  // 최종 결과
                  string result = voskRecognizer.Result();
                  ProcessVoskResult(result);
               }
               else
               {
                  // 부분 결과
                  string partial = voskRecognizer.PartialResult();
                  if (!string.IsNullOrWhiteSpace(partial))
                  {
                     ProcessVoskPartial(partial);
                  }
               }
            }
         }
         catch (Exception ex)
         {
            this.Invoke((MethodInvoker)delegate
            {
               System.Diagnostics.Debug.WriteLine($"실시간 처리 오류: {ex.Message}");
            });
         }
         finally
         {
            isProcessing = false;
         }
      }

      /// <summary>
      /// Vosk 최종 결과 처리
      /// </summary>
      private void ProcessVoskResult(string jsonResult)
      {
         try
         {
            // JSON 파싱: {"text": "인식된 텍스트"}
            if (string.IsNullOrWhiteSpace(jsonResult))
               return;

            int textIdx = jsonResult.IndexOf("\"text\"");
            if (textIdx < 0)
               return;

            int colonIdx = jsonResult.IndexOf(':', textIdx);
            if (colonIdx < 0)
               return;

            int quoteStart = jsonResult.IndexOf('"', colonIdx);
            if (quoteStart < 0)
               return;

            int quoteEnd = jsonResult.IndexOf('"', quoteStart + 1);
            if (quoteEnd <= quoteStart)
               return;

            string recognizedText = jsonResult.Substring(quoteStart + 1, quoteEnd - quoteStart - 1).Trim();

            if (!string.IsNullOrWhiteSpace(recognizedText))
            {
               this.Invoke((MethodInvoker)delegate
               {
                  string currentText = textBox.Text;
                  if (!string.IsNullOrEmpty(currentText) && !currentText.EndsWith("\r\n") && !currentText.EndsWith("\n"))
                  {
                     textBox.AppendText(" ");
                  }
                  textBox.AppendText(recognizedText);
                  UpdateStatus($"인식: {recognizedText}");
               });
            }
         }
         catch (Exception ex)
         {
            System.Diagnostics.Debug.WriteLine($"Vosk 결과 처리 오류: {ex.Message}");
         }
      }

      /// <summary>
      /// Vosk 부분 결과 처리 (선택적)
      /// </summary>
      private void ProcessVoskPartial(string jsonPartial)
      {
         // 부분 결과는 상태 표시에만 사용 (선택적)
         // 필요시 구현
      }

      /// <summary>
      /// Vosk 모델 파일을 여러 위치에서 검색
      /// </summary>
      private string FindVoskModel()
      {
         // 검색할 경로 목록
         List<string> searchPaths = new List<string>
            {
                Path.Combine(Application.StartupPath, "models"),
                Application.StartupPath,
                Path.Combine(Application.StartupPath, "vosk", "models"),
                Path.GetDirectoryName(Application.StartupPath)
            };

         // 검색할 모델 폴더 이름
         string[] modelNames = { "vosk-model-small-ko-0.22", "vosk-model-ko-0.22" };

         foreach (string searchPath in searchPaths)
         {
            if (string.IsNullOrEmpty(searchPath) || !Directory.Exists(searchPath))
               continue;

            foreach (string modelName in modelNames)
            {
               string fullPath = Path.Combine(searchPath, modelName);
               if (Directory.Exists(fullPath))
               {
                  // 상대 경로로 변환
                  Uri startupUri = new Uri(Application.StartupPath + Path.DirectorySeparatorChar);
                  Uri fileUri = new Uri(fullPath);
                  Uri relativeUri = startupUri.MakeRelativeUri(fileUri);
                  return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
               }
            }

            // models 하위 디렉토리도 검색
            try
            {
               string modelsPath = Path.Combine(searchPath, "models");
               if (Directory.Exists(modelsPath))
               {
                  foreach (string modelName in modelNames)
                  {
                     string fullPath = Path.Combine(modelsPath, modelName);
                     if (Directory.Exists(fullPath))
                     {
                        // 상대 경로로 변환
                        Uri startupUri = new Uri(Application.StartupPath + Path.DirectorySeparatorChar);
                        Uri fileUri = new Uri(fullPath);
                        Uri relativeUri = startupUri.MakeRelativeUri(fileUri);
                        return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
                     }
                  }
               }
            }
            catch { }
         }

         return null;
      }

      // TTS 관련 메서드들
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
         if (labelStatus.InvokeRequired)
         {
            labelStatus.Invoke((MethodInvoker)delegate
            {
               labelStatus.Text = $"상태: {message}";
            });
         }
         else
         {
            labelStatus.Text = $"상태: {message}";
         }
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
         // 실시간 처리 타이머 중지
         if (realtimeTimer != null)
         {
            realtimeTimer.Stop();
            realtimeTimer.Dispose();
         }

         if (isRecording && waveIn != null)
         {
            waveIn.StopRecording();
         }
         if (writer != null)
         {
            writer.Dispose();
         }
         if (waveIn != null)
         {
            waveIn.Dispose();
         }
         if (audioBuffer != null)
         {
            lock (bufferLock)
            {
               audioBuffer.Dispose();
               audioBuffer = null;
            }
         }

         // Vosk 리소스 정리
         if (voskRecognizer != null)
         {
            voskRecognizer.Dispose();
            voskRecognizer = null;
         }
         if (voskModel != null)
         {
            voskModel.Dispose();
            voskModel = null;
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