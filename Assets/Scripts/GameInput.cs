using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour {


    private const string PLAYER_PREFS_BINDINGS = "InputBindings";


    public static GameInput Instance { get; private set; }



    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternateAction;
    public event EventHandler OnPauseAction;
    public event EventHandler OnTogglePanelAction;
    public event EventHandler OnBindingRebind;


    public enum Binding {
        Move_Up,
        Move_Down,
        Move_Left,
        Move_Right,
        Interact,
        InteractAlternate,
        Pause,
        TogglePanel,
        Gamepad_Interact,
        Gamepad_InteractAlternate,
        Gamepad_Pause
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
        playerInputActions.Player.TogglePanel.performed += TogglePanel_performed;
    }

    private void OnDestroy() {
        playerInputActions.Player.Interact.performed -= Interact_performed;
        playerInputActions.Player.InteractAlternate.performed -= InteractAlternate_performed;
        playerInputActions.Player.Pause.performed -= Pause_performed;
        playerInputActions.Player.TogglePanel.performed -= TogglePanel_performed;

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

    private void TogglePanel_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        OnTogglePanelAction?.Invoke(this, EventArgs.Empty);
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
        string displayString;
        switch (binding) {
            default:
            case Binding.Move_Up:
                displayString = playerInputActions.Player.Move.bindings[1].ToDisplayString();
                break;
            case Binding.Move_Down:
                displayString = playerInputActions.Player.Move.bindings[2].ToDisplayString();
                break;
            case Binding.Move_Left:
                displayString = playerInputActions.Player.Move.bindings[3].ToDisplayString();
                break;
            case Binding.Move_Right:
                displayString = playerInputActions.Player.Move.bindings[4].ToDisplayString();
                break;
            case Binding.Interact:
                displayString = playerInputActions.Player.Interact.bindings[0].ToDisplayString();
                break;
            case Binding.InteractAlternate:
                displayString = playerInputActions.Player.InteractAlternate.bindings[0].ToDisplayString();
                break;
            case Binding.Pause:
                displayString = playerInputActions.Player.Pause.bindings[0].ToDisplayString();
                break;
            case Binding.TogglePanel:
                displayString = playerInputActions.Player.TogglePanel.bindings[0].ToDisplayString();
                break;
            case Binding.Gamepad_Interact:
                displayString = playerInputActions.Player.Interact.bindings[1].ToDisplayString();
                break;
            case Binding.Gamepad_InteractAlternate:
                displayString = playerInputActions.Player.InteractAlternate.bindings[1].ToDisplayString();
                break;
            case Binding.Gamepad_Pause:
                displayString = playerInputActions.Player.Pause.bindings[1].ToDisplayString();
                break;
        }

        // 缩写常见长键名，避免窄按钮内折行
        if (displayString == "Escape") {
            displayString = "Esc";
        }
        return displayString;
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
            case Binding.TogglePanel:
                inputAction = playerInputActions.Player.TogglePanel;
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
        }

        inputAction.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(callback => {
                callback.Dispose();
                playerInputActions.Player.Enable();
                onActionRebound();

                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, playerInputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();

                OnBindingRebind?.Invoke(this, EventArgs.Empty);
            })
            .OnCancel(callback => {
                callback.Dispose();
                playerInputActions.Player.Enable();
                onActionRebound();
            })
            .Start();
    }

    public void ResetBindings() {
        foreach (InputAction action in playerInputActions.asset) {
            action.RemoveAllBindingOverrides();
        }

        PlayerPrefs.DeleteKey(PLAYER_PREFS_BINDINGS);
        PlayerPrefs.Save();

        OnBindingRebind?.Invoke(this, EventArgs.Empty);
    }

}