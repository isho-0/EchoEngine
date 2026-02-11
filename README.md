# EchoEngine

C# WinForms 환경에서 다양한 STT(Speech-to-Text) 및 TTS(Text-to-Speech) 엔진의 성능을 테스트하고 비교하는 개인 프로젝트입니다.

## 📋 프로젝트 개요

이 프로젝트는 온라인 및 오프라인 환경에서 동작하는 다양한 음성 인식 및 음성 합성 엔진의 성능을 테스트하기 위해 개발되었습니다. 각 엔진의 특징과 장단점을 비교 분석할 수 있습니다.

## ✨ 주요 기능

### 구현 완료된 엔진

- **Azure Speech Services** (온라인)
  - STT: 실시간 음성 인식, 중간 결과 표시 지원
  - TTS: 다양한 음성 선택, 속도/볼륨 조절, 일시정지/재개 기능
  - 한국어 및 영어 지원

- **Whisper** (오프라인)
  - OpenAI의 Whisper 모델 기반 오프라인 STT
  - 실시간 음성 인식 지원
  - 한국어 및 영어 지원

- **Vosk** (오프라인)
  - 경량 오프라인 STT 엔진
  - 실시간 음성 인식 지원
  - 한국어 모델 지원

### 미구현/제외된 엔진

- **Windows Speech API**: Windows 지원 종료로 인해 구현 실패
- **Chrome Web Speech API**: 제외됨 (별도 HTML 예제 파일 제공)

## 🛠️ 기술 스택

- **프레임워크**: .NET Framework 4.7.2
- **UI**: Windows Forms
- **개발 환경**: 
  - Windows 11
  - Visual Studio 2017 Professional
- **주요 라이브러리**:
  - Microsoft.CognitiveServices.Speech (Azure Speech Services)
  - Vosk (오프라인 STT)
  - NAudio (오디오 처리)
  - System.Speech (Windows TTS)

## 📦 요구사항

### 시스템 요구사항

- Windows 10 이상
- .NET Framework 4.7.2 이상
- 마이크 (STT 테스트용)
- 스피커/헤드폰 (TTS 테스트용)

### Azure Speech Services 사용 시

- Azure 구독 및 Speech Services 리소스
- **API 키 (Key)**: Azure 포털에서 발급
- **엔드포인트 (Endpoint)**: Azure 포털에서 발급
- 리전 정보

**Azure 포털에서 발급 방법**:
1. Azure Portal (https://portal.azure.com) 접속
2. Speech Services 리소스 생성 또는 기존 리소스 선택
3. "Keys and Endpoint" 메뉴에서 Key와 Endpoint 복사
4. `App.config` 또는 코드에서 설정

## 🚀 설치 및 실행

### 1. 저장소 클론

```bash
git clone https://github.com/yourusername/EchoEngine.git
cd EchoEngine
```

### 2. NuGet 패키지 복원

Visual Studio에서 솔루션을 열고 NuGet 패키지를 자동으로 복원하거나, 다음 명령어를 실행하세요:

```bash
nuget restore EchoEngine.sln
```

#### 사용된 NuGet 패키지 목록

이 프로젝트에서 사용하는 주요 NuGet 패키지:

- **Azure.Core** (1.44.1) - Azure 서비스 공통 라이브러리
- **Microsoft.CognitiveServices.Speech** (1.48.1) - Azure Speech Services SDK
- **Vosk** (0.3.38) - 오프라인 STT 엔진
- **NAudio** (1.10.0) - 오디오 처리 라이브러리
- **MaterialDesignColors** (5.3.0) - Material Design 색상
- **MaterialDesignThemes** (5.3.0) - Material Design 테마
- **Microsoft.Xaml.Behaviors.Wpf** (1.1.77) - WPF Behaviors
- **System.Text.Json** (6.0.10) - JSON 처리
- **System.Buffers** (4.5.1) - 버퍼 관리
- **System.Memory** (4.5.4) - 메모리 관리
- **System.ClientModel** (1.1.0) - 클라이언트 모델
- 기타 의존성 패키지들 (System.Diagnostics.DiagnosticSource, System.Memory.Data, System.Numerics.Vectors 등)

### 3. 모델 파일 준비 ⚠️ **중요**

**용량 제한으로 인해 모델 파일은 Git에 포함되지 않습니다.** 실행 전에 다음 파일들을 별도로 준비해야 합니다:

#### Whisper 모델 파일

다음 파일들을 `EchoEngine\bin\Debug\` 또는 `EchoEngine\bin\Release\` 폴더에 배치하세요:

- `whisper.exe` (또는 `whisper-cli.exe`)
- `ggml-base.bin` (또는 다른 모델 파일)
- `ggml.dll`
- `ggml-cpu.dll`
- `whisper.dll`
- `models\ggml-base.bin` (모델 파일은 `models` 폴더에 배치)

**Whisper 설정 상세 가이드**: [WHISPER_SETUP.md](WHISPER_SETUP.md) 참조

#### Vosk 모델 파일

다음 폴더를 `EchoEngine\bin\Debug\models\` 또는 `EchoEngine\bin\Release\models\` 폴더에 배치하세요:

- `models\vosk-model-small-ko-0.22\` (전체 모델 폴더)

**Vosk 모델 다운로드**: https://alphacephei.com/vosk/models

### 4. 프로젝트 빌드 및 실행

Visual Studio에서 솔루션을 빌드하고 실행하거나, 명령어로 빌드:

```bash
msbuild EchoEngine.sln /p:Configuration=Release
```

## 📁 프로젝트 구조

```
EchoEngine/
├── EchoEngine/
│   ├── AzureForm.cs              # Azure Speech Services 폼
│   ├── WindowsWhisperForm.cs     # Whisper STT 폼
│   ├── WindowsVoskForm.cs        # Vosk STT 폼
│   ├── WindowsSpeechForm.cs      # Windows Speech API 폼 (구현 실패)
│   ├── ChromeForm.cs             # Chrome Web Speech API 폼
│   ├── MainForm.cs               # 메인 메뉴 폼
│   └── ...
├── chromeEchoEngineExample.html  # Chrome Web Speech API 예제
├── WHISPER_SETUP.md              # Whisper 설정 가이드
└── README.md                     # 이 파일
```

## 🎯 사용 방법

1. **프로그램 실행**: `EchoEngine.exe` 실행
2. **엔진 선택**: 메인 화면에서 테스트할 엔진 선택
3. **STT 테스트**:
   - 언어 선택 (한국어/영어)
   - 🎤 시작 버튼 클릭하여 녹음 시작
   - 음성을 입력하면 텍스트로 변환되어 표시됨
4. **TTS 테스트**:
   - 텍스트 입력
   - 음성 선택 (Azure의 경우)
   - 속도/볼륨 조절
   - 🔊 읽기 버튼 클릭

## ⚠️ 주의사항

### 모델 파일 필수

프로그램을 실행하기 전에 반드시 다음 모델 파일들을 준비해야 합니다:

- **Whisper**: `whisper.exe`, `ggml-base.bin`, 관련 DLL 파일들
- **Vosk**: `vosk-model-small-ko-0.22` 모델 폴더

모델 파일이 없으면 해당 기능을 사용할 수 없습니다.

### Azure Speech Services 설정

Azure Speech Services를 사용하려면 다음 정보를 설정해야 합니다:

1. **Azure 포털에서 발급받기**:
   - Azure Portal (https://portal.azure.com) 접속
   - Speech Services 리소스 생성 또는 선택
   - "Keys and Endpoint" 메뉴에서 다음 정보 확인:
     - **Key 1** 또는 **Key 2** (API 키)
     - **Endpoint** (엔드포인트 URL)
     - **Location/Region** (리전 정보, 예: koreacentral, eastus)

2. **프로젝트에 설정**:
   - `App.config` 파일에 설정하거나
   - 코드에서 직접 설정 (예: `AzureForm.cs`)

**주의**: API 키와 엔드포인트는 민감한 정보이므로 Git에 커밋하지 않도록 주의하세요.

### 파일 경로

모델 파일은 실행 파일(`EchoEngine.exe`)과 같은 폴더 또는 하위 폴더(`models\`)에 배치해야 합니다.

## 📝 참고 자료

- **Azure Speech Services**: https://azure.microsoft.com/services/cognitive-services/speech-services/
- **Whisper.cpp**: https://github.com/ggml-org/whisper.cpp
- **Vosk**: https://alphacephei.com/vosk/
- **Whisper 모델 다운로드**: https://huggingface.co/ggerganov/whisper.cpp
- **Vosk 모델 다운로드**: https://alphacephei.com/vosk/models

## 🔧 문제 해결

### Whisper가 작동하지 않을 때

1. `whisper.exe` 파일이 실행 파일과 같은 폴더에 있는지 확인
2. `models\ggml-base.bin` 파일이 존재하는지 확인
3. 필요한 DLL 파일들(`ggml.dll`, `ggml-cpu.dll`, `whisper.dll`)이 있는지 확인
4. [WHISPER_SETUP.md](WHISPER_SETUP.md) 가이드 참조

### Vosk가 작동하지 않을 때

1. `models\vosk-model-small-ko-0.22\` 폴더가 존재하는지 확인
2. 모델 폴더 내부에 필요한 파일들이 모두 있는지 확인
3. 다른 Vosk 모델을 사용하려면 코드에서 경로 수정

### Azure Speech Services 오류

1. 인터넷 연결 확인
2. API 키 및 리전 설정 확인
3. Azure 구독 상태 확인

## 📄 라이선스

이 프로젝트는 개인 프로젝트입니다.

## 👤 작성자

개인 프로젝트

---

**참고**: 이 프로젝트는 학습 및 테스트 목적으로 개발되었습니다. 프로덕션 환경에서 사용하기 전에 충분한 테스트를 권장합니다.
