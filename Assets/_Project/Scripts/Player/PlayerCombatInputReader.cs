using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HelicopterCombat.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Helicopter";
        [SerializeField] private string fireMissileActionName = "FireMissile";
        [SerializeField] private string dropBombActionName = "DropBomb";

        public event Action FireMissilePressed;
        public event Action DropBombPressed;

        private InputAction fireMissileAction;
        private InputAction dropBombAction;
        private bool missingSetupReported;

        public void Configure(InputActionAsset configuredInputActions)
        {
            inputActions = configuredInputActions;
            ResolveActions();
        }

        private void OnEnable()
        {
            if (!ResolveActions())
            {
                return;
            }

            fireMissileAction.performed += HandleFireMissilePerformed;
            dropBombAction.performed += HandleDropBombPerformed;
            fireMissileAction.Enable();
            dropBombAction.Enable();
        }

        private void OnDisable()
        {
            if (fireMissileAction != null)
            {
                fireMissileAction.performed -= HandleFireMissilePerformed;
                fireMissileAction.Disable();
            }

            if (dropBombAction != null)
            {
                dropBombAction.performed -= HandleDropBombPerformed;
                dropBombAction.Disable();
            }
        }

        private bool ResolveActions()
        {
            if (inputActions == null)
            {
                ReportMissingSetup("Input Actions asset is missing.");
                return false;
            }

            InputActionMap actionMap = inputActions.FindActionMap(actionMapName, false);

            if (actionMap == null)
            {
                ReportMissingSetup($"Action Map '{actionMapName}' was not found.");
                return false;
            }

            fireMissileAction = actionMap.FindAction(fireMissileActionName, false);
            dropBombAction = actionMap.FindAction(dropBombActionName, false);

            if (fireMissileAction == null || dropBombAction == null)
            {
                ReportMissingSetup("FireMissile or DropBomb action is missing.");
                return false;
            }

            missingSetupReported = false;
            return true;
        }

        private void HandleFireMissilePerformed(InputAction.CallbackContext context)
        {
            FireMissilePressed?.Invoke();
        }

        private void HandleDropBombPerformed(InputAction.CallbackContext context)
        {
            DropBombPressed?.Invoke();
        }

        private void ReportMissingSetup(string message)
        {
            if (missingSetupReported)
            {
                return;
            }

            Debug.LogError($"{nameof(PlayerCombatInputReader)} on '{name}': {message}", this);
            missingSetupReported = true;
        }
    }
}
