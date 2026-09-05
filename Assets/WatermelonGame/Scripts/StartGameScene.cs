using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class StartGameScene : MonoBehaviour
{
    public void StartGame() // 게임 시작, 재시작
    {
        Time.timeScale = 1.0f;

        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        Application.Quit(); // 어플리케이션 종료
    }
}
