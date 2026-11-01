using UnityEngine;

// Общий интерфейс загадки (ГДД 8.8). MemoryObject не знает, как решается загадка:
// он вызывает Begin и ждёт, пока загадка вызовет owner.CompletePuzzle().
public abstract class MemoryPuzzle : MonoBehaviour
{
    protected MemoryObject owner;

    public virtual void Begin(MemoryObject o) { owner = o; }
    public abstract void Cancel();   // закрыть загадку без результата
    public abstract void Hint();     // показать подсказку (штраф начисляет GameManager)
}
