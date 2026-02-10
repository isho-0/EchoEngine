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

namespace EchoEngine
{
   public partial class WindowsWhisperForm : Form
   {
      private WaveInEvent waveIn;
      private WaveFileWriter writer;
      private SpeechSynthesizer synthesizer;
      private bool isRecording = false;
      private bool isSpeaking = false;
      private string whisperExePath = "whisper.exe";
      private string whisperModelPath = "models\\ggml-base.bin";
      private string tempWavPath = "temp_input.wav";
      private string tempOutputPath = "temp_output.txt";
      
      // 실시간 처리를 위한 변수
      private System.Windows.Forms.Timer realtimeTimer;
      private MemoryStream audioBuffer;
      private WaveFormat audioFormat;
      private int bufferDurationSeconds = 3; // 3초마다 처리
      private int bufferSizeBytes;
      private long lastProcessTime = 0;
      private object bufferLock = new object();
      private bool isProcessing = false;

      public WindowsWhisperForm()
      {
         InitializeComponent();
         InitializeSpeech();
         CheckWhisperFiles();

         // 언어 선택 기본값 설정
         if (comboSTTLang.Items.Count > 0)
         {
            comboSTTLang.SelectedIndex = 0; // 기본값: 한국어
         }

         // 실시간 처리 타이머 초기화
         realtimeTimer = new System.Windows.Forms.Timer();
         realtimeTimer.Interval = bufferDurationSeconds * 1000; // 3초마다
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

      private void CheckWhisperFiles()
      {
         // 여러 가능한 실행 파일 이름 검색 (현재 실행 파일 위치)
         string[] possibleExeNames = { "whisper-cli.exe", "whisper.exe", "main.exe" };
         string foundExePath = null;
         string foundExeName = null;

         // 1. 현재 실행 파일 위치에서 먼저 검색
         foreach (string exeName in possibleExeNames)
         {
            string testPath = Path.Combine(Application.StartupPath, exeName);
            if (File.Exists(testPath))
            {
               foundExePath = testPath;
               foundExeName = exeName;
               whisperExePath = exeName; // 상대 경로로 저장
               break;
            }
         }

         // 2. 자동 검색 (bin\Debug, obj, 프로젝트 루트 등)
         if (foundExePath == null)
         {
            string autoFound = FindWhisperExe();
            if (!string.IsNullOrEmpty(autoFound) && File.Exists(autoFound))
            {
               foundExePath = autoFound;
               foundExeName = Path.GetFileName(autoFound);
               // 전체 경로를 저장하거나 상대 경로로 변환
               if (autoFound.StartsWith(Application.StartupPath, StringComparison.OrdinalIgnoreCase))
               {
                  // 상대 경로로 변환 (.NET Framework 4.7.2 호환)
                  Uri startupUri = new Uri(Application.StartupPath + Path.DirectorySeparatorChar);
                  Uri fileUri = new Uri(autoFound);
                  Uri relativeUri = startupUri.MakeRelativeUri(fileUri);
                  whisperExePath = Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
               }
               else
               {
                  // 절대 경로로 저장
                  whisperExePath = autoFound;
               }
            }
         }

         string whisperPath = foundExePath ?? Path.Combine(Application.StartupPath, whisperExePath);
         string modelPath = Path.Combine(Application.StartupPath, whisperModelPath);

         if (foundExePath == null || !File.Exists(whisperPath))
         {
            UpdateStatus("⚠ Whisper 실행 파일을 찾을 수 없습니다. (whisper-cli.exe 또는 whisper.exe 필요)");
            // 버튼은 활성화 상태로 유지 (실행 시 오류 메시지 표시)
         }
         else if (!File.Exists(modelPath))
         {
            UpdateStatus("⚠ Whisper 모델을 찾을 수 없습니다. (models\\ggml-base.bin 필요)");
            // 버튼은 활성화 상태로 유지 (실행 시 오류 메시지 표시)
         }
         else
         {
            UpdateStatus($"Whisper 준비 완료 ({foundExeName ?? Path.GetFileName(whisperPath)})");
         }

         // 시작 버튼은 항상 활성화 (파일이 없으면 실행 시 오류 표시)
         btnStartSTT.Enabled = true;
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
         // Whisper 파일 확인 (절대 경로 또는 상대 경로 처리)
         string whisperPath = Path.IsPathRooted(whisperExePath)
            ? whisperExePath
            : Path.Combine(Application.StartupPath, whisperExePath);
         string modelPath = Path.Combine(Application.StartupPath, whisperModelPath);

         if (!File.Exists(whisperPath))
         {
            // 대체 경로 검색 (bin\Debug, obj, 프로젝트 루트 등)
            string alternativePath = FindWhisperExe();

            string message = "Whisper 실행 파일을 찾을 수 없습니다.\n\n" +
                "필요한 파일:\n" +
                $"- whisper-cli.exe 또는 whisper.exe\n" +
                $"- 예상 위치: {whisperPath}\n\n" +
                "⚠ 중요: GitHub Releases에 Windows용 미리 빌드된 파일이 없을 수 있습니다.\n" +
                "따라서 직접 빌드하는 것을 권장합니다.\n\n" +
                "해결 방법 1: 직접 빌드 (권장)\n" +
                "1. 필수 도구 설치:\n" +
                "   - Git: https://git-scm.com/download/win\n" +
                "   - CMake: https://cmake.org/download/\n" +
                "   - Visual Studio 2022 (Community 버전 무료)\n\n" +
                "2. PowerShell에서 실행:\n" +
                "   git clone https://github.com/ggml-org/whisper.cpp.git\n" +
                "   cd whisper.cpp\n" +
                "   cmake -B build\n" +
                "   cmake --build build -j --config Release\n\n" +
                "3. 빌드된 파일 복사:\n" +
                "   build\\bin\\Release\\whisper-cli.exe를 다음 위치에 복사:\n" +
                $"   {Application.StartupPath}\n\n" +
                "해결 방법 2: 대안 프로젝트 사용\n" +
                "- Whisper Standalone Win:\n" +
                "  https://github.com/Purfview/whisper-standalone-win\n\n" +
                "모델 파일도 필요합니다:\n" +
                "- models 폴더 생성 후 ggml-base.bin 다운로드\n" +
                "- 다운로드: https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin";

            if (!string.IsNullOrEmpty(alternativePath))
            {
               message += $"\n\n참고: {alternativePath}에서 파일을 찾았습니다.\n" +
                   "이 경로를 사용하시겠습니까?";

               DialogResult result = MessageBox.Show(
                   message,
                   "whisper.exe 없음",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question);

               if (result == DialogResult.Yes)
               {
                  whisperExePath = alternativePath;
                  whisperPath = alternativePath;
               }
               else
               {
                  return;
               }
            }
            else
            {
               MessageBox.Show(
                   message,
                   "whisper.exe 없음",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Warning);
               return;
            }
         }

         if (!File.Exists(modelPath))
         {
            // models 폴더가 없으면 생성
            string modelsDir = Path.GetDirectoryName(modelPath);
            if (!Directory.Exists(modelsDir))
            {
               try
               {
                  Directory.CreateDirectory(modelsDir);
               }
               catch (Exception ex)
               {
                  MessageBox.Show(
                      $"models 폴더를 생성할 수 없습니다: {ex.Message}",
                      "오류",
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
                  return;
               }
            }

            // 대체 모델 파일 검색
            string alternativeModel = FindModelFile();

            string message = "Whisper 모델 파일을 찾을 수 없습니다.\n\n" +
                "필요한 파일:\n" +
                $"- ggml-base.bin (또는 다른 모델)\n" +
                $"- 예상 위치: {modelPath}\n\n" +
                "해결 방법 (가장 간단):\n" +
                "1. 브라우저에서 직접 다운로드:\n" +
                "   https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin\n\n" +
                "2. 사용 가능한 모델 (직접 다운로드 링크):\n" +
                "   - ggml-tiny.bin (~39MB):\n" +
                "     https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin\n" +
                "   - ggml-base.bin (~74MB) 권장:\n" +
                "     https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin\n" +
                "   - ggml-small.bin (~244MB):\n" +
                "     https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin\n" +
                "   - ggml-medium.bin (~769MB):\n" +
                "     https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin\n" +
                "   - ggml-large.bin (~1550MB):\n" +
                "     https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large.bin\n\n" +
                "3. 다운로드한 모델 파일을 다음 위치에 복사:\n" +
                $"   {modelPath}";

            if (!string.IsNullOrEmpty(alternativeModel))
            {
               message += $"\n\n참고: {alternativeModel}에서 모델을 찾았습니다.\n" +
                   "이 모델을 사용하시겠습니까?";

               DialogResult result = MessageBox.Show(
                   message,
                   "모델 파일 없음",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question);

               if (result == DialogResult.Yes)
               {
                  // 절대 경로로 변환하여 저장
                  if (Path.IsPathRooted(alternativeModel))
                  {
                     whisperModelPath = alternativeModel;
                     modelPath = alternativeModel;
                  }
                  else
                  {
                     modelPath = Path.Combine(Application.StartupPath, alternativeModel);
                     whisperModelPath = alternativeModel; // 상대 경로로 저장 (RunWhisper에서 처리)
                  }
               }
               else
               {
                  return;
               }
            }
            else
            {
               MessageBox.Show(
                   message,
                   "모델 파일 없음",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Warning);
               return;
            }
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
            lastProcessTime = Environment.TickCount;
            
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
      /// 실시간 오디오 버퍼 처리
      /// </summary>
      private void ProcessRealtimeAudio(byte[] audioData)
      {
         if (isProcessing || audioData == null || audioData.Length < 16000)
            return;

         isProcessing = true;
         try
         {
            // 임시 WAV 파일로 저장
            string tempFile = Path.Combine(Application.StartupPath, $"temp_realtime_{DateTime.Now.Ticks}.wav");
            using (var fileStream = new FileStream(tempFile, FileMode.Create))
            using (var writer = new WaveFileWriter(fileStream, audioFormat))
            {
               writer.Write(audioData, 0, audioData.Length);
            }

            // Whisper 처리
            RunWhisperRealtime(tempFile);

            // 임시 파일 삭제
            try { File.Delete(tempFile); } catch { }
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
      /// 실시간 처리용 Whisper 실행 (짧은 오디오 청크 처리)
      /// </summary>
      private void RunWhisperRealtime(string wavFilePath)
      {
         try
         {
            // 절대 경로 또는 상대 경로 처리
            string whisperPath = Path.IsPathRooted(whisperExePath)
               ? whisperExePath
               : Path.Combine(Application.StartupPath, whisperExePath);
            string modelPath = Path.IsPathRooted(whisperModelPath)
               ? whisperModelPath
               : Path.Combine(Application.StartupPath, whisperModelPath);

            if (!File.Exists(whisperPath) || !File.Exists(modelPath) || !File.Exists(wavFilePath))
               return;

            // 언어 선택 확인 (UI 스레드에서 접근)
            string[] langCodeArray = new string[] { "ko" };
            if (this.InvokeRequired)
            {
               this.Invoke((MethodInvoker)delegate
               {
                  if (comboSTTLang.SelectedItem != null)
                  {
                     string selectedLang = comboSTTLang.SelectedItem.ToString();
                     langCodeArray[0] = selectedLang == "한국어" ? "ko" : "en";
                  }
               });
            }
            else
            {
               if (comboSTTLang.SelectedItem != null)
               {
                  string selectedLang = comboSTTLang.SelectedItem.ToString();
                  langCodeArray[0] = selectedLang == "한국어" ? "ko" : "en";
               }
            }
            string langCode = langCodeArray[0];

            // 출력 파일 경로
            string outputDir = Path.GetDirectoryName(wavFilePath);
            string outputFile = Path.ChangeExtension(wavFilePath, ".txt");

            // 처리 시간 최적화: 스레드 수 증가, 타임스탬프 제거
            // 실시간 처리는 짧은 오디오(3초)를 처리하므로 전체 처리보다 훨씬 빠름
            int threadCount = Math.Max(4, Environment.ProcessorCount);
            ProcessStartInfo psi = new ProcessStartInfo
            {
               FileName = whisperPath,
               // -otxt: 텍스트만, -nt: 타임스탬프 없음, -t: 스레드 수
               Arguments = $"-m \"{modelPath}\" -f \"{wavFilePath}\" -l {langCode} -t {threadCount} -otxt -nt -of \"{outputDir}\"",
               RedirectStandardOutput = true,
               RedirectStandardError = true,
               UseShellExecute = false,
               CreateNoWindow = true,
               WorkingDirectory = Application.StartupPath,
               StandardOutputEncoding = Encoding.UTF8,
               StandardErrorEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(psi))
            {
               StringBuilder outputBuilder = new StringBuilder();
               StringBuilder errorBuilder = new StringBuilder();

               process.OutputDataReceived += (sender, e) =>
               {
                  if (!string.IsNullOrEmpty(e.Data))
                  {
                     outputBuilder.AppendLine(e.Data);
                  }
               };

               process.ErrorDataReceived += (sender, e) =>
               {
                  if (!string.IsNullOrEmpty(e.Data))
                  {
                     errorBuilder.AppendLine(e.Data);
                  }
               };

               process.BeginOutputReadLine();
               process.BeginErrorReadLine();

               // 실시간 처리는 짧은 타임아웃 (30초)
               bool finished = process.WaitForExit(30000);

               if (!finished)
               {
                  process.Kill();
                  return;
               }

               string output = outputBuilder.ToString();
               string error = errorBuilder.ToString();

               if (process.ExitCode == 0)
               {
                  string recognizedText = string.Empty;

                  // 출력 파일에서 읽기
                  if (File.Exists(outputFile))
                  {
                     Encoding[] encodings = new Encoding[]
                     {
                        Encoding.UTF8,
                        new UTF8Encoding(false),
                        Encoding.GetEncoding("utf-8"),
                        Encoding.GetEncoding(949),
                        Encoding.GetEncoding(65001),
                        Encoding.Default
                     };

                     foreach (Encoding enc in encodings)
                     {
                        try
                        {
                           recognizedText = File.ReadAllText(outputFile, enc).Trim();
                           if (!string.IsNullOrWhiteSpace(recognizedText))
                           {
                              recognizedText = ExtractTextFromSRT(recognizedText);
                              break;
                           }
                        }
                        catch { }
                     }
                  }

                  // 표준 출력에서 추출
                  if (string.IsNullOrWhiteSpace(recognizedText))
                  {
                     recognizedText = ExtractTextFromWhisperOutput(output);
                  }

                  // UI에 텍스트 추가 (타임스탬프 없이)
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
            }
         }
         catch (Exception ex)
         {
            System.Diagnostics.Debug.WriteLine($"실시간 Whisper 오류: {ex.Message}");
         }
      }

      private void RunWhisper()
      {
         try
         {
            // 절대 경로 또는 상대 경로 처리
            string whisperPath = Path.IsPathRooted(whisperExePath)
               ? whisperExePath
               : Path.Combine(Application.StartupPath, whisperExePath);
            string modelPath = Path.IsPathRooted(whisperModelPath)
               ? whisperModelPath
               : Path.Combine(Application.StartupPath, whisperModelPath);
            string wavPath = Path.Combine(Application.StartupPath, tempWavPath);

            // 파일 존재 여부 확인 및 상세 오류 메시지
            if (!File.Exists(whisperPath) || !File.Exists(modelPath) || !File.Exists(wavPath))
            {
               this.Invoke((MethodInvoker)delegate
               {
                  List<string> missingFiles = new List<string>();
                  if (!File.Exists(whisperPath))
                     missingFiles.Add($"Whisper 실행 파일: {whisperPath}");
                  if (!File.Exists(modelPath))
                     missingFiles.Add($"모델 파일: {modelPath}");
                  if (!File.Exists(wavPath))
                     missingFiles.Add($"녹음 파일: {wavPath}");

                  string errorMessage = "다음 파일을 찾을 수 없습니다:\n\n" + string.Join("\n", missingFiles);
                  if (!File.Exists(modelPath))
                  {
                     errorMessage += "\n\n모델 파일 다운로드:\n";
                     errorMessage += "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin\n\n";
                     errorMessage += $"다운로드 후 다음 위치에 복사:\n{Path.Combine(Application.StartupPath, "models")}";
                  }
                  MessageBox.Show(errorMessage, "파일 없음", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  btnStartSTT.Enabled = true;
                  UpdateStatus("오류 발생");
               });
               return;
            }

            // 언어 선택 확인 (UI 스레드에서 접근)
            string[] langCodeArray = new string[] { "ko" }; // 기본값: 한국어 (배열로 감싸서 참조 전달)
            if (this.InvokeRequired)
            {
               this.Invoke((MethodInvoker)delegate
               {
                  if (comboSTTLang.SelectedItem != null)
                  {
                     string selectedLang = comboSTTLang.SelectedItem.ToString();
                     langCodeArray[0] = selectedLang == "한국어" ? "ko" : "en";
                  }
               });
            }
            else
            {
               if (comboSTTLang.SelectedItem != null)
               {
                  string selectedLang = comboSTTLang.SelectedItem.ToString();
                  langCodeArray[0] = selectedLang == "한국어" ? "ko" : "en";
               }
            }
            string langCode = langCodeArray[0];

            // 출력 파일 경로
            string outputPath = Path.Combine(Application.StartupPath, tempOutputPath);

            // 기존 출력 파일 삭제 (있다면)
            if (File.Exists(outputPath))
            {
               try { File.Delete(outputPath); } catch { }
            }

            // whisper.cpp 실행 인자: 텍스트 파일 출력 옵션 추가
            // 
            // 처리 시간이 오래 걸리는 이유:
            // 1. 모델 크기: base 모델(~74MB)은 상대적으로 빠르지만, large 모델(~1.5GB)은 매우 느림
            // 2. 오디오 길이: 긴 오디오일수록 처리 시간이 선형적으로 증가
            // 3. CPU 성능: Whisper는 CPU 기반으로 동작하므로 CPU 성능에 크게 의존
            // 4. 스레드 수: 기본값(4)보다 CPU 코어 수에 맞춰 증가시키면 속도 향상
            // 5. 메모리: 모델 로딩 및 처리에 많은 메모리 필요
            //
            // 최적화 방법:
            // - 더 작은 모델 사용 (tiny: ~39MB, base: ~74MB 권장)
            // - 스레드 수를 CPU 코어 수에 맞춤 (현재 자동 설정)
            // - 짧은 오디오 구간으로 나누어 처리 (실시간 처리 방식)
            // - GPU 가속 사용 (CUDA 지원 빌드 필요)
            //
            int threadCount = Math.Max(4, Environment.ProcessorCount);
            ProcessStartInfo psi = new ProcessStartInfo
            {
               FileName = whisperPath,
               // -otxt: 텍스트만 출력 (타임스탬프 제거), -t: 스레드 수, -nt: 타임스탬프 없음
               Arguments = $"-m \"{modelPath}\" -f \"{wavPath}\" -l {langCode} -t {threadCount} -otxt -nt -of \"{Path.GetDirectoryName(outputPath)}\"",
               RedirectStandardOutput = true,
               RedirectStandardError = true,
               UseShellExecute = false,
               CreateNoWindow = true,
               WorkingDirectory = Application.StartupPath,
               StandardOutputEncoding = Encoding.UTF8,
               StandardErrorEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(psi))
            {
               // 비동기으로 출력 읽기 (타임아웃 방지)
               StringBuilder outputBuilder = new StringBuilder();
               StringBuilder errorBuilder = new StringBuilder();

               process.OutputDataReceived += (sender, e) =>
               {
                  if (!string.IsNullOrEmpty(e.Data))
                  {
                     outputBuilder.AppendLine(e.Data);
                  }
               };

               process.ErrorDataReceived += (sender, e) =>
               {
                  if (!string.IsNullOrEmpty(e.Data))
                  {
                     errorBuilder.AppendLine(e.Data);
                  }
               };

               process.BeginOutputReadLine();
               process.BeginErrorReadLine();

               // 프로세스 완료 대기 (최대 5분)
               bool finished = process.WaitForExit(300000);

               if (!finished)
               {
                  process.Kill();
                  this.Invoke((MethodInvoker)delegate
                  {
                     MessageBox.Show("Whisper 처리 시간이 초과되었습니다.", "타임아웃", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                     btnStartSTT.Enabled = true;
                     UpdateStatus("처리 시간 초과");
                  });
                  return;
               }

               string output = outputBuilder.ToString();
               string error = errorBuilder.ToString();

               this.Invoke((MethodInvoker)delegate
               {
                  if (process.ExitCode == 0)
                  {
                     string recognizedText = string.Empty;

                     // 1순위: 출력 파일에서 읽기 (가장 정확)
                     string outputFile = Path.ChangeExtension(wavPath, ".txt");
                     if (File.Exists(outputFile))
                     {
                        // 여러 인코딩 시도 (우선순위 순)
                        Encoding[] encodings = new Encoding[]
                        {
                           Encoding.UTF8,
                           new UTF8Encoding(false), // UTF-8 without BOM
                           Encoding.GetEncoding("utf-8"),
                           Encoding.GetEncoding(949), // CP949 (한국어)
                           Encoding.GetEncoding(65001), // UTF-8 (코드 페이지)
                           Encoding.Default
                        };

                        foreach (Encoding enc in encodings)
                        {
                           try
                           {
                              recognizedText = File.ReadAllText(outputFile, enc).Trim();
                              if (!string.IsNullOrWhiteSpace(recognizedText))
                              {
                                 // SRT 형식에서 텍스트만 추출
                                 recognizedText = ExtractTextFromSRT(recognizedText);
                                 break;
                              }
                           }
                           catch { }
                        }
                     }

                     // 2순위: 표준 출력에서 추출
                     if (string.IsNullOrWhiteSpace(recognizedText))
                     {
                        recognizedText = ExtractTextFromWhisperOutput(output);
                     }

                     // 3순위: 에러 출력에서도 시도 (일부 whisper 버전은 에러 스트림에 출력)
                     if (string.IsNullOrWhiteSpace(recognizedText) && !string.IsNullOrWhiteSpace(error))
                     {
                        recognizedText = ExtractTextFromWhisperOutput(error);
                     }

                     if (!string.IsNullOrWhiteSpace(recognizedText))
                     {
                        string currentText = textBox.Text;
                        if (!string.IsNullOrEmpty(currentText) && !currentText.EndsWith("\r\n") && !currentText.EndsWith("\n"))
                        {
                           textBox.AppendText("\r\n");
                        }
                        textBox.AppendText(recognizedText);

                        // 상태 메시지 (긴 텍스트는 축약)
                        string statusText = recognizedText.Length > 30
                            ? recognizedText.Substring(0, 30) + "..."
                            : recognizedText;
                        UpdateStatus($"인식 완료: {statusText}");
                     }
                     else
                     {
                        UpdateStatus("인식된 텍스트가 없습니다.");
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                           // 디버깅용: 출력 내용 표시
                           System.Diagnostics.Debug.WriteLine($"Whisper 출력: {output}");
                        }
                     }
                  }
                  else
                  {
                     string errorMsg = !string.IsNullOrWhiteSpace(error) ? error : output;
                     UpdateStatus($"Whisper 오류 (코드: {process.ExitCode})");
                     MessageBox.Show($"Whisper 실행 실패 (종료 코드: {process.ExitCode}):\n\n{errorMsg}",
                         "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  }
                  btnStartSTT.Enabled = true;
               });
            }
         }
         catch (Exception ex)
         {
            this.Invoke((MethodInvoker)delegate
            {
               MessageBox.Show($"Whisper 실행 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
               btnStartSTT.Enabled = true;
               UpdateStatus("오류 발생");
            });
         }
      }

      /// <summary>
      /// SRT 형식에서 텍스트만 추출
      /// </summary>
      private string ExtractTextFromSRT(string srtContent)
      {
         if (string.IsNullOrWhiteSpace(srtContent))
            return string.Empty;

         var lines = srtContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
         List<string> textLines = new List<string>();
         bool skipNext = false;

         foreach (var line in lines)
         {
            string trimmed = line.Trim();

            // 빈 줄은 구분자로 사용
            if (string.IsNullOrWhiteSpace(trimmed))
            {
               skipNext = false;
               continue;
            }

            // 숫자만 있는 줄 (시퀀스 번호) 건너뛰기
            if (int.TryParse(trimmed, out _))
            {
               skipNext = true;
               continue;
            }

            // 타임스탬프 줄 건너뛰기 (예: [00:00:00.000 --> 00:00:02.000] 또는 00:00:00,000 --> 00:00:02,000)
            if (trimmed.Contains("-->") || (trimmed.StartsWith("[") && trimmed.Contains("]")))
            {
               skipNext = false;
               continue;
            }

            // 실제 텍스트 줄
            if (!skipNext && !string.IsNullOrWhiteSpace(trimmed))
            {
               textLines.Add(trimmed);
            }
         }

         return string.Join(" ", textLines).Trim();
      }

      private string ExtractTextFromWhisperOutput(string output)
      {
         if (string.IsNullOrWhiteSpace(output))
            return string.Empty;

         var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
         List<string> textLines = new List<string>();

         // whisper.cpp 출력 형식에 따라 텍스트 추출
         // 일반적으로 [시간] 텍스트 형식 또는 단순 텍스트
         foreach (var line in lines)
         {
            string trimmed = line.Trim();

            // 빈 줄 건너뛰기
            if (string.IsNullOrWhiteSpace(trimmed))
               continue;

            // 시스템 메시지 건너뛰기 (대소문자 무시)
            string lowerTrimmed = trimmed.ToLower();
            if (lowerTrimmed.StartsWith("whisper") ||
                lowerTrimmed.StartsWith("model") ||
                lowerTrimmed.StartsWith("loading") ||
                lowerTrimmed.StartsWith("processing") ||
                lowerTrimmed.StartsWith("system") ||
                lowerTrimmed.StartsWith("using") ||
                lowerTrimmed.Contains("gpu") ||
                lowerTrimmed.Contains("cpu") ||
                lowerTrimmed.Contains("thread") ||
                lowerTrimmed.Contains("memory") ||
                (lowerTrimmed.Contains("error") && !lowerTrimmed.Contains("text")) ||
                trimmed.Length < 2)
            {
               continue;
            }

            // SRT 타임스탬프 줄 건너뛰기 (예: 00:00:00,000 --> 00:00:02,000)
            if (trimmed.Contains("-->") && (trimmed.Contains(":") || trimmed.Contains(",")))
            {
               continue;
            }

            // 숫자만 있는 줄 (시퀀스 번호) 건너뛰기
            if (int.TryParse(trimmed, out _) && trimmed.Length < 5)
            {
               continue;
            }

            // [시간] 형식 제거 (예: [00:00.000 --> 00:05.000] 또는 [00:00:00.000 --> 00:00:05.000])
            if (trimmed.StartsWith("["))
            {
               int endBracket = trimmed.IndexOf(']');
               if (endBracket > 0 && endBracket < trimmed.Length - 1)
               {
                  trimmed = trimmed.Substring(endBracket + 1).Trim();
               }
               else
               {
                  // 닫는 괄호가 없으면 건너뛰기
                  continue;
               }
            }

            // 의미있는 텍스트인지 확인 (구두점이나 공백만 있는 경우 제외)
            if (trimmed.Length > 1 && !trimmed.All(c => char.IsPunctuation(c) || char.IsWhiteSpace(c) || char.IsDigit(c)))
            {
               textLines.Add(trimmed);
            }
         }

         // 여러 줄을 하나로 합치기
         if (textLines.Count > 0)
         {
            return string.Join(" ", textLines).Trim();
         }

         // JSON 형식인 경우 (일부 whisper 구현체)
         if (output.Contains("\"text\""))
         {
            try
            {
               // JSON 파싱 시도
               int textIdx = output.IndexOf("\"text\"");
               if (textIdx >= 0)
               {
                  int colonIdx = output.IndexOf(':', textIdx);
                  if (colonIdx > 0)
                  {
                     int quoteStart = output.IndexOf('"', colonIdx);
                     if (quoteStart > 0)
                     {
                        int quoteEnd = output.IndexOf('"', quoteStart + 1);
                        if (quoteEnd > quoteStart)
                        {
                           string jsonText = output.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                           // JSON 이스케이프 문자 처리
                           jsonText = jsonText.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\"", "\"");
                           return jsonText.Trim();
                        }
                     }
                  }
               }
            }
            catch { }
         }

         // 마지막 시도: 전체 출력에서 의미있는 부분 추출
         // 시스템 메시지가 아닌 실제 텍스트 부분 찾기
         string result = output.Trim();

         // 너무 짧거나 시스템 메시지만 있으면 빈 문자열 반환
         if (result.Length < 3 ||
             result.ToLower().StartsWith("whisper") ||
             result.ToLower().StartsWith("model") ||
             result.ToLower().StartsWith("loading"))
         {
            return string.Empty;
         }

         // 길면 마지막 의미있는 부분만 추출
         if (result.Length > 500)
         {
            result = result.Substring(Math.Max(0, result.Length - 500));
         }

         return result;
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

      /// <summary>
      /// whisper.exe 파일을 여러 위치에서 검색
      /// </summary>
      private string FindWhisperExe()
      {
         // 프로젝트 루트 경로 찾기
         string projectRoot = Application.StartupPath;
         string solutionRoot = projectRoot;

         // bin\Debug 또는 bin\Release에서 실행 중이면 상위로 이동
         if (projectRoot.Contains("\\bin\\Debug") || projectRoot.Contains("\\bin\\Release"))
         {
            solutionRoot = Directory.GetParent(Directory.GetParent(projectRoot).FullName).FullName;
         }

         // 검색할 경로 목록 (우선순위 순)
         List<string> searchPaths = new List<string>
            {
                // 1. 현재 실행 파일 위치 (bin\Debug 또는 bin\Release)
                Application.StartupPath,
                
                // 2. 빌드 출력 폴더들
                Path.Combine(solutionRoot, "bin", "Debug"),
                Path.Combine(solutionRoot, "bin", "Release"),
                Path.Combine(solutionRoot, "EchoEngine", "bin", "Debug"),
                Path.Combine(solutionRoot, "EchoEngine", "bin", "Release"),
                
                // 3. obj 폴더들
                Path.Combine(solutionRoot, "obj", "Debug"),
                Path.Combine(solutionRoot, "obj", "Release"),
                Path.Combine(solutionRoot, "EchoEngine", "obj", "Debug"),
                Path.Combine(solutionRoot, "EchoEngine", "obj", "Release"),
                
                // 4. 프로젝트 루트
                solutionRoot,
                Path.Combine(solutionRoot, "EchoEngine"),
                
                // 5. 하위 폴더들
                Path.Combine(solutionRoot, "whisper"),
                Path.Combine(Application.StartupPath, "whisper"),
                Path.Combine(Application.StartupPath, "bin"),
                
                // 6. 상위 디렉토리
                Path.GetDirectoryName(solutionRoot),
                Path.Combine(Path.GetDirectoryName(solutionRoot), "whisper"),
                
                // 7. 시스템 경로
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

         foreach (string searchPath in searchPaths)
         {
            if (string.IsNullOrEmpty(searchPath) || !Directory.Exists(searchPath))
               continue;

            // whisper.cpp 빌드 시 생성되는 실행 파일 이름들 (우선순위 순)
            string[] possibleNames = {
                    "whisper-cli.exe",  // 최신 빌드에서 생성되는 이름 (우선순위 높음)
                    "whisper.exe",     // 일부 빌드에서 사용
                    "main.exe"         // 예전 빌드
                };
            foreach (string name in possibleNames)
            {
               string fullPath = Path.Combine(searchPath, name);
               if (File.Exists(fullPath))
               {
                  return fullPath;
               }
            }

            // 하위 디렉토리도 검색 (최대 2단계)
            try
            {
               foreach (string subDir in Directory.GetDirectories(searchPath))
               {
                  foreach (string name in possibleNames)
                  {
                     string fullPath = Path.Combine(subDir, name);
                     if (File.Exists(fullPath))
                     {
                        return fullPath;
                     }
                  }
               }
            }
            catch { }
         }

         return null;
      }

      /// <summary>
      /// Whisper 모델 파일을 여러 위치에서 검색
      /// </summary>
      private string FindModelFile()
      {
         // 검색할 경로 목록
         List<string> searchPaths = new List<string>
            {
                Path.Combine(Application.StartupPath, "models"),
                Application.StartupPath,
                Path.Combine(Application.StartupPath, "whisper", "models"),
                Path.GetDirectoryName(Application.StartupPath)
            };

         // 검색할 모델 파일 이름 (우선순위 순)
         string[] modelNames = { "ggml-base.bin", "ggml-tiny.bin", "ggml-small.bin", "ggml-medium.bin", "ggml-large.bin" };

         foreach (string searchPath in searchPaths)
         {
            if (string.IsNullOrEmpty(searchPath) || !Directory.Exists(searchPath))
               continue;

            foreach (string modelName in modelNames)
            {
               string fullPath = Path.Combine(searchPath, modelName);
               if (File.Exists(fullPath))
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
                     if (File.Exists(fullPath))
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
         if (synthesizer != null)
         {
            synthesizer.SpeakAsyncCancelAll();
            synthesizer.Dispose();
         }
         base.OnFormClosing(e);
      }
   }
}
