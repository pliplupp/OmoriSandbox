using Godot;
using OmoriSandbox.Modding;

internal partial class ModContainer : Node
{
    private readonly Mod Mod;
    public ModContainer(Mod mod)
    {
        Mod = mod;
    }

    public override void _Ready()
    {
        Mod.OnLoad();
    }

    public override void _Process(double delta)
    {
        Mod.OnProcess(delta);
    }

    public override void _ExitTree()
    {
        Mod.OnUnload();
    }
}
