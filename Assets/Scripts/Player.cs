using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

    private NetworkVariable<int> itemSlot0 = new NetworkVariable<int>(-1);
    private NetworkVariable<int> itemSlot1 = new NetworkVariable<int>(-1);

    private const float PLAYER_TARGET_RADIUS = 1.5f;
    private int selectedSlot = 0;
    private LayerMask playerLayerMask;


    private void Start() {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        GameInput.Instance.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
        GameInput.Instance.OnUseItemAction += GameInput_OnUseItemAction;
        GameInput.Instance.OnSelectSlot1Action += GameInput_OnSelectSlot1;
        GameInput.Instance.OnSelectSlot2Action += GameInput_OnSelectSlot2;

        effectHost = GetComponent<PlayerEffectHost>();
        playerLayerMask = LayerMask.GetMask("Players");

        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(OwnerClientId);
        int teamId = playerData.teamId;
        int indexInTeam = KitchenGameMultiplayer.Instance.GetPlayerIndexInTeam(OwnerClientId);
        playerVisual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetTeamColor(teamId, indexInTeam));
    }

    public override void OnDestroy() {
        if (GameInput.Instance != null) {
            GameInput.Instance.OnUseItemAction -= GameInput_OnUseItemAction;
            GameInput.Instance.OnSelectSlot1Action -= GameInput_OnSelectSlot1;
            GameInput.Instance.OnSelectSlot2Action -= GameInput_OnSelectSlot2;
        }
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

    private void GameInput_OnSelectSlot1(object sender, EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;
        if (IsStunned()) return;

        HandleSlotSelect(0);
    }

    private void GameInput_OnSelectSlot2(object sender, EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;
        if (IsStunned()) return;

        HandleSlotSelect(1);
    }

    private void HandleSlotSelect(int slotIndex) {
        int itemIndex = slotIndex == 0 ? itemSlot0.Value : itemSlot1.Value;
        if (itemIndex == -1) {
            SoundManager.Instance.PlayWarningSound(transform.position);
            return;
        }

        SabotageItemSO item = KitchenGameMultiplayer.Instance.GetSabotageItemFromIndex(itemIndex);
        if (item == null) return;

        if (item.targetType == TargetType.Player) {
            selectedSlot = slotIndex;
        } else {
            // Counter/Self: use immediately
            NetworkObjectReference counterRef = default;
            if (selectedCounter != null) {
                counterRef = selectedCounter.GetNetworkObject();
            }

            if (item.targetType == TargetType.Counter && selectedCounter == null) {
                SoundManager.Instance.PlayWarningSound(transform.position);
                return;
            }

            UseItemServerRpc(slotIndex, Vector3.zero, counterRef);
        }
    }

    private void GameInput_OnUseItemAction(object sender, EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;
        if (IsStunned()) return;

        int itemIndex = selectedSlot == 0 ? itemSlot0.Value : itemSlot1.Value;
        if (itemIndex == -1) {
            SoundManager.Instance.PlayWarningSound(transform.position);
            return;
        }

        SabotageItemSO item = KitchenGameMultiplayer.Instance.GetSabotageItemFromIndex(itemIndex);
        if (item == null) return;

        if (item.targetType != TargetType.Player) return;

        Vector3 aimPos = GameInput.Instance.GetMouseWorldPosition();
        UseItemServerRpc(selectedSlot, aimPos, default);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UseItemServerRpc(int slotIndex, Vector3 aimPosition, NetworkObjectReference counterRef) {
        int itemIndex = slotIndex == 0 ? itemSlot0.Value : itemSlot1.Value;
        if (itemIndex == -1) return;

        SabotageItemSO item = KitchenGameMultiplayer.Instance.GetSabotageItemFromIndex(itemIndex);
        if (item == null) return;

        int myTeamId = KitchenGameMultiplayer.Instance.GetTeamIdFromClientId(OwnerClientId);
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
                if (!counterRef.TryGet(out NetworkObject counterNetObj)) return;
                BaseCounter targetCounter = counterNetObj.GetComponent<BaseCounter>();
                if (targetCounter == null) return;
                var counterHost = targetCounter.GetComponent<CounterEffectHost>();
                if (counterHost != null) {
                    counterHost.ApplyEffect(item.effectType, item.duration, myTeamId);
                    hitAnyTarget = true;
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

        if (!hitAnyTarget) return;

        // Remove item from inventory
        if (slotIndex == 0) {
            itemSlot0.Value = -1;
        } else {
            itemSlot1.Value = -1;
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

    public int GetSelectedSlot() {
        return selectedSlot;
    }

    public int GetSelectedItemIndex() {
        return selectedSlot == 0 ? itemSlot0.Value : itemSlot1.Value;
    }

    public bool HasPlayerTargetItemSelected() {
        int itemIndex = GetSelectedItemIndex();
        if (itemIndex == -1) return false;
        SabotageItemSO item = KitchenGameMultiplayer.Instance.GetSabotageItemFromIndex(itemIndex);
        return item != null && item.targetType == TargetType.Player;
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


    public int GetEmptySlot() {
        if (itemSlot0.Value == -1) return 0;
        if (itemSlot1.Value == -1) return 1;
        return -1;
    }

    public void SetItemSlot(int slot, int itemIndex) {
        if (!IsServer) return;
        if (slot == 0) {
            itemSlot0.Value = itemIndex;
        } else if (slot == 1) {
            itemSlot1.Value = itemIndex;
        }
    }

    public int GetItemSlot(int slot) {
        return slot == 0 ? itemSlot0.Value : itemSlot1.Value;
    }

}
