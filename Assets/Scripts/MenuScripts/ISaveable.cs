using System;

public interface ISaveable
{
    // Return a serializable POCO (class/struct/dictionary) that represents your state.
    object CaptureState();

    // Receive the object you returned earlier and apply it back to the component.
    void RestoreState(object state);
}


