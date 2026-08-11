using Unity.Netcode;
using UnityEngine;

public class CounterEffectHost : NetworkBehaviour, IEffectHost {

    private NetworkVariable<float> lockTimer = new NetworkVariable<float>(0f);

    private GameObject lockVisual;
    private Material lockMat;


    private void Start() {
        CreateLockVisual();
    }

    private void CreateLockVisual() {
        lockVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lockVisual.name = "LockVisual";
        lockVisual.transform.SetParent(transform, false);
        lockVisual.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        lockVisual.transform.localScale = new Vector3(1.1f, 0.08f, 1.1f);
        Destroy(lockVisual.GetComponent<Collider>());

        lockMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lockMat.color = new Color(1f, 0.1f, 0.1f, 0.5f);
        lockMat.SetFloat("_Surface", 1);
        lockMat.SetFloat("_Blend", 0);
        lockMat.SetOverrideTag("RenderType", "Transparent");
        lockMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        lockVisual.GetComponent<MeshRenderer>().material = lockMat;

        lockVisual.SetActive(false);
    }


    private void Update() {
        if (!IsServer) return;

        if (lockTimer.Value > 0f) {
            lockTimer.Value -= Time.deltaTime;
            if (lockTimer.Value < 0f) lockTimer.Value = 0f;
        }

        UpdateLockVisual();
    }

    private void UpdateLockVisual() {
        if (lockVisual == null) return;
        bool locked = lockTimer.Value > 0f;
        if (lockVisual.activeSelf != locked) {
            lockVisual.SetActive(locked);
        }
        if (locked && lockMat != null) {
            float pulse = 0.35f + Mathf.Sin(Time.time * 4f) * 0.15f;
            lockMat.color = new Color(1f, 0.1f, 0.1f, pulse);
        }
    }

    public void ApplyEffect(EffectType type, float duration, int sourceTeamId) {
        if (!IsServer) return;

        if (type == EffectType.LockCounter) {
            if (duration > lockTimer.Value) {
                lockTimer.Value = duration;
            }
        }
    }

    public VictimState GetVictimState() {
        return VictimState.Normal;
    }

    public void ClearEffect(EffectType type) {
        if (!IsServer) return;

        if (type == EffectType.LockCounter) {
            lockTimer.Value = 0f;
        }
    }

    public void ClearAllCounterEffects() {
        if (!IsServer) return;
        lockTimer.Value = 0f;
    }

    public float GetEffectRemaining(EffectType type) {
        if (type == EffectType.LockCounter) {
            return lockTimer.Value;
        }
        return 0f;
    }

    public bool IsLocked() {
        return lockTimer.Value > 0f;
    }

}
