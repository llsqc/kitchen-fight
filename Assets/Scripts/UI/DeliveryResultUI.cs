using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryResultUI : MonoBehaviour {


    private const string POPUP = "Popup";


    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Color successColor;
    [SerializeField] private Color failedColor;
    [SerializeField] private Sprite successSprite;
    [SerializeField] private Sprite failedSprite;


    private Animator animator;
    private DeliveryCounter deliveryCounter;

    private void Awake() {
        animator = GetComponent<Animator>();
        deliveryCounter = GetComponentInParent<DeliveryCounter>();
    }

    private void Start() {
        DeliveryManager.Instance.OnRecipeSuccess += DeliveryManager_OnRecipeSuccess;
        DeliveryManager.Instance.OnRecipeFailed += DeliveryManager_OnRecipeFailed;

        gameObject.SetActive(false);
    }

    private bool IsLocalTeam(int teamId) {
        return teamId == KitchenGameMultiplayer.Instance.GetTeamIdFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    private bool IsSourceCounter(ulong deliveryCounterNetworkObjectId) {
        return deliveryCounter != null
            && deliveryCounter.IsSpawned
            && deliveryCounter.NetworkObjectId == deliveryCounterNetworkObjectId;
    }

    private void DeliveryManager_OnRecipeFailed(object sender, DeliveryManager.DeliveryEventArgs e) {
        if (!IsLocalTeam(e.teamId) || !IsSourceCounter(e.deliveryCounterNetworkObjectId)) return;

        gameObject.SetActive(true);
        animator.SetTrigger(POPUP);
        backgroundImage.color = failedColor;
        iconImage.sprite = failedSprite;
        messageText.text = "提交\n失败";
    }

    private void DeliveryManager_OnRecipeSuccess(object sender, DeliveryManager.DeliveryEventArgs e) {
        if (!IsLocalTeam(e.teamId) || !IsSourceCounter(e.deliveryCounterNetworkObjectId)) return;

        gameObject.SetActive(true);
        animator.SetTrigger(POPUP);
        backgroundImage.color = successColor;
        iconImage.sprite = successSprite;
        messageText.text = "提交\n成功";
    }

}
