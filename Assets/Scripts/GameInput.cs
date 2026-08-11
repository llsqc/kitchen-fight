using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour {


    private const string PLAYER_PREFS_BINDINGS = "InputBindings";


    public static GameInput Instance { get; private set; }



    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternateAction;
    public event EventHandler OnPauseAction;
    public event EventHandler OnUseItemAction;
    public event EventHandler OnSelectSlot1Action;
    public event EventHandler OnSelectSlot2Action;
    public event EventHandler OnBindingRebind;


    public enum Binding {
        Move_Up,
        Move_Down,
        Move_Left,
        Move_Right,
        Interact,
        InteractAlternate,
        Pause,
        UseItem,
        SelectSlot1,
        SelectSlot2,
        Gamepad_Interact,
        Gamepad_InteractAlternate,
        Gamepad_Pause,
        Gamepad_UseItem,
        Gamepad_SelectSlot1,
        Gamepad_SelectSlot2
    }


    private PlayerInputActions playerInputActions;


    private void Awake() {
        Instance = this;


        playerInputActions = new PlayerInputActions();

        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS)) {
            playerInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }

        playerInputActions.Player.Enable();

        playerInputActions.Player.Interact.performed += Interact_performed;
        playerInputActions.Player.InteractAlternate.performed += InteractAlternate_performed;
        playerInputActions.Player.Pause.performed += Pause_performed;
        playerInputActions.Player.UseItem.performed += UseItem_performed;
        playerInputActions.Player.SelectSlot1.performed += SelectSlot1_performed;
        playerInputActions.Player.SelectSlot2.performed += SelectSlot2_performed;
    }

    private void OnDestroy() {
        playerInputActions.Player.Interact.performed -= Interact_performed;
        playerInputActions.Player.InteractAlternate.performed -= InteractAlternate_performed;
        playerInputActions.Player.Pause.performed -= Pause_performed;
        playerInputActions.Player.UseItem.performed -= UseItem_performed;
        playerInputActions.Player.SelectSlot1.performed -= SelectSlot1_performed;
        playerInputActions.Player.SelectSlot2.performed -= SelectSlot2_performed;

        playerInputActions.Dispose();
    }

    private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void InteractAlternate_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnInteractAlternateAction?.Invoke(this, EventArgs.Empty);
    }

    private void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    private void UseItem_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnUseItemAction?.Invoke(this, EventArgs.Empty);
    }

    private void SelectSlot1_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnSelectSlot1Action?.Invoke(this, EventArgs.Empty);
    }

    private void SelectSlot2_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnSelectSlot2Action?.Invoke(this, EventArgs.Empty);
    }

    public Vector2 GetMovementVectorNormalized() {
        Vector2 inputVector = playerInputActions.Player.Move.ReadValue<Vector2>();

        inputVector = inputVector.normalized;

        // Reverse controls modifier (料酒上头 effect)
        if (Player.LocalInstance != null) {
            var host = Player.LocalInstance.GetComponent<PlayerEffectHost>();
            if (host != null && host.GetEffectRemaining(EffectType.ReverseControls) > 0f) {
                inputVector = -inputVector;
            }
        }

        return inputVector;
    }

    public Vector3 GetMouseWorldPosition() {
        if (Camera.main == null) return Vector3.zero;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance)) {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    public Vector3 GetMouseAimDirection(Vector3 fromPosition) {
        if (Camera.main == null) return Vector3.forward;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance)) {
            Vector3 mouseWorldPos = ray.GetPoint(distance);
            Vector3 dir = (mouseWorldPos - fromPosition);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) {
                return dir.normalized;
            }
        }

        return Vector3.forward;
    }

    public string GetBindingText(Binding binding) {
        switch (binding) {
            default:
            case Binding.Move_Up:
                return playerInputActions.Player.Move.bindings[1].ToDisplayString();
            case Binding.Move_Down:
                return playerInputActions.Player.Move.bindings[2].ToDisplayString();
            case Binding.Move_Left:
                return playerInputActions.Player.Move.bindings[3].ToDisplayString();
            case Binding.Move_Right:
                return playerInputActions.Player.Move.bindings[4].ToDisplayString();
            case Binding.Interact:
                return playerInputActions.Player.Interact.bindings[0].ToDisplayString();
            case Binding.InteractAlternate:
                return playerInputActions.Player.InteractAlternate.bindings[0].ToDisplayString();
            case Binding.Pause:
                return playerInputActions.Player.Pause.bindings[0].ToDisplayString();
            case Binding.UseItem:
                return playerInputActions.Player.UseItem.bindings[0].ToDisplayString();
            case Binding.SelectSlot1:
                return playerInputActions.Player.SelectSlot1.bindings[0].ToDisplayString();
            case Binding.SelectSlot2:
                return playerInputActions.Player.SelectSlot2.bindings[0].ToDisplayString();
            case Binding.Gamepad_Interact:
                return playerInputActions.Player.Interact.bindings[1].ToDisplayString();
            case Binding.Gamepad_InteractAlternate:
                return playerInputActions.Player.InteractAlternate.bindings[1].ToDisplayString();
            case Binding.Gamepad_Pause:
                return playerInputActions.Player.Pause.bindings[1].ToDisplayString();
            case Binding.Gamepad_UseItem:
                return playerInputActions.Player.UseItem.bindings[1].ToDisplayString();
            case Binding.Gamepad_SelectSlot1:
                return playerInputActions.Player.SelectSlot1.bindings[1].ToDisplayString();
            case Binding.Gamepad_SelectSlot2:
                return playerInputActions.Player.SelectSlot2.bindings[1].ToDisplayString();
        }
    }

    public void RebindBinding(Binding binding, Action onActionRebound) {
        playerInputActions.Player.Disable();

        InputAction inputAction;
        int bindingIndex;

        switch (binding) {
            default:
            case Binding.Move_Up:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 1;
                break;
            case Binding.Move_Down:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 2;
                break;
            case Binding.Move_Left:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 3;
                break;
            case Binding.Move_Right:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 4;
                break;
            case Binding.Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 0;
                break;
            case Binding.InteractAlternate:
                inputAction = playerInputActions.Player.InteractAlternate;
                bindingIndex = 0;
                break;
            case Binding.Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 0;
                break;
            case Binding.UseItem:
                inputAction = playerInputActions.Player.UseItem;
                bindingIndex = 0;
                break;
            case Binding.SelectSlot1:
                inputAction = playerInputActions.Player.SelectSlot1;
                bindingIndex = 0;
                break;
            case Binding.SelectSlot2:
                inputAction = playerInputActions.Player.SelectSlot2;
                bindingIndex = 0;
                break;
            case Binding.Gamepad_Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_InteractAlternate:
                inputAction = playerInputActions.Player.InteractAlternate;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_UseItem:
                inputAction = playerInputActions.Player.UseItem;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_SelectSlot1:
                inputAction = playerInputActions.Player.SelectSlot1;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_SelectSlot2:
                inputAction = playerInputActions.Player.SelectSlot2;
                bindingIndex = 1;
                break;
        }

        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback => {
                callback.Dispose();
                playerInputActions.Player.Enable();
                onActionRebound();

                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, playerInputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();

                OnBindingRebind?.Invoke(this, EventArgs.Empty);
            })
            .Start();
    }

}