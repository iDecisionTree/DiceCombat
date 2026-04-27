using System;
using DiceCombat.scripts.card;
using Godot;

namespace DiceCombat.scripts.state_machine;

public sealed class NullCombatCameraDirector : ICombatCameraDirector
{
	public static readonly NullCombatCameraDirector Instance = new NullCombatCameraDirector();

	private NullCombatCameraDirector()
	{
	}

	public void ResetCamera()
	{
	}

	public void OnBattleStarted()
	{
	}

	public void OnTurnChanged(CombatTurn turn, int roundCount)
	{
	}

	public void OnDamageResolved(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public void OnBattleEnded(CombatState finalState)
	{
	}

	public void FocusOnNode(Node3D target, float duration, Vector3? customOffset = null, Action onFinished = null)
	{
		onFinished?.Invoke();
	}
}

