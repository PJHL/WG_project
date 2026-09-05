using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class Block : MonoBehaviour
{
    public GameManager gameManager;
    public ParticleSystem effect;

    // public 빼기
    public int level; // n단계
    int maxLevel;
    public bool isDrag; // 떨어지는 중인지
    public bool isMerge; // 합쳐지는 중인지

    [SerializeField] bool bInBasket = false; // 게임오버 선을 한번 지나갔을 경우 다시 접촉시 게임오버 실행
    [SerializeField] float timer = 0; // 게임오버 선에서 못 벗어날 경우 게임오버 실행

    Rigidbody2D rigid;
    CircleCollider2D blockColl;
    Animator anim;

    void Awake()
    {
        maxLevel = 7;

        rigid = GetComponent<Rigidbody2D>();
        blockColl = GetComponent<CircleCollider2D>();
        anim = GetComponent<Animator>();
    }

    void OnEnable() // 스크립트가 활성화 될 때 실행되는 함수
    {
        anim.SetInteger("Level", level);

        blockColl.enabled = true;
    }

    void OnDisable() // 스크립트 비활성화 시 실행되는 함수
    {
        // 블럭 속성 초기화
        level = 0;
        isDrag = false;
        isMerge = false;
        bInBasket = false;
        timer = 0;

        // 블럭 위치 초기화
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.zero;

        // 블럭 물리 초기화
        rigid.simulated = false;
        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (isDrag) // 드래그 중에 오브젝트 이동
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float leftBorder = -4f + transform.localScale.x / 2f;
            float rightBorder = 4f - transform.localScale.x / 2f;
            if (mousePos.x < leftBorder)
            {
                mousePos.x = leftBorder;
            }
            if (mousePos.x > rightBorder)
            {
                mousePos.x = rightBorder;
            }

            mousePos.y = 6f;
            mousePos.z = 0;
            transform.position = Vector3.Lerp(transform.position, mousePos, 0.2f);
        }
    }

    public void Drag()
    {
        isDrag = true;
    }
    public void Drop()
    {
        isDrag = false;
        rigid.simulated = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 합치기 로직
        if (collision.gameObject.tag == "Block")
        {
            Block other = collision.gameObject.GetComponent<Block>();

            // 두개의 단계가 같고 오브젝트가 합쳐지는 중이 아니고 상대 오브젝트가 합쳐지는 중이 아닐 때
            if (level == other.level && !isMerge && !other.isMerge)
            {
                // 나와 상대 오브젝트 위치 가져오기
                float myX = this.transform.position.x;
                float myY = this.transform.position.y;
                float otherX = other.transform.position.x;
                float otherY = other.transform.position.y;

                // 점수 부여
                gameManager.score += (int)Mathf.Pow(2, level); // Mathf.Pow : 지정 숫자의 거듭제곱 // 1, 2, 4, 8, 16, 32
                gameManager.scoreText.text = gameManager.score.ToString();

                if (gameManager.score > gameManager.highScore) // 최고 점수 갱신
                {
                    gameManager.highScore = gameManager.score;
                    gameManager.highScoreText.text = gameManager.highScore.ToString();

                    // 데이터 저장
                    PlayerPrefs.SetInt("HighScore", gameManager.highScore);
                }
                if (level < maxLevel) // 최고 레벨 이전
                {
                    // 1. 내가 아래에 있을 때
                    // 2. 동일한 높이이고 내가 오른쪽에 있을 때
                    if (myY < otherY || (myY == otherY && myX > otherX))
                    {
                        // 상대방 숨기기
                        other.Hide();
                        // 나는 레벨업
                        LevelUp();
                    }
                }
            }
        }
    }

    public void Hide()
    {
        isMerge = true;

        rigid.simulated = false;
        blockColl.enabled = false;

        isMerge = false;

        gameObject.SetActive(false);
    }

    void LevelUp() // 레벨업 함수
    {
        isMerge = true;

        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;

        anim.SetInteger("Level", ++level);
        EffectPlay();
        gameManager.SfxPlay(GameManager.Sfx.LevelUp);

        isMerge = false;
    }

    void EffectPlay()
    {
        effect.transform.position = this.transform.position;
        effect.transform.localScale = transform.localScale;
        effect.Play();
    }

    // 라인에 접촉했을 때 실행
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 만약, 라인을 한번 지나갔었다면
        if (bInBasket)
        {
            if (!gameManager.bGameOver)
            {
                gameManager.GameOver();
            }
            return;
        }

        // 첫 접촉이면 라인을 지나갔다는 표시 활성화
        bInBasket = true;
    }

    //라인을 지나가지 못하고 걸쳐있을 때
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (timer > 1f) // 1초 이상 선에 걸쳐있을 때
        {
            if (!gameManager.bGameOver)
            {
                gameManager.GameOver();
            }
            return;
        }
        timer += Time.deltaTime;
    }
}