using UnityEngine;

public sealed class BarracksItemPopup : MonoBehaviour
{
    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

    }

    public void Hide()
    {
        gameObject.SetActive(false);

    }

}
