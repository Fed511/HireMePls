using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal2D : MonoBehaviour
{
    public string toSceneName;
    public string toSpawnId = "Default";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerPersistence.NextSpawnId = toSpawnId;
        // se usi fader, inserisci prima il fade-out
        SceneManager.LoadScene(toSceneName);
    }
}
