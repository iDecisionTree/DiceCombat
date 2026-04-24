using Godot;

namespace DiceCombat.scripts.rich_text_3d;

public class RichText3DWrapper
{
  private readonly GodotObject _node;

  public RichText3DWrapper(GodotObject node) => _node = node;

  public string Text
  {
    get => _node != null ? (string)_node.Get("text") : string.Empty;
    set
    {
      if (_node == null)
      {
        return;
      }

      _node.Set("text", value);
    }
  }
}
