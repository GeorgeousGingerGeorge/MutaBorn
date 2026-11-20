using UnityEngine;

public sealed class TutorialMovementHintSpawner : MonoBehaviour
{
    [SerializeField] private Sprite greenTilesHint;
    [SerializeField] private Sprite brownTilesHint;
    [SerializeField] private Sprite pathPreviewHint;
    [SerializeField] private Vector3 firstHintLocalPosition = new Vector3(6f, 2.6f, 12f);
    [SerializeField] private Vector3 secondHintLocalPosition = new Vector3(8.6f, 3.35f, 12.2f);
    [SerializeField] private Vector3 thirdHintLocalPosition = new Vector3(11.2f, 4.1f, 12.4f);
    [SerializeField] private Vector3 hintScale = new Vector3(3f, 1.55f, 1f);
    [SerializeField] private int sortingOrder = 75;

    private const string RootName = "Tutorial Movement Hints";

    private void Start()
    {
        BuildHints();

    }

    private void BuildHints()
    {
        Transform existing = transform.Find(RootName);
        if (existing != null)
        {
            Destroy(existing.gameObject);

        }

        GameObject root = new GameObject(RootName);
        root.transform.SetParent(transform, false);

        CreateHint(root.transform, "Hint - Green Tiles", greenTilesHint, firstHintLocalPosition);
        CreateHint(root.transform, "Hint - Brown Tiles", brownTilesHint, secondHintLocalPosition);
        CreateHint(root.transform, "Hint - Path Preview", pathPreviewHint, thirdHintLocalPosition);

    }

    private void CreateHint(Transform parent, string hintName, Sprite sprite, Vector3 localPosition)
    {
        if (sprite == null)
        {
            return;

        }

        GameObject hint = new GameObject(hintName);
        hint.transform.SetParent(parent, false);
        hint.transform.localPosition = localPosition;
        hint.transform.localRotation = Quaternion.identity;
        hint.transform.localScale = hintScale;

        SpriteRenderer renderer = hint.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        hint.AddComponent<FaceCameraBillboard>();

    }

}
