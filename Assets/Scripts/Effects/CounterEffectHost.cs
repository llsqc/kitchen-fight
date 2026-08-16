using Unity.Netcode;
using UnityEngine;

public class CounterEffectHost : NetworkBehaviour, IEffectHost {

    private NetworkVariable<float> lockTimer = new NetworkVariable<float>(0f);

    private GameObject lockVisual;
    private Material lockMat;


    private void Start() {
        CreateLockVisual();
        lockTimer.OnValueChanged += LockTimer_OnValueChanged;
    }

    private void LockTimer_OnValueChanged(float previousValue, float newValue) {
        // NetworkVariable 回调在所有端触发，客户端也能看到锁定板显隐
        UpdateLockVisual();
    }

    private void CreateLockVisual() {
        lockVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lockVisual.name = "LockVisual";
        lockVisual.transform.SetParent(transform, false);
        lockVisual.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        lockVisual.transform.localScale = new Vector3(1.1f, 0.08f, 1.1f);
        Destroy(lockVisual.GetComponent<Collider>());
        // 先隐藏，材质创建失败（shader 剥离等）也不会留下可见薄板
        lockVisual.SetActive(false);

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) {
            Debug.LogError("[CounterEffectHost] URP/Unlit shader 未找到（被构建剥离？），锁定视觉效果不可用", this);
            return;
        }

        lockMat = new Material(unlitShader);
        lockMat.color = new Color(1f, 0.1f, 0.1f, 0.5f);
        // URP 透明设置需同时设浮点与关键字，否则按不透明渲染
        lockMat.SetFloat("_Surface", 1);
        lockMat.SetFloat("_Blend", 0);
        lockMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        lockMat.SetFloat("_SrcBlend", 5);   // SrcAlpha
        lockMat.SetFloat("_DstBlend", 10);  // OneMinusSrcAlpha
        lockMat.SetFloat("_ZWrite", 0);
        lockMat.SetOverrideTag("RenderType", "Transparent");
        lockMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        lockVisual.GetComponent<MeshRenderer>().material = lockMat;
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

    public void ApplyEffect(EffectType type, float duration, int sourceTeamId, ulong sourceClientId) {
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
