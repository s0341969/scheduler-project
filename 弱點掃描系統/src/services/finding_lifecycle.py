from src.models.user import UserRole
from src.models.vulnerability import FindingStatus


ALLOWED_TRANSITIONS: dict[FindingStatus, set[FindingStatus]] = {
    FindingStatus.OPEN: {FindingStatus.ACKNOWLEDGED, FindingStatus.RISK_ACCEPTED},
    FindingStatus.ACKNOWLEDGED: {FindingStatus.FIXED, FindingStatus.RISK_ACCEPTED},
    FindingStatus.FIXED: {FindingStatus.VERIFIED, FindingStatus.ACKNOWLEDGED},
    FindingStatus.VERIFIED: set(),
    FindingStatus.RISK_ACCEPTED: set(),
}


def validate_status_transition(
    current_status: FindingStatus,
    new_status: FindingStatus,
    actor_role: UserRole,
) -> None:
    if current_status == new_status:
        return

    if new_status not in ALLOWED_TRANSITIONS.get(current_status, set()):
        raise ValueError(f'不允許從 {current_status.value} 轉換為 {new_status.value}')

    if new_status == FindingStatus.VERIFIED and actor_role != UserRole.AUDITOR:
        raise PermissionError('僅 Auditor 可以驗證修復結果')
