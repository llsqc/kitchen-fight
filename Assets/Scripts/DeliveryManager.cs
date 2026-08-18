using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour {


    public class RecipeEventArgs : EventArgs {
        public int teamId;
    }

    public class DeliveryEventArgs : EventArgs {
        public int teamId;
        public Vector3 deliveryPosition;
    }


    public event EventHandler<RecipeEventArgs> OnRecipeSpawned;
    public event EventHandler<RecipeEventArgs> OnRecipeCompleted;
    public event EventHandler<DeliveryEventArgs> OnRecipeSuccess;
    public event EventHandler<DeliveryEventArgs> OnRecipeFailed;
    public event EventHandler OnTeamScoreChanged;


    public static DeliveryManager Instance { get; private set; }


    [SerializeField] private RecipeListSO recipeListSO;


    private List<RecipeSO>[] teamWaitingLists;
    private NetworkVariable<int> team0Score = new NetworkVariable<int>(0);
    private NetworkVariable<int> team1Score = new NetworkVariable<int>(0);
    private float spawnRecipeTimer = 4f;
    private float spawnRecipeTimerMax = 4f;
    private int waitingRecipesMax = 4;


    private void Awake() {
        Instance = this;

        teamWaitingLists = new List<RecipeSO>[2];
        teamWaitingLists[0] = new List<RecipeSO>();
        teamWaitingLists[1] = new List<RecipeSO>();

        team0Score.OnValueChanged += (old, newVal) => OnTeamScoreChanged?.Invoke(this, EventArgs.Empty);
        team1Score.OnValueChanged += (old, newVal) => OnTeamScoreChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Update() {
        if (!IsServer) {
            return;
        }

        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f) {
            spawnRecipeTimer = spawnRecipeTimerMax;

            if (KitchenGameManager.Instance.IsGamePlaying()) {
                // Generate one random recipe index, both teams get the same (mirror mode for fairness)
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);
                for (int teamId = 0; teamId < 2; teamId++) {
                    if (teamWaitingLists[teamId].Count < waitingRecipesMax) {
                        SpawnNewWaitingRecipeClientRpc(teamId, waitingRecipeSOIndex);
                    }
                }
            }
        }
    }

    [ClientRpc]
    private void SpawnNewWaitingRecipeClientRpc(int teamId, int waitingRecipeSOIndex) {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];

        teamWaitingLists[teamId].Add(waitingRecipeSO);

        OnRecipeSpawned?.Invoke(this, new RecipeEventArgs { teamId = teamId });
    }

    public void DeliverRecipe(PlateKitchenObject plateKitchenObject, int teamId, Vector3 deliveryPosition) {
        for (int i = 0; i < teamWaitingLists[teamId].Count; i++) {
            RecipeSO waitingRecipeSO = teamWaitingLists[teamId][i];

            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count) {
                // Has the same number of ingredients
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList) {
                    // Cycling through all ingredients in the Recipe
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList()) {
                        // Cycling through all ingredients in the Plate
                        if (plateKitchenObjectSO == recipeKitchenObjectSO) {
                            // Ingredient matches!
                            ingredientFound = true;
                            break;
                        }
                    }
                    if (!ingredientFound) {
                        // This Recipe ingredient was not found on the Plate
                        plateContentsMatchesRecipe = false;
                    }
                }

                if (plateContentsMatchesRecipe) {
                    // Player delivered the correct recipe!
                    DeliverCorrectRecipeServerRpc(teamId, i, deliveryPosition);
                    return;
                }
            }
        }

        // No matches found!
        // Player did not deliver a correct recipe
        DeliverIncorrectRecipeServerRpc(teamId, deliveryPosition);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeliverIncorrectRecipeServerRpc(int teamId, Vector3 deliveryPosition) {
        DeliverIncorrectRecipeClientRpc(teamId, deliveryPosition);
    }

    [ClientRpc]
    private void DeliverIncorrectRecipeClientRpc(int teamId, Vector3 deliveryPosition) {
        OnRecipeFailed?.Invoke(this, new DeliveryEventArgs { teamId = teamId, deliveryPosition = deliveryPosition });
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeliverCorrectRecipeServerRpc(int teamId, int waitingRecipeSOListIndex, Vector3 deliveryPosition) {
        GetTeamScoreNetworkVariable(teamId).Value += GetScorePerRecipe(teamId);
        DeliverCorrectRecipeClientRpc(teamId, waitingRecipeSOListIndex, deliveryPosition);
    }

    [ClientRpc]
    private void DeliverCorrectRecipeClientRpc(int teamId, int waitingRecipeSOListIndex, Vector3 deliveryPosition) {
        teamWaitingLists[teamId].RemoveAt(waitingRecipeSOListIndex);

        OnRecipeCompleted?.Invoke(this, new RecipeEventArgs { teamId = teamId });
        OnRecipeSuccess?.Invoke(this, new DeliveryEventArgs { teamId = teamId, deliveryPosition = deliveryPosition });
    }

    public void SubmitAllWaitingRecipes(int teamId, Vector3 deliveryPosition) {
        if (!IsServer) return;
        if (teamId < 0 || teamId >= teamWaitingLists.Length) return;

        int recipeCount = teamWaitingLists[teamId].Count;
        if (recipeCount == 0) return;

        GetTeamScoreNetworkVariable(teamId).Value += recipeCount * GetScorePerRecipe(teamId);
        SubmitAllWaitingRecipesClientRpc(teamId, deliveryPosition);
    }

    [ClientRpc]
    private void SubmitAllWaitingRecipesClientRpc(int teamId, Vector3 deliveryPosition) {
        if (teamId < 0 || teamId >= teamWaitingLists.Length) return;
        if (teamWaitingLists[teamId].Count == 0) return;

        teamWaitingLists[teamId].Clear();
        OnRecipeCompleted?.Invoke(this, new RecipeEventArgs { teamId = teamId });
        OnRecipeSuccess?.Invoke(this, new DeliveryEventArgs { teamId = teamId, deliveryPosition = deliveryPosition });
    }

    private int GetScorePerRecipe(int teamId) {
        int scoreToAdd = 1;

        // 检查该队伍是否有玩家持有 DoubleScore Buff
        foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None)) {
            if (KitchenGameMultiplayer.Instance.GetTeamIdFromClientId(player.OwnerClientId) == teamId) {
                var host = player.GetComponent<PlayerEffectHost>();
                if (host != null && host.HasDoubleScore()) {
                    scoreToAdd = Mathf.RoundToInt(scoreToAdd * host.GetDoubleScoreMultiplier());
                    break;
                }
            }
        }

        return scoreToAdd;
    }

    private NetworkVariable<int> GetTeamScoreNetworkVariable(int teamId) {
        return teamId == 0 ? team0Score : team1Score;
    }


    public List<RecipeSO> GetTeamWaitingList(int teamId) {
        return teamWaitingLists[teamId];
    }

    public int GetTeamScore(int teamId) {
        return GetTeamScoreNetworkVariable(teamId).Value;
    }

}
