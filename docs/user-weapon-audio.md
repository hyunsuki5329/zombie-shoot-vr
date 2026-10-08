# 사용자 제공 무기 음원 적용과 복원

사용자가 지정한 원본은 `reload.mp3`, `empty shoot.mp3`, `shoot sound.mp3`이다. 다운로드 원본은 변경하지 않는다.

## 출처

사용자 제공 출처: [무료 소리 창고 — 총 소리 mp3 무료 다운로드](https://pgtd.tistory.com/227), 게시자 〖 -라일락- 〗, 게시일 2019-07-14. 페이지에서 연결한 라이선스는 [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/deed.ko)이며 출처·라이선스·가공 내역을 빌드 고지에 표시한다. 이 기록은 원 게시자의 권리 보유를 별도로 입증한 것은 아니다. 사용자 파일명과 SHA256으로 취득본을 식별한다. 원본/가공 음원은 로컬 복원 자원으로 관리하며 Git에는 도구와 출처·증빙을 포함한다.

## 재생 방식

- 발사: 원본의 세 연사 구간 중 가장 짧은 구간의 마지막 발사 부분 0.322–0.702초를 추출한다. 결과는 0.38초이며, 후속 연사 구간이 들어가지 않는다. 실제 `Fired` 이벤트마다 한 번 재생한다.
- 빈 총: 0.207–0.570초를 추출해 앞 무음을 줄인다. 기존 규칙대로 발사 조건이 맞고 잔탄이 없는 새 트리거 입력에서 한 번 재생한다. 계속 누른 상태에서 반복 클릭하지 않는다.
- 장전: 0.063–0.840초를 사용해 성공한 수동 탄창 삽입 시 재생한다. 탈거만 하거나 삽입에 실패했을 때는 재생하지 않는다.
- mono PCM/원래44100Hz, 시작1ms 페이드. 끝은 발사50ms·나머지25ms 페이드. 최대 진폭은 발사/장전0.65, 빈 총0.55. 기존 믹서·볼륨·햅틱을 유지한다.
- 10Hz 발사에 4개의 고정 AudioSource를 순환한다. 같은 채널 재사용까지0.4초여서0.38초 잔향이 다음 발사에 잘리지 않는다. 재생 객체는 발사4+빈 총1+삽입1로 제한한다. 정지·사망·비활성화 시 모두 멈춘다.

파형과 이벤트 검증은 청감 검증을 대신하지 않는다. 실제 Quest Pro에서 단발/짧은 연사/긴 연사, 빈 총과 삽입의 음량·지연·잔향을 확인한다.

## 복원

원본3개를 같은 폴더에 둔다. Unity를 닫은 격리 프로젝트에서 저장소 루트 기준으로 실행한다.

```powershell
python -B scripts/Restore-UserAudio.py --source 'C:/Users/User/Downloads' --project './Zombie Shoot VR'
& 'D:/Unity editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath "$PWD/Zombie Shoot VR" -executeMethod VRFPS.Editor.UserWeaponAudioSetup.Configure -logFile "$PWD/tmp/user-audio-restore.log"
```

도구는 모든 원본/기존 목적 파일을 먼저 해시 확인하며 다른 바이트는 덮어쓰지 않는다. Unity 적용기는 WAV와 프리팹 연결을 생성한다. 원본 일부만 있거나 해시가 틀리면 FeedbackSetup도 기존 프리팹을 수정하기 전에 실패한다. 외부 모델 복원과 마찬가지로 최종 자산 시험 전 이 단계가 필수다. 원본이 없을 때의 합성음은 초기 개발용이며 최종 음원 시험을 통과한 것으로 간주하지 않는다.
