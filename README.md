# Science Simulation 2

C 언어로 작성한 시뮬레이션 코어를 Unity 화면에서 호출하는 프로젝트입니다.

Unity는 기본 스크립트 언어가 C#이라서, C 코드는 네이티브 플러그인 DLL로 빌드하고 Unity C# 래퍼가 `DllImport`로 호출하는 구조를 사용합니다.

## 폴더 구조

- `native/`: C 시뮬레이션 라이브러리
- `UnityProject/`: Unity 프로젝트
- `tools/`: 빌드 보조 스크립트

## 필요 도구

- Unity 2022.3 LTS 이상
- CMake 3.20 이상
- C 컴파일러
  - Windows: Visual Studio Build Tools, MSVC
  - macOS/Linux: Clang 또는 GCC

## 첫 실행

1. Unity Hub에서 `UnityProject` 폴더를 엽니다.
2. 네이티브 플러그인을 빌드합니다.

   ```powershell
   ./tools/build-native.ps1
   ```

3. 빌드 결과가 아래 위치에 생성되는지 확인합니다.

   ```text
   UnityProject/Assets/Plugins/x86_64/SimulationCore.dll
   ```

4. Unity에서 새 씬을 만들고 다음을 추가합니다.
   - 빈 GameObject: `SimulationController`
   - `SimulationController` 스크립트 부착
   - 작은 Sphere prefab을 `Particle Prefab`에 연결

`Particle Prefab`을 연결하지 않아도 실행 시 기본 Sphere를 만들어 사용합니다.

## 현재 샘플

초기 C 코어는 간단한 입자 낙하/반발 시뮬레이션입니다.

- `sim_create`
- `sim_destroy`
- `sim_reset`
- `sim_step`
- `sim_get_particle_count`
- `sim_get_positions`

Unity 쪽 호출 코드는 `UnityProject/Assets/Scripts/NativeSimulation.cs`에 있습니다.
