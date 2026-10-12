# VR 좀비 FPS 개발 문서

설계 기준: **2026-09-20, 문서 묶음 1.1**. 구현 현황은 아래 진행 기록의 최신 날짜를 따른다.

- [전체 개발 문서](vr-fps-project-docs/README.md)
- [Goal 실행 현황과 검증 결과](progress.md)
- [최신 빌드로 진행하는 Quest Pro 실기 검증](hardware-validation.md)
- [실제 방 스캔·게임용 복도·로컬 자산 복원](room-scan.md)
- [소스 복원·자동 시험·Windows 빌드](source-reproduction.md)
- [현재 자산 출처와 라이선스 원문](asset-provenance.md)
- [Mixamo 근거리·원거리 좀비와 동작 복원](mixamo-visual-restore.md)
- [구현·시험 결과보고서 초안](results-report.md)
- [방 촬영 가이드: 앱 스캔·사진·영상 선택](vr-fps-project-docs/05-capture-map-workflow.md)
- [개발 계획서](vr-fps-project-docs/01-development-plan.md)
- [Goal 실행 프롬프트: 구현·검증·커밋·PR·병합](prompts/vr-zombie-fps-goal.md)
- [개발 계획 PDF](vr-fps-project-docs/VR-FPS-development-plan.pdf)
- [문서 ZIP](VR-FPS-project-documents.zip)
- 2026-09-18 공유 대화 원본 자료와 강의 자료: 로컬 `shared-chat-2026-09-18/README.md`에 보관하며 Git 업로드에서 제외한다.

개발에는 `vr-fps-project-docs/`를 사용한다. 공유 대화에서 가져온 `shared-chat-2026-09-18/outputs/`는 당시의 원본 보관본이다.

이번 갱신은 기존 VR 템플릿의 Unity 6000.3.23f1·XRI 3.5.1을 기준으로 계획서·기술 설계·구현 계획·출처를 맞추고 촬영 지침을 보완한 것이다. 패키지 설치 상태와 실제 Quest Pro 검증 결과는 구분한다. 연결된 계획 PDF와 ZIP은 당시 문서1.1 배포본이며, 후속 구현 기록과 동기화된 최종 제출본이 아니다.
