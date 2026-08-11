using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour {


    [SerializeField] private TextMeshProUGUI recipesDeliveredText;
    [SerializeField] private Button playAgainButton;


    private void Awake() {
        playAgainButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });
    }

    private void Start() {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;

        Hide();
    }

    private void KitchenGameManager_OnStateChanged(object sender, System.EventArgs e) {
        if (KitchenGameManager.Instance.IsGameOver()) {
            Show();

            int team0Score = DeliveryManager.Instance.GetTeamScore(0);
            int team1Score = DeliveryManager.Instance.GetTeamScore(1);

            if (team0Score > team1Score) {
                recipesDeliveredText.text = $"红队获胜！\n蓝队 {team1Score} : {team0Score} 红队";
            } else if (team1Score > team0Score) {
                recipesDeliveredText.text = $"蓝队获胜！\n蓝队 {team1Score} : {team0Score} 红队";
            } else {
                recipesDeliveredText.text = $"平局！\n蓝队 {team1Score} : {team0Score} 红队";
            }
        } else {
            Hide();
        }
    }

    private void Show() {
        gameObject.SetActive(true);
        playAgainButton.Select();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }


}