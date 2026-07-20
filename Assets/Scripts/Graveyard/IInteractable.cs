// IInteractable.cs — anything the player can use with the E key implements this.
// Harvestables, openable coffins, and the escape gate all share it, so the
// PlayerInteractor can drive several interaction systems through one path.

public interface IInteractable
{
    bool CanInteract { get; }
    string Prompt { get; }
    void Interact();
}
