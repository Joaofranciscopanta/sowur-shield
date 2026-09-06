using UnityEngine;

namespace SowurShield.Interiors
{

/// <summary>
/// Chama DontDestroyOnLoad no fim do frame, e depois desaparece.
///
/// Existe por um motivo so: sair do Awake. Dentro do Awake a cena de um GameObject
/// ainda nao foi reatribuida, entao "este objeto ja esta em DontDestroyOnLoad?" nao
/// tem resposta fiavel — e uma duzia de singletons do projeto (InteractionManager,
/// SaveManager, ...) chamam DontDestroyOnLoad no SEU proprio Awake. Marcar duas vezes
/// o mesmo objeto da "Assertion failed on expression: m_GameObjects.find(...)".
///
/// No fim do frame a cena ja e a definitiva, e a pergunta passa a ter resposta certa.
///
/// Ver <see cref="PersistentRoot.MarcarComoPersistente"/>.
/// </summary>
[DisallowMultipleComponent]
public class AdiarPersistencia : MonoBehaviour
{
    private System.Collections.IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        if (gameObject.scene.buildIndex != -1)
            Object.DontDestroyOnLoad(gameObject);

        Destroy(this);
    }
}

}
