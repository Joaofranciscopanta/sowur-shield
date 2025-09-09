using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private float typeSpeed = 50f; // caracteres por segundo
    [SerializeField] private float endDelay = 1f;   // pausa após o texto terminar
    [SerializeField] private bool showBlinkingCursor = true;

    [Header("Som de digitação")]
    [SerializeField] private AudioSource typeSound;

    private bool skipRequested = false;
    private Coroutine typingCoroutine;

    // Inicia o efeito
    public Coroutine Run(string textToType, TMP_Text textLabel)
    {
        // Reseta o estado de skip
        skipRequested = false;
        return StartCoroutine(routine: TypeText(textToType, textLabel));
    }

    // Solicita que o texto pule para o final
    public void Skip()
    {
        skipRequested = true;
    }

    private IEnumerator TypeText(string textToType, TMP_Text textLabel)
    {
        textLabel.text = string.Empty;
        
        float t = 0;
        int charIndex = 0;
        textLabel.text = "";

        while (charIndex < textToType.Length && !skipRequested)
        {
            t += Time.deltaTime * typeSpeed;
            int nextCharIndex = Mathf.Clamp(Mathf.FloorToInt(t), 0, textToType.Length);

            if (nextCharIndex != charIndex)
            {
                charIndex = nextCharIndex;
                textLabel.text = textToType.Substring(0, charIndex);

                if (typeSound != null && !skipRequested && charIndex > 0)
                    typeSound.Play();
            }

            yield return null;
        }

        textLabel.text = textToType;

        if (showBlinkingCursor)
        {
            yield return StartCoroutine(BlinkingCursor(textLabel, textToType));
        }
        else
        {
            yield return new WaitForSeconds(endDelay);
        }
    }

    private IEnumerator BlinkingCursor(TMP_Text textLabel, string baseText)
    {
        float timer = 0f;
        bool visible = true;

        while (!skipRequested)
        {
            timer += Time.deltaTime;
            if (timer >= 0.5f)
            {
                visible = !visible;
                textLabel.text = visible ? baseText + "_" : baseText;
                timer = 0f;
            }

            yield return null;
        }

        textLabel.text = baseText;
    }
}
