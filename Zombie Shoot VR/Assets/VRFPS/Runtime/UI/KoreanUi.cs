using UnityEngine;
using UnityEngine.UI;
namespace VRFPS
{
    public static class KoreanUi
    {
        public const string Instructions = "이동: 왼쪽 스틱    회전: 오른쪽 스틱\n사격: 왼손으로 총 앞부분을 잡고 오른손 트리거를 누르세요.\n재장전 (왼손)\n1. 총 앞부분을 놓고 탄창을 잡아 빼세요.\n2. 탄창을 놓으면 남은 탄약은 버려집니다.\n3. 왼쪽 허리에서 새 탄창을 잡으세요.\n4. 탄창을 삽입한 뒤 총 앞부분을 다시 잡으세요.\n체력 · 웨이브 · 처치 수: 오른쪽 팔에서 확인\n일시정지: 왼손 X    전투 중 점프: 오른손 A\n버튼을 포인터로 가리킨 뒤 선택하세요.";
        public static void Apply(Transform root)
        {
            var font=Resources.Load<Font>("Fonts/NanumGothic-Regular");
            foreach(var text in root.GetComponentsInChildren<Text>(true))
            {
                if(font!=null)text.font=font;
                if(text.name=="InstructionPanel") {text.text=Instructions;text.fontSize=23;}
                else text.text=StaticLabel(text.text);
            }
        }
        public static string StaticLabel(string value)
        {
            switch(value)
            {
                case "Back": return "뒤로";
                case "Resume": return "계속하기";
                case "Retry": return "다시 시작";
                case "Retry save": return "저장 재시도";
                case "Settings": return "조작 방법";
                case "Controls": return "게임 종료";
                case "Maps unavailable": case "Map selection": case "Maps": return "맵 선택";
                case "Move -": return "이동 −";
                case "Move +": return "이동 +";
                case "Turn -": return "회전 −";
                case "Turn +": return "회전 +";
                default:return value;
            }
        }
    }
}
