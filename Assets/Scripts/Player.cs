using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Player : NetworkBehaviour, IKitchenObjectParent {


    public static event EventHandler OnAnyPlayerSpawned;
    public static event EventHandler OnAnyPickedSomething;


    public static void ResetStaticData() {
        OnAnyPlayerSpawned = null;
    }


    public static Player LocalInstance { get; private set; }



    public event EventHandler OnPickedSomething;
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter selectedCounter;
    }


    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private LayerMask collisionsLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;
    [SerializeField] private List<Vector3> spawnPositionList;
    [SerializeField] private PlayerVisual playerVisual;


    private bool isWalking;
    private Vector3 lastInteractDir;
    private BaseCounter selectedCounter;
    private KitchenObject kitchenObject;
    private PlayerEffectHost effectHost;

    private const float PLAYER_TARGET_RADIUS = 1.5f;
    private LayerMask playerLayerMask;

    public event UnityAction<int> OnCardAdded;


    private void Start() {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        GameInput.Instance.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;

        effectHost = GetComponent<PlayerEffectHost>();
        playerLayerMask = LayerMask.GetMask("Players");

        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(OwnerClientId);
        int teamId = playerData.teamId;
        int indexInTeam = KitchenGameMultiplayer.Instance.GetPlayerIndexInTeam(OwnerClientId);
        playerVisual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetTeamColor(teamId, indexInTeam));
    }

    public override void OnDestroy() {
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            LocalInstance = this;
        }

        transform.position = spawnPositionList[KitchenGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(OwnerClientId)];

        OnAnyPlayerSpawned?.Invoke(this, EventArgs.Empty);

        if (IsServer) {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId) {
        if (clientId == OwnerClientId && HasKitchenObject()) {
            KitchenObject.DestroyKitchenObject(GetKitchenObject());
        }
    }

    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (IsStunned()) return;

        if (selectedCounter != null && !IsCounterLocked(selectedCounter)) {
            selectedCounter.InteractAlternate(this);
        }
    }

    private void GameInput_OnInteractAction(object sender, System.EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (IsStunned()) return;

        if (selectedCounter != null && !IsCounterLocked(selectedCounter)) {
            selectedCounter.Interact(this);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseCardServerRpc(int itemIndex, Vector3 aimPosition, NetworkObjectReference counterRef) {
        SabotageItemSO item = KitchenGameMultiplayer.Instance.GetSabotageItemFromIndex(itemIndex);
        if (item == null) return;

        int myTeamId = KitchenGameMultiplayer.Instance.GetTeamIdFromClientId(OwnerClientId);
        ExecuteItemEffect(item, aimPosition, counterRef, myTeamId);
    }

    [ClientRpc]
    public void AddCardClientRpc(int itemIndex, ClientRpcParams rpcParams = default) {
        OnCardAdded?.Invoke(itemIndex);
    }

    private void ExecuteItemEffect(SabotageItemSO item, Vector3 aimPosition, NetworkObjectReference counterRef, int myTeamId) {
        bool hitAnyTarget = false;

        switch (item.targetType) {
            case TargetType.Player:
                Collider[] hits = Physics.OverlapSphere(aimPosition, PLAYER_TARGET_RADIUS, playerLayerMask);
                foreach (var hit in hits) {
                    Player targetPlayer = hit.GetComponent<Player>();
                    if (targetPlayer == null || targetPlayer == this) continue;
                    var playerHost = targetPlayer.GetComponent<PlayerEffectHost>();
                    if (playerHost != null) {
                        playerHost.ApplyEffect(item.effectType, item.duration, myTeamId);
                        hitAnyTarget = true;
                    }
                }
                break;
            case TargetType.Counter:
                if (counterRef.TryGet(out NetworkObject counterNetObj)) {
                    var counterHost = counterNetObj.GetComponent<CounterEffectHost>();
                    if (counterHost != null) {
                        counterHost.ApplyEffect(item.effectType, item.duration, myTeamId);
                        hitAnyTarget = true;
                    }
                }
                break;
            case TargetType.Self:
                var selfHost = GetComponent<PlayerEffectHost>();
                if (selfHost != null) {
                    selfHost.ClearAllPlayerEffects();
                    selfHost.TriggerCleanWipeFlash();
                }
                if (counterRef.TryGet(out NetworkObject selfCounterNetObj)) {
                    var selectedCounterHost = selfCounterNetObj.GetComponent<CounterEffectHost>();
                    if (selectedCounterHost != null) {
                        selectedCounterHost.ClearAllCounterEffects();
                    }
                }
                hitAnyTarget = true;
                break;
        }
    }

    private bool IsStunned() {
        return effectHost != null && effectHost.GetEffectRemaining(EffectType.Stun) > 0f;
    }

    private bool IsCounterLocked(BaseCounter counter) {
        var host = counter.GetComponent<CounterEffectHost>();
        return host != null && host.IsLocked();
    }

    private void Update() {
        if (!IsOwner) {
            return;
        }

        if (IsStunned()) {
            isWalking = false;
            return;
        }

        HandleMovement();
        HandleInteractions();
    }

    public bool IsWalking() {
        return isWalking;
    }

    private void HandleInteractions() {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        // Make movement camera-relative
        if (Camera.main != null) {
            moveDir = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * moveDir;
        }

        if (moveDir != Vector3.zero) {
            lastInteractDir = moveDir;
        }

        float interactDistance = 2f;
        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask)) {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter)) {
                // Has ClearCounter
                if (baseCounter != selectedCounter) {
                    SetSelectedCounter(baseCounter);
                }
            } else {
                SetSelectedCounter(null);

            }
        } else {
            SetSelectedCounter(null);
        }
    }

    private void HandleMovement() {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        // Make movement camera-relative
        if (Camera.main != null) {
            moveDir = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * moveDir;
        }

        float moveDistance = moveSpeed * Time.deltaTime;
        float playerRadius = .6f;
        bool canMove = !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDir, Quaternion.identity, moveDistance, collisionsLayerMask);

        if (!canMove) {
            // Cannot move towards moveDir

            // Attempt only X movement
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirX, Quaternion.identity, moveDistance, collisionsLayerMask);

            if (canMove) {
                // Can move only on the X
                moveDir = moveDirX;
            } else {
                // Cannot move only on the X

                // Attempt only Z movement
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirZ, Quaternion.identity, moveDistance, collisionsLayerMask);

                if (canMove) {
                    // Can move only on the Z
                    moveDir = moveDirZ;
                } else {
                    // Cannot move in any direction
                }
            }
        }

        if (canMove) {
            transform.position += moveDir * moveDistance;
        }

        isWalking = moveDir != Vector3.zero;

        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }

    private void SetSelectedCounter(BaseCounter selectedCounter) {
        this.selectedCounter = selectedCounter;

        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs {
            selectedCounter = selectedCounter
        });
    }

    public Transform GetKitchenObjectFollowTransform() {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject) {
        this.kitchenObject = kitchenObject;

        if (kitchenObject != null) {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
            OnAnyPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject() {
        return kitchenObject;
    }

    public void ClearKitchenObject() {
        kitchenObject = null;
    }

    public bool HasKitchenObject() {
        return kitchenObject != null;
    }


    public NetworkObject GetNetworkObject() {
        return NetworkObject;
    }


    public Unity.Netcode.NetworkObjectReference GetSelectedCounterRef() {
        if (selectedCounter != null) {
            return selectedCounter.GetNetworkObject();
        }
        return default;
    }

}
