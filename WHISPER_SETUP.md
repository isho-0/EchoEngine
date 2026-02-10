# Whisper STT 설정 가이드

EchoEngine의 Whisper STT 기능을 사용하기 위한 설정 가이드입니다.

## 필요한 파일

### 1. whisper-cli.exe (또는 whisper.exe)
Whisper 실행 파일이 필요합니다.

⚠️ **중요**: GitHub Releases에 Windows용 미리 빌드된 파일이 **없을 수 있습니다**.  
따라서 **직접 빌드하는 방법을 권장**합니다.

#### 방법 1: 소스에서 직접 빌드 (권장)
**필수 도구:**
- Git for Windows
- CMake (https://cmake.org/download/)
- Visual Studio 2019 이상 (C++ 개발 도구 포함)

**빌드 단계:**

1. **필수 도구 설치:**
   - **Git for Windows**: https://git-scm.com/download/win
   - **CMake**: https://cmake.org/download/ (설치 시 "Add CMake to system PATH" 선택)
   - **Visual Studio 2022 Community** (무료): https://visualstudio.microsoft.com/
     - 설치 시 "Desktop development with C++" 워크로드 선택

2. **PowerShell 또는 명령 프롬프트 열기**

3. **저장소 클론:**
   ```bash
   git clone https://github.com/ggml-org/whisper.cpp.git
   cd whisper.cpp
   ```

4. **CMake로 빌드:**
   ```bash
   cmake -B build
   cmake --build build -j --config Release
   ```

5. **빌드 완료 후 실행 파일 위치:**
   - `build\bin\Release\whisper-cli.exe` ← **이 파일을 사용**
   - 또는 `build\bin\Release\whisper.exe` (일부 빌드)

6. **빌드된 파일을 EchoEngine 실행 파일과 같은 폴더에 복사:**
   ```bash
   copy build\bin\Release\whisper-cli.exe "D:\Project\EchoEngine\EchoEngine\bin\Debug\"
   ```
   (실제 경로는 프로젝트 위치에 맞게 수정)

**빌드 옵션:**
- GPU 지원 (CUDA): `cmake -B build -DWHISPER_CUDA=ON`
- OpenBLAS 지원: `cmake -B build -DWHISPER_OPENBLAS=ON`

#### 방법 2: 미리 빌드된 실행 파일 다운로드 (있는 경우)
1. GitHub Releases 확인:
   - https://github.com/ggerganov/whisper.cpp/releases
   - 또는 https://github.com/ggml-org/whisper.cpp/releases
2. Windows용 빌드가 있다면 다운로드
3. 압축 해제 후 `whisper-cli.exe` 또는 `whisper.exe` 찾기
4. EchoEngine 실행 파일과 같은 폴더에 복사

#### 방법 3: 대안 프로젝트 사용
- **Whisper Standalone Win**: https://github.com/Purfview/whisper-standalone-win
  - Windows용 미리 빌드된 버전 제공
  - 6GB VRAM에서도 Large v3 모델 구동 가능

### 2. Whisper 모델 파일
음성 인식을 위한 모델 파일이 필요합니다.

#### 모델 다운로드 방법

**방법 1: 스크립트로 자동 다운로드 (Linux/Mac, Windows Git Bash)**
```bash
cd whisper.cpp
sh ./models/download-ggml-model.sh base
```

**방법 2: 직접 다운로드 (Windows 권장)**
1. Hugging Face에서 모델 다운로드:
   - https://huggingface.co/ggerganov/whisper.cpp/tree/main
   - 또는 직접 링크:
     - **base 모델**: https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
     - **tiny 모델**: https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin
     - **small 모델**: https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin
     - **medium 모델**: https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin
     - **large 모델**: https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large.bin

2. 사용 가능한 모델 비교:
   - **ggml-tiny.bin** (~39MB) - 가장 작고 빠름, 정확도 낮음
   - **ggml-base.bin** (~74MB) - **권장**, 균형잡힌 성능 ⭐
   - **ggml-small.bin** (~244MB) - 더 정확함
   - **ggml-medium.bin** (~769MB) - 매우 정확함
   - **ggml-large.bin** (~1550MB) - 가장 정확함, 느림

3. 언어별 모델:
   - 영어 전용: `base.en`, `small.en`, `medium.en`, `large.en` (더 작고 빠름)
   - 다국어: `base`, `small`, `medium`, `large` (한국어 포함)

#### 모델 파일 배치
1. EchoEngine 실행 파일과 같은 폴더에 `models` 폴더 생성
2. 다운로드한 모델 파일을 `models` 폴더에 복사
3. 예: `EchoEngine\models\ggml-base.bin`

## 폴더 구조 예시

```
EchoEngine/
├── EchoEngine.exe
├── whisper-cli.exe      ← 여기에 배치 (또는 whisper.exe)
└── models/
    └── ggml-base.bin    ← 여기에 배치
```

**참고**: 애플리케이션은 다음 파일 이름을 자동으로 인식합니다:
- `whisper-cli.exe` (최신 빌드, 우선순위 높음)
- `whisper.exe`
- `main.exe`

## 빠른 시작

### 단계 1: whisper-cli.exe 빌드 (약 10-15분)

**필수 도구 설치:**
1. Git: https://git-scm.com/download/win
2. CMake: https://cmake.org/download/
3. Visual Studio 2022 Community: https://visualstudio.microsoft.com/
   - "Desktop development with C++" 워크로드 선택

**빌드 명령어 (PowerShell):**
```powershell
# 1. 저장소 클론
git clone https://github.com/ggml-org/whisper.cpp.git
cd whisper.cpp

# 2. 빌드
cmake -B build
cmake --build build -j --config Release

# 3. 빌드된 파일 확인
# build\bin\Release\whisper-cli.exe 파일이 생성됨

# 4. EchoEngine 폴더로 복사 (경로는 실제 위치에 맞게 수정)
copy build\bin\Release\whisper-cli.exe "D:\Project\EchoEngine\EchoEngine\bin\Debug\"
```

**대안: 미리 빌드된 파일 찾기 (있는 경우)**
- GitHub Releases 확인: https://github.com/ggml-org/whisper.cpp/releases
- Windows용 빌드가 있다면 다운로드
- 또는 Whisper Standalone Win 사용: https://github.com/Purfview/whisper-standalone-win

### 단계 2: 모델 파일 다운로드
1. 브라우저에서 다음 링크 열기:
   - https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
2. 파일 다운로드 시작 (약 74MB)
3. EchoEngine.exe와 같은 폴더에 `models` 폴더 생성
4. 다운로드한 `ggml-base.bin`을 `models` 폴더에 복사

### 단계 3: 애플리케이션 실행
1. EchoEngine 실행
2. Whisper STT 메뉴 선택
3. 언어 선택 (한국어/영어)
4. 🎤 시작 버튼 클릭하여 녹음 시작

## 문제 해결

### whisper.exe를 찾을 수 없습니다
- whisper.exe가 EchoEngine.exe와 같은 폴더에 있는지 확인
- 파일 이름이 정확히 `whisper.exe`인지 확인
- 애플리케이션을 재시작

### 모델 파일을 찾을 수 없습니다
- `models` 폴더가 EchoEngine.exe와 같은 폴더에 있는지 확인
- 모델 파일 이름이 정확한지 확인 (예: `ggml-base.bin`)
- 파일 확장자가 `.bin`인지 확인

### 인식이 안 됩니다
- 마이크가 제대로 연결되어 있는지 확인
- Windows 마이크 권한이 허용되어 있는지 확인
- 충분히 큰 소리로 말하기
- 배경 소음 최소화

## 직접 빌드 상세 가이드 (Windows)

### 필수 도구 설치
1. **Git for Windows** 다운로드 및 설치:
   - https://git-scm.com/download/win

2. **CMake** 다운로드 및 설치:
   - https://cmake.org/download/
   - 설치 시 "Add CMake to system PATH" 옵션 선택

3. **Visual Studio 2022** (Community 버전 무료):
   - https://visualstudio.microsoft.com/
   - 설치 시 "Desktop development with C++" 워크로드 선택

### 빌드 명령어 (PowerShell 또는 명령 프롬프트)
```bash
# 1. 저장소 클론
git clone https://github.com/ggml-org/whisper.cpp.git
cd whisper.cpp

# 2. 빌드 디렉토리 생성 및 빌드
cmake -B build
cmake --build build -j --config Release

# 3. 빌드된 파일 확인
# build\bin\Release\whisper.exe 또는 whisper-cli.exe
```

### 모델 다운로드 (Windows에서)
```bash
# PowerShell에서 직접 다운로드
Invoke-WebRequest -Uri "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin" -OutFile "models\ggml-base.bin"
```

또는 브라우저에서 직접 다운로드:
- https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin

## 추가 정보

- **Whisper.cpp 공식 저장소**: 
  - https://github.com/ggerganov/whisper.cpp
  - https://github.com/ggml-org/whisper.cpp (새 주소)
- **모델 다운로드**: https://huggingface.co/ggerganov/whisper.cpp
- **OpenAI Whisper 정보**: https://openai.com/research/whisper
- **빌드 가이드**: https://github.com/ggml-org/whisper.cpp#building
