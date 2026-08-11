using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManagerUI : MonoBehaviour {


    [SerializeField] private Transform container;
    [SerializeField] private Transform recipeTemplate;


    private void Awake() {
        recipeTemplate.gameObject.SetActive(false);
    }

    private void Start() {
        DeliveryManager.Instance.OnRecipeSpawned += DeliveryManager_OnRecipeSpawned;
        DeliveryManager.Instance.OnRecipeCompleted += DeliveryManager_OnRecipeCompleted;

        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeCompleted(object sender, DeliveryManager.RecipeEventArgs e) {
        if (IsLocalTeam(e.teamId)) {
            UpdateVisual();
        }
    }

    private void DeliveryManager_OnRecipeSpawned(object sender, DeliveryManager.RecipeEventArgs e) {
        if (IsLocalTeam(e.teamId)) {
            UpdateVisual();
        }
    }

    private bool IsLocalTeam(int teamId) {
        return teamId == KitchenGameMultiplayer.Instance.GetTeamIdFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    private void UpdateVisual() {
        foreach (Transform child in container) {
            if (child == recipeTemplate) continue;
            Destroy(child.gameObject);
        }

        int localTeamId = KitchenGameMultiplayer.Instance.GetTeamIdFromClientId(NetworkManager.Singleton.LocalClientId);
        foreach (RecipeSO recipeSO in DeliveryManager.Instance.GetTeamWaitingList(localTeamId)) {
            Transform recipeTransform = Instantiate(recipeTemplate, container);
            recipeTransform.gameObject.SetActive(true);
            recipeTransform.GetComponent<DeliveryManagerSingleUI>().SetRecipeSO(recipeSO);
        }
    }

}
