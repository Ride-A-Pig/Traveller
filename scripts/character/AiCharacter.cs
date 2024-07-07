using System;
using System.Collections.Generic;
using ColdMint.scripts.bubble;
using ColdMint.scripts.camp;
using ColdMint.scripts.stateMachine;
using ColdMint.scripts.utils;
using Godot;

namespace ColdMint.scripts.character;

/// <summary>
/// <para>The role played by computers</para>
/// <para>由电脑扮演的角色</para>
/// </summary>
public sealed partial class AiCharacter : CharacterTemplate
{
    //Used to detect rays on walls
    //用于检测墙壁的射线
    private RayCast2D? _wallDetection;

    public RayCast2D? WallDetection => _wallDetection;
    private Vector2 _wallDetectionOrigin;
    private Area2D? _attackArea;

    /// <summary>
    /// <para>Reconnaissance area</para>
    /// <para>侦察区域</para>
    /// </summary>
    /// <remarks>
    ///<para>Most of the time, when the enemy enters the reconnaissance area, the character will issue a "question mark" and try to move slowly towards the event point.</para>
    ///<para>大多数情况下，当敌人进入侦察区域后，角色会发出“疑问（问号）”，并尝试向事件点缓慢移动。</para>
    /// </remarks>
    private Area2D? _scoutArea;

    /// <summary>
    /// <para>All enemies within striking distance</para>
    /// <para>在攻击范围内的所有敌人</para>
    /// </summary>
    private List<CharacterTemplate>? _enemyInTheAttackRange;

    /// <summary>
    /// <para>Scout all enemies within range</para>
    /// <para>在侦察范围内所有的敌人</para>
    /// </summary>
    private List<CharacterTemplate>? _enemyInTheScoutRange;


    /// <summary>
    /// <para>Obstacle detection ray during attack</para>
    /// <para>攻击时的障碍物检测射线</para>
    /// </summary>
    /// <remarks>
    ///<para></para>
    ///<para>检测与目标点直接是否间隔墙壁</para>
    /// </remarks>
    private RayCast2D? _attackObstacleDetection;


    private VisibleOnScreenEnabler2D? _screenEnabler2D;

    /// <summary>
    /// <para>Navigation agent</para>
    /// <para>导航代理</para>
    /// </summary>
    public NavigationAgent2D? NavigationAgent2D { get; set; }


    public IStateMachine? StateMachine { get; set; }


    public RayCast2D? AttackObstacleDetection => _attackObstacleDetection;


    /// <summary>
    /// <para>Exclamation bubble Id</para>
    /// <para>感叹气泡Id</para>
    /// </summary>
    private const int plaintBubbleId = 0;

    /// <summary>
    /// <para>Query bubble Id</para>
    /// <para>疑问气泡Id</para>
    /// </summary>
    private const int queryBubbleId = 1;

    /// <summary>
    /// <para>BubbleMarker</para>
    /// <para>气泡标记</para>
    /// </summary>
    /// <remarks>
    ///<para>Subsequent production of dialogue bubbles can be put into the parent class for players to use.</para>
    ///<para>后续制作对话泡时可进其放到父类，供玩家使用。</para>
    /// </remarks>
    private BubbleMarker? _bubbleMarker;

    public override void _Ready()
    {
        base._Ready();

        _enemyInTheAttackRange = new List<CharacterTemplate>();
        _enemyInTheScoutRange = new List<CharacterTemplate>();
        _screenEnabler2D = GetNode<VisibleOnScreenEnabler2D>("VisibleOnScreenEnabler2D");
        _screenEnabler2D.ScreenEntered += () =>
        {
            //When the character enters the screen.
            //当角色进入屏幕。
            ProcessMode = ProcessModeEnum.Disabled;
        };
        _screenEnabler2D.ScreenExited += () =>
        {
            //When the character leaves the screen.
            //当角色离开屏幕。
            ProcessMode = ProcessModeEnum.Inherit;
        };
        _bubbleMarker = GetNode<BubbleMarker>("BubbleMarker");
        if (_bubbleMarker != null)
        {
            using var plaintScene = GD.Load<PackedScene>("res://prefab/ui/plaint.tscn");
            var plaint = NodeUtils.InstantiatePackedScene<Node2D>(plaintScene);
            if (plaint != null)
            {
                _bubbleMarker.AddBubble(plaintBubbleId, plaint);
            }

            using var queryScene = GD.Load<PackedScene>("res://prefab/ui/query.tscn");
            var query = NodeUtils.InstantiatePackedScene<Node2D>(queryScene);
            if (query != null)
            {
                _bubbleMarker.AddBubble(queryBubbleId, query);
            }
        }

        _wallDetection = GetNode<RayCast2D>("WallDetection");
        _attackArea = GetNode<Area2D>("AttackArea2D");
        _scoutArea = GetNode<Area2D>("ScoutArea2D");
        NavigationAgent2D = GetNode<NavigationAgent2D>("NavigationAgent2D");
        if (ItemMarker2D != null)
        {
            _attackObstacleDetection = ItemMarker2D.GetNode<RayCast2D>("AttackObstacleDetection");
        }

        if (_attackArea != null)
        {
            //If true, the zone will detect objects or areas entering and leaving the zone.
            //如果为true，该区域将检测进出该区域的物体或区域。
            _attackArea.Monitoring = true;
            //Other areas can't detect our attack zone
            //其他区域不能检测到我们的攻击区域
            _attackArea.Monitorable = false;
            _attackArea.BodyEntered += EnterTheAttackArea;
            _attackArea.BodyExited += ExitTheAttackArea;
        }

        if (_scoutArea != null)
        {
            _scoutArea.Monitoring = true;
            _scoutArea.Monitorable = false;
            _scoutArea.BodyEntered += EnterTheScoutArea;
            _scoutArea.BodyExited += ExitTheScoutArea;
        }

        _wallDetectionOrigin = _wallDetection.TargetPosition;
        StateMachine = new PatrolStateMachine();
        StateMachine.Context = new StateContext
        {
            CurrentState = State.Patrol,
            Owner = this
        };
        if (StateMachine != null)
        {
            StateMachine.Start();
        }
    }

    /// <summary>
    /// <para>Display exclamation marks</para>
    /// <para>显示感叹号</para>
    /// </summary>
    public void DispladyPlaint()
    {
        _bubbleMarker?.ShowBubble(plaintBubbleId);
    }

    public void HidePlaint()
    {
        _bubbleMarker?.HideBubble(plaintBubbleId);
    }

    /// <summary>
    /// <para>Displady Query</para>
    /// <para>显示疑问</para>
    /// </summary>
    public void DispladyQuery()
    {
        _bubbleMarker?.ShowBubble(queryBubbleId);
    }
    
    public void HiddenQuery()
    {
        _bubbleMarker?.HideBubble(queryBubbleId);
    }

    /// <summary>
    /// <para>Whether the enemy has been detected in the reconnaissance area</para>
    /// <para>侦察范围是否发现敌人</para>
    /// </summary>
    /// <returns>
    ///<para>Have you spotted the enemy?</para>
    ///<para>是否发现敌人</para>
    /// </returns>
    public bool ScoutEnemyDetected()
    {
        if (_enemyInTheScoutRange == null)
        {
            return false;
        }

        return _enemyInTheScoutRange.Count > 0;
    }

    /// <summary>
    /// <para>Get the first enemy in range</para>
    /// <para>获取第一个进入侦察范围的敌人</para>
    /// </summary>
    /// <returns></returns>
    public CharacterTemplate? GetFirstEnemyInScoutArea()
    {
        if (_enemyInTheScoutRange == null || _enemyInTheScoutRange.Count == 0)
        {
            return null;
        }

        return _enemyInTheScoutRange[0];
    }

    /// <summary>
    /// <para>Get the first enemy within striking range</para>
    /// <para>获取第一个进入攻击范围的敌人</para>
    /// </summary>
    /// <returns></returns>
    public CharacterTemplate? GetFirstEnemyInAttackArea()
    {
        if (_enemyInTheAttackRange == null || _enemyInTheAttackRange.Count == 0)
        {
            return null;
        }

        return _enemyInTheAttackRange[0];
    }

    protected override void HookPhysicsProcess(ref Vector2 velocity, double delta)
    {
        StateMachine?.Execute();
        if (NavigationAgent2D != null && IsOnFloor())
        {
            var nextPathPosition = NavigationAgent2D.GetNextPathPosition();
            var direction = (nextPathPosition - GlobalPosition).Normalized();
            velocity = direction * Config.CellSize * Speed * ProtectedSpeedScale;
        }
    }

    /// <summary>
    /// <para>When the node enters the reconnaissance area</para>
    /// <para>当节点进入侦察区域后</para>
    /// </summary>
    /// <param name="node"></param>
    private void EnterTheScoutArea(Node node)
    {
        CanCauseHarmNode(node, (canCause, characterTemplate) =>
        {
            if (canCause && characterTemplate != null)
            {
                _enemyInTheScoutRange?.Add(characterTemplate);
            }
        });
    }

    /// <summary>
    /// <para>When the node exits the reconnaissance area</para>
    /// <para>当节点退出侦察区域后</para>
    /// </summary>
    /// <param name="node"></param>
    private void ExitTheScoutArea(Node node)
    {
        if (node == this)
        {
            return;
        }

        if (node is CharacterTemplate characterTemplate)
        {
            _enemyInTheScoutRange?.Remove(characterTemplate);
        }
    }

    /// <summary>
    /// <para>When a node enters the attack zone</para>
    /// <para>当节点进入攻击区域后</para>
    /// </summary>
    /// <param name="node"></param>
    private void EnterTheAttackArea(Node node)
    {
        CanCauseHarmNode(node, (canCause, characterTemplate) =>
        {
            if (canCause && characterTemplate != null)
            {
                _enemyInTheAttackRange?.Add(characterTemplate);
            }
        });
    }

    /// <summary>
    /// <para>CanCauseHarmNode</para>
    /// <para>是否可伤害某个节点</para>
    /// </summary>
    /// <param name="node"></param>
    /// <param name="action"></param>
    private void CanCauseHarmNode(Node node, Action<bool, CharacterTemplate?> action)
    {
        if (node == this)
        {
            //The target can't be yourself.
            //攻击目标不能是自己。
            action.Invoke(false, null);
            return;
        }

        if (node is not CharacterTemplate characterTemplate)
        {
            action.Invoke(false, null);
            return;
        }

        //Determine if damage can be done between factions
        //判断阵营间是否可造成伤害
        var camp = CampManager.GetCamp(CampId);
        var enemyCamp = CampManager.GetCamp(characterTemplate.CampId);
        if (enemyCamp != null && camp != null)
        {
            action.Invoke(CampManager.CanCauseHarm(camp, enemyCamp), characterTemplate);
            return;
        }

        action.Invoke(false, characterTemplate);
    }

    private void ExitTheAttackArea(Node node)
    {
        if (node == this)
        {
            return;
        }

        if (node is CharacterTemplate characterTemplate)
        {
            _enemyInTheAttackRange?.Remove(characterTemplate);
        }
    }


    /// <summary>
    /// <para>Set target location</para>
    /// <para>设置目标位置</para>
    /// </summary>
    /// <param name="targetPosition"></param>
    public void SetTargetPosition(Vector2 targetPosition)
    {
        if (NavigationAgent2D == null)
        {
            return;
        }

        NavigationAgent2D.TargetPosition = targetPosition;
    }


    public override void _ExitTree()
    {
        base._ExitTree();
        if (_attackArea != null)
        {
            _attackArea.BodyEntered -= EnterTheAttackArea;
            _attackArea.BodyExited -= ExitTheAttackArea;
        }

        if (_scoutArea != null)
        {
            _scoutArea.BodyEntered -= EnterTheScoutArea;
            _scoutArea.BodyExited -= ExitTheScoutArea;
        }

        if (StateMachine != null)
        {
            StateMachine.Stop();
        }
    }
}