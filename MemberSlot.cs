using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MemberSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RoleHandler roleHandler;
    public Image slotIcon;
    private Transform trainerTransform => PokemonSwitchManager.Instance.GetTrainerLeader().transform;

    public float releaseRadius = 6.5f;

    private Vector3 originalPosition;
    private bool isInteractable = true;

    public CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetInteractable(bool active)
    {
        isInteractable = active;
        slotIcon.color = active ? Color.white : Color.gray;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isInteractable || roleHandler == null) return;

        if (canvasGroup != null) canvasGroup.alpha = 0.7f;

        if (MemberManager.Instance.IsPokemonOnField(roleHandler))
        {
            Debug.Log("Pokémon já em campo. Retorne-o para lançá-lo novamente.");
            return;
        }

        originalPosition = transform.position;
        MemberManager.Instance.summonRadius = releaseRadius;
        MemberManager.Instance.ShowSummonRadius(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isInteractable || roleHandler == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(eventData.position);
        mouseWorld.z = 0f;
        transform.position = mouseWorld;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isInteractable || roleHandler == null)
        {
            transform.position = originalPosition;
            return;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(eventData.position);
        mouseWorld.z = 0f;

        bool isInsideRadius = Vector3.Distance(trainerTransform.position, mouseWorld) <= releaseRadius;

        if (!isInsideRadius)
        {
            Debug.Log("Fora do raio de lançamento");
            transform.position = originalPosition;
            MemberManager.Instance.ShowSummonRadius(false);
            return;
        }

        if (MemberManager.Instance.IsPokemonOnField(roleHandler))
        {
            Debug.Log("Pokémon já está em campo.");
            transform.position = originalPosition;
            MemberManager.Instance.ShowSummonRadius(false);
            return;
        }

        // Agora: chama o PokeballLauncher para fazer a animação completa
        PokeballLauncher launcher = FindFirstObjectByType<PokeballLauncher>();
        if (launcher == null)
        {
            Debug.LogWarning("PokeballLauncher não encontrado na cena. Usando fallback (PSM direto).");
            PokemonSwitchManager.Instance.ReleasePokemonAtPosition(roleHandler, mouseWorld);
            MemberManager.Instance.SetPokemonOnField(roleHandler, true);
        }
        else
        {
            bool ok = launcher.RequestReleasePokemon(roleHandler, mouseWorld);
            if (!ok)
            {
                Debug.Log("Não foi possível lançar via PokeballLauncher.");
            }
        }

        transform.position = originalPosition;
        MemberManager.Instance.ShowSummonRadius(false);
    }

    public void SetRoleHandler(RoleHandler rh)
    {
        roleHandler = rh;
    }
}