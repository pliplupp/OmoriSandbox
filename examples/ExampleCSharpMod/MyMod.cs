using Godot;
using OmoriSandbox.Modding;

namespace OmoriSandboxSampleMod
{
    public class MyMod : Mod
    {
        public override void OnLoad()
        {
            RegisterPartyMember<Tony>("Tony");

            GD.Print("MyMod loaded!");
        }
    }
}
