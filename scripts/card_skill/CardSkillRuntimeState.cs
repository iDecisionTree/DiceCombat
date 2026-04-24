using System;
using System.Collections.Generic;

namespace DiceCombat.scripts.card_skill;

public sealed class CardSkillRuntimeState
{
	private readonly Dictionary<string, int> _intValues = new(StringComparer.Ordinal);
	private readonly HashSet<string> _flags = new(StringComparer.Ordinal);

	public int PendingDamageBonus { get; private set; }
	public int PendingDamageReduction { get; private set; }

	public void AddPendingDamageBonus(int amount)
	{
		PendingDamageBonus += amount;
	}

	public void AddPendingDamageReduction(int amount)
	{
		PendingDamageReduction += amount;
	}

	public int ConsumePendingDamageBonus()
	{
		int amount = PendingDamageBonus;
		PendingDamageBonus = 0;
		return amount;
	}

	public int ConsumePendingDamageReduction()
	{
		int amount = PendingDamageReduction;
		PendingDamageReduction = 0;
		return amount;
	}

	public void SetFlag(string key, bool enabled = true)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}

		if (enabled)
		{
			_flags.Add(key);
			return;
		}

		_flags.Remove(key);
	}

	public bool HasFlag(string key)
	{
		return !string.IsNullOrWhiteSpace(key) && _flags.Contains(key);
	}

	public void ClearFlag(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}

		_flags.Remove(key);
	}

	public int GetValue(string key, int defaultValue = 0)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return defaultValue;
		}

		return _intValues.TryGetValue(key, out int value) ? value : defaultValue;
	}

	public void SetValue(string key, int value)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}

		_intValues[key] = value;
	}

	public int AddValue(string key, int amount)
	{
		int nextValue = GetValue(key) + amount;
		SetValue(key, nextValue);
		return nextValue;
	}

	public void Reset()
	{
		PendingDamageBonus = 0;
		PendingDamageReduction = 0;
		_intValues.Clear();
		_flags.Clear();
	}
}

