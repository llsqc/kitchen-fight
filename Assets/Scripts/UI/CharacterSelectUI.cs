using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour {


    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;


    private void Awake() {
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });
        readyButton.onClick.AddListener(() => {
            CharacterSelectReady.Instance.SetPlayerReady();
        });
    }

    private void Start() {
        Lobby lobby = KitchenGameLobby.Instance.GetLobby();
        if (lobby == null) {
            lobbyNameText.text = "Direct Connection";
            lobbyCodeText.text = NetworkManager.Singleton.IsHost
                ? "Port: " + KitchenGameMultiplayer.DEFAULT_PORT + " - Share your virtual LAN IP"
                : "Host: " + KitchenGameMultiplayer.Instance.GetDirectConnectionAddress();
            return;
        }

        lobbyNameText.text = "房间名称: " + lobby.Name;
        lobbyCodeText.text = "房间代码: " + lobby.LobbyCode;
    }
}
