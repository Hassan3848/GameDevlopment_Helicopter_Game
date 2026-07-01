using UnityEngine;
using UnityEngine.InputSystem;

namespace HelicopterCombat.Player
{
    /// <summary>
    /// Reads the configured helicopter actions. This component owns input reading only;
    /// movement, rotation, camera, and weapons remain in separate components.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class HelicopterInputReader : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInput playerInput;

        [Header("Action Names")]
        [SerializeField] private string actionMapName = "Helicopter";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string altitudeActionName = "Altitude";
        [SerializeField] private string yawActionName = "Yaw";
        [SerializeField] private string lookActionName = "Look";

        public Vector2 Move { get; private set; }
        public float Altitude { get; private set; }
        public float Yaw { get; private set; }
        public Vector2 Look { get; private set; }

        private InputAction moveAction;
        private InputAction altitudeAction;
        private InputAction yawAction;
        private InputAction lookAction;
        private bool actionsResolved;
        private bool missingSetupReported;

        public void Configure(PlayerInput configuredPlayerInput)
        {
            playerInput = configuredPlayerInput;
            actionsResolved = false;
        }

        private void Reset()
        {
            playerInput = GetComponent<PlayerInput>();
        }

        private void Awake()
        {
            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }
        }

        private void Start()
        {
            ResolveActions();
        }

        private void Update()
        {
            if (!actionsResolved && !ResolveActions())
            {
                ResetValues();
                return;
            }

            Move = moveAction.ReadValue<Vector2>();
            Altitude = altitudeAction.ReadValue<float>();
            Yaw = yawAction.ReadValue<float>();
            Look = lookAction.ReadValue<Vector2>();
        }

        private bool ResolveActions()
        {
            if (playerInput == null || playerInput.actions == null)
            {
                ReportMissingSetup("PlayerInput or Input Actions asset is missing.");
                return false;
            }

            InputActionMap actionMap = playerInput.actions.FindActionMap(actionMapName, false);

            if (actionMap == null)
            {
                ReportMissingSetup($"Action Map '{actionMapName}' was not found.");
                return false;
            }

            if (playerInput.currentActionMap == null ||
                playerInput.currentActionMap.name != actionMapName)
            {
                playerInput.SwitchCurrentActionMap(actionMapName);
            }

            moveAction = actionMap.FindAction(moveActionName, false);
            altitudeAction = actionMap.FindAction(altitudeActionName, false);
            yawAction = actionMap.FindAction(yawActionName, false);
            lookAction = actionMap.FindAction(lookActionName, false);

            if (moveAction == null || altitudeAction == null ||
                yawAction == null || lookAction == null)
            {
                ReportMissingSetup("One or more helicopter actions are missing.");
                return false;
            }

            actionsResolved = true;
            missingSetupReported = false;
            return true;
        }

        private void ResetValues()
        {
            Move = Vector2.zero;
            Altitude = 0f;
            Yaw = 0f;
            Look = Vector2.zero;
        }

        private void ReportMissingSetup(string message)
        {
            if (missingSetupReported)
            {
                return;
            }

            Debug.LogError($"{nameof(HelicopterInputReader)} on '{name}': {message}", this);
            missingSetupReported = true;
        }
    }
}
