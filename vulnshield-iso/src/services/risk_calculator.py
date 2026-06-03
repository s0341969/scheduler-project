def calculate_risk_score(cvss_score: float, asset_criticality: int) -> float:
    """
    ISO 27001 Risk Calculation: Final Risk = Severity * Criticality
    """
    return round(cvss_score * asset_criticality, 2)
