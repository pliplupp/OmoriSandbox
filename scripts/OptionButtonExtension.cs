using Godot;

namespace OmoriSandbox.Extensions;

public static class OptionButtonExtension
{
    /// <summary>
    /// Retrves the index of the specified item in the OptionButton.
    /// </summary>
    /// <returns>The index of the item, otherwise -1.</returns>
    public static int GetItemIndex(this OptionButton button, string item)
    {
        for (int i = 0; i < button.GetItemCount(); i++)
        {
            if (button.GetItemText(i) == item)
                return i;
        }
        return -1;
    }
}