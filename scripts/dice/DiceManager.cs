using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceCombat.scripts.dice;

[GlobalClass]
[Tool]
public partial class DiceManager : Node3D
{
	[Signal] public delegate void SelectionChangedEventHandler(int selectedCount);

	[Export] public PackedScene Dice4 { get; set; }
	[Export] public PackedScene Dice6 { get; set; }
	[Export] public PackedScene Dice8 { get; set; }
	[Export] public PackedScene Dice12 { get; set; }
	
	[ExportGroup("Physics")]
	[Export] public float DiceSpacing { get; set; } = 0.8f;
	[Export] public float SpawnHeight { get; set; } = 2f;
	[Export] public Vector2 SpawnAreaHalfExtents { get; set; } = new Vector2(0.5f, 0.5f);
	[Export] public float RollUpImpulse { get; set; } = 2f;
	[Export] public float RollHorizontalImpulse { get; set; } = 1f;
	[Export] public float TorqueImpulseStrength { get; set; } = 5f;
	[Export] public float StableTimeRequired { get; set; } = 0.2f;
	[Export] public float MaxRollTime { get; set; } = 10f;
	[Export] public float LinearSleepThreshold { get; set; } = 0.05f;
	[Export] public float AngularSleepThreshold { get; set; } = 0.2f;
	[Export] public float PresentationDuration { get; set; } = 0.45f;
	[Export] public float PresentationDiceSpacing { get; set; } = 2f;
	[Export] public float PresentationForwardOffset { get; set; } = 8f;
	[Export] public float EnemySelectionStartDelay { get; set; } = 0.5f;
	[Export] public float EnemySelectionRevealDelay { get; set; } = 0.4f;
	[Export] public float EnemySelectionEndDelay { get; set; } = 0.5f;
	
	private int _finishedCount;
	private int _totalCount;
	private int _selectedCount;
	private bool _presentationIsEnemyTurn;
	private Action _onFinished;

	public int CurrentSelectedCount => _selectedCount;

	public void ResetBattlefield()
	{
		ClearCurrentDice();
		ResetSelectionCount();
		_finishedCount = 0;
		_totalCount = 0;
		_selectedCount = 0;
		_presentationIsEnemyTurn = false;
		_onFinished = null;
	}

	public void PlaySelectionAnimation(List<DiceData> selectedDiceData, Action<int> onDiceRevealed, Action onFinished)
	{
		List<Dice> currentDice = GetCurrentDice();
		if (currentDice.Count == 0 || selectedDiceData == null || selectedDiceData.Count == 0)
		{
			ClearAllSelections();
			onFinished?.Invoke();
			return;
		}

		HashSet<DiceData> targetSet = new HashSet<DiceData>(selectedDiceData);
		Dictionary<DiceData, Dice> diceByData = currentDice
			.Where(dice => dice.DiceData != null && targetSet.Contains(dice.DiceData))
			.ToDictionary(dice => dice.DiceData, dice => dice);
		List<Dice> targetDice = new List<Dice>();
		foreach (DiceData data in selectedDiceData)
		{
			if (data != null && diceByData.TryGetValue(data, out Dice dice))
			{
				targetDice.Add(dice);
			}
		}

		ClearAllSelections();
		if (targetDice.Count == 0)
		{
			onFinished?.Invoke();
			return;
		}

		Tween tween = CreateTween();
		tween.TweenInterval(EnemySelectionStartDelay);
		for (int i = 0; i < targetDice.Count; i++)
		{
			Dice dice = targetDice[i];
			tween.TweenCallback(Callable.From(() =>
			{
				dice.SetSelected(true, true);
				onDiceRevealed?.Invoke(dice.DiceData != null ? dice.DiceData.Num : 0);
				_selectedCount++;
				EmitSignal(SignalName.SelectionChanged, _selectedCount);
			}));

			if (i < targetDice.Count - 1)
			{
				tween.TweenInterval(EnemySelectionRevealDelay);
			}
		}

		tween.TweenInterval(EnemySelectionEndDelay);

		tween.Finished += () => onFinished?.Invoke();
	}

	public void PlayAllRollAnim(List<DiceData> dices, Action onFinished, bool isEnemyTurn = false)
	{
		ClearCurrentDice();
		ResetSelectionCount();
		_presentationIsEnemyTurn = isEnemyTurn;

		if (dices == null || dices.Count == 0)
		{
			GD.Print("DiceManager: 没有骰子需要播放");
			onFinished?.Invoke();
			return;
		}

		_finishedCount = 0;
		_totalCount = dices.Count;
		_onFinished = onFinished;
		float startX = -((dices.Count - 1) * DiceSpacing) * 0.5f;
		
		for (int i = 0; i < dices.Count; i++)
		{
			DiceData data = dices[i];
			if (data == null)
			{
				OnSingleDiceRollFinished(null, 0);
				continue;
			}

			Dice dice = CreateDiceNode(data.DiceType);
			if (dice == null)
			{
				GD.PrintErr($"DiceManager: 创建骰子失败, diceType={data.DiceType}");
				OnSingleDiceRollFinished(data, 0);
				continue;
			}

			AddChild(dice);
			dice.CanBeSelected = false;
			dice.DiceData = data;

			float x = startX + i * DiceSpacing;
			Vector3 landingPosition = new Vector3(x, 0f, 0f);
			Vector3 spawnPosition = landingPosition + new Vector3(
				(float)GD.RandRange(-SpawnAreaHalfExtents.X, SpawnAreaHalfExtents.X),
				SpawnHeight,
				(float)GD.RandRange(-SpawnAreaHalfExtents.Y, SpawnAreaHalfExtents.Y)
			);

			dice.ConfigureRollTiming(StableTimeRequired, MaxRollTime, LinearSleepThreshold, AngularSleepThreshold);
			dice.Position = spawnPosition;
			dice.DiceType = data.DiceType;
			dice.Freeze = true;
			dice.Sleeping = false;
			dice.LinearVelocity = Vector3.Zero;
			dice.AngularVelocity = Vector3.Zero;
			dice.RotationDegrees = new Vector3(
				GD.Randf() * 360f,
				GD.Randf() * 360f,
				GD.Randf() * 360f
			);
			dice.Freeze = false;
			dice.BeginRollTracking();

			(float horizontalScale, float torqueScale) = GetImpulseScale(data.DiceType);

			Vector3 launchImpulse = new Vector3(
				(float)GD.RandRange(-RollHorizontalImpulse, RollHorizontalImpulse),
				RollUpImpulse,
				(float)GD.RandRange(-RollHorizontalImpulse, RollHorizontalImpulse)
			) * new Vector3(horizontalScale, 1f, horizontalScale);
			Vector3 spinImpulse = new Vector3(
				(float)GD.RandRange(-1f, 1f),
				(float)GD.RandRange(-1f, 1f),
				(float)GD.RandRange(-1f, 1f)
			) * (TorqueImpulseStrength * torqueScale);

			dice.RollFinished += result => OnSingleDiceRollFinished(data, result);
			dice.Selected += OnDiceSelected;
			
			dice.ApplyCentralImpulse(launchImpulse);
			dice.ApplyTorqueImpulse(spinImpulse);
		}
	}

	private Dice CreateDiceNode(DiceType diceType)
	{
		Node3D visualRoot = InstantiateVisualRoot(diceType);
		if (visualRoot is Dice existingDice)
		{
			return existingDice;
		}

		Dice dice = new Dice();
		if (visualRoot != null)
		{
			dice.AddChild(visualRoot);
		}

		return dice;
	}

	private Node3D InstantiateVisualRoot(DiceType diceType)
	{
		PackedScene scene = GetSceneByDiceType(diceType);
		if (scene == null)
		{
			return null;
		}

		Node instance = scene.Instantiate();
		if (instance is Node3D node3D)
		{
			return node3D;
		}

		GD.PrintErr($"DiceManager: 场景根节点不是 3D 节点, diceType={diceType}, scene={scene.ResourcePath}");
		instance.QueueFree();
		return null;
	}

	private PackedScene GetSceneByDiceType(DiceType diceType)
	{
		return diceType switch
		{
			DiceType.Dice4 => Dice4,
			DiceType.Dice6 => Dice6,
			DiceType.Dice8 => Dice8,
			DiceType.Dice12 => Dice12,
			_ => Dice6
		};
	}

	private void ClearCurrentDice()
	{
		foreach (Node child in GetChildren())
		{
			if (child is Dice)
			{
				child.QueueFree();
			}
		}
	}

	private void ResetSelectionCount()
	{
		_selectedCount = 0;
		EmitSignal(SignalName.SelectionChanged, _selectedCount);
	}

	public List<DiceData> GetSelectedDiceData()
	{
		List<DiceData> selectedDices = new List<DiceData>();

		foreach (Node child in GetChildren())
		{
			if (child is Dice dice && dice.IsSelected && dice.DiceData != null)
			{
				selectedDices.Add(dice.DiceData);
			}
		}

		return selectedDices;
	}

	public int GetSelectedDiceTotalPoints()
	{
		int totalPoints = 0;

		foreach (Node child in GetChildren())
		{
			if (child is Dice dice && dice.IsSelected && dice.DiceData != null)
			{
				totalPoints += dice.DiceData.Num;
			}
		}

		return totalPoints;
	}

	private (float horizontalScale, float torqueScale) GetImpulseScale(DiceType diceType)
	{
		return diceType switch
		{
			DiceType.Dice4 => (1.0f, 1.0f),
			DiceType.Dice6 => (0.9f, 0.85f),
			DiceType.Dice8 => (0.8f, 0.7f),
			DiceType.Dice12 => (0.6f, 0.5f),
			_ => (0.9f, 0.85f)
		};
	}

	private void OnSingleDiceRollFinished(DiceData data, int num)
	{
		if (data != null)
		{
			data.Num = num;
		}

		_finishedCount++;
		GD.Print($"DiceManager: 单个骰子结束, face={num}, progress={_finishedCount}/{_totalCount}");

		if (_finishedCount >= _totalCount)
		{
			ArrangeDicePresentation();
		}
	}

	private void ArrangeDicePresentation()
	{
		List<Dice> currentDice = new List<Dice>();
		foreach (Node child in GetChildren())
		{
			if (child is Dice dice)
			{
				currentDice.Add(dice);
			}
		}

		if (currentDice.Count == 0)
		{
			FinishPresentation();
			return;
		}

		Tween tween = CreateTween();
		for (int i = 0; i < currentDice.Count; i++)
		{
			Dice dice = currentDice[i];
			float centerOffset = (currentDice.Count - 1) * 0.5f;
			float x = (i - centerOffset) * PresentationDiceSpacing;
			float forwardOffset = _presentationIsEnemyTurn ? -PresentationForwardOffset : PresentationForwardOffset;
			Vector3 targetPosition = new Vector3(x, dice.Position.Y, forwardOffset);
			Transform3D targetTransform = dice.GetPresentationTransform(targetPosition);

			dice.Freeze = true;
			tween.Parallel().TweenProperty(dice, "transform", targetTransform, PresentationDuration)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.Out);
		}

		tween.Finished += FinishPresentation;
	}

	private void FinishPresentation()
	{
		SetCurrentDiceSelectable(!_presentationIsEnemyTurn);
		GD.Print("所有骰子动画播放完成");
		Action temp = _onFinished;
		_onFinished = null;
		temp?.Invoke();
	}

	private void SetCurrentDiceSelectable(bool canSelect)
	{
		foreach (Node child in GetChildren())
		{
			if (child is Dice dice)
			{
				dice.CanBeSelected = canSelect;
			}
		}
	}
	
	private void OnDiceSelected(Dice dice)
	{
		GD.Print($"选中了骰子: {dice.Name}");
		bool nextSelected = !dice.IsSelected;
		dice.SetSelected(nextSelected, true);
		_selectedCount += nextSelected ? 1 : -1;
		if (_selectedCount < 0)
		{
			_selectedCount = 0;
		}

		EmitSignal(SignalName.SelectionChanged, _selectedCount);
	}

	private List<Dice> GetCurrentDice()
	{
		List<Dice> currentDice = new List<Dice>();
		foreach (Node child in GetChildren())
		{
			if (child is Dice dice)
			{
				currentDice.Add(dice);
			}
		}

		return currentDice;
	}

	private void ClearAllSelections()
	{
		foreach (Dice dice in GetCurrentDice())
		{
			dice.SetSelected(false);
		}

		ResetSelectionCount();
	}
}
