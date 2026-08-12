public enum VictimState {
    Normal,
    ProtectedHalve,
    ImmuneBlock,
}

public interface IEffectHost {

    void ApplyEffect(EffectType type, float duration, int sourceTeamId, ulong sourceClientId);
    VictimState GetVictimState();
    void ClearEffect(EffectType type);
    float GetEffectRemaining(EffectType type);

}
