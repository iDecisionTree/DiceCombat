using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DiceCombat.scripts.card;
using DiceCombat.scripts.card_skill;
using DiceCombat.scripts.coin;
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
	[Export] public DiceRerollUI DiceRerollUI { get; set; }
	[Export] public Coin TurnOrderCoin { get; set; }
	[Export] public TurnIconSwapAnimator TurnIconSwapAnimator { get; set; }

	[ExportGroup("Presentation Hooks")]
	[Export] public CombatCameraDirector CameraDirector { get; set; }
	[Export] public CombatEffectDirector EffectDirector { get; set; }
	[Export] public CombatInfoPanel PlayerInfoPanel { get; set; }
	[Export] public CombatInfoPanel EnemyInfoPanel { get; set; }

	private readonly List<DiceData> _playerRollDices = new();
	private readonly List<DiceData> _playerChooseDices = new();
	private readonly List<DiceData> _enemyRollDices = new();
	private readonly List<DiceData> _enemyChooseDices = new();

	private ICombatCameraDirector _cameraDirector = NullCombatCameraDirector.Instance;
	private ICombatEffectDirector _effectDirector = NullCombatEffectDirector.Instance;

	private CombatState _currentState;
	private CombatTurn _currentTurn;
	private int _roundCount;
	private bool _combatStarted;
	private bool _waitingForPlayerConfirm;
	private bool _isPlayerRerolling;
	private int _currentRequiredSelectionCount;
	private int _playerRerollRemaining;
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

		if (DiceRerollUI != null)
		{
			DiceRerollUI.RerollClicked += OnDiceRerollClicked;
		}

		CallDeferred(MethodName.StartBattle);
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

		if (DiceRerollUI != null)
		{
			DiceRerollUI.RerollClicked -= OnDiceRerollClicked;
		}
	}

	public void StartBattle()
	{
		_resolutionSequenceToken++;
		_combatStarted = true;
		_roundCount = 1;
		_currentState = CombatState.Init;
		_currentRequiredSelectionCount = 0;
		_waitingForPlayerConfirm = false;
		_isPlayerRerolling = false;
		ResetPlayerChoiceState();
		_playerRerollRemaining = GetPlayerMaxReroll();
		ResetCardSkillRuntimes();

		ResetBattlePresentation();
		_cameraDirector.OnBattleStarted();

		GD.Print($"初始化完成, round={_roundCount}, state={_currentState}, 开始抛硬币决定先手");
		BeginTurnOrderDecision();
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

		TurnOrderCoin?.ResetToIdle();
		DiceManager?.ResetBattlefield();
		CloseChoiceUi();
		TurnIconSwapAnimator?.ResetToCenter();
		_cameraDirector.ResetCamera();
		_effectDirector.ResetEffects();
		SetBattleCardsVisible(false);
		RefreshBattleInfoPanels();
		HideBattleInfoPanelsForCoinToss();
	}

	private void BeginTurnOrderDecision()
	{
		if (TurnOrderCoin != null)
		{
			TurnOrderCoin.PlayToss(OnTurnOrderCoinFinished);
			return;
		}

		CombatTurn firstTurn = GD.Randf() < 0.5f ? CombatTurn.Player : CombatTurn.Enemy;
		GD.PrintErr($"TurnOrderCoin 未绑定, 使用随机结果决定先手: {firstTurn}");
		BeginFirstTurn(firstTurn);
	}

	private void OnTurnOrderCoinFinished(CoinSide side)
	{
		CombatTurn firstTurn = side == CoinSide.Front ? CombatTurn.Player : CombatTurn.Enemy;
		GD.Print($"抛硬币完成, 结果={side}, firstTurn={firstTurn}");
		BeginFirstTurn(firstTurn);
	}

	private void BeginFirstTurn(CombatTurn firstTurn)
	{
		_currentTurn = firstTurn;
		_currentState = firstTurn == CombatTurn.Player ? CombatState.PlayerRollAllDice : CombatState.EnemyRollAllDice;
		_currentRequiredSelectionCount = 0;
		_waitingForPlayerConfirm = false;
		_isPlayerRerolling = false;
		_cameraDirector.OnTurnChanged(_currentTurn, _roundCount);

		void StartFirstState()
		{
			GD.Print($"首回合确定, round={_roundCount}, turn={_currentTurn}, state={_currentState}");
			ProcessCurrentState();
		}

		PlayOpeningPresentation(StartFirstState);
	}

	private void HideBattleInfoPanelsForCoinToss()
	{
		PlayerInfoPanel?.HideForIntro();
		EnemyInfoPanel?.HideForIntro();
	}

	private void PlayOpeningPresentation(Action onFinished)
	{
		int pendingCount = 0;

		void CompleteOne()
		{
			pendingCount--;
			if (pendingCount <= 0)
			{
				onFinished?.Invoke();
			}
		}

		if (TurnIconSwapAnimator != null)
		{
			pendingCount++;
			TurnIconSwapAnimator.PlayIntroAnimation(_currentTurn == CombatTurn.Enemy, CompleteOne);
		}

		if (PlayerInfoPanel != null || EnemyInfoPanel != null)
		{
			pendingCount++;
			PlayBattleInfoPanelIntro(CompleteOne);
		}

		if (pendingCount == 0)
		{
			onFinished?.Invoke();
		}
	}

	private void PlayBattleInfoPanelIntro(Action onFinished)
	{
		int pendingCount = 0;

		void CompleteOne()
		{
			pendingCount--;
			if (pendingCount <= 0)
			{
				onFinished?.Invoke();
			}
		}

		if (PlayerInfoPanel != null)
		{
			pendingCount++;
			PlayerInfoPanel.PlayIntroReveal(CompleteOne);
		}

		if (EnemyInfoPanel != null)
		{
			pendingCount++;
			EnemyInfoPanel.PlayIntroReveal(CompleteOne);
		}

		if (pendingCount == 0)
		{
			onFinished?.Invoke();
		}
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
		_isPlayerRerolling = false;
		_currentRequiredSelectionCount = GetRequiredSelectionCount(PlayerCard, _currentTurn);

		DiceSelectUI?.Open();
		DiceRerollUI?.Open();
		RefreshPlayerChoiceUI();

		GD.Print("等待玩家点击确认按钮");
	}

	private void OnDiceSelectionChanged(int selectedCount)
	{
		if (_currentState != CombatState.PlayerConfirm || DiceSelectUI == null)
		{
			return;
		}

		RefreshPlayerChoiceUI();
	}

	private void OnDiceSelectUIConfirmClicked()
	{
		if (!_waitingForPlayerConfirm || _currentState != CombatState.PlayerConfirm || _isPlayerRerolling)
		{
			GD.Print("确认按钮点击被忽略, 当前不在等待确认状态");
			return;
		}

		int selectedCount = GetCurrentSelectedCount();
		if (selectedCount != _currentRequiredSelectionCount)
		{
			GD.Print($"确认按钮点击被忽略, 选择骰子数量不匹配, 当前={selectedCount}, 需要={_currentRequiredSelectionCount}");
			DiceSelectUI?.SetSelectionProgress(selectedCount, _currentRequiredSelectionCount, GetCurrentSelectedTotalPoints());
			return;
		}

		ContinueAfterPlayerConfirm();
	}

	private void OnDiceRerollClicked()
	{
		if (!_waitingForPlayerConfirm || _currentState != CombatState.PlayerConfirm || _isPlayerRerolling)
		{
			return;
		}

		if (_playerRerollRemaining <= 0 || DiceManager == null)
		{
			RefreshPlayerChoiceUI();
			return;
		}

		List<DiceData> selectedDice = GetCurrentSelectedDiceSnapshot();
		if (selectedDice.Count == 0)
		{
			RefreshPlayerChoiceUI();
			return;
		}

		_isPlayerRerolling = true;
		RefreshPlayerChoiceUI();

		if (!DiceManager.PlayRerollAnim(selectedDice, OnPlayerRerollFinished))
		{
			_isPlayerRerolling = false;
			RefreshPlayerChoiceUI();
		}
	}

	private void OnPlayerRerollFinished()
	{
		_isPlayerRerolling = false;
		_playerRerollRemaining = Math.Max(_playerRerollRemaining - 1, 0);
		RefreshPlayerChoiceUI();
	}

	private void ContinueAfterPlayerConfirm()
	{
		ResetPlayerChoiceState();
		_playerChooseDices.Clear();
		_playerChooseDices.AddRange(GetCurrentSelectedDiceSnapshot());
		ApplyAfterDiceSelected(PlayerCard, EnemyCard, _playerChooseDices);
		CloseChoiceUi();
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
		ApplyAfterDiceSelected(EnemyCard, PlayerCard, _enemyChooseDices);

		if (DiceManager == null)
		{
			ContinueAfterEnemyChoose();
			return;
		}

		PrepareEnemyRevealUi();

		int revealTotalPoints = 0;
		DiceManager.PlaySelectionAnimation(_enemyChooseDices, revealedPoints =>
		{
			revealTotalPoints += revealedPoints;
			DiceSelectUI?.SetSelection3DText(revealTotalPoints.ToString());
		}, () =>
		{
			FinishEnemyRevealUi();
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
			ResolveDamage(CombatTurn.Player, playerCard, enemyCard, _playerChooseDices, _enemyChooseDices, playerValue, enemyValue, "玩家造成伤害, 目标=敌人");
		}
		else
		{
			ResolveDamage(CombatTurn.Enemy, enemyCard, playerCard, _enemyChooseDices, _playerChooseDices, enemyValue, playerValue, "敌人造成伤害, 目标=玩家");
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
		FinishBattle(CombatState.Victory, "游戏结束, 玩家胜利");
	}

	private void ExecuteDefeat()
	{
		FinishBattle(CombatState.Defeat, "游戏结束, 玩家失败");
	}

	private void FinishResolveDamageAfterEffect()
	{
		SceneTree tree = GetTree();
		float resolutionDuration = Mathf.Max(_effectDirector.GetResolutionDuration(), 0f);

		if (tree == null || resolutionDuration <= 0f)
		{
			CompleteResolution();
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

		CompleteResolution();
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

	private void RefreshPlayerChoiceUI()
	{
		int selectedCount = GetCurrentSelectedCount();
		int selectedTotalPoints = GetCurrentSelectedTotalPoints();

		if (DiceSelectUI != null)
		{
			DiceSelectUI.SetSelectionProgress(selectedCount, _currentRequiredSelectionCount, selectedTotalPoints);
			DiceSelectUI.SetSelectionDamagePreview(GetCurrentSelectionDamagePreviewBonus());
			DiceSelectUI.SetConfirmEnabled(_waitingForPlayerConfirm && !_isPlayerRerolling && _currentState == CombatState.PlayerConfirm && selectedCount == _currentRequiredSelectionCount);
		}

		if (DiceRerollUI != null)
		{
			DiceRerollUI.SetRerollCount(_playerRerollRemaining);
			DiceRerollUI.SetEnabled(_waitingForPlayerConfirm && !_isPlayerRerolling && _currentState == CombatState.PlayerConfirm && _playerRerollRemaining > 0 && selectedCount > 0);
		}
	}

	private void ResetPlayerChoiceState()
	{
		_waitingForPlayerConfirm = false;
		_isPlayerRerolling = false;
		_currentRequiredSelectionCount = 0;
	}

	private void CloseChoiceUi()
	{
		DiceSelectUI?.Close();
		DiceRerollUI?.Close();
	}

	private int GetCurrentSelectedCount()
	{
		return DiceManager != null ? DiceManager.CurrentSelectedCount : 0;
	}

	private int GetCurrentSelectedTotalPoints()
	{
		return DiceManager != null ? DiceManager.GetSelectedDiceTotalPoints() : 0;
	}

	private int GetCurrentSelectionDamagePreviewBonus()
	{
		if (_currentState != CombatState.PlayerConfirm || PlayerCard == null)
		{
			return 0;
		}

		List<DiceData> selectedDice = GetCurrentSelectedDiceSnapshot();
		DiceSelectionPreviewContext context = new DiceSelectionPreviewContext(
			_currentTurn,
			PlayerCard,
			EnemyCard,
			GetSelectionRole(PlayerCard, _currentTurn),
			selectedDice);

		return PlayerCard.GetSelectionDamagePreviewBonus(context);
	}

	private List<DiceData> GetCurrentSelectedDiceSnapshot()
	{
		return DiceManager != null ? DiceManager.GetSelectedDiceData() : new List<DiceData>();
	}

	private void PrepareEnemyRevealUi()
	{
		if (DiceSelectUI != null)
		{
			DiceSelectUI.ResetSelectionProgress();
			DiceSelectUI.SetSelection3DText("0");
			DiceSelectUI.SetSelection3DVisible(true);
		}

		DiceRerollUI?.Close();
	}

	private void FinishEnemyRevealUi()
	{
		DiceSelectUI?.SetSelection3DVisible(false);
	}

	private void ResolveDamage(CombatTurn turn, Card sourceCard, Card targetCard, IReadOnlyList<DiceData> sourceDice, IReadOnlyList<DiceData> targetDice, int attackValue, int defenseValue, string damageLogLabel)
	{
		int damage = Math.Max(attackValue - defenseValue, 0);
		damage += sourceCard.SkillRuntime.ConsumePendingDamageBonus();
		damage -= targetCard.SkillRuntime.ConsumePendingDamageReduction();
		damage = Math.Max(damage, 0);

		DamageResolutionSkillContext sourceContext = CreateDamageResolutionContext(turn, sourceCard, targetCard, sourceCard, targetCard, sourceDice, targetDice, attackValue, defenseValue, damage);
		DamageResolutionSkillContext targetContext = sourceContext.CreateForOwner(targetCard, sourceCard, targetCard.SkillRuntime, sourceCard.SkillRuntime);

		sourceCard.ApplyBeforeDamageResolved(sourceContext);
		targetCard.ApplyBeforeDamageResolved(targetContext);

		damage = sourceContext.Damage;
		GD.Print($"{damageLogLabel}, 伤害={damage}");
		_effectDirector.OnAttackStarted(turn, sourceCard, targetCard, damage);
		_effectDirector.PlayAttackEffect(turn, sourceCard, targetCard, damage);
		_effectDirector.OnAttackImpact(turn, sourceCard, targetCard, damage);
		_effectDirector.PlayDamageEffect(targetCard, damage);
		targetCard.TakeDamage(damage);
		sourceContext.SetAppliedDamage(damage);
		targetContext.SetAppliedDamage(damage);
		_effectDirector.OnDefenseStarted(turn, sourceCard, targetCard, damage);
		_effectDirector.PlayDefenseEffect(turn, sourceCard, targetCard, damage);
		_effectDirector.OnDefenseImpact(turn, sourceCard, targetCard, damage);
		sourceCard.ApplyAfterDamageResolved(sourceContext);
		targetCard.ApplyAfterDamageResolved(targetContext);
		RefreshBattleInfoPanels();
	}

	private void FinishBattle(CombatState resultState, string logMessage)
	{
		ResetPlayerChoiceState();
		CloseChoiceUi();
		SetBattleCardsVisible(false);
		_cameraDirector.OnBattleEnded(resultState);
		GD.Print(logMessage);
	}

	private void CompleteResolution()
	{
		SetBattleCardsVisible(false);
		RefreshBattleInfoPanels();
		TransitionTo(CombatState.CheckEnd);
	}

	private int GetPlayerMaxReroll()
	{
		if (PlayerCard?.CardData == null)
		{
			return 0;
		}

		return Math.Max(PlayerCard.CardData.MaxReroll, 0);
	}

	private static string FormatDiceValues(IEnumerable<DiceData> dices)
	{
		return string.Join(", ", dices.Select(x => x.Num));
	}

	private static int SumDiceValues(IEnumerable<DiceData> dices)
	{
		return dices.Sum(x => x.Num);
	}

	private void ResetCardSkillRuntimes()
	{
		PlayerCard?.ResetSkillRuntime();
		EnemyCard?.ResetSkillRuntime();
	}

	private void ApplyAfterDiceSelected(Card ownerCard, Card otherCard, IReadOnlyList<DiceData> selectedDice)
	{
		if (ownerCard == null)
		{
			return;
		}

		DiceSelectionSkillContext context = new DiceSelectionSkillContext(
			_currentTurn,
			ownerCard,
			otherCard,
			GetSelectionRole(ownerCard, _currentTurn),
			selectedDice,
			ownerCard.SkillRuntime,
			otherCard?.SkillRuntime);

		ownerCard.ApplyAfterDiceSelected(context);
		RefreshBattleInfoPanels();
	}

	private DamageResolutionSkillContext CreateDamageResolutionContext(
		CombatTurn turn,
		Card ownerCard,
		Card otherCard,
		Card sourceCard,
		Card targetCard,
		IReadOnlyList<DiceData> sourceDice,
		IReadOnlyList<DiceData> targetDice,
		int attackValue,
		int defenseValue,
		int damage)
	{
		return new DamageResolutionSkillContext(
			turn,
			ownerCard,
			otherCard,
			sourceCard,
			targetCard,
			sourceDice,
			targetDice,
			attackValue,
			defenseValue,
			damage,
			ownerCard?.SkillRuntime,
			otherCard?.SkillRuntime);
	}

	private CardSelectionRole GetSelectionRole(Card card, CombatTurn turn)
	{
		bool isAttacker = (card == PlayerCard && turn == CombatTurn.Player) ||
			(card == EnemyCard && turn == CombatTurn.Enemy);

		return isAttacker ? CardSelectionRole.Attack : CardSelectionRole.Defense;
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
