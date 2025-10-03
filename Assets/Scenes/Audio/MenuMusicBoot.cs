using UnityEngine;

public class MenuMusicBoot : MonoBehaviour
{
    void Awake()
    {
        AudioManagerMusic.I?.Play("menu", 0.4f); // crossfade-in dolce
    }
}
