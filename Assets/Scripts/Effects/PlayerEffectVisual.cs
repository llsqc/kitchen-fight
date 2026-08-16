using UnityEngine;

public class PlayerEffectVisual : MonoBehaviour {

    private const float STUN_ORBIT_RADIUS = 0.4f;
    private const float STUN_STAR_SIZE = 0.15f;
    private const float STUN_Y_OFFSET = 1.8f;
    private const float REVERSE_RING_Y = 0.1f;
    private const float CLEANWIPE_DURATION = 0.8f;

    private PlayerEffectHost effectHost;

    private Transform stunStarsParent;
    private GameObject reverseRing;
    private GameObject cleanWipeFlash;
    private Material cleanWipeMat;


    private void Start() {
        effectHost = GetComponent<PlayerEffectHost>();

        CreateStunStars();
        CreateReverseRing();
        CreateCleanWipeFlash();
    }

    private void Update() {
        if (effectHost == null) return;

        UpdateStun();
        UpdateReverse();
        UpdateCleanWipe();
    }

    private void CreateStunStars() {
        stunStarsParent = new GameObject("StunStars").transform;
        stunStarsParent.SetParent(transform, false);
        stunStarsParent.localPosition = new Vector3(0, STUN_Y_OFFSET, 0);
        stunStarsParent.gameObject.SetActive(false);

        Material starMat = CreateSolidMaterial(new Color(1f, 0.85f, 0f, 1f));
        if (starMat == null) return;

        for (int i = 0; i < 3; i++) {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Star_" + i;
            star.transform.SetParent(stunStarsParent, false);
            star.transform.localScale = Vector3.one * STUN_STAR_SIZE;
            star.GetComponent<MeshRenderer>().material = starMat;
            Destroy(star.GetComponent<Collider>());
            float angle = i * 120f * Mathf.Deg2Rad;
            star.transform.localPosition = new Vector3(Mathf.Cos(angle) * STUN_ORBIT_RADIUS, 0, Mathf.Sin(angle) * STUN_ORBIT_RADIUS);
        }
    }

    private void CreateReverseRing() {
        reverseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        reverseRing.name = "ReverseRing";
        reverseRing.transform.SetParent(transform, false);
        reverseRing.transform.localPosition = new Vector3(0, REVERSE_RING_Y, 0);
        reverseRing.transform.localScale = new Vector3(1f, 0.1f, 1f);
        Destroy(reverseRing.GetComponent<Collider>());
        reverseRing.SetActive(false);

        Material ringMat = CreateSolidMaterial(new Color(0.6f, 0.2f, 0.9f, 1f));
        if (ringMat == null) return;
        ringMat.SetColor("_EmissionColor", new Color(0.4f, 0.1f, 0.6f, 1f));
        ringMat.EnableKeyword("_EMISSION");
        reverseRing.GetComponent<MeshRenderer>().material = ringMat;
    }

    private void CreateCleanWipeFlash() {
        cleanWipeFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cleanWipeFlash.name = "CleanWipeFlash";
        cleanWipeFlash.transform.SetParent(transform, false);
        cleanWipeFlash.transform.localPosition = new Vector3(0, 1f, 0);
        cleanWipeFlash.transform.localScale = Vector3.zero;
        Destroy(cleanWipeFlash.GetComponent<Collider>());
        cleanWipeFlash.SetActive(false);

        cleanWipeMat = CreateTransparentMaterial(new Color(1f, 1f, 1f, 0.6f));
        if (cleanWipeMat == null) return;
        cleanWipeFlash.GetComponent<MeshRenderer>().material = cleanWipeMat;
    }

    private void UpdateStun() {
        if (stunStarsParent == null) return;
        bool stunActive = effectHost.GetEffectRemaining(EffectType.Stun) > 0f;
        if (stunStarsParent.gameObject.activeSelf != stunActive) {
            stunStarsParent.gameObject.SetActive(stunActive);
        }
        if (stunActive) {
            stunStarsParent.Rotate(0, 180f * Time.deltaTime, 0);
        }
    }

    private void UpdateReverse() {
        if (reverseRing == null) return;
        bool reverseActive = effectHost.GetEffectRemaining(EffectType.ReverseControls) > 0f;
        if (reverseRing.activeSelf != reverseActive) {
            reverseRing.SetActive(reverseActive);
        }
        if (reverseActive) {
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.1f;
            reverseRing.transform.localScale = new Vector3(1f * pulse, 0.1f, 1f * pulse);
        }
    }

    private void UpdateCleanWipe() {
        if (cleanWipeFlash == null) return;
        float flash = effectHost.GetCleanWipeFlash();
        if (flash > 0f) {
            if (!cleanWipeFlash.activeSelf) cleanWipeFlash.SetActive(true);
            float t = 1f - flash / CLEANWIPE_DURATION;
            float scale = Mathf.Lerp(0f, 2.5f, t);
            cleanWipeFlash.transform.localScale = Vector3.one * scale;
            if (cleanWipeMat != null) {
                Color c = cleanWipeMat.color;
                c.a = Mathf.Lerp(0.6f, 0f, t);
                cleanWipeMat.color = c;
            }
        } else {
            if (cleanWipeFlash.activeSelf) cleanWipeFlash.SetActive(false);
        }
    }

    private Material CreateSolidMaterial(Color color) {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) {
            Debug.LogError("[PlayerEffectVisual] URP/Lit shader 未找到（被构建剥离？），特效视觉不可用", this);
            return null;
        }
        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private Material CreateTransparentMaterial(Color color) {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) {
            Debug.LogError("[PlayerEffectVisual] URP/Unlit shader 未找到（被构建剥离？），特效视觉不可用", this);
            return null;
        }
        Material mat = new Material(shader);
        mat.color = color;
        // URP 透明设置需同时设浮点与关键字，否则按不透明渲染
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetFloat("_SrcBlend", 5);   // SrcAlpha
        mat.SetFloat("_DstBlend", 10);  // OneMinusSrcAlpha
        mat.SetFloat("_ZWrite", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return mat;
    }

}
