using TMPro;
using UnityEngine;

public class TeamScoreUI : MonoBehaviour {


    [SerializeField] private TextMeshProUGUI scoreText;


    private void Start() {
        DeliveryManager.Instance.OnTeamScoreChanged += DeliveryManager_OnTeamScoreChanged;

        UpdateVisual();
    }

    private void DeliveryManager_OnTeamScoreChanged(object sender, System.EventArgs e) {
        UpdateVisual();
    }

    private void UpdateVisual() {
        int team0Score = DeliveryManager.Instance.GetTeamScore(0);
        int team1Score = DeliveryManager.Instance.GetTeamScore(1);

        scoreText.text = $"蓝队 {team1Score} : {team0Score} 红队";
    }


}
