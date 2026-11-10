using UnityEngine;
using UnityEngine.SceneManagement;

// The end of the run.
//
// It watches the one door that means escape, freezes the player, silences the house and puts
// the panel up. The two buttons on that panel call back into here, which is why Restart and
// Quit are public.
//
// The ticking stops dead rather than fading. It has run under every second of the game, so the
// silence is the ending: the player is outside, and the shop is behind them.
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    [SerializeField] private Openable wayOut;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private PlayerModeController modes;
    [SerializeField] private AudioSource ambience;

    private void Awake()
    {
        winPanel.SetActive(false);
    }

    private void OnEnable() => wayOut.Opened += HandleEscaped;
    private void OnDisable() => wayOut.Opened -= HandleEscaped;

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleEscaped()
    {
        winPanel.SetActive(true);
        modes.SwitchTo(PlayerMode.Frozen);
        ambience.Stop();
    }
}
