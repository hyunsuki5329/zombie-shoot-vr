# Granny house의 가구·표면 적용

2026-10-13 사용자 지정 ZIP: `C:/Users/User/Downloads/dvloper-grannys-house-official-model.zip`.
페이지: https://sketchfab.com/3d-models/dvloper-grannys-house-official-model-1bb0b0e98a474910b8de30bf80c5a096
업로더 danielthesecond, 페이지가 지칭하는 원 게임 제작자 DVloper, 작품 Granny. 페이지 표시 라이선스 CC Attribution 4.0. 제목의 Official은 업로더의 표기이며 별도로 권한을 검증했다는 의미가 아니다. 표시 라이선스와 원 게임 자산의 권리 관계는 별도 확인 대상이다. 원본·텍스처·추출 메시/프리팹은 로컬에 보존하고 Git에 업로드하지 않는다.

현재10.75×8.75m Room과2.5배 구조/생성점은 보존한다. 침대는 FurnitureSovrum1의 한 침대, 책상은 HallFurniture2와 서랍2개, 의자는 Furniture___FIXAD의 한 의자, 수납 구역은 HallFurniture2의 선반, Keyboard 자리의 장식 가구는 FurnitureSovrum1의 서랍장을 사용한다. 원본 게임 집 전체는 씬에 넣지 않는다. 벽은 벽면 atlas의 벗겨진 도장 부분, 마루는 WoodPlanksFloors0026_1_350.jpg, 천장/걸레받이는 old wood.png를 사용한다. 이는 자체 제작 미술이 아니라 외부 자산을 분리·재배치한 것이다.

구조 배치를 지키기 위해 각 가구의 전체 bounds를 기존 가구 크기에 맞춘다. 가구의 세부 비율은 원본과 다를 수 있다. 현재 충돌은 기존 보수적 BoxCollider이며 책상·의자 다리 사이도 막는 단순화가 남아 있다. 전체 가구 외곽 bounds와 위치를 검사하지만 세부 표면 충돌 일치나 VR 인수를 주장하지 않는다.

복원: 제공 ZIP과 `scripts/Restore-GrannyHouse.py`로 외부 자산 및 meta를 복원한 뒤, 격리 Unity에서 `VRFPS.Editor.GrannyHouseSetup.Configure`를 실행한다. 기존 RoomHousePreview와 RoomStructure가 필요하다. 기본 폐가를 재생성했다면 이 명령을 다시 실행해야 외부 자산 테마가 적용된다. 다음 `RoomHouseSetup.PromoteAndBuild`로 Room에 반영하고 Windows 빌드를 생성한다. 원본 ZIP 해시와 meta 목록은 `docs/granny-house-manifest.json`을 따른다.

2026-10-14 검증: EditMode97/97, PlayMode127/127 및 Windows 빌드 성공. 벽/걸레받이 겹침을 실패 재현 후 면 분리로 수정했다. 빈 GrannyHouse 폴더에 복원했을 때 Unity가 고아 meta를 삭제하는 문제도 재현했다. 이제 생성 시점에 씬이 참조하는 프리팹18개의 meta를 지정하고, 내부 메시·재질은 생성된 프리팹이 새 참조를 기록한다. 프리팹18개 GUID 일치 및 기존 Room 씬의 메시·재질·텍스처 참조 검사를 통과했다. 원본 FBX 전체는 씬 의존성에 포함되지 않는다. 실기 공간감·충돌과 성능은 확인 대기다.

출처 동봉: `RoomHouseSetup.PromoteAndBuild`는 Granny 의존성이 있으면 `Assets/VRFPS/Documentation/GrannyHouse-Attribution.txt`를 실행 파일 옆에 자동 복사한다. 원본 안내가 없으면 빌드 전에 실패한다. 복원 프로젝트에서 실제 재빌드와 출력 파일 SHA256 일치를 확인했다(`tmp/granny-source/build-attribution.log`). 기존 로컬 실행 빌드에는 같은 안내를 추가했으며 게임 실행 파일은 교체하지 않았다.
