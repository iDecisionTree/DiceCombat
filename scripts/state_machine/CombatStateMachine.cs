using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DiceCombat.scripts.card;
using DiceCombat.scripts.dice;
using DiceCombat.scripts.ui;

namespace DiceCombat.scripts.state_machine;

[GlobalClass]
[Tool]
public partial class CombatStateMachine : Node
{
  [Export] public Card PlayerCard { get; set; }
  [Export] public Card EnemyCard { get; set; }

  [Export] public DiceManager DiceManager { get; set; }
  [Export] public DiceSelectUI DiceSelectUI { get; set; }
  [Export] public TurnIconSwapAnimator TurnIconSwapAnimator { get; set; }

  [ExportGroup("Presentation Hooks")]
  [Export] public CombatCameraDirector CameraDirector { get; set; }
  [Export] public CombatEffectDirector EffectDirector { get; set; }
  [Export] public CombatInfoPanel PlayerInfoPanel { get; set; }
  [Export] public CombatInfoPanel EnemyInfoPanel { get; set; }

  private readonly List<DiceData> _playerRollDices = new List<DiceData>();
  private readonly List<DiceData> _playerChooseDices = new List<DiceData>();
  private readonly List<DiceData> _enemyRollDices = new List<DiceData>();
  private readonly List<DiceData> _enemyChooseDices = new List<DiceData>();

  private ICombatCameraDirector _cameraDirector = NullCombatCameraDirector.Instance;
  private ICombatEffectDirector _effectDirector = NullCombatEffectDirector.Instance;

  private CombatState _currentState;
  private CombatTurn _currentTurn;
  private int _roundCount;
  private bool _combatStarted;
  private bool _waitingForPlayerConfirm;
  private int _currentRequiredSelectionCount;
  private int _resolutionSequenceToken;

  public override void _Ready()
  {
    BindPresentationDirectors();

    if (DiceManager != null)
    {
      DiceManager.SelectionChanged += OnDiceSelectionChanged;
    }

    if (DiceSelectUI != null)
    {
      DiceSelectUI.ConfirmClicked += OnDiceSelectUIConfirmClicked;
    }

    StartBattle();
  }

  public override void _ExitTree()
  {
    if (DiceManager != null)
    {
      DiceManager.SelectionChanged -= OnDiceSelectionChanged;
    }

    if (DiceSelectUI != null)
    {
      DiceSelectUI.ConfirmClicked -= OnDiceSelectUIConfirmClicked;
    }
  }

  public void StartBattle()
  {
    _resolutionSequenceToken++;
    _combatStarted = true;
    _roundCount = 1;
    _currentTurn = CombatTurn.Player;
    _currentState = CombatState.PlayerRollAllDice;
    _waitingForPlayerConfirm = false;
    _currentRequiredSelectionCount = 0;

    ResetBattlePresentation();
    _cameraDirector.OnBattleStarted();

    GD.Print($"初始化完成, round={_roundCount}, turn={_currentTurn}, state={_currentState}");
    ProcessCurrentState();
  }

  private void BindPresentationDirectors()
  {
    _cameraDirector = (ICombatCameraDirector)CameraDirector ?? NullCombatCameraDirector.Instance;
    _effectDirector = (ICombatEffectDirector)EffectDirector ?? NullCombatEffectDirector.Instance;
    BindBattleInfoPanels();
  }

  private void BindBattleInfoPanels()
  {
    PlayerInfoPanel?.BindCard(PlayerCard);
    EnemyInfoPanel?.BindCard(EnemyCard);
  }

  private void ResetBattlePresentation()
  {
    _playerRollDices.Clear();
    _playerChooseDices.Clear();
    _enemyRollDices.Clear();
    _enemyChooseDices.Clear();

    DiceManager?.ResetBattlefield();
    DiceSelectUI?.Close();
    TurnIconSwapAnimator?.ResetToHome();
    _cameraDirector.ResetCamera();
    _effectDirector.ResetEffects();
    SetBattleCardsVisible(false);
    RefreshBattleInfoPanels();
  }

  private void ProcessCurrentState()
  {
    if (!_combatStarted)
    {
      GD.Print("初始化未完成");
      return;
    }

    GD.Print($"状态切换到: {_currentState}");

    switch (_currentState)
    {
      case CombatState.PlayerRollAllDice:
        ExecutePlayerRollAllDice();
        break;
      case CombatState.PlayerChoose:
        ExecutePlayerChoose();
        break;
      case CombatState.PlayerConfirm:
        ExecutePlayerConfirm();
        break;
      case CombatState.EnemyRollAllDice:
        ExecuteEnemyRollAllDice();
        break;
      case CombatState.EnemyChoose:
        ExecuteEnemyChooseDice();
        break;
      case CombatState.ResolveDamage:
        ExecuteResolveDamage();
        break;
      case CombatState.CheckEnd:
        ExecuteCheckEnd();
        break;
      case CombatState.SwitchTurn:
        ExecuteSwitchTurn();
        break;
      case CombatState.Victory:
        ExecuteVictory();
        break;
      case CombatState.Defeat:
        ExecuteDefeat();
        break;
    }
  }

  private void TransitionTo(CombatState nextState)
  {
    _currentState = nextState;
    ProcessCurrentState();
  }

  private void ExecutePlayerRollAllDice()
  {
    GD.Print("开始玩家掷骰");
    _playerChooseDices.Clear();

    if (!TryGetCard(PlayerCard, "PlayerCard", out Card playerCard))
    {
      return;
    }

    _playerRollDices.Clear();
    _playerRollDices.AddRange(playerCard.RollAllDice());

    if (DiceManager == null)
    {
      GD.PrintErr("DiceManager 未绑定, 直接进入玩家选择状态。");
      TransitionTo(CombatState.PlayerChoose);
      return;
    }

    DiceManager.PlayAllRollAnim(_playerRollDices, () =>
    {
      GD.Print($"玩家掷骰完成, 数量={_playerRollDices.Count}, 结果={FormatDiceValues(_playerRollDices)}");
      TransitionTo(CombatState.PlayerChoose);
    });
  }

  private void ExecutePlayerChoose()
  {
    GD.Print($"开始玩家选骰, 可用数量={_playerRollDices.Count}");
    TransitionTo(CombatState.PlayerConfirm);
  }

  private void ExecutePlayerConfirm()
  {
    _waitingForPlayerConfirm = true;
    _currentRequiredSelectionCount = GetRequiredSelectionCount(PlayerCard, _currentTurn);

    if (DiceSelectUI != null)
    {
      int selectedCount = DiceManager != null ? DiceManager.CurrentSelectedCount : 0;
      int selectedTotalPoints = DiceManager != null ? DiceManager.GetSelectedDiceTotalPoints() : 0;
      DiceSelectUI.SetSelectionProgress(selectedCount, _currentRequiredSelectionCount, selectedTotalPoints);
      DiceSelectUI.Open();
    }

    GD.Print("等待玩家点击确认按钮");
  }

  private void OnDiceSelectionChanged(int selectedCount)
  {
    if (_currentState != CombatState.PlayerConfirm || DiceSelectUI == null)
    {
      return;
    }

    int selectedTotalPoints = DiceManager != null ? DiceManager.GetSelectedDiceTotalPoints() : 0;
    DiceSelectUI.SetSelectionProgress(selectedCount, _currentRequiredSelectionCount, selectedTotalPoints);
  }

  private void OnDiceSelectUIConfirmClicked()
  {
    if (!_waitingForPlayerConfirm || _currentState != CombatState.PlayerConfirm)
    {
      GD.Print("确认按钮点击被忽略, 当前不在等待确认状态");
      return;
    }

    int selectedCount = DiceManager != null ? DiceManager.CurrentSelectedCount : 0;
    if (selectedCount < _currentRequiredSelectionCount)
    {
      GD.Print($"确认按钮点击被忽略, 选择骰子数量不足, 当前={selectedCount}, 需要={_currentRequiredSelectionCount}");
      if (DiceSelectUI != null)
      {
        int selectedTotalPoints = DiceManager != null ? DiceManager.GetSelectedDiceTotalPoints() : 0;
        DiceSelectUI.SetSelectionProgress(selectedCount, _currentRequiredSelectionCount, selectedTotalPoints);
      }

      return;
    }

    ContinueAfterPlayerConfirm();
  }

  private void ContinueAfterPlayerConfirm()
  {
    _waitingForPlayerConfirm = false;
    _playerChooseDices.Clear();

    if (DiceManager != null)
    {
      _playerChooseDices.AddRange(DiceManager.GetSelectedDiceData());
    }

    DiceSelectUI?.Close();
    TransitionTo(_currentTurn == CombatTurn.Player ? CombatState.EnemyRollAllDice : CombatState.ResolveDamage);
  }

  private void ExecuteEnemyRollAllDice()
  {
    GD.Print("开始敌人掷骰");
    _enemyRollDices.Clear();

    if (!TryGetCard(EnemyCard, "EnemyCard", out Card enemyCard))
    {
      return;
    }

    _enemyRollDices.AddRange(enemyCard.RollAllDice());

    if (DiceManager == null)
    {
      GD.PrintErr("DiceManager 未绑定, 直接进入敌人选择状态。");
      TransitionTo(CombatState.EnemyChoose);
      return;
    }

    DiceManager.PlayAllRollAnim(_enemyRollDices, () =>
    {
      GD.Print($"敌人掷骰完成, 数量={_enemyRollDices.Count}, 结果={FormatDiceValues(_enemyRollDices)}");
      TransitionTo(CombatState.EnemyChoose);
    }, true);
  }

  private void ExecuteEnemyChooseDice()
  {
    GD.Print($"开始敌人选骰, 可用数量={_enemyRollDices.Count}");

    int requiredSelectionCount = GetRequiredSelectionCount(EnemyCard, _currentTurn);
    int selectCount = Math.Max(0, Math.Min(requiredSelectionCount, _enemyRollDices.Count));

    _enemyChooseDices.Clear();
    _enemyChooseDices.AddRange(_enemyRollDices
      .Select((dice, index) => new { dice, index })
      .OrderByDescending(x => x.dice.Num)
      .ThenBy(x => x.index)
      .Take(selectCount)
      .Select(x => x.dice));

    GD.Print($"敌人选骰完成, 选中数量={_enemyChooseDices.Count}, 结果={FormatDiceValues(_enemyChooseDices)}");

    if (DiceManager == null)
    {
      ContinueAfterEnemyChoose();
      return;
    }

    if (DiceSelectUI != null)
    {
      DiceSelectUI.ResetSelectionProgress();
      DiceSelectUI.SetSelection3DText("0");
      DiceSelectUI.SetSelection3DVisible(true);
    }

    int revealTotalPoints = 0;
    DiceManager.PlaySelectionAnimation(_enemyChooseDices, revealedPoints =>
    {
      revealTotalPoints += revealedPoints;
      if (DiceSelectUI != null)
      {
        DiceSelectUI.SetSelection3DText(revealTotalPoints.ToString());
      }
    }, () =>
    {
      if (DiceSelectUI != null)
      {
        DiceSelectUI.SetSelection3DVisible(false);
      }

      ContinueAfterEnemyChoose();
    });
  }

  private void ContinueAfterEnemyChoose()
  {
    TransitionTo(_currentTurn == CombatTurn.Enemy ? CombatState.PlayerRollAllDice : CombatState.ResolveDamage);
  }

  private void ExecuteResolveDamage()
  {
    if (!TryGetCard(PlayerCard, "PlayerCard", out Card playerCard) || !TryGetCard(EnemyCard, "EnemyCard", out Card enemyCard))
    {
      GD.PrintErr("CombatStateMachine: PlayerCard 或 EnemyCard 未绑定，无法结算伤害。");
      TransitionTo(CombatState.CheckEnd);
      return;
    }

    int playerValue = SumDiceValues(_playerChooseDices);
    int enemyValue = SumDiceValues(_enemyChooseDices);
    GD.Print($"开始结算伤害, turn={_currentTurn}, 玩家选骰总和={playerValue}, 敌人选骰总和={enemyValue}");

    SetBattleCardsVisible(false);
    RefreshBattleInfoPanels();

    if (_currentTurn == CombatTurn.Player)
    {
      int damage = Math.Max(playerValue - enemyValue, 0);
      GD.Print($"玩家造成伤害, 目标=敌人, 伤害={damage}");
      _effectDirector.OnAttackStarted(CombatTurn.Player, playerCard, enemyCard, damage);
      _effectDirector.PlayAttackEffect(CombatTurn.Player, playerCard, enemyCard, damage);
      _effectDirector.OnAttackImpact(CombatTurn.Player, playerCard, enemyCard, damage);
        _effectDirector.PlayDamageEffect(enemyCard, damage);
      enemyCard.TakeDamage(damage);
      _effectDirector.OnDefenseStarted(CombatTurn.Player, playerCard, enemyCard, damage);
      _effectDirector.PlayDefenseEffect(CombatTurn.Player, playerCard, enemyCard, damage);
      _effectDirector.OnDefenseImpact(CombatTurn.Player, playerCard, enemyCard, damage);
      RefreshBattleInfoPanels();
    }
    else
    {
      int damage = Math.Max(enemyValue - playerValue, 0);
      GD.Print($"敌人造成伤害, 目标=玩家, 伤害={damage}");
      _effectDirector.OnAttackStarted(CombatTurn.Enemy, enemyCard, playerCard, damage);
      _effectDirector.PlayAttackEffect(CombatTurn.Enemy, enemyCard, playerCard, damage);
      _effectDirector.OnAttackImpact(CombatTurn.Enemy, enemyCard, playerCard, damage);
        _effectDirector.PlayDamageEffect(playerCard, damage);
      playerCard.TakeDamage(damage);
      _effectDirector.OnDefenseStarted(CombatTurn.Enemy, enemyCard, playerCard, damage);
      _effectDirector.PlayDefenseEffect(CombatTurn.Enemy, enemyCard, playerCard, damage);
      _effectDirector.OnDefenseImpact(CombatTurn.Enemy, enemyCard, playerCard, damage);
      RefreshBattleInfoPanels();
    }

    FinishResolveDamageAfterEffect();
  }

  private void ExecuteCheckEnd()
  {
    if (!TryGetCard(PlayerCard, "PlayerCard", out Card playerCard) || !TryGetCard(EnemyCard, "EnemyCard", out Card enemyCard))
    {
      GD.PrintErr("CombatStateMachine: 无法检查战斗结果，因为卡牌未正确绑定。");
      return;
    }

    GD.Print($"检查战斗结果, round={_roundCount}, turn={_currentTurn}, 玩家HP={playerCard.CurrentHealth}, 敌人HP={enemyCard.CurrentHealth}");

    if (_currentTurn == CombatTurn.Player && enemyCard.IsDead())
    {
      GD.Print("敌人死亡, 玩家胜利");
      TransitionTo(CombatState.Victory);
      return;
    }

    if (_currentTurn == CombatTurn.Enemy && playerCard.IsDead())
    {
      GD.Print("玩家死亡, 玩家失败");
      TransitionTo(CombatState.Defeat);
      return;
    }

    TransitionTo(CombatState.SwitchTurn);
  }

  private void ExecuteSwitchTurn()
  {
    _roundCount++;

    CombatTurn nextTurn = _currentTurn == CombatTurn.Player ? CombatTurn.Enemy : CombatTurn.Player;
    CombatState nextState = nextTurn == CombatTurn.Player ? CombatState.PlayerRollAllDice : CombatState.EnemyRollAllDice;

    void FinishSwitch()
    {
      _currentTurn = nextTurn;
      _currentState = nextState;
      _currentRequiredSelectionCount = 0;
      _waitingForPlayerConfirm = false;
      _playerChooseDices.Clear();
      _enemyChooseDices.Clear();
      GD.Print($"切换回合完成, round={_roundCount}, turn={_currentTurn}, nextState={_currentState}");
      ProcessCurrentState();
    }

    if (TurnIconSwapAnimator != null)
    {
      TurnIconSwapAnimator.PlaySwapAnimation(FinishSwitch);
      return;
    }

    FinishSwitch();
  }

  private void ExecuteVictory()
  {
    _waitingForPlayerConfirm = false;
    DiceSelectUI?.Close();
    SetBattleCardsVisible(false);
    _cameraDirector.OnBattleEnded(CombatState.Victory);
    GD.Print("游戏结束, 玩家胜利");
  }

  private void ExecuteDefeat()
  {
    _waitingForPlayerConfirm = false;
    DiceSelectUI?.Close();
    SetBattleCardsVisible(false);
    _cameraDirector.OnBattleEnded(CombatState.Defeat);
    GD.Print("游戏结束, 玩家失败");
  }

  private void FinishResolveDamageAfterEffect()
  {
    SceneTree tree = GetTree();
    float resolutionDuration = Mathf.Max(_effectDirector.GetResolutionDuration(), 0f);

    if (tree == null || resolutionDuration <= 0f)
    {
      SetBattleCardsVisible(false);
      RefreshBattleInfoPanels();
      TransitionTo(CombatState.CheckEnd);
      return;
    }

    int resolutionToken = ++_resolutionSequenceToken;
    SceneTreeTimer timer = tree.CreateTimer(resolutionDuration);
    timer.Timeout += () => OnResolveDamageTimeout(resolutionToken);
  }

  private void OnResolveDamageTimeout(int resolutionToken)
  {
    if (resolutionToken != _resolutionSequenceToken)
    {
      return;
    }

    SetBattleCardsVisible(false);
    RefreshBattleInfoPanels();
    TransitionTo(CombatState.CheckEnd);
  }

  private void SetBattleCardsVisible(bool visible)
  {
    if (PlayerCard != null)
    {
      PlayerCard.Visible = visible;
    }

    if (EnemyCard != null)
    {
      EnemyCard.Visible = visible;
    }
  }

  private void RefreshBattleInfoPanels()
  {
    PlayerInfoPanel?.RefreshFromCard();
    EnemyInfoPanel?.RefreshFromCard();
  }

  private static string FormatDiceValues(IEnumerable<DiceData> dices)
  {
    return string.Join(", ", dices.Select(x => x.Num));
  }

  private static int SumDiceValues(IEnumerable<DiceData> dices)
  {
    return dices.Sum(x => x.Num);
  }

  private int GetRequiredSelectionCount(Card card, CombatTurn turn)
  {
    if (card == null || card.CardData == null)
    {
      return 0;
    }

    bool isActiveSide = (card == PlayerCard && turn == CombatTurn.Player) ||
                        (card == EnemyCard && turn == CombatTurn.Enemy);

    return isActiveSide ? card.CardData.Attack : card.CardData.Defense;
  }

  private static bool TryGetCard(Card card, string cardLabel, out Card resolvedCard)
  {
    resolvedCard = card;
    if (resolvedCard != null)
    {
      return true;
    }

    GD.PrintErr($"CombatStateMachine: {cardLabel} 未绑定。");
    return false;
  }
}
