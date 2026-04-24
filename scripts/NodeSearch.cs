using Godot;

namespace DiceCombat.scripts;

public static class NodeSearch
{
	public static T FindDescendantByName<T>(Node root, string nodeName) where T : Node
	{
		if (root == null || string.IsNullOrEmpty(nodeName))
		{
			return null;
		}

		foreach (Node child in root.GetChildren())
		{
			if (child is T typed && child.Name == nodeName)
			{
				return typed;
			}

			T found = FindDescendantByName<T>(child, nodeName);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	public static T FindFirstDescendant<T>(Node root) where T : Node
	{
		if (root == null)
		{
			return null;
		}

		foreach (Node child in root.GetChildren())
		{
			if (child is T typed)
			{
				return typed;
			}
			
			T found = FindFirstDescendant<T>(child);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}
}