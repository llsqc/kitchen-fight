using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour {


    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private LobbyCreateUI lobbyCreateUI;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private Transform lobbyTemplate;


    private void Awake() {
        mainMenuButton.onClick.AddListener(() => {
            Loader.Load(Loader.Scene.MainMenuScene);
        });
        createLobbyButton.onClick.AddListener(() => {
            KitchenGameMultiplayer.Instance.StartDirectHost();
        });
        quickJoinButton.onClick.AddListener(() => {
            KitchenGameMultiplayer.Instance.StartDirectClient("127.0.0.1");
        });
        joinCodeButton.onClick.AddListener(() => {
            KitchenGameMultiplayer.Instance.StartDirectClient(joinCodeInputField.text);
        });

        createLobbyButton.GetComponentInChildren<TextMeshProUGUI>().text = "创建主机";
        quickJoinButton.GetComponentInChildren<TextMeshProUGUI>().text = "本机加入";
        joinCodeButton.GetComponentInChildren<TextMeshProUGUI>().text = "加入此IP";

        lobbyTemplate.gameObject.SetActive(false);
        lobbyContainer.gameObject.SetActive(false);
        lobbyCreateUI.gameObject.SetActive(false);
    }

    private void Start() {
        playerNameInputField.text = KitchenGameMultiplayer.Instance.GetPlayerName();
        joinCodeInputField.text = "127.0.0.1";
        playerNameInputField.onValueChanged.AddListener((string newText) => {
            KitchenGameMultiplayer.Instance.SetPlayerName(newText);
        });
    }

}
