using UnityEngine;
using TMPro; 



public class ExplorerStateManager : MonoBehaviour
{
    public enum ExplorerState { Idle, Walk, Search, Danger, Dead, Success }
    public ExplorerState currentState;

    public float visionRange = 2f;

    public float moveSpeed = 2f; // speed of explorer
    private Vector2 randomDirection;
    private float changeDirectionTime = 2f; // how often to pick new random direction
    private float directionTimer;

    private bool canMove = true;

    public TextMeshProUGUI gameOverText;

    public TextMeshProUGUI messageText;

    private Transform dangerSource;

    private Animator animator;

    private bool isSandstorm = false;
    private float sandstormTimer = 0f;
    [SerializeField]
    private float timeBetweenSandstorms = 15f; // Every 15 seconds possible
    private float sandstormDuration = 5f; // Sandstorm lasts for 5 seconds

    private float originalVisionRange; // To remember normal vision range

    public GameObject sandstormOverlay;

    private bool isNight = false;
    private float dayNightTimer = 0f;
    private float dayDuration = 20f; // 20 seconds for day
    private float nightDuration = 15f; // 15 seconds for night
    private Camera mainCamera; // to change background color

    public GameObject sunImage;
    public GameObject moonImage;


    private void Start()
    {
        animator = GetComponent<Animator>();
        currentState = ExplorerState.Idle; // Start in Idle
        originalVisionRange = visionRange;

        if (sandstormOverlay != null)
            sandstormOverlay.SetActive(false);

        mainCamera = Camera.main;
        SetDay();
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
                // Just stay idle
                break;

            case ExplorerState.Walk:
                // Move around randomly
                WalkAround();
                break;

            case ExplorerState.Search:
                // Searching behavior
                SearchForWater();
                break;

            case ExplorerState.Danger:
                // Run or hide
                RunFromDanger();
                break;

            case ExplorerState.Dead:
                // Stop everything
                Die();
                break;

            case ExplorerState.Success:
                // Celebrate or finish
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
            gameOverText.text = "Game Over!";
            gameOverText.gameObject.SetActive(true);
        }
    }
    void Celebrate()
    {
        Debug.Log("Explorer survived!");
        canMove = false;
        if (gameOverText != null)
        {
            gameOverText.text = "You Found the Oasis!";
            gameOverText.gameObject.SetActive(true);
        }
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
        // Only reset if no sandstorm and it's daytime
        if (!isSandstorm && !isNight)
        {
            if (messageText != null)
                messageText.text = "Searching...";
        }
        else if (isNight && !isSandstorm)
        {
            if (messageText != null)
                messageText.text = "Nighttime: Harder to see!";
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Water"))
            {
                if (messageText != null)
                    messageText.text = "Water found!";
                currentState = ExplorerState.Success;
            }
            else if (hit.CompareTag("Enemy"))
            {
                if (messageText != null)
                    messageText.text = "Enemy spotted!";
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
                sandstormOverlay.SetActive(true); // Show storm effect
            Debug.Log("Sandstorm started, overlay on");
        }

        if (isSandstorm && sandstormTimer > sandstormDuration)
        {
            // End sandstorm
            isSandstorm = false;
            sandstormTimer = 0f;
            if (messageText != null)
                messageText.text = "Searching...";

            if (sandstormOverlay != null)
                sandstormOverlay.SetActive(false); // Hide storm effect
            Debug.Log("Sandsorm Ended, Overlay is off");
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



}
