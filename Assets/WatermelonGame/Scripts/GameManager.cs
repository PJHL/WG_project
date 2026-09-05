using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using static Unity.Collections.AllocatorManager;
using UnityEditor;

// 화면 8:16 Aspect 사이즈
/* 모바일 세로모드 고정 방법
Project Settings - Player - Resolution and Presentation - Default Orientation
1. Portrait - 디바이스 홈 버튼이 아래에 있는 세로모드로 고정
2. PortraitUpsideDown - 디바이스 홈 버튼이 위에 있는 세로 모드로 고정
3. LandscapeLeft - 디바이스 홈 버튼이 오른쪽에 있는 가로모드로 고정
4. LandscapeRight - 디바이스 홈 버튼이 왼쪽에 있는 가로모드로 고정
5. AutoRotation - 휴대폰 방향에 따라 화면이 변경
*/
/* 안드로이드 버전 선택
Project Settings - Player - Other Settings - Minimum API Level
Android 9.0 'Pie'(API level 28)
*/
public class GameManager : MonoBehaviour
{
    [Header("게임 오브젝트(블럭) 관리")]
    public Transform line;
    public GameObject blockPrefab;
    public Transform blockGroup;
    public List<Block> blockPool; // 오브젝트 풀링

    [Header("특수효과 관리")] // 파티클 관리
    public GameObject effectPrefab;
    public Transform effectGroup;
    public List<ParticleSystem> effectPool; // 오브젝트 풀링

    [Header("오브젝트 풀링 관리")]
    [Range(1, 30)]
    [SerializeField] int poolSize = 10;
    [SerializeField] int poolCursor = 0;
    public Block lastBlock;

    [Header("점수 관리")]
    public Text scoreText;
    public Text highScoreText;
    public int score;
    public int highScore;

    [Header("음향 관리")]
    [SerializeField] AudioSource bgmSource;
    public AudioSource[] sfxSource;
    [SerializeField] AudioClip[] sfxClip; // 사용할 효과음을 넣는 배열 변수
    public enum Sfx { LevelUp, Next, button, Over }; // 효과음 종류
    int sfxCursor = 0;

    [Header("Next 관리")]
    [SerializeField] Image nextImage; // 다음 이미지
    [SerializeField] Sprite[] blockImage = new Sprite[6];
    private int nextLevel;

    [Header("UI창 관리")]
    [SerializeField] GameObject gameConfigUI;
    [SerializeField] GameObject gameOverUI;
    [SerializeField] GameObject touchPad;
    public bool bGameOver;

    void Awake()
    {
        // 프레임 세팅
        Application.targetFrameRate = 60;

        line = GameObject.Find("Line").GetComponent<Transform>();
        //blockPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/WatermelonGame/Prefabs/Block.prefab", typeof(GameObject));
        blockGroup = GameObject.Find("Block Group").GetComponent<Transform>();
        blockPool = new List<Block>();

        //effectPrefab= (GameObject)AssetDatabase.LoadAssetAtPath("Assets/WatermelonGame/Prefabs/Effect Particle.prefab", typeof(GameObject));
        effectPool = new List<ParticleSystem>();
        for (int i = 0; i < poolSize; i++)
        {
            MakeBlock();
        }

        scoreText = GameObject.Find("Score Text").GetComponent<Text>();
        highScoreText = GameObject.Find("High Score Text").GetComponent<Text>();
        score = 0;
        highScore = PlayerPrefs.GetInt("HighScore");
        scoreText.text = score.ToString();
        highScoreText.text = highScore.ToString();

        bgmSource = GameObject.Find("BGM Source").GetComponent<AudioSource>();
        {
            sfxSource = new AudioSource[3]; // 배열의 크기 생성
            sfxSource[0] = GameObject.Find("SFX Source").GetComponent<AudioSource>();
            sfxSource[1] = GameObject.Find("SFX Source (1)").GetComponent<AudioSource>();
            sfxSource[2] = GameObject.Find("SFX Source (2)").GetComponent<AudioSource>();
        }
        //유니티 에디터에서만 AssetDatabase 사용 가능 -> 개발중 유지 보수를 위해서 사용함
        //{
        //    sfxClip = new AudioClip[6];
        //    sfxClip[0] = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/WatermelonGame/Casual Physics Puzzle BE6/Audio/LevelUp A.wav", typeof(AudioClip));
        //    sfxClip[1] = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/WatermelonGame/Casual Physics Puzzle BE6/Audio/LevelUp B.wav", typeof(AudioClip));
        //    sfxClip[2] = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/WatermelonGame/Casual Physics Puzzle BE6/Audio/LevelUp C.wav", typeof(AudioClip));
        //    sfxClip[3] = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/WatermelonGame/Casual Physics Puzzle BE6/Audio/Next.ogg", typeof(AudioClip));
        //    sfxClip[4] = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/WatermelonGame/Casual Physics Puzzle BE6/Audio/Button.wav", typeof(AudioClip));
        //    sfxClip[5] = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/WatermelonGame/Casual Physics Puzzle BE6/Audio/GameOver.mp3", typeof(AudioClip));
        //}
        nextImage = GameObject.Find("NextBlock").GetComponent<Image>();
        NextLevel(3);

        gameConfigUI = GameObject.Find("GameConfigurationUI");
        gameOverUI = GameObject.Find("GameOverUI");
        gameConfigUI.SetActive(false);
        gameOverUI.SetActive(false);

        touchPad = GameObject.Find("Touch Pad");

        bGameOver = false;
    }

    void Start()
    {
        NextBlock();
    }

    // 오브젝트 풀에 새 오브젝트를 생성하는 함수
    Block MakeBlock()
    {
        // 이펙트 생성
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name= "Effect " + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);

        // 블럭 생성
        GameObject instantBlockObj = Instantiate(blockPrefab, blockGroup);
        instantBlockObj.name= "Block " + blockPool.Count;
        Block instantBlock = instantBlockObj.GetComponent<Block>();
        instantBlock.gameManager = this;
        instantBlock.effect = instantEffect;
        blockPool.Add(instantBlock);

        return instantBlock;
    }

    // 오브젝트풀에서 오브젝트를 가져오거나 생성하는 함수
    Block GetBlock()
    {
        for (int i = 0; i < blockPool.Count; i++)
        {
            poolCursor = (poolCursor + 1) % blockPool.Count;
            if (!blockPool[poolCursor].gameObject.activeSelf)
            {
                return blockPool[poolCursor];
            }
        }
        return MakeBlock();
    }

    void NextBlock()
    {
        lastBlock = GetBlock();

        lastBlock.level = nextLevel;

        NextLevel(4);

        lastBlock.gameObject.SetActive(true);

        SfxPlay(Sfx.Next);

        StartCoroutine(WaitNext()); // == StartCoroutine("WaitNext");
    }

    void NextLevel(int maxRandomLevel)
    {
        nextLevel = Random.Range(0, maxRandomLevel);
        nextImage.sprite = blockImage[nextLevel];
    }

    IEnumerator WaitNext()
    {
        while(lastBlock != null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        NextBlock();
    }

    //Touch Pad 오브젝트에 이벤트 연결
    public void TouchDown()
    {
        if (lastBlock == null) return;

        lastBlock.Drag();
    }
    //Touch Pad 오브젝트에 이벤트 연결
    public void TouchUp()
    {
        if (lastBlock == null) return;

        lastBlock.Drop();
        lastBlock = null;
    }

    public void GameConfigUIOpenAndClose()
    {
        if (!gameConfigUI.activeSelf) // 환경설정 창 열기
        {
            touchPad.SetActive(false);
            Time.timeScale = 0;

            gameConfigUI.SetActive(true);
        }
        else // 환경설정 창 닫기
        {
            touchPad.SetActive(true);
            Time.timeScale = 1;

            gameConfigUI.SetActive(false);
        }
    }

    public void GameOver()
    {
        // 게임 오버시 즉시 터치패드를 끄고 타임 스케일을 0으로 만들어서 게임화면이 안돌아가게 만들기
        touchPad.SetActive(false);
        Time.timeScale = 0;
        bGameOver = true;

        SfxPlay(Sfx.Over);

        // 게임오버 창 띄우기
        gameOverUI.SetActive(true);
    }

    public void SfxPlay(Sfx tpye) // 효과음 플레이
    {
        switch (tpye)
        {
            case Sfx.LevelUp:
                sfxSource[sfxCursor].clip = sfxClip[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxSource[sfxCursor].clip = sfxClip[3];
                break;
            case Sfx.button:
                sfxSource[sfxCursor].clip = sfxClip[4];
                break;
            case Sfx.Over:
                sfxSource[sfxCursor].clip = sfxClip[5];
                break;
        }

        sfxSource[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxSource.Length;
    }
}