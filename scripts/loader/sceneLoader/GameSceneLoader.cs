using System.Threading.Tasks;
using ColdMint.scripts.inventory;
using ColdMint.scripts.map;
using ColdMint.scripts.map.LayoutParsingStrategy;
using ColdMint.scripts.map.layoutStrategy;
using ColdMint.scripts.map.RoomPlacer;
using ColdMint.scripts.utils;
using Godot;

namespace ColdMint.scripts.loader.sceneLoader;

/// <summary>
/// <para>Game scene loader</para>
/// <para>游戏场景加载器</para>
/// </summary>
public partial class GameSceneLoader : SceneLoaderTemplate
{
    private Label? _seedLabel;

    public override Task InitializeData()
    {
        RenderingServer.SetDefaultClearColor(Color.FromHsv(0, 0, 0));
        //Loading the blood bar scene
        //加载血条场景
        var healthBarUi = GetNode<HealthBarUi>("CanvasLayer/Control/VBoxContainer/HealthBarUi");
        GameSceneDepend.HealthBarUi = healthBarUi;
        //Load HotBar
        //加载HotBar
        var hotBar = GetNode<HotBar>("CanvasLayer/Control/VBoxContainer/HotBar");
        GameSceneDepend.HotBar = hotBar;
        //Backpack Ui container
        //背包Ui容器
        var backpackUiContainer = GetNode<Control>("CanvasLayer/BackpackUIContainer");
        GameSceneDepend.BackpackUiContainer = backpackUiContainer;
        //Load operation prompt
        //加载操作提示
        var operationTip = GetNode<RichTextLabel>("CanvasLayer/Control/VBoxContainer/OperationTip");
        GameSceneDepend.OperationTipLabel = operationTip;
        //Loaded weapon container
        //加载武器容器
        var weaponContainer = GetNode<Node2D>("WeaponContainer");
        GameSceneDepend.WeaponContainer = weaponContainer;
        //Load projectile container
        //加载抛射体容器
        var projectileContainer = GetNode<Node2D>("ProjectileContainer");
        GameSceneDepend.ProjectileContainer = projectileContainer;
        //Load Packsack container
        //加载背包容器
        var packsackContainer = GetNode<Node2D>("PacksackContainer");
        GameSceneDepend.PacksackContainer = packsackContainer;
        //Load AICharacter container
        //加载AICharacter容器
        var aiCharacterContainer = GetNode<Node2D>("AICharacterContainer");
        GameSceneDepend.AiCharacterContainer = aiCharacterContainer;
        //Load player container
        //加载玩家容器
        var playerContainer = GetNode<Node2D>("PlayerContainer");
        GameSceneDepend.PlayerContainer = playerContainer;
        //Load the room container node
        //加载房间容器节点
        var miniMapContainerNode = GetNode<Node2D>("CanvasLayer/Control/MapContainer/MiniMap/RoomPreviewContainer");
        GameSceneDepend.MiniMapContainerNode = miniMapContainerNode;
        //计算迷你地图的中点
        var mapContainer = GetNode<NinePatchRect>("CanvasLayer/Control/MapContainer/MiniMap");
        GameSceneDepend.MiniMapMidpointCoordinate = mapContainer.Size / 2;
        return Task.CompletedTask;
    }

    public override async Task LoadScene()
    {
        var debugMode = Config.IsDebug();
        var recreateMapButton = GetNodeOrNull<Button>("CanvasLayer/Control/RecreateMapButton");
        if (recreateMapButton != null)
        {
            recreateMapButton.Visible = debugMode;
            recreateMapButton.Pressed += () => { _ = GenerateMap(); };
        }

        _seedLabel = GetNodeOrNull<Label>("CanvasLayer/Control/SeedLabel");
        if (_seedLabel != null)
        {
            _seedLabel.Visible = debugMode;
        }

        MapGenerator.MapRoot = GetNode<Node>("MapRoot");
        MapGenerator.LayoutStrategy = new TestLayoutStrategy();
        MapGenerator.LayoutParsingStrategy = new SequenceLayoutParsingStrategy();
        MapGenerator.RoomPlacementStrategy = new PatchworkRoomPlacementStrategy();
        await GenerateMap();
    }

    /// <summary>
    /// <para>Generate map</para>
    /// <para>生成地图</para>
    /// </summary>
    private async Task GenerateMap()
    {
        MapGenerator.Seed = GuidUtils.GetGuid();
        if (_seedLabel != null)
        {
            //If you have a seedLabel, then set the seed to it.
            //如果有seedLabel，那么将种子设置上去。
            var seedInfo = TranslationServerUtils.TranslateWithFormat("ui_seed_info", MapGenerator.Seed);
            _seedLabel.Text = seedInfo ?? $"Seed: {MapGenerator.Seed}";
        }

        await MapGenerator.GenerateMap();
    }
}