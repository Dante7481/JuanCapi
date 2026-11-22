using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroVideo : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public float tiempoAntesDeSaltar = 3f;
    public string escenaSiguiente = "Menu";

    private float tiempoTranscurrido;
    private bool escenaCargada;

    private void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += VideoTerminado;
            videoPlayer.Play();
        }
    }

    private void Update()
    {
        if (escenaCargada)
        {
            return;
        }

        tiempoTranscurrido += Time.unscaledDeltaTime;

        if (tiempoTranscurrido >= tiempoAntesDeSaltar && Input.anyKeyDown)
        {
            SaltarVideo();
        }
    }

    private void VideoTerminado(VideoPlayer reproductor)
    {
        CargarSiguienteEscena();
    }

    public void SaltarVideo()
    {
        if (tiempoTranscurrido < tiempoAntesDeSaltar || escenaCargada)
        {
            return;
        }

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        CargarSiguienteEscena();
    }

    private void CargarSiguienteEscena()
    {
        if (escenaCargada)
        {
            return;
        }

        escenaCargada = true;

        if (string.IsNullOrWhiteSpace(escenaSiguiente))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            return;
        }

        SceneManager.LoadScene(escenaSiguiente);
    }
}
