using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMultiplayerUI : MonoBehaviour {



    private void Start() {
        KitchenGameManager.Instance.OnMultiplayerGamePaused += KitchenGameManager_OnGamePauseStateChanged;
        KitchenGameManager.Instance.OnMultiplayerGameUnpaused += KitchenGameManager_OnGamePauseStateChanged;
        KitchenGameManager.Instance.OnLocalGamePaused += KitchenGameManager_OnGamePauseStateChanged;
        KitchenGameManager.Instance.OnLocalGameUnpaused += KitchenGameManager_OnGamePauseStateChanged;

        Hide();
    }

    private void KitchenGameManager_OnGamePauseStateChanged(object sender, System.EventArgs e) {
        // 只在"游戏被其他玩家暂停、而我没有打开暂停菜单"时显示等待提示，
        // 避免与 GamePauseUI 的"暂停中"标题叠印在同一位置
        if (KitchenGameManager.Instance.IsGamePaused() && !KitchenGameManager.Instance.IsLocalGamePaused()) {
            Show();
        } else {
            Hide();
        }
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
}
