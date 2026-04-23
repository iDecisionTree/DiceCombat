namespace DiceCombat.scripts.state_machine;

public enum CombatState
{
    Init,
    PlayerRollAllDice,
    PlayerChoose,
    PlayerConfirm,
    EnemyRollAllDice,
    EnemyChoose,
    ResolveDamage,
    CheckEnd,
    SwitchTurn,
    Victory,
    Defeat,
}