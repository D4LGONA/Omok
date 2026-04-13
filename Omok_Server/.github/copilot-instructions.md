# Copilot Instructions

## 프로젝트 지침
- User preference: use signed int for client IDs and have GenerateClientId return -1 when maximum users exceeded.
- 수신된 패킷을 세션 내부 메모리 큐에 저장하도록 처리하는 것을 선호함 (Session::OnRecvCompleted에서 패킷을 파싱해 큐에 넣음).