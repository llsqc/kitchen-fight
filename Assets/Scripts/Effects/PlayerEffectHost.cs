using Unity.Netcode;
using UnityEngine;

public class PlayerEffectHost : NetworkBehaviour, IEffectHost {

    private const float PROTECTION_WINDOW = 10f;

    private NetworkVariable<float> stunTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> reverseTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> recentVictimTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> cleanWipeFlash = new NetworkVariable<float>(0f);


    private void Update() {
        if (!IsServer) return;

        if (stunTimer.Value > 0f) {
            stunTimer.Value -= Time.deltaTime;
            if (stunTimer.Value < 0f) stunTimer.Value = 0f;
        }
        if (reverseTimer.Value > 0f) {
            reverseTimer.Value -= Time.deltaTime;
            if (reverseTimer.Value < 0f) reverseTimer.Value = 0f;
        }
        if (recentVictimTimer.Value > 0f) {
            recentVictimTimer.Value -= Time.deltaTime;
            if (recentVictimTimer.Value < 0f) recentVictimTimer.Value = 0f;
        }
        if (cleanWipeFlash.Value > 0f) {
            cleanWipeFlash.Value -= Time.deltaTime;
            if (cleanWipeFlash.Value < 0f) cleanWipeFlash.Value = 0f;
        }
    }

    public void ApplyEffect(EffectType type, float duration, int sourceTeamId) {
        if (!IsServer) return;

        // Victim protection: halve duration if within protection window
        float actualDuration = duration;
        if (IsPlayerStateEffect(type) && recentVictimTimer.Value > 0f) {
            actualDuration = duration * 0.5f;
        }

        // Set protection timer
        if (IsPlayerStateEffect(type)) {
            recentVictimTimer.Value = PROTECTION_WINDOW;
        }

        switch (type) {
            case EffectType.Stun:
                // Same effect: take longer duration
                if (actualDuration > stunTimer.Value) {
                    stunTimer.Value = actualDuration;
                }
                break;
            case EffectType.ReverseControls:
                if (actualDuration > reverseTimer.Value) {
                    reverseTimer.Value = actualDuration;
                }
                break;
        }
    }

    private bool IsPlayerStateEffect(EffectType type) {
        return type == EffectType.Stun || type == EffectType.ReverseControls;
    }

    public VictimState GetVictimState() {
        if (recentVictimTimer.Value > 0f) {
            return VictimState.ProtectedHalve;
        }
        return VictimState.Normal;
    }

    public void ClearEffect(EffectType type) {
        if (!IsServer) return;

        switch (type) {
            case EffectType.Stun:
                stunTimer.Value = 0f;
                break;
            case EffectType.ReverseControls:
                reverseTimer.Value = 0f;
                break;
        }
    }

    public void ClearAllPlayerEffects() {
        if (!IsServer) return;
        stunTimer.Value = 0f;
        reverseTimer.Value = 0f;
    }

    public void TriggerCleanWipeFlash() {
        if (!IsServer) return;
        cleanWipeFlash.Value = 0.8f;
    }

    public float GetCleanWipeFlash() {
        return cleanWipeFlash.Value;
    }

    public float GetEffectRemaining(EffectType type) {
        return type switch {
            EffectType.Stun => stunTimer.Value,
            EffectType.ReverseControls => reverseTimer.Value,
            _ => 0f
        };
    }

}
