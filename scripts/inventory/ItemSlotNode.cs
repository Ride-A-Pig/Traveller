using System;
using ColdMint.scripts.debug;
using ColdMint.scripts.pickable;
using ColdMint.scripts.utils;
using Godot;

namespace ColdMint.scripts.inventory;

/// <summary>
/// <para>A slot in the inventory</para>
/// <para>物品栏内的一个插槽</para>
/// </summary>
public partial class ItemSlotNode : MarginContainer
{
    private TextureRect? _backgroundTextureRect;
    private TextureRect? _iconTextureRect;
    private Label? _quantityLabel;
    private Control? _control;
    private bool _isSelect;
    private Texture2D? _backgroundTexture;
    private Texture2D? _backgroundTextureWhenSelect;
    private IItem? _item;

    public IItem? Item
    {
        set
        {
            //Set item
            //设置物品
            _item = value;
            UpdateAllDisplay();
        }
    }

    public override void _Ready()
    {
        _backgroundTexture = GD.Load<Texture2D>("res://sprites/ui/ItemBarEmpty.png");
        _backgroundTextureWhenSelect = GD.Load<Texture2D>("res://sprites/ui/ItemBarFocus.png");
        _backgroundTextureRect =
            GetNode<TextureRect>("BackgroundTexture");
        _iconTextureRect = GetNode<TextureRect>("BackgroundTexture/IconTextureRect");
        _quantityLabel = GetNode<Label>("Control/QuantityLabel");
        _control = GetNode<Control>("Control");
        _quantityLabel.Hide();
        UpdateBackground(_isSelect);
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (_isSelect || _iconTextureRect == null)
        {
            //Drag is not allowed if there is no icon or no pile of items.
            //如果没有图标或者没有物品堆，那么不允许拖动。
            return new Variant();
        }

        var textureRect = new TextureRect();
        textureRect.ExpandMode = _iconTextureRect.ExpandMode;
        textureRect.Size = _iconTextureRect.Size;
        textureRect.Texture = _iconTextureRect.Texture;
        SetDragPreview(textureRect);
        return Variant.CreateFrom(this);
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (_isSelect)
        {
            //Do not place items in the selected item slot, even if the item slot is empty.
            //禁止在已选中的物品槽内放置物品，即使物品槽是空的。
            LogCat.Log("item_slot_is_selected_and_not_allowed");
            return false;
        }

        //If the preplaced slot does not have an icon, the preplaced slot is not allowed.
        //如果预放置的槽位没有图标，那么不允许放置。
        if (_iconTextureRect == null)
        {
            return false;
        }

        var type = data.VariantType;
        if (type == Variant.Type.Nil)
        {
            //The preplaced data is null.
            //预放置的数据为null。
            return false;
        }

        var itemSlotNode = data.As<ItemSlotNode>();
        var item = itemSlotNode.GetItem();
        if (item == null)
        {
            //Return null when trying to get the source item.
            //尝试获取源物品时返回null。
            return false;
        }

        if (item is Packsack packsack)
        {
            if (_item == null)
            {
                //If the dragged item is a backpack and there are no items in the current slot, return whether the backpack is allowed.
                //如果拖拽的物品是背包，且当前槽位没有物品，那么返回是否允许放置背包。
                return BackpackAllowed;
            }

            if (packsack.ItemContainer == null)
            {
                LogCat.Log("item_container_is_null");
                return false;
            }

            return packsack.ItemContainer.CanAddItem(_item);
        }

        if (_item is Packsack currentPacksack)
        {
            if (currentPacksack.ItemContainer == null)
            {
                LogCat.Log("item_container_is_null");
                return false;
            }

            return currentPacksack.ItemContainer.CanAddItem(item);
        }

        return CanAddItem(item);
    }

    /// <summary>
    /// <para>Get the items in the item container</para>
    /// <para>获取物品容器内的物品</para>
    /// </summary>
    /// <returns>
    ///<para>There may be multiple quantities</para>
    ///<para>数量可能有多个</para>
    /// </returns>
    public IItem? GetItem()
    {
        return _item;
    }

    /// <summary>
    /// <para>CreateItemInstance</para>
    /// <para>创建物品槽内的物品实例</para>
    /// </summary>
    /// <param name="number">
    ///<para>Quantity (pass in a value less than 0 to create an instance using all the quantities of built-in items)</para>
    ///<para>数量（传入小于0的值，使用内置物品的所有数量创建实例）</para>
    /// </param>
    /// <returns>
    ///<para>Newly created item</para>
    ///<para>新创建的物品</para>
    /// </returns>
    public IItem? CreateItemInstance(int number)
    {
        if (number == 0)
        {
            return null;
        }

        if (_item is not Node2D node2D)
        {
            return null;
        }

        var duplicate = node2D.Duplicate();
        if (node2D is PickAbleTemplate pickAbleTemplate)
        {
            pickAbleTemplate.CopyAttributes(duplicate);
        }

        if (duplicate is not Node2D newNode2D)
        {
            return null;
        }

        newNode2D.GlobalPosition = node2D.GlobalPosition;
        if (duplicate is not IItem newItem)
        {
            return null;
        }

        if (number < 0)
        {
            newItem.Quantity = _item.Quantity;
        }
        else
        {
            newItem.Quantity = number >= _item.Quantity ? _item.Quantity : number;
        }

        return newItem;
    }

    /// <summary>
    /// <para>Clean out the items in the item slot</para>
    /// <para>清理物品槽内的物品</para>
    /// </summary>
    /// <param name="queueFree">
    ///<para>Whether to release a node</para>
    ///<para>是否释放节点</para>
    /// </param>
    /// <remarks>
    ///<para>Clean up item object references in item slots.</para>
    ///<para>清理物品槽内的物品对象引用。</para>
    /// </remarks>
    public void ClearItem(bool queueFree = true)
    {
        if (_item == null)
        {
            return;
        }

        if (queueFree && _item is Node node)
        {
            node.QueueFree();
        }

        _item = null;
        UpdateAllDisplay();
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (_iconTextureRect == null)
        {
            return;
        }

        var type = data.VariantType;
        if (type == Variant.Type.Nil)
        {
            //The passed variable is null.
            //传入的变量为null。
            return;
        }

        var itemSlotNode = data.As<ItemSlotNode>();
        var sourceItem = itemSlotNode.GetItem();
        if (sourceItem == null)
        {
            //Return null when trying to get the source item.
            //尝试获取源物品时返回null。
            return;
        }

        if (sourceItem is Packsack packsack)
        {
            //If the source item is a backpack.
            //如果源物品是背包。
            if (packsack.ItemContainer != null && _item != null)
            {
                packsack.ItemContainer.AddItem(_item);
                ClearItem(false);
                return;
            }
        }

        if (_item is Packsack customPacksack)
        {
            if (customPacksack.ItemContainer == null)
            {
                return;
            }

            customPacksack.ItemContainer.AddItem(sourceItem);
            itemSlotNode.ClearItem(false);
            return;
        }

        AddItem(sourceItem);
        itemSlotNode.ClearItem(false);
    }

    /// <summary>
    /// <para>Whether to place a backpack in the current slot</para>
    /// <para>当前槽位是否允许放置背包</para>
    /// </summary>
    public bool BackpackAllowed { get; set; }

    public bool IsSelect
    {
        get => _isSelect;
        set
        {
            UpdateBackground(value);
            _isSelect = value;
        }
    }

    private void UpdateBackground(bool isSelect)
    {
        if (_backgroundTextureRect == null)
        {
            return;
        }

        _backgroundTextureRect.Texture = isSelect ? _backgroundTextureWhenSelect : _backgroundTexture;
    }

    public TextureRect? BackgroundTextureRect => _backgroundTextureRect;

    /// <summary>
    /// <para>Whether the item in this node is empty</para>
    /// <para>此节点内的物品是否为空的</para>
    /// </summary>
    /// <returns>
    ///<para>Return true if the number of items is 0 or the item object does not exist</para>
    ///<para>当物品数量为0或物品对象不存在时，返回true</para>
    /// </returns>
    public bool IsEmpty()
    {
        if (_item == null || _item.Quantity == 0)
        {
            return true;
        }

        return false;
    }


    /// <summary>
    /// <para>Update all displays of this slot</para>
    /// <para>更新该槽位的一切显示信息</para>
    /// </summary>
    private void UpdateAllDisplay()
    {
        UpdateIconTexture();
        UpdateQuantityLabel();
        UpdateTooltipText();
    }

    /// <summary>
    /// <para>Update item tips</para>
    /// <para>更新物品的提示内容</para>
    /// </summary>
    private void UpdateTooltipText()
    {
        if (_item == null)
        {
            TooltipText = null;
            return;
        }

        if (Config.IsDebug())
        {
            var debugText = TranslationServerUtils.Translate("item_prompt_debug");
            if (debugText != null)
            {
                TooltipText = string.Format(debugText, _item.Id,
                    TranslationServerUtils.Translate(_item.Name),
                    _item.Quantity, _item.MaxQuantity, _item.GetType().Name,
                    TranslationServerUtils.Translate(_item.Description));
            }
        }
        else
        {
            TooltipText = TranslationServerUtils.Translate(_item.Name) + "\n" +
                          TranslationServerUtils.Translate(_item.Description);
        }
    }

    /// <summary>
    /// <para>Update quantity label</para>
    /// <para>更新数量标签</para>
    /// </summary>
    private void UpdateQuantityLabel()
    {
        if (_quantityLabel == null)
        {
            return;
        }

        switch (_item?.Quantity)
        {
            case null or 1:
                _quantityLabel.Hide();
                return;
            default:
                //When the quantity is not null or 1, we display the quantity.
                //当数量不为null或1时，我们显示数量
                _quantityLabel.Text = _item?.Quantity.ToString();
                _quantityLabel.Show();
                break;
        }
    }


    /// <summary>
    /// <para>Update texture of the icon rect</para>
    /// <para>更新显示的物品图标</para>
    /// </summary>
    private void UpdateIconTexture()
    {
        if (_iconTextureRect != null)
        {
            _iconTextureRect.Texture = _item?.Icon;
        }
    }

    public bool CanAddItem(IItem item)
    {
        if (!BackpackAllowed && item is Packsack)
        {
            //如果禁止放置背包，且新物品是背包
            LogCat.Log("backpack_not_allowed");
            return false;
        }

        if (_item == null)
        {
            //If there is no item in the current item slot, it is allowed to add.
            //如果当前物品槽内没物品，那么允许添加。
            LogCat.Log("item_is_null");
            return true;
        }

        if (item.Id != _item.Id)
        {
            //If the item ID you want to add is different from the current item ID, disable.
            //如果要添加的物品ID和当前的物品ID不一样，那么禁止。
            LogCat.Log("item_id_not_same");
            return false;
        }

        var newQuantity = item.Quantity + _item.Quantity;
        if (newQuantity > _item.MaxQuantity)
        {
            //The maximum number is exceeded and items cannot be added.
            //超过了最大数量，无法添加物品。
            LogCat.Log("max_quantity_exceeded");
            return false;
        }

        return true;
    }

    /// <summary>
    /// <para>Remove items from the item slot</para>
    /// <para>从物品槽内移除物品</para>
    /// </summary>
    /// <param name="number">
    ///<para>number(Less than 0, remove all items)</para>
    ///<para>物品数量(小于0，则移除全部物品)</para>
    /// </param>
    /// <returns>
    ///<para>How many items were actually removed</para>
    ///<para>实际移除了多少个物品</para>
    /// </returns>
    public int RemoveItem(int number)
    {
        if (_item == null)
        {
            return 0;
        }

        //The number of actual removals
        //实际移除的数量
        var removeNumber = number < 0 ? _item.Quantity : number;
        _item.Quantity -= removeNumber;
        if (_item.Quantity <= 0)
        {
            ClearItem();
        }

        return removeNumber;
    }

    public bool AddItem(IItem item)
    {
        if (_item == null)
        {
            Item = item;
            return true;
        }

        var newQuantity = item.Quantity + _item.Quantity;
        _item.Quantity = Math.Min(newQuantity, _item.MaxQuantity);
        if (item is Node2D node2D)
        {
            node2D.QueueFree();
        }

        UpdateQuantityLabel();
        return true;
    }
}