# Quest Pro 실행 확인과 Virtual Desktop 설정

확인일: 2026-09-22. 출처: 사용자의 직접 실행 보고 및 제공한 Virtual Desktop 화면 사진 2장.

## 실제 실행 결과

사용자는 앞서 요청한 VR 실행 확인에 대해 “문제없이 머리, 양손 추적 잘 되는 것도 확인”했다고 보고했다. VR 실행 및 머리·왼손·오른손 추적은 **사용자 실기 확인 완료**로 기록한다. 이 결과는 앞서 전달한 InteractionLab 시험 요청에 대한 회신이며, 사용자가 실행 파일 해시를 직접 대조한 것은 아니다.

양안 가림·벽 뒤 표적·총기 조작·연속 회전·장시간 안정성·CPU/GPU 프레임 시간까지 확인한 결과로 확대하지 않는다. 전체 P01에는 전용 입력과 어셈블리 구성이 남아 있다.

## VR 스트리밍 설정 — 첫 번째 사진

| 항목 | 화면에서 확인한 값 |
|---|---|
| Headset | Meta Quest Pro |
| VD 헤드셋 앱 버전 | 1.34.22 - 207 |
| VR Graphics Quality | High |
| 표시된 Render resolution | 2496 × 2688 |
| VR Frame Rate | 90 fps 선택 |
| VR Bitrate | 270 Mbps |
| Sharpening | 70% |
| Gamma | 1.00 |
| Synchronous Spacewarp (SSW) | Always enabled |
| Snapdragon Game Super Resolution | 켜짐 |
| Foveated streaming | 꺼짐 |
| Video buffering | 켜짐 |
| Center to play space / Stage tracking | 꺼짐 |
| Track controllers | 켜짐 |
| Forward tracking data to PC | 켜짐 |
| Emulate SteamVR Vive trackers / Index controllers | 둘 다 꺼짐 |
| Local dimming | 켜짐 |
| Increase color vibrance | 꺼짐 |
| Show performance overlay | 꺼짐 |
| VR Passthrough: Environment / Hands / Desk Portal | 모두 꺼짐 |
| 코덱 | 사진에서 확인 불가 |

## 데스크톱·환경 설정 — 두 번째 사진

VR 스트리밍 설정과 구분하여 기록한다.

- Environment Quality: Medium, Frame Rate: 90 fps, Desktop Bitrate: 70 Mbps.
- Auto connect / Use optimal resolution: 켜짐. Show Games tab on connect / Arrange monitors on recenter: 꺼짐.
- Screen Brightness: 100%, Dynamic Lighting: Enabled when controller inactive.
- Background music when disconnected / Microphone passthrough / Noise cancellation: 켜짐, 마이크 85%.
- Boost clock rates / Copy screenshots to desktop / Local dimming: 켜짐.
- Allow custom orientation / Increase color vibrance / Remove head lock delay: 꺼짐.

## 성능 시험에 적용할 구분

현재 실제 플레이 프로필은 **High / 90Hz / 270Mbps / SSW 항상 켜짐 / SGSR 켜짐**이다. 화면 하단의 90 fps 또는 순간 87 fps 표시는 CPU/GPU p95 측정이나 앱의 보간 없는 90fps 달성 증거로 사용하지 않는다.

08 테스트 계획의 기본 성능 시험은 **72Hz·프레임 보간 끔** 조건이며 이번 설정과 다르다. 이후 T17에서는 현재 프로필 결과와 기본 비교 조건의 결과를 별도로 기록한다. 이번 보고만으로 T17을 통과 처리하거나 사용자의 설정을 변경하지 않는다. 코덱과 실제 게임 렌더 해상도·프레임 측정은 해당 시험 시 확인한다.

원본 사진은 사용자가 제공한 로컬 파일이며 Git에 업로드하지 않는다. 파일 이름과 SHA-256은 동명 JSON 증빙에 보관한다.
