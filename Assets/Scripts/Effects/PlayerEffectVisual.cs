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

        Material starMat = CreateSolidMaterial(new Color(1f, 0.85f, 0f, 1f));

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

        stunStarsParent.gameObject.SetActive(false);
    }

    private void CreateReverseRing() {
        reverseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        reverseRing.name = "ReverseRing";
        reverseRing.transform.SetParent(transform, false);
        reverseRing.transform.localPosition = new Vector3(0, REVERSE_RING_Y, 0);
        reverseRing.transform.localScale = new Vector3(1f, 0.1f, 1f);
        Destroy(reverseRing.GetComponent<Collider>());

        Material ringMat = CreateSolidMaterial(new Color(0.6f, 0.2f, 0.9f, 1f));
        ringMat.SetColor("_EmissionColor", new Color(0.4f, 0.1f, 0.6f, 1f));
        ringMat.EnableKeyword("_EMISSION");
        reverseRing.GetComponent<MeshRenderer>().material = ringMat;

        reverseRing.SetActive(false);
    }

    private void CreateCleanWipeFlash() {
        cleanWipeFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cleanWipeFlash.name = "CleanWipeFlash";
        cleanWipeFlash.transform.SetParent(transform, false);
        cleanWipeFlash.transform.localPosition = new Vector3(0, 1f, 0);
        cleanWipeFlash.transform.localScale = Vector3.zero;
        Destroy(cleanWipeFlash.GetComponent<Collider>());

        cleanWipeMat = CreateTransparentMaterial(new Color(1f, 1f, 1f, 0.6f));
        cleanWipeFlash.GetComponent<MeshRenderer>().material = cleanWipeMat;

        cleanWipeFlash.SetActive(false);
    }

    private void UpdateStun() {
        bool stunActive = effectHost.GetEffectRemaining(EffectType.Stun) > 0f;
        if (stunStarsParent.gameObject.activeSelf != stunActive) {
            stunStarsParent.gameObject.SetActive(stunActive);
        }
        if (stunActive) {
            stunStarsParent.Rotate(0, 180f * Time.deltaTime, 0);
        }
    }

    private void UpdateReverse() {
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
        float flash = effectHost.GetCleanWipeFlash();
        if (flash > 0f) {
            if (!cleanWipeFlash.activeSelf) cleanWipeFlash.SetActive(true);
            float t = 1f - flash / CLEANWIPE_DURATION;
            float scale = Mathf.Lerp(0f, 2.5f, t);
            cleanWipeFlash.transform.localScale = Vector3.one * scale;
            Color c = cleanWipeMat.color;
            c.a = Mathf.Lerp(0.6f, 0f, t);
            cleanWipeMat.color = c;
        } else {
            if (cleanWipeFlash.activeSelf) cleanWipeFlash.SetActive(false);
        }
    }

    private Material CreateSolidMaterial(Color color) {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        return mat;
    }

    private Material CreateTransparentMaterial(Color color) {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = color;
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return mat;
    }

}
