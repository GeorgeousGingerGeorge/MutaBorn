using UnityEngine;

// Standalone test-scene only: activate an authored roster before the grid deploys crew.
[DefaultExecutionOrder(-1000)]
public sealed class StandaloneSandboxRosterStartup : MonoBehaviour
{
    [SerializeField] private UnitRoster testRoster;

    private void Awake()
    {
        if (UnitRoster.Instance == null && testRoster != null)
        {
            testRoster.gameObject.SetActive(true);

        }

    }
}
