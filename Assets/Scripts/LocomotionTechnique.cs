using UnityEngine;

public class LocomotionTechnique : MonoBehaviour
{
    public OVRInput.Controller leftController;
    public OVRInput.Controller rightController;
    public float speedMultiplier = 5.0f;
    public GameObject hmd;
    public float maxHeight = 100f; // Directly controls maximum flying height
    public float minHeight = 0.5f;
    public float verticalInfluence = 0.5f;

    [SerializeField] private float leftTriggerValue;
    [SerializeField] private float rightTriggerValue;
    private Vector3 velocity;
    private float damping = 0.98f;
    private float initialHeight;

    // Game mechanism variables
    public ParkourCounter parkourCounter;
    public string stage;
    public SelectionTaskMeasure selectionTaskMeasure;

    private Vector3 prevLeftPos;
    private Vector3 prevRightPos;
    private bool isPulling;

    void Start()
    {
        prevLeftPos = OVRInput.GetLocalControllerPosition(leftController);
        prevRightPos = OVRInput.GetLocalControllerPosition(rightController);
        initialHeight = transform.position.y;
    }

    void Update()
    {
        HandlePullingMovement();
        ApplyHapticFeedback();
        HandleRespawn();
    }

    void HandlePullingMovement()
    {
        leftTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, leftController);
        rightTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, rightController);

        Vector3 leftHandPos = OVRInput.GetLocalControllerPosition(leftController);
        Vector3 rightHandPos = OVRInput.GetLocalControllerPosition(rightController);

        Vector3 leftHandVelocity = (leftHandPos - prevLeftPos) / Time.deltaTime;
        Vector3 rightHandVelocity = (rightHandPos - prevRightPos) / Time.deltaTime;

        prevLeftPos = leftHandPos;
        prevRightPos = rightHandPos;

        if (leftTriggerValue > 0.95f || rightTriggerValue > 0.95f)
        {
            isPulling = true;

            // Calculate base velocity from controllers
            if (leftTriggerValue > 0.95f && rightTriggerValue > 0.95f)
                velocity = (leftHandVelocity + rightHandVelocity) * 0.5f * speedMultiplier;
            else if (leftTriggerValue > 0.95f)
                velocity = leftHandVelocity * speedMultiplier;
            else if (rightTriggerValue > 0.95f)
                velocity = rightHandVelocity * speedMultiplier;

            // Get head direction
            Vector3 headDirection = hmd.transform.forward;
            headDirection.Normalize();

            // Calculate vertical influence
            float verticalFactor = Mathf.Clamp(headDirection.y, -0.5f, 0.5f) * verticalInfluence;

            // Create movement vector
            Vector3 horizontalMovement = new Vector3(headDirection.x, 0, headDirection.z).normalized * velocity.magnitude;
            Vector3 verticalMovement = Vector3.up * verticalFactor * velocity.magnitude;

            // Combine movements
            velocity = horizontalMovement + verticalMovement;
        }
        else
        {
            isPulling = false;
        }

        // Apply movement with height constraints
        if (isPulling)
        {
            Vector3 newPosition = transform.position + velocity * Time.deltaTime;

            // Simple world-space height limits
            newPosition.y = Mathf.Clamp(newPosition.y, minHeight, maxHeight);

            transform.position = newPosition;
        }
        else
        {
            velocity *= damping;
        }
    }

    void ApplyHapticFeedback()
    {
        float vibrationStrength = isPulling ? Mathf.Clamp(velocity.magnitude * 0.1f, 0, 1) : 0;
        OVRInput.SetControllerVibration(0.1f, vibrationStrength, leftController);
        OVRInput.SetControllerVibration(0.1f, vibrationStrength, rightController);
    }

    void HandleRespawn()
    {
        if (OVRInput.Get(OVRInput.Button.Two) || OVRInput.Get(OVRInput.Button.Four))
        {
            if (parkourCounter.parkourStart)
            {
                transform.position = parkourCounter.currentRespawnPos;
                transform.rotation = Quaternion.LookRotation(-hmd.transform.forward);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("banner"))
        {
            stage = other.gameObject.name;
            parkourCounter.isStageChange = true;
        }
        else if (other.CompareTag("objectInteractionTask"))
        {
            selectionTaskMeasure.isTaskStart = true;
            selectionTaskMeasure.scoreText.text = "";
            selectionTaskMeasure.partSumErr = 0f;
            selectionTaskMeasure.partSumTime = 0f;

            float tempValueY = other.transform.position.y > 0 ? 12 : 0;
            Vector3 tmpTarget = new Vector3(hmd.transform.position.x, tempValueY, hmd.transform.position.z);
            selectionTaskMeasure.taskUI.transform.LookAt(tmpTarget);
            selectionTaskMeasure.taskUI.transform.Rotate(new Vector3(0, 180f, 0));
            selectionTaskMeasure.taskStartPanel.SetActive(true);
        }
        else if (other.CompareTag("coin"))
        {
            parkourCounter.coinCount += 1;
            GetComponent<AudioSource>().Play();
            other.gameObject.SetActive(false);
        }
    }
}