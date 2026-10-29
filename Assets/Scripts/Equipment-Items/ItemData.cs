using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public abstract class ItemData : ScriptableObject
{
    [Header("Item Information Name/ID")]
    [SerializeField] private string _itemName;
    [SerializeField] private string _itemID;


    [Header("Item Information- Description")]
    [SerializeField]
    [TextArea(minLines: 3, maxLines: 10)]
    private string _description;

    [Header("Item Information- Icons")]
    [SerializeField]private Sprite _sprite;


    [Header("Item Information- ReadOnly Fields")]
    public string itemName => _itemName;
    public string itemID => _itemID;
    public string description => _description;
    public Sprite sprite => _sprite;
}
