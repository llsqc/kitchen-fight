using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerEffectHost : NetworkBehaviour, IEffectHost {

    private const float PROTECTION_WINDOW = 10f;

    // 负面效果计时器
    private NetworkVariable<float> stunTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> reverseTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> recentVictimTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> cleanWipeFlash = new NetworkVariable<float>(0f);

    // 正面效果计时器
    private NetworkVariable<float> moveSpeedTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> moveSpeedMagnitude = new NetworkVariable<float>(1f);
    private NetworkVariable<float> interactionSpeedTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> interactionSpeedMagnitude = new NetworkVariable<float>(1f);
    private NetworkVariable<float> doubleScoreTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> doubleScoreMagnitude = new NetworkVariable<float>(1f);

    // 防御状态计时器
    private NetworkVariable<float> shieldTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<int> shieldCharges = new NetworkVariable<int>(0);
    private NetworkVariable<float> reflectTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<int> reflectCharges = new NetworkVariable<int>(0);


    private void Update() {
        if (!IsServer) return;

        if (stunTimer.Value > 0f) { stunTimer.Value -= Time.deltaTime; if (stunTimer.Value < 0f) stunTimer.Value = 0f; }
        if (reverseTimer.Value > 0f) { reverseTimer.Value -= Time.deltaTime; if (reverseTimer.Value < 0f) reverseTimer.Value = 0f; }
        if (recentVictimTimer.Value > 0f) { recentVictimTimer.Value -= Time.deltaTime; if (recentVictimTimer.Value < 0f) recentVictimTimer.Value = 0f; }
        if (cleanWipeFlash.Value > 0f) { cleanWipeFlash.Value -= Time.deltaTime; if (cleanWipeFlash.Value < 0f) cleanWipeFlash.Value = 0f; }

        if (moveSpeedTimer.Value > 0f) { moveSpeedTimer.Value -= Time.deltaTime; if (moveSpeedTimer.Value < 0f) moveSpeedTimer.Value = 0f; }
        if (interactionSpeedTimer.Value > 0f) { interactionSpeedTimer.Value -= Time.deltaTime; if (interactionSpeedTimer.Value < 0f) interactionSpeedTimer.Value = 0f; }
        if (doubleScoreTimer.Value > 0f) { doubleScoreTimer.Value -= Time.deltaTime; if (doubleScoreTimer.Value < 0f) doubleScoreTimer.Value = 0f; }

        if (shieldTimer.Value > 0f) { shieldTimer.Value -= Time.deltaTime; if (shieldTimer.Value < 0f) shieldTimer.Value = 0f; }
        if (reflectTimer.Value > 0f) { reflectTimer.Value -= Time.deltaTime; if (reflectTimer.Value < 0f) reflectTimer.Value = 0f; }
    }

    public void ApplyEffect(EffectType type, float duration, int sourceTeamId, ulong sourceClientId) {
        if (!IsServer) return;

        // ── 反制拦截链 ──
        if (IsPlayerStateEffect(type)) {
            // Shield: 吞掉效果
            if (shieldCharges.Value > 0) {
                shieldCharges.Value -= 1;
                if (shieldCharges.Value <= 0) {
                    shieldTimer.Value = 0f;
                }
                return;
            }

            // Reflect: 反弹给攻击者
            if (reflectCharges.Value > 0) {
                reflectCharges.Value -= 1;
                if (reflectCharges.Value <= 0) {
                    reflectTimer.Value = 0f;
                }

                // 找到攻击者的 PlayerEffectHost 并直接施加效果（绕过拦截链）
                var sourcePlayer = FindPlayerByClientId(sourceClientId);
                if (sourcePlayer != null) {
                    var sourceHost = sourcePlayer.GetComponent<PlayerEffectHost>();
                    if (sourceHost != null) {
                        sourceHost.ApplyEffectDirect(type, duration, sourceTeamId);
                    }
                }
                return;
            }
        }

        ApplyEffectDirect(type, duration, sourceTeamId);
    }

    private void ApplyEffectDirect(EffectType type, float duration, int sourceTeamId) {
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

    public void ApplyBuff(EffectType type, float duration, float magnitude) {
        if (!IsServer) return;

        switch (type) {
            case EffectType.MoveSpeedUp:
                if (duration > moveSpeedTimer.Value) {
                    moveSpeedTimer.Value = duration;
                    moveSpeedMagnitude.Value = magnitude;
                }
                break;
            case EffectType.InteractionSpeedUp:
                if (duration > interactionSpeedTimer.Value) {
                    interactionSpeedTimer.Value = duration;
                    interactionSpeedMagnitude.Value = magnitude;
                }
                break;
            case EffectType.DoubleScore:
                if (duration > doubleScoreTimer.Value) {
                    doubleScoreTimer.Value = duration;
                    doubleScoreMagnitude.Value = magnitude;
                }
                break;
        }
    }

    public void ApplyCounter(EffectType type, float duration, float magnitude) {
        if (!IsServer) return;

        int charges = Mathf.RoundToInt(magnitude);
        if (charges < 1) charges = 1;

        switch (type) {
            case EffectType.Shield:
                shieldTimer.Value = duration;
                shieldCharges.Value = charges;
                break;
            case EffectType.Reflect:
                reflectTimer.Value = duration;
                reflectCharges.Value = charges;
                break;
        }
    }

    public bool HasShield() {
        return shieldCharges.Value > 0;
    }

    public bool HasReflect() {
        return reflectCharges.Value > 0;
    }

    public float GetMoveSpeedMultiplier() {
        return moveSpeedTimer.Value > 0f ? moveSpeedMagnitude.Value : 1f;
    }

    public float GetInteractionSpeedMultiplier() {
        return interactionSpeedTimer.Value > 0f ? interactionSpeedMagnitude.Value : 1f;
    }

    public float GetDoubleScoreMultiplier() {
        return doubleScoreTimer.Value > 0f ? doubleScoreMagnitude.Value : 1f;
    }

    public bool HasDoubleScore() {
        return doubleScoreTimer.Value > 0f;
    }

    private bool IsPlayerStateEffect(EffectType type) {
        return type == EffectType.Stun || type == EffectType.ReverseControls;
    }

    public VictimState GetVictimState() {
        if (shieldCharges.Value > 0) return VictimState.ImmuneBlock;
        if (recentVictimTimer.Value > 0f) return VictimState.ProtectedHalve;
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
            EffectType.MoveSpeedUp => moveSpeedTimer.Value,
            EffectType.InteractionSpeedUp => interactionSpeedTimer.Value,
            EffectType.DoubleScore => doubleScoreTimer.Value,
            EffectType.Shield => shieldTimer.Value,
            EffectType.Reflect => reflectTimer.Value,
            _ => 0f
        };
    }

    private Player FindPlayerByClientId(ulong clientId) {
        foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None)) {
            if (player.OwnerClientId == clientId) {
                return player;
            }
        }
        return null;
    }

}
