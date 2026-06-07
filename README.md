# Science Simulation 2

C 언어로 작성한 시뮬레이션 코어를 Unity 화면에서 호출하는 프로젝트입니다.

Unity는 기본 스크립트 언어가 C#이라서, C 코드는 네이티브 플러그인 DLL로 빌드하고 Unity C# 래퍼가 `DllImport`로 호출하는 구조를 사용합니다.

## 폴더 구조

- `native/`: C 시뮬레이션 라이브러리
- `UnityProject/`: Unity 프로젝트
- `tools/`: 빌드 보조 스크립트

## 필요 도구

- Unity 6000.4.10f1 이상
- CMake 3.20 이상
- C 컴파일러
  - Windows: Visual Studio Build Tools, MSVC
  - macOS/Linux: Clang 또는 GCC

## 첫 실행

1. Unity Hub에서 `UnityProject` 폴더를 엽니다.
2. `Assets/atom.unity` 씬을 엽니다.
3. Play 버튼을 누릅니다.

현재 메인 예시는 원자번호 1번부터 20번까지 볼 수 있는 원자 모형입니다. Play를 누르면 왼쪽 패널의 원소 버튼으로 원자 모형을 바꿀 수 있습니다.

각 원소는 입력된 양성자, 중성자, 전자 수를 사용합니다. 전자는 선으로 된 궤도 대신 구형/아령형 오비탈 경로를 따라 움직이며, 일정 시간마다 사라졌다가 다시 나타납니다.

카메라 조작:

- 왼쪽 마우스 드래그: 원자핵 중심으로 카메라 회전
- 마우스 휠: 확대/축소

`atom.unity` 씬의 기본 오브젝트 이름은 다음과 같이 사용됩니다.

- `nucleus`: 양성자 템플릿
- `neutron`: 중성자 템플릿
- `electron`: 전자 템플릿

## C 네이티브 플러그인 빌드

네이티브 C 코어로 실행하려면 플러그인을 빌드합니다.

   ```powershell
   ./tools/build-native.ps1
   ```

빌드 결과가 아래 위치에 생성되면 Unity가 자동으로 `SimulationCore` DLL을 사용합니다.

   ```text
   UnityProject/Assets/Plugins/x86_64/SimulationCore.dll
   ```

## WebGL 배포

WebGL 빌드는 Unity 메뉴에서 `Build > Build WebGL`을 실행하거나 아래 명령으로 생성합니다.

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe" -batchmode -quit -projectPath ".\UnityProject" -executeMethod WebGLBuildTools.BuildWebGL
```

빌드 결과:

```text
UnityProject/Builds/WebGL
```

로컬 확인:

```powershell
cd UnityProject/Builds/WebGL
python -m http.server 8080
```

브라우저에서 `http://localhost:8080`으로 접속합니다. 파일을 직접 더블클릭해서 여는 방식은 WebGL 로딩이 막힐 수 있으니 로컬 서버로 확인하세요.

현재 WebGL 설정:

- WebGL 빌드 타겟 자동 전환
- 128MB WebGL 메모리
- 예외 지원 비활성화
- 데이터 캐싱 활성화
- Decompression Fallback 활성화
- 씬 내 콜라이더 제거
- 모바일/태블릿 터치 회전 및 핀치 줌 지원

배포 후보:

- 간단한 정적 호스팅: GitHub Pages, Netlify, Vercel
- 학교/수업용 공유: itch.io HTML5 업로드도 가능
- 직접 서버: `index.html`, `Build/`, `TemplateData/` 전체를 같은 폴더 구조로 업로드

## 현재 샘플

초기 C 코어는 간단한 입자 낙하/반발 시뮬레이션입니다.

- `sim_create`
- `sim_destroy`
- `sim_reset`
- `sim_step`
- `sim_get_particle_count`
- `sim_get_positions`

Unity 쪽 호출 코드는 `UnityProject/Assets/Scripts/NativeSimulation.cs`에 있습니다.
