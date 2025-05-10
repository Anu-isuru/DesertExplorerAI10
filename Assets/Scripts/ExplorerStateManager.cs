using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;




public class ExplorerStateManager : MonoBehaviour
{
    public enum ExplorerState { Idle, Walk, Search, Danger, Dead, Success }
    public ExplorerState currentState;

    public float visionRange = 2f;

    public float moveSpeed = 2f; // speed of explorer
    private Vector2 randomDirection;
    private float changeDirectionTime = 2f; // how often to pick new random direction
    private float directionTimer;

    private bool canMove = false;

    public TextMeshProUGUI gameOverText;

    public TextMeshProUGUI messageText;

    private Transform dangerSource;

    private Animator animator;

    private bool isSandstorm = false;
    private float sandstormTimer = 0f;
    [SerializeField]
    private float timeBetweenSandstorms = 20f; // Every 15 seconds possible
    private float sandstormDuration = 10f; // Sandstorm lasts for 5 seconds

    private float originalVisionRange; // To remember normal vision range

    public GameObject sandstormOverlay;

    private bool isNight = false;
    private float dayNightTimer = 0f;
    private float dayDuration = 30f; // 20 seconds for day
    private float nightDuration = 15f; // 15 seconds for night
    private Camera mainCamera; // to change background color

    public GameObject sunImage;
    public GameObject moonImage;

    public int maxHealth = 100;
    private int currentHealth;

    public TextMeshProUGUI healthText; // To show health on screen

    public GameObject sandstormParticles;

    private CanvasGroup sandstormCanvasGroup;

    public GameObject nightOverlay;
    private CanvasGroup nightCanvasGroup;

    private float damageCooldown = 1f; // 1 second between hits
    private float lastDamageTime = -1f; // time of last hit

        private void Start()
    {
        animator = GetComponent<Animator>();
        currentState = ExplorerState.Idle; // Start in Idle
        originalVisionRange = visionRange;

        if (sandstormOverlay != null)
        {
            sandstormCanvasGroup = sandstormOverlay.GetComponent<CanvasGroup>();
            sandstormOverlay.SetActive(true); // Ensure it's active
            if (sandstormCanvasGroup != null)
                sandstormCanvasGroup.alpha = 0f; // Fully transparent initially
        }

        if (sandstormParticles != null)
            sandstormParticles.SetActive(false);


        mainCamera = Camera.main;
        SetDay();

        currentHealth = maxHealth;
        UpdateHealthUI();

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }

        if (nightOverlay != null)
        {
            nightCanvasGroup = nightOverlay.GetComponent<CanvasGroup>();
            nightOverlay.SetActive(true); // make sure it's active
            if (nightCanvasGroup != null)
            {
                nightCanvasGroup.alpha = 0f; // fully transparent at start
            }
        }
    }

    private void Update()
    {
        HandleState();
        CheckVision();
        HandleSandstorm();
        HandleDayNightCycle();
        UpdateVision();

    }

    void HandleState()
    {
        switch (currentState)
        {
            case ExplorerState.Idle:
                UpdateMessage("Idle");
                break;

            case ExplorerState.Walk:
                UpdateMessage("Walking...");
                WalkAround();
                break;

            case ExplorerState.Search:
                UpdateMessage("Searching...");
                SearchForWater();
                break;

            case ExplorerState.Danger:
                UpdateMessage("Enemy spotted!!");
                RunFromDanger();
                break;

            case ExplorerState.Dead:
                Die();
                break;

            case ExplorerState.Success:
                UpdateMessage("Water Found");
                Celebrate();
                break;
        }
    }

    void WalkAround()
    {
        RandomMovement();
    }

    void SearchForWater()
    {
        RandomMovement(); // Same random movement
    }

    void Die()
    {
        Debug.Log("Explorer died.");
        canMove = false;
        if (gameOverText != null)
        {
            ShowMessage(gameOverText, "Game Over");
        }
        Invoke("RestartGame", 3f); //restart the game after 3 seconds
    }
    void Celebrate()
    {
        Debug.Log("Explorer survived!");
        canMove = false;
        if (gameOverText != null)
        {
            gameOverText.text = "You Found the Oasis!";
        }
    }

    void UpdateMessage(string msg)
    {
        if (messageText != null)
            messageText.text = msg;
    }


    private void OnDrawGizmosSelected()
    {
        // Set Gizmo color to yellow
        Gizmos.color = Color.yellow;

        // Draw a wire circle around the player to show vision range
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
    void RandomMovement()
    {
        if (!canMove) return; // ? If cannot move, immediately stop

        directionTimer += Time.deltaTime;

        if (directionTimer > changeDirectionTime)
        {
            // Pick a new random direction
            randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            directionTimer = 0f;
        }

        // Move in the current random direction
        transform.Translate(randomDirection * moveSpeed * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat("MoveX", randomDirection.x);
            animator.SetFloat("MoveY", randomDirection.y);
            animator.SetFloat("Speed", randomDirection.sqrMagnitude); // Optional if you use Speed for idle detection
        }
    }
    void CheckVision()
    {
         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Water"))
            {
                currentState = ExplorerState.Success;
            }
            else if (hit.CompareTag("Enemy"))
            {
                currentState = ExplorerState.Danger;
                dangerSource = hit.transform; // Save the enemy transform
            }
        }
    }
    void RunFromDanger()
    {
        if (dangerSource == null)
        {
            currentState = ExplorerState.Walk;
            return;
        }

        float distanceFromDanger = Vector2.Distance(transform.position, dangerSource.position);

        if (distanceFromDanger > visionRange * 2f) // Safe distance
        {
            dangerSource = null; // Forget the enemy
            currentState = ExplorerState.Walk;
            return;
        }

        // Still in danger, run away
        Vector2 directionAway = (transform.position - dangerSource.position).normalized;
        transform.Translate(directionAway * moveSpeed * 1.5f * Time.deltaTime);
        //take damage while close to the enemy
        if (Time.time - lastDamageTime >= damageCooldown)
        {
            TakeDamage(1);
            lastDamageTime = Time.time;
        }
    }

    void HandleSandstorm()
    {
        sandstormTimer += Time.deltaTime;

        if (!isSandstorm && sandstormTimer > timeBetweenSandstorms)
        {
            // Start a new sandstorm
            isSandstorm = true;
            sandstormTimer = 0f;
            visionRange = originalVisionRange * 0.5f; // Reduce vision by half
           
            if (messageText != null)
                messageText.text = "Sandstorm! Vision reduced!";

            if (sandstormOverlay != null)
                StartCoroutine(FadeCanvas(sandstormCanvasGroup, 1f, 1f));

            if (sandstormParticles != null)
                sandstormParticles.SetActive(true);
        }

        if (isSandstorm && sandstormTimer > sandstormDuration)
        {
            // End sandstorm
            isSandstorm = false;
            sandstormTimer = 0f;
            visionRange = originalVisionRange;

            if (messageText != null)
                messageText.text = "Searching...";

            if (sandstormOverlay != null)
                StartCoroutine(FadeCanvas(sandstormCanvasGroup, 0f, 1f));

            if (sandstormParticles != null)
                sandstormParticles.SetActive(false);
        }
    }
    void SetDay()
    {
        isNight = false;
        dayNightTimer = 0f;
        if (mainCamera != null)
            mainCamera.backgroundColor = new Color(0.5f, 0.8f, 1f); // Light blue sky
       
        if (sunImage != null) sunImage.SetActive(true);
        if (moonImage != null) moonImage.SetActive(false);
                
        if (messageText != null)
            messageText.text = "Daytime: Searching...";

        if (nightCanvasGroup != null)
            StartCoroutine(FadeCanvas(nightCanvasGroup, 0f, 1f)); // fade out
    }

    void SetNight()
    {
        isNight = true;
        dayNightTimer = 0f;
        if (mainCamera != null)
            mainCamera.backgroundColor = new Color(0.05f, 0.05f, 0.2f); // Dark blue night sky
        
        if (sunImage!= null) sunImage.SetActive(false);
        if (moonImage != null) moonImage.SetActive (true);

        if (messageText != null)
            messageText.text = "Nighttime: Harder to see!";

        if (nightCanvasGroup != null)
            StartCoroutine(FadeCanvas(nightCanvasGroup, 1f, 1f)); // fade in
    }

    void HandleDayNightCycle()
    {
        dayNightTimer += Time.deltaTime;

        if (!isNight && dayNightTimer > dayDuration)
        {
            SetNight();
        }
        else if (isNight && dayNightTimer > nightDuration)
        {
            SetDay();
        }
    }
        void UpdateVision()
    {
        visionRange = originalVisionRange * (isSandstorm ? 0.5f : 1f) * (isNight ? 0.6f : 1f);
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + currentHealth;
        }
    }

    void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0)
            return; // Already dead, no more damage

        currentHealth -= damageAmount;
        if (currentHealth < 0)
            currentHealth = 0;

        UpdateHealthUI();

        if (currentHealth == 0)
        {
            currentState = ExplorerState.Dead; // Move to Dead state
            if (animator != null)
                animator.SetTrigger("Die"); // Play dying animation!

            Die();
        }
    }
    void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.
            GetActiveScene().buildIndex);
    }
    void ShowMessage(TextMeshProUGUI textElement, string message)
    {
        if (textElement != null)
        {
            textElement.text = message;
            if (!textElement.gameObject.activeSelf)
                textElement.gameObject.SetActive(true);
        }
    }

    IEnumerator FadeCanvas(CanvasGroup canvas, float targetAlpha, float duration)
    {
        float startAlpha = canvas.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        canvas.alpha = targetAlpha;
    }


}
